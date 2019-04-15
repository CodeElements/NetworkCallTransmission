using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodeElements.NetworkCall.Extensions;
using CodeElements.NetworkCall.Internal;

namespace CodeElements.NetworkCall
{
    public class NetworkCallServer<TInterface> : DataTransmitter, IDisposable
    {
        private readonly TInterface _implementation;
        private readonly INetworkSerializer _serializer;
        private const int EstimatedResultBufferSize = 1024;
        private const int EstimatedEventParameterSize = 512;
        private readonly FifoAsyncLock _eventLock;
        private bool _isDisposed;
        private readonly object _disposedLock = new object();

        /// <summary>
        ///     Initialize a new instance of <see cref="NetworkCallServer{TInterface}" />
        /// </summary>
        /// <param name="implementation">The interface which can be called by the remote side</param>
        /// <param name="serializer">The serializer used to serialize/deserialize the objects</param>
        public NetworkCallServer(TInterface implementation, INetworkSerializer serializer) : this(implementation,
            serializer, NetworkCallServerCache.Build<TInterface>())
        {
        }

        public NetworkCallServer(TInterface implementation, INetworkSerializer serializer, NetworkCallServerCache cache)
        {
            _implementation = implementation;
            _serializer = serializer;
            Cache = cache;
            _eventLock = new FifoAsyncLock();
        }

        public NetworkCallServerCache Cache { get; }

        public override async void ReceiveData(byte[] data, int offset)
        {
            var opCode = (NetworkCallOpCode) data[offset++];
            switch (opCode)
            {
                case NetworkCallOpCode.ExecuteMethod:
                    var bufferSegment = await ExecuteMethod(data, offset);
                    OnSendData(bufferSegment).Forget();
                    break;
                case NetworkCallOpCode.SubscribeEvent:
                    ChangeEventSubscription(data, offset, subscribe: true);
                    break;
                case NetworkCallOpCode.UnsubscribeEvent:
                    ChangeEventSubscription(data, offset, subscribe: false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void ChangeEventSubscription(byte[] data, int offset, bool subscribe)
        {
            //PROTOCOL
            //HEAD      - uint32                    - the amount of events that are affected by this package
            //BODY      - integer * events          - the id of all events

            var eventCount = BitConverter.ToUInt16(data, offset);
            var events = new List<uint>(eventCount);

            for (var i = 0; i < eventCount; i++)
                events.Add(BitConverter.ToUInt32(data, offset + 2 /* HEAD */ + i * 4));

            foreach (var eventId in events)
            {
                //calls are thread safe
                if (subscribe)
                {
                    if (Cache.NetworkEvents.TryGetValue(eventId, out var networkEvent))
                        networkEvent.Subscribe(_implementation,
                            parameter => HandleEventAction(networkEvent, parameter));
                }
                else
                {
                    if (Cache.NetworkEvents.TryGetValue(eventId, out var networkEvent))
                        networkEvent.Unsubscribe(_implementation);
                }
            }
        }

        private async void HandleEventAction(NetworkEventInfo eventInfo, object parameter)
        {
            if (_isDisposed)
                return;

            //RETURN:
            //HEAD      - byte                      - response type
            //HEAD      - uinteger                  - event id

            //(BODY     - integer                   - body length)
            //(BODY     - body length               - the serialized object)

            var dataLength = 1 /* response type */ + 4 /* event id */;
            if (parameter != null)
                dataLength += 4 /* length */;

            var takenBuffer = Cache.Pool.Rent(dataLength + EstimatedEventParameterSize + CustomOffset);
            var data = takenBuffer;

            int parameterLength;
            if (parameter != null)
            {
                parameterLength = _serializer.Serialize(eventInfo.EventArgsType, ref data, CustomOffset + 9, parameter,
                    Cache.Pool);
                if (data != takenBuffer)
                    Cache.Pool.Return(takenBuffer);

                data[CustomOffset] = (byte) NetworkCallResponse.TriggerEventWithParameter;
                BinaryUtils.WriteInt32(data, CustomOffset + 5, parameterLength);
            }
            else
            {
                parameterLength = 0;
                data[CustomOffset] = (byte) NetworkCallResponse.TriggerEvent;
            }

            BinaryUtils.WriteUInt32(data, CustomOffset + 1, eventInfo.EventId);

            Task eventLockTask;
            lock (_disposedLock)
            {
                if (_isDisposed)
                {
                    Cache.Pool.Return(data);
                    return;
                }

                eventLockTask = _eventLock.EnterAsync();
            }

            //keep event order
            await eventLockTask;
            try
            {
                await OnSendData(new BufferSegment(data, CustomOffset, dataLength + parameterLength, Cache.Pool));
            }
            finally
            {
                _eventLock.Release();
            }
        }

        private async Task<BufferSegment> ExecuteMethod(byte[] data, int offset)
        {
            //PROTOCOL
            //CALL:
            //HEAD      - integer                   - callback identifier
            //HEAD      - uinteger                  - The method identifier
            //HEAD      - integer * parameters      - the length of each parameter
            //--------------------------------------------------------------------------
            //DATA      - length of the parameters  - serialized parameters
            //
            //RETURN:
            //HEAD      - 1 byte                    - the response type (0 = executed, 1 = result returned, 2 = exception, 3 = not implemented)
            //HEAD      - integer                   - callback identifier
            //(BODY     - return object length      - the serialized return object)

            var methodId = BitConverter.ToUInt32(data, offset + 4);
            var callbackId = BitConverter.ToInt32(data, offset);

            const int responseHeaderLength = 4;
            void WriteResponseHeader(byte[] buffer)
            {
                //Buffer.BlockCopy(data, offset, buffer, CustomOffset + 1, 4);
                BinaryUtils.WriteInt32(buffer, CustomOffset + 1, callbackId);
            }

            if (!Cache.MethodInvokers.TryGetValue(methodId, out var methodInvoker))
            {
                var responseLength = responseHeaderLength + 1 /* response type */;
                var response = Cache.Pool.Rent(responseLength + CustomOffset);
                WriteResponseHeader(response);
                response[CustomOffset + responseLength] = (byte) NetworkCallResponse.MethodNotImplemented;
                return new BufferSegment(response, CustomOffset, responseLength, Cache.Pool);
            }

            var parameters = new object[methodInvoker.ParameterCount];
            var parameterOffset = offset + 8 + parameters.Length * 4;

            for (var i = 0; i < methodInvoker.ParameterCount; i++)
            {
                var type = methodInvoker.ParameterTypes[i];
                var parameterLength = BitConverter.ToInt32(data, offset + 8 + i * 4);

                parameters[i] = _serializer.Deserialize(type, data, parameterOffset);
                parameterOffset += parameterLength;
            }

            Task task;
            try
            {
                task = methodInvoker.Invoke(_implementation, parameters);
                await task.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var responseLength = responseHeaderLength + 1 /* response type */ + EstimatedResultBufferSize /* exception */;
                var takenBuffer = Cache.Pool.Rent(responseLength + CustomOffset);
                var response = takenBuffer;

                var length = _serializer.SerializeException(ref response, CustomOffset + responseHeaderLength + 1, e, Cache.Pool);

                WriteResponseHeader(response);
                response[CustomOffset] = (byte) NetworkCallResponse.Exception;

                if (takenBuffer == response)
                    return new BufferSegment(response, CustomOffset, length + responseHeaderLength + 1, Cache.Pool);

                Cache.Pool.Return(takenBuffer);
                return new BufferSegment(response, CustomOffset, length + responseHeaderLength + 1, Cache.Pool);
            }

            if (methodInvoker.ReturnsResult)
            {
                var result = methodInvoker.TaskReturnPropertyInfo.GetValue(task);

                var takenBuffer = Cache.Pool.Rent(CustomOffset + EstimatedResultBufferSize);
                var response = takenBuffer;

                WriteResponseHeader(response);
                response[CustomOffset] = (byte) NetworkCallResponse.ResultReturned;

                var responseLength = _serializer.Serialize(methodInvoker.ReturnType, ref response,
                    responseHeaderLength + 1 + CustomOffset, result, Cache.Pool);

                if (takenBuffer == response)
                    return new BufferSegment(response, CustomOffset, responseLength + responseHeaderLength + 1,
                        Cache.Pool);

                Cache.Pool.Return(takenBuffer);
                return new BufferSegment(response, CustomOffset, responseLength + responseHeaderLength + 1, Cache.Pool);
            }
            else
            {
                var responseLength = responseHeaderLength + 1 /* response type */;
                var response = Cache.Pool.Rent(responseLength + CustomOffset);
                WriteResponseHeader(response);
                response[CustomOffset] = (byte) NetworkCallResponse.MethodExecuted;
                return new BufferSegment(response, CustomOffset, responseLength, Cache.Pool);
            }
        }

        public void Dispose()
        {
            //unsubscribe events
            foreach (var cacheNetworkEvent in Cache.NetworkEvents)
                cacheNetworkEvent.Value.Unsubscribe(_implementation);

            lock (_disposedLock)
            {
                _isDisposed = true;

                _eventLock.EnterAsync().ContinueWith(_ => _eventLock.Dispose());
            }
        }
    }
}