using System;
using System.Threading.Tasks;

namespace CodeElements.NetworkCallTransmissionProtocol
{
    /// <summary>
    ///     The delegate which will get invoked when a package should be sent to the remote site
    /// </summary>
    /// <param name="data">The data to send.</param>
    /// <returns>Return the task which completes once the package is sent</returns>
    public delegate Task SendDataDelegate(ResponseData data);

    public abstract class DataTransmitter
    {
        /// <summary>
        ///     Reserve bytes at the beginning of the <see cref="SendData" /> buffer for custom headers
        /// </summary>
        public int CustomOffset { get; set; }

        /// <summary>
        ///     The delegate which will get invoked when a package should be sent to the remote site. This property must be set
        ///     before the interface is used.
        /// </summary>
        public SendDataDelegate SendData { get; set; }

        /// <summary>
        /// Receive data
        /// </summary>
        /// <param name="data">An array of bytes</param>
        /// <param name="offset">The starting position within the buffer</param>
        public abstract void ReceiveData(byte[] data, int offset);

        protected virtual Task OnSendData(ResponseData data)
        {
            if (SendData == null)
                throw new InvalidOperationException(
                    "The SendData delegate is null. Please initialize this class before using.");

            return SendData(data);
        }
    }
}