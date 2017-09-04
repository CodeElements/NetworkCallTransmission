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

        /// <summary>
        ///     Initialize a new instance of <see cref="BufferSegment" /> with a buffer
        /// </summary>
        /// <param name="buffer">The buffer that contains the segment</param>
        /// <param name="offset">The offset in the buffer at which the segment begins</param>
        /// <param name="length">The length of the segment in the buffer</param>
        public BufferSegment(byte[] buffer, int offset, int length)
        {
            Buffer = buffer;
            Offset = offset;
            Length = length;
        }

        /// <summary>
        ///     Initialize a new instance of <see cref="BufferSegment" /> with a buffer that is the segment
        /// </summary>
        /// <param name="buffer">The buffer that is the segment</param>
        public BufferSegment(byte[] buffer) : this(buffer, 0, buffer.Length)
        {
        }

        /// <summary>
        ///     Initialize a new instance of <see cref="BufferSegment" /> with a buffer and the buffer manager which provided the
        ///     buffer
        /// </summary>
        /// <param name="buffer">The buffer that contains the segment</param>
        /// <param name="offset">The offset in the buffer at which the segment begins</param>
        /// <param name="length">The length of the segment in the buffer</param>
        /// <param name="bufferManager">The buffer manager which is the origin of the buffer</param>
        public BufferSegment(byte[] buffer, int offset, int length, BufferManager bufferManager) : this(buffer, offset,
            length)
        {
            _bufferManager = bufferManager;
        }

        /// <summary>
        ///     The buffer that should be sent
        /// </summary>
        public byte[] Buffer { get; private set; }

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
            Buffer = null;
        }
    }
}