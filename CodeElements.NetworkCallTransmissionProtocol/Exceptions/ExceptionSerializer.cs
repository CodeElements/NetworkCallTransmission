using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace CodeElements.NetworkCallTransmissionProtocol.Exceptions
{
    internal static class ExceptionSerializer
    {
        private static readonly byte[] IntPlaceholder = new byte[4];

        public static byte[] Serialize(Exception exception)
        {
            if (exception == null)
                return BitConverter.GetBytes(0);

            var binaryFormatter = new BinaryFormatter();
            using (var memoryStream = new MemoryStream(1000))
            {
                memoryStream.Write(IntPlaceholder, 0, 4);
                try
                {
                    binaryFormatter.Serialize(memoryStream, exception);
                }
                catch (Exception)
                {
                    binaryFormatter.Serialize(memoryStream, new RemoteCallException(exception));
                }

                var serializedData = memoryStream.ToArray();
                Buffer.BlockCopy(BitConverter.GetBytes(serializedData.Length - 4), 0, serializedData, 0, 4);
                return serializedData;
            }
        }

        public static Exception Deserialize(byte[] data, int offset)
        {
            var length = BitConverter.ToInt32(data, offset);
            if (length < 0)
                return null;

            var binaryFormatter = new BinaryFormatter();
            using (var memoryStream = new MemoryStream(data, offset + 4, length))
            {
                return (Exception) binaryFormatter.Deserialize(memoryStream);
            }
        }
    }
}