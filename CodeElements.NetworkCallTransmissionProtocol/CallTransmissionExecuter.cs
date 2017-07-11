using System;
using System.Threading.Tasks;
using CodeElements.NetworkCallTransmissionProtocol.Exceptions;
using CodeElements.NetworkCallTransmissionProtocol.Internal;
using ZeroFormatter;

namespace CodeElements.NetworkCallTransmissionProtocol
{
    /// <summary>
    ///     The server side of the network protocol
    /// </summary>
    /// <typeparam name="TInterface">The remote interface. The receiving site must have the same interface available.</typeparam>
    public class CallTransmissionExecuter<TInterface>
    {
        private readonly TInterface _interfaceImplementation;
        private const int EstimatedResultBufferSize = 1000;

        /// <summary>
        ///     Initialize a new instance of <see cref="CallTransmissionExecuter{TInterface}" />
        /// </summary>
        /// <param name="interfaceImplementation">The interface which can be called by the remote side</param>
        public CallTransmissionExecuter(TInterface interfaceImplementation)
            : this(interfaceImplementation, ExecuterInterfaceCache.Build<TInterface>())
        {
        }

        /// <summary>
        ///     Initialize a new instance of <see cref="CallTransmissionProtocol{TInterface}" /> with a cache
        /// </summary>
        /// <param name="interfaceImplementation">The interface which can be called by the remote side</param>
        /// <param name="cache">Contains thread-safe information about the interface methods</param>
        public CallTransmissionExecuter(TInterface interfaceImplementation, ExecuterInterfaceCache cache)
        {
            _interfaceImplementation = interfaceImplementation;
            Cache = cache;
        }

        /// <summary>
        ///     Contains thread-safe information about the interface methods. Please reuse this object when possible to minimize
        ///     the initialization time.
        /// </summary>
        public ExecuterInterfaceCache Cache { get; }

        /// <summary>
        ///     Reserve bytes at the beginning of the response buffer for custom headers
        /// </summary>
        public int CustomOffset { get; set; }

        /// <summary>
        ///     Called when data was received by the client side
        /// </summary>
        /// <param name="buffer">The array of unsigned bytes which contains the information to execute the method.</param>
        /// <param name="offset">The index into buffer at which the data begins</param>
        /// <param name="length">The length of the data in bytes.</param>
        /// <returns>Returns the answer which should be sent back to the client</returns>
        public async Task<ResponseData> ReceiveData(byte[] buffer, int offset, int length)
        {
            //PROTOCOL
            //CALL:
            //HEAD      - 4 bytes                   - Identifier, ASCII (NTC1)
            //HEAD      - integer                   - callback identifier
            //HEAD      - uinteger                  - The method identifier
            //HEAD      - integer * parameters      - the length of each parameter
            //--------------------------------------------------------------------------
            //DATA      - length of the parameters  - serialized parameters
            //
            //RETURN:
            //HEAD      - 4 bytes                   - Identifier, ASCII (NTR1)
            //HEAD      - integer                   - callback identifier
            //HEAD      - 1 byte                    - the response type (0 = executed, 1 = result returned, 2 = exception, 3 = not implemented)
            //(BODY     - return object length      - the serialized return object)

            if (buffer[offset++] != ProtocolInfo.Header1 || buffer[offset++] != ProtocolInfo.Header2 ||
                buffer[offset++] != ProtocolInfo.Header3Call)
                throw new ArgumentException("Invalid package format. Invalid header.");

            if (buffer[offset++] != 1)
                throw new NotSupportedException($"The version {buffer[offset - 1]} is not supported.");

            var id = BitConverter.ToUInt32(buffer, offset + 4);

            void WriteResponseHeader(byte[] data)
            {
                data[CustomOffset] = ProtocolInfo.Header1;
                data[CustomOffset + 1] = ProtocolInfo.Header2;
                data[CustomOffset + 2] = ProtocolInfo.Header3Return;
                data[CustomOffset + 3] = ProtocolInfo.Header4;
                Buffer.BlockCopy(buffer, 4, data, CustomOffset + 4, 4); //copy callback id
            }

            //method not found/implemented
            if (!Cache.MethodInvokers.TryGetValue(id, out var methodInvoker))
            {
                var response = new byte[CustomOffset /* user offset */ + 8 /* Header */ + 1 /* response type */];
                WriteResponseHeader(response);
                response[CustomOffset + 8] = (byte) ResponseType.MethodNotImplemented;
                return new ResponseData(response);
            }

            var parameters = new object[methodInvoker.ParameterCount];
            var parameterOffset = offset + 8 + parameters.Length * 4;

            for (int i = 0; i < methodInvoker.ParameterCount; i++)
            {
                var type = methodInvoker.ParameterTypes[i];
                var parameterLength = BitConverter.ToInt32(buffer, offset + 8 + i * 4);

                parameters[i] = ZeroFormatterSerializer.NonGeneric.Deserialize(type, buffer, parameterOffset);
                parameterOffset += parameterLength;
            }

            Task task;
            try
            {
                task = methodInvoker.Invoke(_interfaceImplementation, parameters);
                await task;
            }
            catch (Exception e)
            {
                var data = ExceptionSerializer.Serialize(e);
                var response = new byte[CustomOffset /* user offset */ + 8 /* Header */ + 1 /* response type */ + data.Length /* exception */];
                WriteResponseHeader(response);
                response[CustomOffset + 8] = (byte) ResponseType.Exception;
                Buffer.BlockCopy(data, 0, response, CustomOffset + 9, data.Length);
                return new ResponseData(response);
            }

            if (methodInvoker.ReturnsResult)
            {
                var result = methodInvoker.TaskReturnPropertyInfo.GetValue(task);

                var response = new byte[CustomOffset + EstimatedResultBufferSize];
                WriteResponseHeader(response);
                response[CustomOffset + 8] = (byte) ResponseType.ResultReturned;

                var responseLength =
                    ZeroFormatterSerializer.NonGeneric.Serialize(methodInvoker.ReturnType, ref response, CustomOffset + 9, result);
                return new ResponseData(response, responseLength + CustomOffset + 9);
            }
            else
            {
                var response = new byte[CustomOffset /* user offset */ + 8 /* Header */ + 1 /* response type */];
                WriteResponseHeader(response);
                response[CustomOffset + 8] = (byte) ResponseType.MethodExecuted;
                return new ResponseData(response);
            }
        }
    }
}