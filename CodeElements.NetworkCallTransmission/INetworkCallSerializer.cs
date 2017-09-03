using System;

namespace CodeElements.NetworkCallTransmission
{
    /// <summary>
    ///     Defines a serializer that serializes/deserializes objects and exceptions from byte arrays. The all methods must be thread-safe.
    /// </summary>
    public interface INetworkCallSerializer : INetworkSerializer
    {
        /// <summary>
        ///     Deserialize an exception
        /// </summary>
        /// <param name="data">The data that contains the serialized exception</param>
        /// <param name="offset">The offset for the <see cref="data" /> at which the serialized exception starts</param>
        /// <returns>Return the deserialized exception</returns>
        Exception DeserializeException(byte[] data, int offset);

        /// <summary>
        ///     Serialize an exception
        /// </summary>
        /// <param name="buffer">
        ///     The buffer the exception should be serialized to. If the buffer is too small, the expected behavior
        ///     is to create a new byte array that satisfies the object and set the reference to the new buffer. All bytes that
        ///     come before the <see cref="offset" /> must be copied into the new buffer
        /// </param>
        /// <param name="offset">The buffer offset at which the serialized exception must start</param>
        /// <param name="exception">The exception to serialize</param>
        /// <returns>Return the exception length in the buffer (without the <see cref="offset" />)</returns>
        int SerializeException(ref byte[] buffer, int offset, Exception exception);
    }
}