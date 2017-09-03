using System;
using CodeElements.NetworkCallTransmission.ZeroFormatter.Exceptions;
using ZeroFormatter;

namespace CodeElements.NetworkCallTransmission.ZeroFormatter
{
    public class ZeroFormatterNetworkSerializer : INetworkCallSerializer
    {
        public static ZeroFormatterNetworkSerializer Instance { get; } = new ZeroFormatterNetworkSerializer();

        public object Deserialize(Type type, byte[] data, int offset)
        {
            return ZeroFormatterSerializer.NonGeneric.Deserialize(type, data, offset);
        }

        public int Serialize(Type type, ref byte[] buffer, int offset, object value)
        {
            return ZeroFormatterSerializer.NonGeneric.Serialize(type,
                ref buffer, offset, value);
        }

        public Exception DeserializeException(byte[] data, int offset)
        {
            return ZeroFormatterSerializer.Deserialize<IExceptionWrapper>(data, offset)
                .GetException();
        }

        public int SerializeException(ref byte[] buffer, int offset, Exception exception)
        {
            var exceptionInfo = ExceptionFactory.PackException(exception);
            return ZeroFormatterSerializer.Serialize(ref buffer, offset, exceptionInfo);
        }
    }
}