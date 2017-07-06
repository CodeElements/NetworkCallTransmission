using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using CodeElements.NetworkCallTransmissionProtocol.Castle;
using CodeElements.NetworkCallTransmissionProtocol.Extensions;
using CodeElements.NetworkCallTransmissionProtocol.NetSerializer;

namespace CodeElements.NetworkCallTransmissionProtocol
{
    /// <summary>
    ///     The client side of the network protocol. Provides the class which mapps the interface methods to the remote methods
    /// </summary>
    /// <typeparam name="TInterface">The remote interface. The receiving site must have the same interface available.</typeparam>
    public class CallTransmissionProtocol<TInterface> : IDisposable, IAsyncInterceptor
    {
        /// <summary>
        ///     The delegate which will get invoked when a package should be sent to the remote site
        /// </summary>
        /// <param name="memoryStream">The memory stream that keeps the data to be sent.</param>
        /// <returns>Return the task which completes once the package is sent</returns>
        public delegate Task SendDataDelegate(MemoryStream memoryStream);

        // ReSharper disable once StaticMemberInGenericType

        private readonly ConcurrentDictionary<uint, ResultCallback> _callbacks;
        private readonly Lazy<Serializer> _exceptionSerializer;
        private readonly Lazy<TInterface> _lazyInterface;
        private readonly MD5 _md5;
        private int _callIdCounter;
        private bool _isDisposed;

        private IReadOnlyDictionary<MethodInfo, MethodCache> _methods;

        /// <summary>
        ///     Initialize a new instance of <see cref="CallTransmissionProtocol{TInterface}" />
        /// </summary>
        public CallTransmissionProtocol()
        {
            if (!typeof(TInterface).IsInterface)
                throw new ArgumentException("Only interfaces accepted.", nameof(TInterface));

            _lazyInterface =
                new Lazy<TInterface>(() => (TInterface) CastleProxyFactory.CreateProxy(typeof(TInterface), this),
                    LazyThreadSafetyMode.ExecutionAndPublication);

            _md5 = MD5.Create();
            InitializeInterface(typeof(TInterface));

            _callbacks = new ConcurrentDictionary<uint, ResultCallback>();
            _exceptionSerializer = new Lazy<Serializer>(() => new Serializer(ProtocolInfo.SupportedExceptionTypes),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        ///     The delegate which will get invoked when a package should be sent to the remote site. This property must be set
        ///     before the interface is used.
        /// </summary>
        public SendDataDelegate SendData { get; set; }

        /// <summary>
        ///     The interface which provides the methods
        /// </summary>
        public TInterface Interface => _lazyInterface.Value;

        /// <summary>
        ///     The timeout after sending a package. If an answer does not come within the timeout, a
        ///     <see cref="TimeoutException" /> will be thrown in the task
        /// </summary>
        public TimeSpan WaitTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        ///     Intercepts a synchronous method <paramref name="invocation" />.
        /// </summary>
        /// <param name="invocation">The method invocation.</param>
        void IAsyncInterceptor.InterceptSynchronous(IInvocation invocation)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Intercepts an asynchronous method <paramref name="invocation" /> with return type of
        ///     <see cref="T:System.Threading.Tasks.Task" />.
        /// </summary>
        /// <param name="invocation">The method invocation.</param>
        void IAsyncInterceptor.InterceptAsynchronous(IInvocation invocation)
        {
            var methodCache = _methods[invocation.Method];
            invocation.ReturnValue = SendMethodCall(methodCache, invocation.Arguments);
        }

        /// <summary>
        ///     Intercepts an asynchronous method <paramref name="invocation" /> with return type of
        ///     <see cref="T:System.Threading.Tasks.Task`1" />.
        /// </summary>
        /// <typeparam name="TResult">
        ///     The type of the <see cref="T:System.Threading.Tasks.Task`1" />
        ///     <see cref="P:System.Threading.Tasks.Task`1.Result" />.
        /// </typeparam>
        /// <param name="invocation">The method invocation.</param>
        void IAsyncInterceptor.InterceptAsynchronous<TResult>(IInvocation invocation)
        {
            var methodCache = _methods[invocation.Method];
            invocation.ReturnValue = Task.Run(async () =>
            {
                var result = await SendMethodCall(methodCache, invocation.Arguments);
                return (TResult) result;
            });
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                _md5?.Dispose();
                foreach (var key in _callbacks.Keys)
                {
                    if (_callbacks.TryRemove(key, out var resultCallback))
                        resultCallback.Dispose();
                }
            }
        }

        private void InitializeInterface(Type interfaceType)
        {
            var members = interfaceType.GetMembers();

            //check that we only have methods and no properties
            if (members.Any(x => x.MemberType != MemberTypes.Method))
                throw new ArgumentException("The interface must only provide methods.", nameof(interfaceType));

            //an interface without any methods is pointless
            var methods = members.Cast<MethodInfo>().ToList();
            if (methods.Count == 0)
                throw new ArgumentException("The interface must at least provide one method.", nameof(interfaceType));

            var dictionary = new Dictionary<MethodInfo, MethodCache>();
            var serializerDictionary = new Dictionary<Type[], Serializer>(new TypeArrayEqualityComparer());

            foreach (var methodInfo in methods)
            {
                Type actualReturnType;
                if (methodInfo.ReturnType == typeof(Task))
                    actualReturnType = null;
                else if (methodInfo.ReturnType.IsGenericType &&
                         methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    actualReturnType = methodInfo.ReturnType.GetGenericArguments()[0];
                else
                    throw new ArgumentException("Only tasks are supported as return type.", methodInfo.ToString());

                var methodCache = new MethodCache(methodInfo.GetMethodId(_md5));

                if (actualReturnType != null) //if the tasks returns something
                {
                    var returnTypesAttribute = methodInfo.GetCustomAttribute<AdditionalTypesAttribute>();
                    var types = new Type[1 + (returnTypesAttribute?.Types.Length ?? 0)];
                    types[0] = actualReturnType;

                    if (returnTypesAttribute?.Types.Length > 0)
                        Array.Copy(returnTypesAttribute.Types, 0, types, 1, returnTypesAttribute.Types.Length);

                    if (!serializerDictionary.TryGetValue(types, out var returnSerializer))
                        serializerDictionary.Add(types, returnSerializer = new Serializer(types));

                    methodCache.ReturnSerializer = returnSerializer;
                }

                var parameters = methodInfo.GetParameters();
                methodCache.ParameterSerializers = new Serializer[parameters.Length];

                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var additionalTypes = parameter.GetCustomAttribute<AdditionalTypesAttribute>();

                    var types = new Type[1 + (additionalTypes?.Types.Length ?? 0)];
                    types[0] = parameter.ParameterType;

                    if (additionalTypes?.Types.Length > 0)
                        Array.Copy(additionalTypes.Types, 0, types, 1, additionalTypes.Types.Length);

                    if (!serializerDictionary.TryGetValue(types, out var serializer))
                        serializerDictionary.Add(types, serializer = new Serializer(types));

                    methodCache.ParameterSerializers[i] = serializer;
                }

                dictionary.Add(methodInfo, methodCache);
            }

            _methods = dictionary;
        }

        public void ReceiveData(byte[] buffer, int offset, int length)
        {
            //PROTOCOL
            //RETURN:
            //HEAD      - 4 bytes                   - Identifier, ASCII (NTR1)
            //HEAD      - integer                   - callback identifier
            //HEAD      - 1 byte                    - the response type (0 = executed, 1 = result returned, 2 = exception, 3 = not implemented)
            //(BODY     - return object length      - the serialized return object)

            if (buffer[offset++] != ProtocolInfo.Header1 || buffer[offset++] != ProtocolInfo.Header2 ||
                buffer[offset++] != ProtocolInfo.Header3Return || buffer[offset++] != ProtocolInfo.Header4)
                throw new ArgumentException("The package is invalid.");

            var memoryStream = new MemoryStream(buffer, offset, length - offset, false);
            var binaryReader = new BinaryReader(memoryStream);

            var callbackId = binaryReader.ReadBigEndian7BitEncodedInt();
            var responseType = (ResponseType)binaryReader.ReadByte();

            if (_callbacks.TryRemove(callbackId, out var callback))
                callback.ReceivedResult(responseType, memoryStream);
            else
                return; //could also throw exception here
        }

        private async Task<object> SendMethodCall(MethodCache methodCache, object[] parameters)
        {
            Contract.Ensures(methodCache != null);
            Contract.Ensures(parameters != null);

            //PROTOCOL
            //CALL:
            //HEAD      - 4 bytes                   - Identifier, ASCII (NTC1)
            //HEAD      - integer                   - callback identifier
            //HEAD      - 16 bytes                  - The method identifier
            //--------------------------------------------------------------------------
            //DATA      - length of the parameters  - serialized parameters

            var callbackId = (uint) Interlocked.Increment(ref _callIdCounter);

            ResultCallback callback;
            Task<bool> callbackWait;

            using (var memoryStream = new MemoryStream(512))
            {
                var binaryWriter = new BinaryWriter(memoryStream);

                //write header
                binaryWriter.Write(ProtocolInfo.Header1);
                binaryWriter.Write(ProtocolInfo.Header2);
                binaryWriter.Write(ProtocolInfo.Header3Call);
                binaryWriter.Write(ProtocolInfo.Header4);

                //write callback id
                binaryWriter.WriteBigEndian7BitEncodedInt(callbackId);

                //method identifier
                binaryWriter.Write(methodCache.MethodId);

                //parameters
                for (int i = 0; i < parameters.Length; i++)
                {
                    //we remember the start position of the parameter
                    var parameter = parameters[i];
                    var serializer = methodCache.ParameterSerializers[i];

                    //serialize the parameter
                    serializer.Serialize(memoryStream, parameter);
                }

                callback = new ResultCallback();
                callbackWait = callback.Wait(WaitTimeout);

                _callbacks.TryAdd(callbackId, callback); //impossible that this goes wrong

                OnSendData(memoryStream).Forget(); //no need to await that
            }

            using (callback)
            {
                if (!await callbackWait)
                {
                    _callbacks.TryRemove(callbackId, out var value);
                    throw new TimeoutException("The method call timed out, no response received.");
                }

                switch (callback.ResponseType)
                {
                    case ResponseType.MethodExecuted:
                        return null;
                    case ResponseType.ResultReturned:
                        return methodCache.ReturnSerializer.Deserialize(callback.Data);
                    case ResponseType.Exception:
                        var exception = (Exception) _exceptionSerializer.Value.Deserialize(callback.Data);
                        throw exception;
                    case ResponseType.MethodNotImplemented:
                        throw new NotImplementedException("The remote method is not implemented.");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        protected virtual Task OnSendData(MemoryStream memoryStream)
        {
            if (SendData == null)
                throw new InvalidOperationException(
                    "The SendData delegate is null. Please initialize this class before using.");

            return SendData(memoryStream);
        }
    }
}