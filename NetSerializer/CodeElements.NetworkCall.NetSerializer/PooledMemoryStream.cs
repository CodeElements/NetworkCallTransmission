using System;
using System.Buffers;
using System.IO;

namespace CodeElements.NetworkCall.NetSerializer
{
    public class PooledMemoryStream : Stream
    {
        private byte[] _currentBuffer;
        private int _length;
        private int _position;
        private readonly ArrayPool<byte> _pool;
        private readonly int _bufferOffset;

        /// <summary>create writable memory stream with default parameters</summary>
        /// <remarks>buffer is allocated from ArrayPool.Shared</remarks>
        public PooledMemoryStream() : this(ArrayPool<byte>.Shared)
        {
        }

        /// <summary>create writable memory stream with specified ArrayPool</summary>
        /// <remarks>buffer is allocated from ArrayPool</remarks>
        public PooledMemoryStream(ArrayPool<byte> pool) : this(pool, 4096)
        {
        }

        /// <summary>create writable memory stream with ensuring buffer length</summary>
        /// <remarks>buffer is allocated from ArrayPool</remarks>
        public PooledMemoryStream(ArrayPool<byte> pool, int capacity)
        {
            _pool = pool;
            _currentBuffer = _pool.Rent(capacity);
            Capacity = _currentBuffer.Length;
        }

        /// <summary>create readonly MemoryStream without buffer copy</summary>
        /// <remarks>data will be read from 'data' parameter</summary>
        public PooledMemoryStream(byte[] data, int offset, ArrayPool<byte> pool)
        {
            _pool = pool;
            _currentBuffer = data;
            _bufferOffset = offset;
            Capacity = data.Length;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => _length;
        public int Capacity { get; private set; }

        public override long Position
        {
            get => _position;
            set => _position = (int) value;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var readlen = count > _length - _position ? _length - _position : count;
            if (readlen > 0)
            {
                Buffer.BlockCopy(_currentBuffer, _position + _bufferOffset, buffer, offset, readlen);
                _position += readlen;
                return readlen;
            }

            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var oldValue = _position;
            switch ((int) origin)
            {
                case (int) SeekOrigin.Begin:
                    _position = (int) offset;
                    break;
                case (int) SeekOrigin.End:
                    _position = _length - (int) offset;
                    break;
                case (int) SeekOrigin.Current:
                    _position += (int) offset;
                    break;
                default:
                    throw new InvalidOperationException("unknown SeekOrigin");
            }

            if (_position < 0 || _position + _bufferOffset > _length)
            {
                _position = oldValue;
                throw new IndexOutOfRangeException();
            }

            return _position;
        }

        private void EnsureCapacity(int value)
        {
            value += _bufferOffset;

            if (value > Capacity)
            {
                var newCapacity = value;
                if (newCapacity < 256)
                    newCapacity = 256;

                if (newCapacity < Capacity * 2)
                    newCapacity = Capacity * 2;

                var newBuffer = _pool.Rent(newCapacity);
                Buffer.BlockCopy(_currentBuffer, 0, newBuffer, 0, _length + _bufferOffset);
                _pool.Return(_currentBuffer);

                _currentBuffer = newBuffer;
                Capacity = newBuffer.Length;
            }
        }

        public override void SetLength(long value)
        {
            if (value > int.MaxValue)
                throw new IndexOutOfRangeException("overflow");
            if (value < 0)
                throw new IndexOutOfRangeException("underflow");

            _length = (int) value;
            EnsureCapacity(_length);
        }

        /// <summary>write data to stream</summary>
        /// <remarks>if stream data length is over int.MaxValue, this method throws IndexOutOfRangeException</remarks>
        public override void Write(byte[] buffer, int offset, int count)
        {
            var endOffset = _position + count;
            EnsureCapacity(endOffset);

            Buffer.BlockCopy(buffer, offset, _currentBuffer, _position + _bufferOffset, count);
            if (endOffset > _length)
                _length = endOffset;

            _position = endOffset;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (_pool != null && _currentBuffer != null)
            {
                _pool.Return(_currentBuffer);
                _currentBuffer = null;
            }
        }

        /// <summary>ensure the buffer size</summary>
        /// <remarks>capacity != stream buffer length</remarks>
        public void Reserve(int capacity)
        {
            if (capacity > _currentBuffer.Length)
                EnsureCapacity(capacity);
        }

        /// <summary>Create newly allocated buffer and copy the stream data</summary>
        public byte[] ToArray()
        {
            var ret = new byte[_length];
            Buffer.BlockCopy(_currentBuffer, 0, ret, 0, _length);
            return ret;
        }

        /// <summary>Create ArraySegment for current stream data without allocation buffer</summary>
        /// <remarks>After disposing stream, manupilating returned value(read or write) may cause undefined behavior</remarks>
        public ArraySegment<byte> ToUnsafeArraySegment() => new ArraySegment<byte>(_currentBuffer, _bufferOffset, _length);
    }
}