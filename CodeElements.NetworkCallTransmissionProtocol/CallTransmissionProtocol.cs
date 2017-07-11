using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using CodeElements.NetworkCallTransmissionProtocol.Exceptions;
using CodeElements.NetworkCallTransmissionProtocol.Extensions;
using CodeElements.NetworkCallTransmissionProtocol.Internal;
using CodeElements.NetworkCallTransmissionProtocol.Proxy;
using ZeroFormatter;

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
        /// <param name="data">The data to send.</param>
        /// <returns>Return the task which completes once the package is sent</returns>
        public delegate Task SendDataDelegate(ResponseData data);

        // ReSharper disable once StaticMemberInGenericType

        private readonly ConcurrentDictionary<uint, ResultCallback> _callbacks;
        private readonly Lazy<TInterface> _lazyInterface;
        private readonly MD5 _md5;
        private int _callIdCounter;
        private bool _isDisposed;
        private IReadOnlyDictionary<MethodInfo, MethodCache> _methods;
        private const int EstimatedDataPerParameter = 200;

        /// <summary>
        ///     Initialize a new instance of <see cref="CallTransmissionProtocol{TInterface}" />
        /// </summary>
        public CallTransmissionProtocol()
        {
            if (!typeof(TInterface).IsInterface)
                throw new ArgumentException("Only interfaces accepted.", nameof(TInterface));

            _lazyInterface =
                new Lazy<TInterface>(() => ProxyFactory.CreateProxy<TInterface>(this),
                    LazyThreadSafetyMode.ExecutionAndPublication);

            _md5 = MD5.Create();
            InitializeInterface(typeof(TInterface));

            _callbacks = new ConcurrentDictionary<uint, ResultCallback>();
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
        ///     Reserve bytes at the beginning of the <see cref="SendData" /> buffer for custom headers
        /// </summary>
        public int CustomOffset { get; set; }

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
                var result = await SendMethodCall(methodCache, invocation.Arguments);
                return (TResult) result;
            });
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

                var methodCache = new MethodCache(methodInfo.GetMethodId(), actualReturnType,
                    methodInfo.GetParameters().Select(x => x.ParameterType).ToArray());
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

            var callbackId = BitConverter.ToUInt32(buffer, offset);
            var responseType = (ResponseType) buffer[offset + 4];

            if (_callbacks.TryRemove(callbackId, out var callback))
                callback.ReceivedResult(responseType, buffer, offset + 5);
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
            //HEAD      - uinteger                  - The method identifier
            //HEAD      - integer * parameters      - the length of each parameter
            //--------------------------------------------------------------------------
            //DATA      - length of the parameters  - serialized parameters

            var callbackId = (uint) Interlocked.Increment(ref _callIdCounter);

            var buffer = new byte[CustomOffset /* user offset */ + 4 /* Header */ + 4 /* Callback id */ +
                                  4 /* method id */ +  parameters.Length * 4 /* parameter meta */ +
                                  EstimatedDataPerParameter * parameters.Length /* parameter data */];
            var bufferOffset = CustomOffset + 24 + parameters.Length * 4;

            for (var i = 0; i < parameters.Length; i++)
            {
                var metaOffset = CustomOffset + 12 + i * 4;
                var parameterLength = ZeroFormatterSerializer.NonGeneric.Serialize(methodCache.ParameterTypes[i],
                    ref buffer, bufferOffset, parameters[i]);
                Buffer.BlockCopy(BitConverter.GetBytes(parameterLength), 0, buffer, metaOffset, 4);

                bufferOffset += parameterLength;
            }

            //write header
            buffer[CustomOffset] = ProtocolInfo.Header1;
            buffer[CustomOffset + 1] = ProtocolInfo.Header2;
            buffer[CustomOffset + 2] = ProtocolInfo.Header3Call;
            buffer[CustomOffset + 3] = ProtocolInfo.Header4;

            //write callback id
            Buffer.BlockCopy(BitConverter.GetBytes(callbackId), 0, buffer, CustomOffset + 4, 4);

            //method identifier
            Buffer.BlockCopy(BitConverter.GetBytes(methodCache.MethodId), 0, buffer, CustomOffset + 8, 4);

            var callback = new ResultCallback();
            var callbackWait = callback.Wait(WaitTimeout);

            _callbacks.TryAdd(callbackId, callback); //impossible that this goes wrong

            OnSendData(new ResponseData(buffer, bufferOffset)).Forget(); //no need to await that

            using (callback)
            {
                if (!await callbackWait)
                {
                    _callbacks.TryRemove(callbackId, out var _);
                    throw new TimeoutException("The method call timed out, no response received.");
                }

                switch (callback.ResponseType)
                {
                    case ResponseType.MethodExecuted:
                        return null;
                    case ResponseType.ResultReturned:
                        return ZeroFormatterSerializer.NonGeneric.Deserialize(methodCache.ReturnType, callback.Data,
                            callback.Offset);
                    case ResponseType.Exception:
                        throw ExceptionSerializer.Deserialize(callback.Data, callback.Offset);
                    case ResponseType.MethodNotImplemented:
                        throw new NotImplementedException("The remote method is not implemented.");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        protected virtual Task OnSendData(ResponseData data)
        {
            if (SendData == null)
                throw new InvalidOperationException(
                    "The SendData delegate is null. Please initialize this class before using.");

            return SendData(data);
        }
    }
}