using System;

namespace CodeElements.NetworkCallTransmission
{
    /// <summary>
    ///     The exception is thrown when the method on the remote site threw an exception which could not be serialized
    /// </summary>
    [Serializable]
    public class RemoteCallException : Exception
    {
        /// <summary>
        ///     Initialize a new instance of <see cref="RemoteCallException" />
        /// </summary>
        /// <param name="exception">The exception that should be wrapped.</param>
        internal RemoteCallException(Exception exception) : base(exception.Message, exception)
        {
            ClassName = exception.GetType().AssemblyQualifiedName;
        }

        internal RemoteCallException(string message, Exception innerException, string className) : base(message, innerException)
        {
            ClassName = className;
        }

        /// <summary>
        ///     Initialize a new instance of <see cref="RemoteCallException" />
        /// </summary>
        public RemoteCallException()
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
                if (!string.IsNullOrEmpty(ClassName))
                    return s + Environment.NewLine + $"Exception of type '{ClassName}' thrown.";
                return s;
            }
        }

        /// <summary>
        ///     The type of the exception (AssemblyQualifiedName)
        /// </summary>
        public string ClassName { get; set; }
    }
}