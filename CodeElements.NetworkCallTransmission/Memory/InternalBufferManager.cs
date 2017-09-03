// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace CodeElements.NetworkCallTransmission.Memory
{
    internal abstract class InternalBufferManager
    {
        public abstract byte[] TakeBuffer(int bufferSize);
        public abstract void ReturnBuffer(byte[] buffer);
        public abstract void Clear();

        public static InternalBufferManager Create(long maxBufferPoolSize, int maxBufferSize)
        {
            if (maxBufferPoolSize == 0)
                return GcBufferManager.Value;

            return new PooledBufferManager(maxBufferPoolSize, maxBufferSize);
        }

        internal class PooledBufferManager : InternalBufferManager
        {
            private const int MinBufferSize = 128;
            private const int MaxMissesBeforeTuning = 8;
            private const int InitialBufferCount = 1;
            private readonly object _tuningLock;

            private readonly int[] _bufferSizes;
            private readonly BufferPool[] _bufferPools;
            private long _memoryLimit;
            private long _remainingMemory;
            private bool _areQuotasBeingTuned;
            private int _totalMisses;
#if DEBUG && !FEATURE_NETNATIVE
            private readonly ConcurrentDictionary<int, string> _buffersPooled = new ConcurrentDictionary<int, string>();
#endif //DEBUG

            public PooledBufferManager(long maxMemoryToPool, int maxBufferSize)
            {
                _tuningLock = new object();
                _memoryLimit = maxMemoryToPool;
                _remainingMemory = maxMemoryToPool;
                var bufferPoolList = new List<BufferPool>();

                for (int bufferSize = MinBufferSize; ;)
                {
                    long bufferCountLong = _remainingMemory / bufferSize;
                    int bufferCount = bufferCountLong > int.MaxValue ? int.MaxValue : (int) bufferCountLong;

                    if (bufferCount > InitialBufferCount)
                        bufferCount = InitialBufferCount;

                    bufferPoolList.Add(BufferPool.CreatePool(bufferSize, bufferCount));

                    _remainingMemory -= (long)bufferCount * bufferSize;

                    if (bufferSize >= maxBufferSize)
                    {
                        break;
                    }

                    long newBufferSizeLong = (long)bufferSize * 2;

                    if (newBufferSizeLong > (long)maxBufferSize)
                    {
                        bufferSize = maxBufferSize;
                    }
                    else
                    {
                        bufferSize = (int)newBufferSizeLong;
                    }
                }

                _bufferPools = bufferPoolList.ToArray();
                _bufferSizes = new int[_bufferPools.Length];
                for (int i = 0; i < _bufferPools.Length; i++)
                {
                    _bufferSizes[i] = _bufferPools[i].BufferSize;
                }
            }

            public override void Clear()
            {
#if DEBUG && !FEATURE_NETNATIVE
                _buffersPooled.Clear();
#endif //DEBUG

                for (int i = 0; i < _bufferPools.Length; i++)
                {
                    BufferPool bufferPool = _bufferPools[i];
                    bufferPool.Clear();
                }
            }

            private void ChangeQuota(ref BufferPool bufferPool, int delta)
            {
                BufferPool oldBufferPool = bufferPool;
                int newLimit = oldBufferPool.Limit + delta;
                BufferPool newBufferPool = BufferPool.CreatePool(oldBufferPool.BufferSize, newLimit);
                for (int i = 0; i < newLimit; i++)
                {
                    byte[] buffer = oldBufferPool.Take();
                    if (buffer == null)
                    {
                        break;
                    }
                    newBufferPool.Return(buffer);
                    newBufferPool.IncrementCount();
                }
                _remainingMemory -= oldBufferPool.BufferSize * delta;
                bufferPool = newBufferPool;
            }

            private void DecreaseQuota(ref BufferPool bufferPool)
            {
                ChangeQuota(ref bufferPool, -1);
            }

            private int FindMostExcessivePool()
            {
                long maxBytesInExcess = 0;
                int index = -1;

                for (int i = 0; i < _bufferPools.Length; i++)
                {
                    BufferPool bufferPool = _bufferPools[i];

                    if (bufferPool.Peak < bufferPool.Limit)
                    {
                        long bytesInExcess = (bufferPool.Limit - bufferPool.Peak) * (long)bufferPool.BufferSize;

                        if (bytesInExcess > maxBytesInExcess)
                        {
                            index = i;
                            maxBytesInExcess = bytesInExcess;
                        }
                    }
                }

                return index;
            }

            private int FindMostStarvedPool()
            {
                long maxBytesMissed = 0;
                int index = -1;

                for (int i = 0; i < _bufferPools.Length; i++)
                {
                    BufferPool bufferPool = _bufferPools[i];

                    if (bufferPool.Peak == bufferPool.Limit)
                    {
                        long bytesMissed = bufferPool.Misses * (long)bufferPool.BufferSize;

                        if (bytesMissed > maxBytesMissed)
                        {
                            index = i;
                            maxBytesMissed = bytesMissed;
                        }
                    }
                }

                return index;
            }

            private BufferPool FindPool(int desiredBufferSize)
            {
                for (int i = 0; i < _bufferSizes.Length; i++)
                {
                    if (desiredBufferSize <= _bufferSizes[i])
                    {
                        return _bufferPools[i];
                    }
                }

                return null;
            }

            private void IncreaseQuota(ref BufferPool bufferPool)
            {
                ChangeQuota(ref bufferPool, 1);
            }

            public override void ReturnBuffer(byte[] buffer)
            {
                BufferPool bufferPool = FindPool(buffer.Length);
                if (bufferPool != null)
                {
                    if (buffer.Length != bufferPool.BufferSize)
                    {
                        throw new ArgumentException("BufferIsNotRightSizeForBufferManager");
                    }

                    if (bufferPool.Return(buffer))
                    {
                        bufferPool.IncrementCount();
                    }
                }
            }

            public override byte[] TakeBuffer(int bufferSize)
            {
                BufferPool bufferPool = FindPool(bufferSize);
                byte[] returnValue;
                if (bufferPool != null)
                {
                    byte[] buffer = bufferPool.Take();
                    if (buffer != null)
                    {
                        bufferPool.DecrementCount();
                        returnValue = buffer;
                    }
                    else
                    {
                        if (bufferPool.Peak == bufferPool.Limit)
                        {
                            bufferPool.Misses++;
                            if (++_totalMisses >= MaxMissesBeforeTuning)
                            {
                                TuneQuotas();
                            }
                        }

                        returnValue = new byte[bufferPool.BufferSize];
                    }
                }
                else
                {
                    returnValue = new byte[bufferSize];
                }

                _buffersPooled.TryRemove(returnValue.GetHashCode(), out var _);

                return returnValue;
            }

            private void TuneQuotas()
            {
                if (_areQuotasBeingTuned)
                {
                    return;
                }

                bool lockHeld = false;
                try
                {
                    Monitor.TryEnter(_tuningLock, ref lockHeld);

                    // Don't bother if another thread already has the lock
                    if (!lockHeld || _areQuotasBeingTuned)
                        return;

                    _areQuotasBeingTuned = true;
                }
                finally
                {
                    if (lockHeld)
                    {
                        Monitor.Exit(_tuningLock);
                    }
                }

                // find the "poorest" pool
                int starvedIndex = FindMostStarvedPool();
                if (starvedIndex >= 0)
                {
                    BufferPool starvedBufferPool = _bufferPools[starvedIndex];

                    if (_remainingMemory < starvedBufferPool.BufferSize)
                    {
                        // find the "richest" pool
                        int excessiveIndex = FindMostExcessivePool();
                        if (excessiveIndex >= 0)
                        {
                            // steal from the richest
                            DecreaseQuota(ref _bufferPools[excessiveIndex]);
                        }
                    }

                    if (_remainingMemory >= starvedBufferPool.BufferSize)
                    {
                        // give to the poorest
                        IncreaseQuota(ref _bufferPools[starvedIndex]);
                    }
                }

                // reset statistics
                for (int i = 0; i < _bufferPools.Length; i++)
                {
                    BufferPool bufferPool = _bufferPools[i];
                    bufferPool.Misses = 0;
                }

                _totalMisses = 0;
                _areQuotasBeingTuned = false;
            }

            internal abstract class BufferPool
            {
                private readonly int _bufferSize;
                private int _count;
                private readonly int _limit;
                private int _misses;
                private int _peak;

                public BufferPool(int bufferSize, int limit)
                {
                    _bufferSize = bufferSize;
                    _limit = limit;
                }

                public int BufferSize
                {
                    get { return _bufferSize; }
                }

                public int Limit
                {
                    get { return _limit; }
                }

                public int Misses
                {
                    get { return _misses; }
                    set { _misses = value; }
                }

                public int Peak
                {
                    get { return _peak; }
                }

                public void Clear()
                {
                    this.OnClear();
                    _count = 0;
                }

                public void DecrementCount()
                {
                    int newValue = _count - 1;
                    if (newValue >= 0)
                    {
                        _count = newValue;
                    }
                }

                public void IncrementCount()
                {
                    int newValue = _count + 1;
                    if (newValue <= _limit)
                    {
                        _count = newValue;
                        if (newValue > _peak)
                        {
                            _peak = newValue;
                        }
                    }
                }

                internal abstract byte[] Take();
                internal abstract bool Return(byte[] buffer);
                internal abstract void OnClear();

                internal static BufferPool CreatePool(int bufferSize, int limit)
                {
                    // To avoid many buffer drops during training of large objects which
                    // get allocated on the LOH, we use the LargeBufferPool and for 
                    // bufferSize < 85000, the SynchronizedPool. However if bufferSize < 85000
                    // and (bufferSize + array-overhead) > 85000, this would still use 
                    // the SynchronizedPool even though it is allocated on the LOH.
                    if (bufferSize < 85000)
                    {
                        return new SynchronizedBufferPool(bufferSize, limit);
                    }
                    else
                    {
                        return new LargeBufferPool(bufferSize, limit);
                    }
                }

                internal class SynchronizedBufferPool : BufferPool
                {
                    private readonly SynchronizedPool<byte[]> _innerPool;

                    internal SynchronizedBufferPool(int bufferSize, int limit)
                        : base(bufferSize, limit)
                    {
                        _innerPool = new SynchronizedPool<byte[]>(limit);
                    }

                    internal override void OnClear()
                    {
                        _innerPool.Clear();
                    }

                    internal override byte[] Take()
                    {
                        return _innerPool.Take();
                    }

                    internal override bool Return(byte[] buffer)
                    {
                        return _innerPool.Return(buffer);
                    }
                }

                internal class LargeBufferPool : BufferPool
                {
                    private readonly Stack<byte[]> _items;

                    internal LargeBufferPool(int bufferSize, int limit)
                        : base(bufferSize, limit)
                    {
                        _items = new Stack<byte[]>(limit);
                    }

                    private object ThisLock
                    {
                        get
                        {
                            return _items;
                        }
                    }

                    internal override void OnClear()
                    {
                        lock (ThisLock)
                        {
                            _items.Clear();
                        }
                    }

                    internal override byte[] Take()
                    {
                        lock (ThisLock)
                        {
                            if (_items.Count > 0)
                            {
                                return _items.Pop();
                            }
                        }

                        return null;
                    }

                    internal override bool Return(byte[] buffer)
                    {
                        lock (ThisLock)
                        {
                            if (_items.Count < this.Limit)
                            {
                                _items.Push(buffer);
                                return true;
                            }
                        }

                        return false;
                    }
                }
            }
        }

        internal class GcBufferManager : InternalBufferManager
        {
            private static readonly GcBufferManager s_value = new GcBufferManager();

            private GcBufferManager()
            {
            }

            public static GcBufferManager Value
            {
                get { return s_value; }
            }

            public override void Clear()
            {
            }

            public override byte[] TakeBuffer(int bufferSize)
            {
                return new byte[bufferSize];
            }

            public override void ReturnBuffer(byte[] buffer)
            {
                // do nothing, GC will reclaim this buffer
            }
        }
    }
}