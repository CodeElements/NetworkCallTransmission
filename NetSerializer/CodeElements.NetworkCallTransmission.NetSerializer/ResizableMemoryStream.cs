using System;
using System.IO;

namespace CodeElements.NetworkCallTransmission.NetSerializer
{
    public class ResizableMemoryStream : Stream
    {
        private int _position;
        private int _length;

        public ResizableMemoryStream(byte[] data, int index)
        {
            _position = index;
            _length = index;
            Data = data;
            Capacity = data.Length;
        }

        public byte[] Data { get; private set; }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var i = _position + count;
            if (i > _length)
            {
                if (i > Capacity)
                {
                    if (EnsureCapacity(i))
                    {
                        var newData = new byte[Capacity];
                        Buffer.BlockCopy(Data, 0, newData, 0, _length);
                        Data = newData;
                    }
                }

                _length = i;
            }

            if (count <= 8 && buffer != Data)
            {
                int byteCount = count;
                while (--byteCount >= 0)
                    Data[_position + byteCount] = buffer[offset + byteCount];
            }
            else
                Buffer.BlockCopy(buffer, offset, Data, _position, count);
            _position = i;
        }

        public override bool CanRead { get; } = false;
        public override bool CanSeek { get; } = false;
        public override bool CanWrite { get; } = true;
        public override long Length => _length;
        public int Capacity { get; set; }

        public override long Position
        {
            get => _position;
            set => _position = (int) value;
        }

        private bool EnsureCapacity(int value)
        {
            if (value > Capacity)
            {
                var newCapacity = value;
                if (newCapacity < 256)
                    newCapacity = 256;

                if (newCapacity < Capacity * 2)
                    newCapacity = Capacity * 2;

                Capacity = newCapacity;
                return true;
            }

            return false;
        }
    }
}