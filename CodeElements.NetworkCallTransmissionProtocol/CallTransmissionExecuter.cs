using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeElements.NetworkCallTransmissionProtocol.Extensions;

namespace CodeElements.NetworkCallTransmissionProtocol
{
    /// <summary>
    ///     The server side of the network protocol
    /// </summary>
    /// <typeparam name="TInterface">The remote interface. The receiving site must have the same interface available.</typeparam>
    public class CallTransmissionExecuter<TInterface>
    {
        private readonly Lazy<Serializer> _exceptionSerializer;
        private readonly TInterface _interfaceImplementation;

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
            _exceptionSerializer = new Lazy<Serializer>(() => new Serializer(ProtocolInfo.SupportedExceptionTypes),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        ///     Contains thread-safe information about the interface methods. Please reuse this object when possible to minimize
        ///     the initialization time.
        /// </summary>
        public ExecuterInterfaceCache Cache { get; }

        /// <summary>
        ///     Called when data was received by the client side
        /// </summary>
        /// <param name="buffer">The array of unsigned bytes which contains the information to execute the method.</param>
        /// <param name="offset">The index into buffer at which the data begins</param>
        /// <param name="length">The length of the data in bytes.</param>
        /// <returns>Returns the answer which should be sent back to the client</returns>
        public async Task<byte[]> ReceiveData(byte[] buffer, int offset, int length)
        {
            //PROTOCOL
            //CALL:
            //HEAD      - 4 bytes                   - Identifier, ASCII (NTC1)
            //HEAD      - integer                   - callback identifier
            //HEAD      - 16 bytes                  - The method identifier
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

            using (var memoryStream = new MemoryStream(buffer, offset, length - offset, false))
            {
                var binaryReader = new BinaryReader(memoryStream);
                binaryReader.ReadBigEndian7BitEncodedInt();

                var callbackIdLength = (int) memoryStream.Position;

                var id = Encoding.ASCII.GetString(buffer, offset + (int) memoryStream.Position, 16); //16 bytes

                memoryStream.Position += 16;

                //method not found/implemented
                if (!Cache.MethodInvokers.TryGetValue(id, out var methodInvoker))
                {
                    var response = new byte[4 /* Header */+ callbackIdLength + 1 /* response type */];
                    response[0] = ProtocolInfo.Header1;
                    response[1] = ProtocolInfo.Header2;
                    response[2] = ProtocolInfo.Header3Return;
                    response[3] = ProtocolInfo.Header4;

                    Buffer.BlockCopy(buffer, offset, response, 4, callbackIdLength);
                    response[response.Length - 1] = (byte) ResponseType.MethodNotImplemented;
                    return response;
                }

                var parameters = new object[methodInvoker.ParametersCount];
                for (int i = 0; i < methodInvoker.ParametersCount; i++)
                {
                    var serializer = methodInvoker.ParameterSerializers[i];
                    parameters[i] = serializer.Deserialize(memoryStream);
                }

                using (var responseStream = new MemoryStream(100))
                {
                    responseStream.WriteByte(ProtocolInfo.Header1);
                    responseStream.WriteByte(ProtocolInfo.Header2);
                    responseStream.WriteByte(ProtocolInfo.Header3Return);
                    responseStream.WriteByte(ProtocolInfo.Header4);
                    responseStream.Write(buffer, offset, callbackIdLength);

                    var task = methodInvoker.Invoke(_interfaceImplementation, parameters);
                    try
                    {
                        await task;
                    }
                    catch (Exception e)
                    {
                        responseStream.WriteByte((byte) ResponseType.Exception);
                        var exceptionType = e.GetType();
                        var exception = Array.IndexOf(ProtocolInfo.SupportedExceptionTypes, exceptionType) > -1
                            ? e
                            : new RemoteException(e);

                        _exceptionSerializer.Value.Serialize(responseStream, exception);
                        return responseStream.ToArray();
                    }

                    if (methodInvoker.ReturnSerializer != null)
                    {
                        var result = methodInvoker.TaskReturnPropertyInfo.GetValue(task);
                        responseStream.WriteByte((byte) ResponseType.ResultReturned);
                        methodInvoker.ReturnSerializer.Serialize(responseStream, result);
                    }
                    else
                        responseStream.WriteByte((byte) ResponseType.MethodExecuted);

                    return responseStream.ToArray();
                }
            }
        }
    }
}