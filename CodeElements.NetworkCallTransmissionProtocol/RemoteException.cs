using System;

namespace CodeElements.NetworkCallTransmissionProtocol
{
    /// <summary>
    ///     The exception is thrown when the method on the remote site threw an exception which is not contained in the
    ///     standard exceptions (<see cref="NullReferenceException" />, <see cref="ArgumentException" />, etc.)
    /// </summary>
    [Serializable]
    public class RemoteException : Exception
    {
        /// <summary>
        ///     Initialize a new instance of <see cref="RemoteException" />
        /// </summary>
        /// <param name="exception">The exception that should be wrapped.</param>
        public RemoteException(Exception exception) : base(exception.Message, exception)
        {
            ExceptionType = exception.GetType().FullName;
        }

        /// <summary>
        ///     Initialize a new instance of <see cref="RemoteException" />
        /// </summary>
        public RemoteException()
        {
        }

        /// <summary>
        ///     The type of the exception (Fullname)
        /// </summary>
        public string ExceptionType { get; set; }
    }
}