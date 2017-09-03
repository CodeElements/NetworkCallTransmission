// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace CodeElements.NetworkCallTransmission.Memory
{
    public abstract class BufferManager
    {
        public abstract byte[] TakeBuffer(int bufferSize);
        public abstract void ReturnBuffer(byte[] buffer);
        public abstract void Clear();

        public static BufferManager CreateBufferManager(long maxBufferPoolSize, int maxBufferSize)
        {
            if (maxBufferPoolSize < 0)
                throw new ArgumentOutOfRangeException("Max pool size must not be negative", nameof(maxBufferPoolSize));

            if (maxBufferSize < 0)
                throw new ArgumentOutOfRangeException("Max buffer size must not be negative", nameof(maxBufferSize));

            return new WrappingBufferManager(InternalBufferManager.Create(maxBufferPoolSize, maxBufferSize));
        }

        internal static InternalBufferManager GetInternalBufferManager(BufferManager bufferManager)
        {
            var manager = bufferManager as WrappingBufferManager;
            return manager != null ? manager.InternalBufferManager : new WrappingInternalBufferManager(bufferManager);
        }

        internal class WrappingBufferManager : BufferManager
        {
            private InternalBufferManager _innerBufferManager;

            public WrappingBufferManager(InternalBufferManager innerBufferManager)
            {
                _innerBufferManager = innerBufferManager;
            }

            public InternalBufferManager InternalBufferManager
            {
                get { return _innerBufferManager; }
            }

            public override byte[] TakeBuffer(int bufferSize)
            {
                if (bufferSize < 0)
                {
                    throw new ArgumentOutOfRangeException("Buffer size must not be negative", nameof(bufferSize));
                }

                return _innerBufferManager.TakeBuffer(bufferSize);
            }

            public override void ReturnBuffer(byte[] buffer)
            {
                if (buffer == null)
                {
                    throw new ArgumentException("Buffer cannot be null", nameof(buffer));
                }

                _innerBufferManager.ReturnBuffer(buffer);
            }

            public override void Clear()
            {
                _innerBufferManager.Clear();
            }
        }

        internal class WrappingInternalBufferManager : InternalBufferManager
        {
            private BufferManager _innerBufferManager;

            public WrappingInternalBufferManager(BufferManager innerBufferManager)
            {
                _innerBufferManager = innerBufferManager;
            }

            public override void Clear()
            {
                _innerBufferManager.Clear();
            }

            public override void ReturnBuffer(byte[] buffer)
            {
                _innerBufferManager.ReturnBuffer(buffer);
            }

            public override byte[] TakeBuffer(int bufferSize)
            {
                return _innerBufferManager.TakeBuffer(bufferSize);
            }
        }
    }
}