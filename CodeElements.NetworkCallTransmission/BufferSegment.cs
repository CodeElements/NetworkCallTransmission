using System;
using CodeElements.NetworkCallTransmission.Memory;

namespace CodeElements.NetworkCallTransmission
{
    /// <summary>
    ///     Contains information which must be sent to the counterpart for the services to work
    /// </summary>
    public class BufferSegment : IDisposable
    {
        private readonly BufferManager _bufferManager;

        internal BufferSegment(byte[] buffer, int offset, int length)
        {
            Buffer = buffer;
            Offset = offset;
            Length = length;
        }

        internal BufferSegment(byte[] buffer) : this(buffer, 0, buffer.Length)
        {
        }

        internal BufferSegment(byte[] buffer, int offset, int length, BufferManager bufferManager) : this(buffer, offset,
            length)
        {
            _bufferManager = bufferManager;
        }

        /// <summary>
        ///     The buffer that should be sent
        /// </summary>
        public byte[] Buffer { get; }

        /// <summary>
        ///     The offset in the <see cref="Buffer" />
        /// </summary>
        public int Offset { get; }

        /// <summary>
        ///     The length of the data that should be sent. Please note that the length of <see cref="Buffer" /> may be greater due
        ///     to fewer data than expected
        /// </summary>
        public int Length { get; }

        /// <summary>
        ///     Dispose the buffer. This is extremly important if buffer pooling is used
        /// </summary>
        public void Dispose()
        {
            _bufferManager?.ReturnBuffer(Buffer);
        }
    }
}