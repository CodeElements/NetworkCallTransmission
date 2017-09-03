using System;

namespace CodeElements.NetworkCallTransmission
{
    /// <summary>
    ///     Defines a basic serializer that serializes/deserializes objects from byte arrays. The all methods must be thread-safe.
    /// </summary>
    public interface INetworkSerializer
    {
        /// <summary>
        ///     Deserialize an object
        /// </summary>
        /// <param name="type">The object type</param>
        /// <param name="data">The data that contains the serialized object</param>
        /// <param name="offset">The offset for the <see cref="data" /> at which the serialized object starts</param>
        /// <returns>Return the deserialized object</returns>
        object Deserialize(Type type, byte[] data, int offset);

        /// <summary>
        ///     Serialize an object
        /// </summary>
        /// <param name="type">The object type</param>
        /// <param name="buffer">
        ///     The buffer the object should be serialized to. If the buffer is too small, the expected behavior
        ///     is to create a new byte array that satisfies the object and set the reference to the new buffer. All bytes that
        ///     come before the <see cref="offset" /> must be copied into the new buffer
        /// </param>
        /// <param name="offset">The buffer offset at which the serialized object must start</param>
        /// <param name="value">The object to serialize</param>
        /// <returns>Return the object length in the buffer (without the <see cref="offset" />)</returns>
        int Serialize(Type type, ref byte[] buffer, int offset, object value);
    }
}