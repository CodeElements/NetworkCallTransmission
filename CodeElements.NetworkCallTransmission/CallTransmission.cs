using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using CodeElements.NetworkCallTransmission.Extensions;
using CodeElements.NetworkCallTransmission.Internal;
using CodeElements.NetworkCallTransmission.Proxy;

namespace CodeElements.NetworkCallTransmission
{
    /// <summary>
    ///     The client side of the network protocol. Provides the class which mapps the interface methods to the remote
    ///     methods. The counterpart is <see cref="CallTransmissionExecuter{TInterface}" />
    /// </summary>
    /// <typeparam name="TInterface">The remote interface. The receiving site must have the same interface available.</typeparam>
    public class CallTransmission<TInterface> : DataTransmitter, IDisposable, IAsyncInterceptor
    {
        private readonly INetworkCallSerializer _serializer;
        private const int EstimatedDataPerParameter = 200;
        // ReSharper disable once StaticMemberInGenericType

        private readonly ConcurrentDictionary<uint, ResultCallback> _callbacks;
        private readonly Lazy<TInterface> _lazyInterface;
        private readonly MD5 _md5;
        private int _callIdCounter;
        private bool _isDisposed;
        private IReadOnlyDictionary<MethodInfo, MethodCache> _methods;

        /// <summary>
        ///     Initialize a new instance of <see cref="CallTransmission{TInterface}" />
        /// </summary>
        /// <param name="serializer">The serializer used to serialize/deserialize the objects</param>
        public CallTransmission(INetworkCallSerializer serializer)
        {
            _serializer = serializer;
            var interfaceType = typeof(TInterface).GetTypeInfo();
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Only interfaces accepted.", nameof(TInterface));

            _lazyInterface =
                new Lazy<TInterface>(() => ProxyFactory.CreateProxy<TInterface>(this),
                    LazyThreadSafetyMode.ExecutionAndPublication);

            _md5 = MD5.Create();
            InitializeInterface(interfaceType);

            _callbacks = new ConcurrentDictionary<uint, ResultCallback>();
        }

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
        ///     Intercepts an asynchronous method <paramref name="invocation" /> with return type of
        ///     <see cref="T:System.Threading.Tasks.Task" />.
        /// </summary>
        /// <param name="invocation">The method invocation.</param>
        void IAsyncInterceptor.InterceptAsynchronous(IInvocation invocation)
        {
            var methodCache = _methods[invocation.MethodInfo];
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
            var methodCache = _methods[invocation.MethodInfo];
            invocation.ReturnValue = Task.Run(async () =>
            {
                var result = await SendMethodCall(methodCache, invocation.Arguments).ConfigureAwait(false);
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
                    if (_callbacks.TryRemove(key, out var resultCallback))
                        resultCallback.Dispose();
            }
        }

        /// <summary>
        ///     Receive data from a <see cref="CallTransmissionExecuter{TInterface}" />
        /// </summary>
        /// <param name="data">An array of bytes</param>
        /// <param name="offset">The starting position within the buffer</param>
        public override void ReceiveData(byte[] data, int offset)
        {
            //PROTOCOL
            //RETURN:
            //HEAD      - 4 bytes                   - Identifier, ASCII (NTR1)
            //HEAD      - integer                   - callback identifier
            //HEAD      - 1 byte                    - the response type (0 = executed, 1 = result returned, 2 = exception, 3 = not implemented)
            //(BODY     - return object length      - the serialized return object)

            if (data[offset++] != CallProtocolInfo.Header1 || data[offset++] != CallProtocolInfo.Header2 ||
                data[offset++] != CallProtocolInfo.Header3Return || data[offset++] != CallProtocolInfo.Header4)
                throw new ArgumentException("The package is invalid.");

            var callbackId = BitConverter.ToUInt32(data, offset);
            var responseType = (CallTransmissionResponseType) data[offset + 4];

            if (_callbacks.TryRemove(callbackId, out var callback))
                callback.ReceivedResult(responseType, data, offset + 5);
            else
                return; //could also throw exception here
        }

        private void InitializeInterface(TypeInfo interfaceType)
        {
            var members = interfaceType.DeclaredMembers;

            //check that we only have methods and no properties
            if (interfaceType.DeclaredMembers.Any(x => !(x is MethodInfo)))
                throw new ArgumentException("The interface must only provide methods.", nameof(interfaceType));

            //an interface without any methods is pointless
            var methods = members.Cast<MethodInfo>().ToList();
            if (methods.Count == 0)
                throw new ArgumentException("The interface must at least provide one method.", nameof(interfaceType));

            var dictionary = new Dictionary<MethodInfo, MethodCache>();

            foreach (var methodInfo in methods)
            {
                Type actualReturnType;
                if (methodInfo.ReturnType == typeof(Task))
                    actualReturnType = null;
                else if (methodInfo.ReturnType.GetTypeInfo().IsGenericType &&
                         methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    actualReturnType = methodInfo.ReturnType.GenericTypeArguments[0];
                else
                    throw new ArgumentException("Only tasks are supported as return type.", methodInfo.ToString());

                var methodCache = new MethodCache(methodInfo.GetMethodId(), actualReturnType,
                    methodInfo.GetParameters().Select(x => x.ParameterType).ToArray());
                dictionary.Add(methodInfo, methodCache);
            }

            _methods = dictionary;
        }

        private async Task<object> SendMethodCall(MethodCache methodCache, object[] parameters)
        {
            if (methodCache == null)
                throw new ArgumentException("The parameter cannot be null.", nameof(methodCache));
            if (methodCache == null)
                throw new ArgumentException("The parameter cannot be null.", nameof(parameters));

            //PROTOCOL
            //CALL:
            //HEAD      - 4 bytes                   - Identifier, ASCII (NTC1)
            //HEAD      - integer                   - callback identifier
            //HEAD      - uinteger                  - The method identifier
            //HEAD      - integer * parameters      - the length of each parameter
            //--------------------------------------------------------------------------
            //DATA      - length of the parameters  - serialized parameters

            var callbackId = (uint) Interlocked.Increment(ref _callIdCounter);

            var buffer = new byte[CustomOffset /* user offset */ + 4 /* Header */ + 4 /* Callback id */ +
                                  4 /* method id */ + parameters.Length * 4 /* parameter meta */ +
                                  EstimatedDataPerParameter * parameters.Length /* parameter data */];
            var bufferOffset = CustomOffset + 12 + parameters.Length * 4;

            for (var i = 0; i < parameters.Length; i++)
            {
                var metaOffset = CustomOffset + 12 + i * 4;
                var parameterLength = _serializer.Serialize(methodCache.ParameterTypes[i],
                    ref buffer, bufferOffset, parameters[i]);
                Buffer.BlockCopy(BitConverter.GetBytes(parameterLength), 0, buffer, metaOffset, 4);

                bufferOffset += parameterLength;
            }

            //write header
            buffer[CustomOffset] = CallProtocolInfo.Header1;
            buffer[CustomOffset + 1] = CallProtocolInfo.Header2;
            buffer[CustomOffset + 2] = CallProtocolInfo.Header3Call;
            buffer[CustomOffset + 3] = CallProtocolInfo.Header4;

            //write callback id
            Buffer.BlockCopy(BitConverter.GetBytes(callbackId), 0, buffer, CustomOffset + 4, 4);

            //method identifier
            Buffer.BlockCopy(BitConverter.GetBytes(methodCache.MethodId), 0, buffer, CustomOffset + 8, 4);

            var callback = new ResultCallback();
            var callbackWait = callback.Wait(WaitTimeout);

            _callbacks.TryAdd(callbackId, callback); //impossible that this goes wrong

            OnSendData(new ArraySegment<byte>(buffer, 0, bufferOffset)).Forget(); //no need to await that

            using (callback)
            {
                if (!await callbackWait.ConfigureAwait(false))
                {
                    _callbacks.TryRemove(callbackId, out var _);
                    throw new TimeoutException("The method call timed out, no response received.");
                }

                switch (callback.ResponseType)
                {
                    case CallTransmissionResponseType.MethodExecuted:
                        return null;
                    case CallTransmissionResponseType.ResultReturned:
                        return _serializer.Deserialize(methodCache.ReturnType, callback.Data,
                            callback.Offset);
                    case CallTransmissionResponseType.Exception:
                        var up = _serializer.DeserializeException(callback.Data, callback.Offset);
                        throw up;
                    case CallTransmissionResponseType.MethodNotImplemented:
                        throw new NotImplementedException("The remote method is not implemented.");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}