﻿using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CodeElements.NetworkCall.Extensions;
using CodeElements.NetworkCall.Internal;
using CodeElements.NetworkCall.Logging;
using CodeElements.NetworkCall.Proxy;

namespace CodeElements.NetworkCall
{
    public class NetworkCallClient<TInterface> : DataTransmitter, IAsyncInterceptor, IDisposable
    {
        private static readonly ILog Logger = LogProvider.For<NetworkCallClient<TInterface>>();

        private const int EstimatedDataPerParameter = 200;

        private readonly INetworkSerializer _serializer;
        private readonly ArrayPool<byte> _pool;
        private int _callIdCounter;
        private readonly IReadOnlyDictionary<MethodInfo, CachedMethod> _cachedMethods;
        private readonly ConcurrentDictionary<uint, (Type, TaskCompletionSource<object>)> _waitingTasks;
        private readonly NetworkCallEventProvider<TInterface> _networkCallEventProvider;
        private readonly ConcurrentDictionary<uint, EventInfo> _subscribedEvents;
        private readonly Lazy<TInterface> _lazyImplementation;

        public NetworkCallClient(INetworkSerializer serializer, ArrayPool<byte> pool)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));

            var interfaceType = typeof(TInterface).GetTypeInfo();
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Only interfaces accepted.", nameof(TInterface));

            _cachedMethods = InitializeMethodCache(interfaceType);
            _waitingTasks = new ConcurrentDictionary<uint, (Type, TaskCompletionSource<object>)>();

            _networkCallEventProvider = new NetworkCallEventProvider<TInterface>(this);
            _subscribedEvents = new ConcurrentDictionary<uint, EventInfo>();

            _lazyImplementation = new Lazy<TInterface>(InterfaceFactory);
        }

        public void Dispose()
        {
            foreach (var waitingTask in _waitingTasks.Keys)
                if (_waitingTasks.TryRemove(waitingTask, out var completionSource))
                    completionSource.Item2.TrySetCanceled();
        }

        /// <summary>
        ///     The timeout after sending a package. If an answer does not come within the timeout, a
        ///     <see cref="TimeoutException" /> will be thrown in the task
        /// </summary>
        public TimeSpan WaitTimeout { get; set; } = TimeSpan.FromSeconds(30);

        public TInterface Interface => _lazyImplementation.Value;

        private IReadOnlyDictionary<MethodInfo, CachedMethod> InitializeMethodCache(TypeInfo interfaceType)
        {
            //an interface without any methods is pointless
            var methods = interfaceType.GetMethods();
            var dictionary = new Dictionary<MethodInfo, CachedMethod>(methods.Length);

            foreach (var methodInfo in methods.Where(x => !x.IsSpecialName))
            {
                Type actualReturnType;
                if (methodInfo.ReturnType == typeof(Task))
                    actualReturnType = null;
                else if (methodInfo.ReturnType.GetTypeInfo().IsGenericType &&
                         methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    actualReturnType = methodInfo.ReturnType.GenericTypeArguments[0];
                else
                    throw new ArgumentException("Only tasks are supported as return type.", methodInfo.ToString());

                var methodCache = new CachedMethod(methodInfo.GetMethodId(), actualReturnType,
                    methodInfo.GetParameters().Select(x => x.ParameterType).ToArray());
                dictionary.Add(methodInfo, methodCache);
            }

            Logger.Debug("Method cache initialized, methods found: {methodsCount}", dictionary.Count);

            if (!dictionary.Any())
                throw new ArgumentException("The interface must at least provide one method.", nameof(interfaceType));

            return dictionary;
        }

        public override void ReceiveData(byte[] data, int offset)
        {
            var responseType = (NetworkCallResponse) data[offset++];
            Logger.Trace("Receive data {dataLength} with offset {offset} and opCode {opCode}", data.Length, offset, responseType);

            switch (responseType)
            {
                case NetworkCallResponse.MethodExecuted:
                case NetworkCallResponse.ResultReturned:
                case NetworkCallResponse.Exception:
                case NetworkCallResponse.MethodNotImplemented:
                    ReceiveMethodResponse(responseType, data, offset);
                    break;
                case NetworkCallResponse.TriggerEvent:
                    TriggerEventReceived(data, offset, withParameter: false);
                    break;
                case NetworkCallResponse.TriggerEventWithParameter:
                    TriggerEventReceived(data, offset, withParameter: true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SuspendSubscribing() => _networkCallEventProvider.SuspendSubscribing();
        public void ResumeSubscribing() => _networkCallEventProvider.ResumeSubscribing();

        private void ReceiveMethodResponse(NetworkCallResponse response, byte[] data, int offset)
        {
            var callbackId = BitConverter.ToUInt32(data, offset);
            offset += 4;

            Logger.Trace("Receive method response for {callbackId}", callbackId);

            if (!_waitingTasks.TryRemove(callbackId, out var waiter))
            {
                Logger.Warn("The method call for id {callbackId} was not found, package is ignored.", callbackId);
                return;
            }

            var (type, completionSource) = waiter;

            switch (response)
            {
                case NetworkCallResponse.MethodExecuted:
                    Logger.Debug("Set method {callbackId} executed without an object result.", callbackId);
                    completionSource.SetResult(null);
                    break;
                case NetworkCallResponse.ResultReturned:
                    Logger.Trace("Deserialize result for {callbackId}...", callbackId);
                    var result = _serializer.Deserialize(type, data, offset);

                    Logger.Debug("Set method {callbackId} executed with result object {@object}", result);
                    completionSource.SetResult(result);
                    break;
                case NetworkCallResponse.Exception:
                    Logger.Trace("Deserialize exception for {callbackId}...", callbackId);
                    var exception = _serializer.DeserializeException(data, offset);

                    Logger.Debug("Set method {callbackId} faulted with exception {@exception}", exception);
                    completionSource.SetException(exception);
                    break;
                case NetworkCallResponse.MethodNotImplemented:
                    Logger.Debug("Set method {callbackId} faulted with NotImplementedException");
                    completionSource.SetException(new NotImplementedException("The remote method is not implemented."));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(response), response, null);
            }
        }

        private void TriggerEventReceived(byte[] data, int offset, bool withParameter)
        {
            var eventId = BitConverter.ToUInt32(data, offset);

            Logger.Trace("Receive event {eventId} triggered", eventId);
            if (!_subscribedEvents.TryGetValue(eventId, out var eventInfo))
            {
                Logger.Warn("Event {eventId} was not found", eventId);
                return;
            }

            object parameter;
            if (!withParameter)
            {
                parameter = null;
            }
            else
            {
                Logger.Trace("Deserialize event {eventId} parameter", eventId);

                var eventParamType = eventInfo.EventHandlerType.GenericTypeArguments[0];
                parameter = _serializer.Deserialize(eventParamType, data, offset + 8);

                Logger.Debug("Deserialized event {eventId} parameter: {@object}", eventId, parameter);
            }

            Logger.Trace("Trigger event {eventId}", eventId);
            _networkCallEventProvider.TriggerEvent(eventInfo, parameter);
        }

        void IAsyncInterceptor.InterceptAsynchronous(IInvocation invocation)
        {
            Logger.Trace("Intercept method invocation {@invocation}", invocation);

            var methodCache = _cachedMethods[invocation.MethodInfo];
            invocation.ReturnValue = SendMethodCall(methodCache, invocation.Arguments);
        }

        void IAsyncInterceptor.InterceptAsynchronous<TResult>(IInvocation invocation)
        {
            Logger.Trace("Intercept generic method invocation {@invocation}", invocation);

            var methodCache = _cachedMethods[invocation.MethodInfo];
            invocation.ReturnValue = Task.Run(async () =>
            {
                var result = await SendMethodCall(methodCache, invocation.Arguments).ConfigureAwait(false);
                return (TResult) result;
            });
        }

        internal void SubscribeEvents(IReadOnlyList<(EventInfo, uint)> events)
        {
            Logger.Trace("Subscribe events {@events}", events);

            if (events.Count > 0)
            {
                foreach (var (eventInfo, eventId) in events)
                {
                    _subscribedEvents.TryAdd(eventId, eventInfo);
                }
                SendEventAction(events.Select(x => x.Item2).ToList(), subscribe: true);
            }
        }

        internal void UnsubscribeEvents(IReadOnlyList<(EventInfo, uint)> events)
        {
            Logger.Trace("Unsubscribe events {@events}", events);

            if (events.Count > 0)
            {
                foreach (var (_, eventId) in events)
                {
                    _subscribedEvents.TryRemove(eventId, out _);
                }
                SendEventAction(events.Select(x => x.Item2).ToList(), subscribe: false);
            }
        }

        private void SendEventAction(IList<uint> events, bool subscribe)
        {
            var bufferLength = 1 /* prefix */ + 2 /* amount of events */ + events.Count * 8;
            var buffer = _pool.Rent(bufferLength + CustomOffset);
            buffer[CustomOffset] =
                (byte) (subscribe ? NetworkCallOpCode.SubscribeEvent : NetworkCallOpCode.UnsubscribeEvent);

            BinaryUtils.WriteUInt16(buffer, CustomOffset + 1, (ushort) events.Count);
            for (var index = 0; index < events.Count; index++)
            {
                var eventId = events[index];
                BinaryUtils.WriteUInt32(buffer, CustomOffset + 3 + index * 4, eventId);
            }

            OnSendData(new BufferSegment(buffer, CustomOffset, bufferLength, _pool)).Forget();
        }

        private async Task<object> SendMethodCall(CachedMethod cachedMethod, object[] parameters)
        {
            if (cachedMethod == null)
                throw new ArgumentException("The cached method cannot be null.", nameof(cachedMethod));
            if (cachedMethod == null)
                throw new ArgumentException("The parameter cannot be null.", nameof(parameters));

            //PROTOCOL
            //CALL:
            //HEAD      - integer                   - callback identifier
            //HEAD      - uinteger                  - The method identifier
            //HEAD      - integer * parameters      - the length of each parameter
            //--------------------------------------------------------------------------
            //DATA      - length of the parameters  - serialized parameters

            var callbackId = (uint) Interlocked.Increment(ref _callIdCounter);
            const int headerLength = 1;

            var estimatedBufferLength = CustomOffset /* user offset */ + headerLength /* Header */ +
                                        4 /* Callback id */ + 4 /* method id */ +
                                        parameters.Length * 4 /* parameter meta */ +
                                        EstimatedDataPerParameter * parameters.Length /* parameter data */;
            var buffer = _pool.Rent(estimatedBufferLength);
            var bufferOffset = CustomOffset + headerLength + 8 + parameters.Length * 4;

            for (var i = 0; i < parameters.Length; i++)
            {
                var metaOffset = CustomOffset + headerLength + 8 + i * 4;
                var parameterLength = _serializer.Serialize(cachedMethod.ParameterTypes[i], ref buffer, bufferOffset,
                    parameters[i], _pool);
                BinaryUtils.WriteInt32(buffer, metaOffset, parameterLength);

                bufferOffset += parameterLength;
            }

            //write opCode
            buffer[CustomOffset] = (byte) NetworkCallOpCode.ExecuteMethod;

            //write callback id
            BinaryUtils.WriteUInt32(buffer, CustomOffset + headerLength, callbackId);

            //method identifier
            BinaryUtils.WriteUInt32(buffer, CustomOffset + headerLength + 4, cachedMethod.MethodId);

            var taskCompletionSource = new TaskCompletionSource<object>();
            _waitingTasks.TryAdd(callbackId,
                (cachedMethod.ReturnType, taskCompletionSource)); //impossible that this goes wrong

            OnSendData(new BufferSegment(buffer, CustomOffset, bufferOffset - CustomOffset, _pool)).Forget(); //no need to await that

            var cancellationToken = parameters.OfType<CancellationToken>().FirstOrDefault();
            CancellationTokenRegistration registration;

            if (cancellationToken != default)
                registration = cancellationToken.Register(() => taskCompletionSource.SetCanceled());

            try
            {
                if (WaitTimeout != TimeSpan.Zero)
                {
                    return await taskCompletionSource.Task.TimeoutAfter(WaitTimeout);
                }

                return await taskCompletionSource.Task;
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException)
            {
                _waitingTasks.TryRemove(callbackId, out _);
                throw;
            }
            finally
            {
                registration.Dispose();
            }
        }

        private TInterface InterfaceFactory()
        {
            var instance = CachedProxyFactory<TInterface>.Create(this, _networkCallEventProvider);
            _networkCallEventProvider.Proxy = (IEventInterceptorProxy)instance;

            Logger.Debug("The interface proxy was created and is initialized.");

            return instance;
        }
    }
}