using System;

namespace CodeElements.NetworkCallTransmission
{
    /// <summary>
    ///     The exception is thrown when the method on the remote site threw an exception which could not be serialized
    /// </summary>
    [Serializable]
    public class NetworkCallException : Exception
    {
        /// <summary>
        ///     Initialize a new instance of <see cref="NetworkCallException" />
        /// </summary>
        /// <param name="exception">The exception that should be wrapped.</param>
        public NetworkCallException(Exception exception) : base(exception.Message, exception)
        {
            ExceptionTypeName = exception.GetType().AssemblyQualifiedName;
        }

        /// <summary>
        ///     Initialize a new instance of <see cref="NetworkCallException" />
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">
        ///     The exception that is the cause of the current exception. If the
        ///     <see cref="innerException" /> parameter is not a null reference, the current exception is raised in a catch block
        ///     that handles the inner exception.
        /// </param>
        public NetworkCallException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initialize a new instance of <see cref="NetworkCallException" />
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="exceptionTypeName">The <see cref="Type.AssemblyQualifiedName" /> of the exception that is wrapped</param>
        /// <param name="innerException">
        ///     The exception that is the cause of the current exception. If the
        ///     <see cref="innerException" /> parameter is not a null reference, the current exception is raised in a catch block
        ///     that handles the inner exception.
        /// </param>
        public NetworkCallException(string message, string exceptionTypeName, Exception innerException) : base(message,
            innerException)
        {
            ExceptionTypeName = exceptionTypeName;
        }

        /// <summary>
        ///     Initialize a new instance of <see cref="NetworkCallException" />
        /// </summary>
        public NetworkCallException()
        {
        }

        /// <summary>
        ///     The exception message
        /// </summary>
        public override string Message
        {
            get
            {
                var s = base.Message;
                if (!string.IsNullOrEmpty(ExceptionTypeName))
                    return s + Environment.NewLine + $"Exception of type '{ExceptionTypeName}' thrown.";
                return s;
            }
        }

        /// <summary>
        ///     The type of the exception (AssemblyQualifiedName)
        /// </summary>
        public string ExceptionTypeName { get; set; }
    }
}