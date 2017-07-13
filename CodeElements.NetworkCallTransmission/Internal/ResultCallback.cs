using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodeElements.NetworkCallTransmission.Internal
{
    internal class ResultCallback : IDisposable
    {
        private readonly SemaphoreSlim _semaphoreSlim;
        private bool _isDisposed;

        public ResultCallback()
        {
            _semaphoreSlim = new SemaphoreSlim(0, 1);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _semaphoreSlim?.Dispose();
                Data = null;
            }
        }

        public CallTransmissionResponseType ResponseType { get; private set; }
        public byte[] Data { get; private set; }
        public int Offset { get; private set; }

        public Task<bool> Wait(TimeSpan timeout)
        {
            return _semaphoreSlim.WaitAsync(timeout);
        }

        public void ReceivedResult(CallTransmissionResponseType responseType, byte[] data, int offset)
        {
            if (_isDisposed)
                return;

            ResponseType = responseType;
            Data = data;
            Offset = offset;

            _semaphoreSlim.Release();
        }
    }
}