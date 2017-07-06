using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CodeElements.NetworkCallTransmissionProtocol
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
                Data?.Dispose();
            }
        }

        public ResponseType ResponseType { get; private set; }
        public MemoryStream Data { get; private set; }

        public Task<bool> Wait(TimeSpan timeout)
        {
            return _semaphoreSlim.WaitAsync(timeout);
        }

        public void ReceivedResult(ResponseType responseType, MemoryStream memoryStream)
        {
            if (_isDisposed)
                return;

            ResponseType = responseType;
            Data = memoryStream;

            _semaphoreSlim.Release();
        }
    }
}