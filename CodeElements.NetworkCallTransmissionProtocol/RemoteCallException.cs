using System;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace CodeElements.NetworkCallTransmissionProtocol
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
        public RemoteCallException(Exception exception) : base(exception.Message, exception)
        {
            ExceptionType = exception.GetType().FullName;
        }

        /// <summary>
        ///     Initialize a new instance of <see cref="RemoteCallException" />
        /// </summary>
        public RemoteCallException()
        {
        }

        /// <summary>
        ///     For deserialization
        /// </summary>
        protected RemoteCallException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ExceptionType = info.GetString("ExceptionType");
        }

        /// <summary>
        ///     The exception message
        /// </summary>
        public override string Message
        {
            get
            {
                var s = base.Message;
                if (!string.IsNullOrEmpty(ExceptionType))
                    return s + Environment.NewLine + $"Exception of type '{ExceptionType}' thrown.";
                return s;
            }
        }

        /// <summary>
        ///     The type of the exception (Fullname)
        /// </summary>
        public string ExceptionType { get; set; }

        /// <summary>
        ///     Serialize the exception
        /// </summary>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            Contract.EndContractBlock();
            base.GetObjectData(info, context);
            info.AddValue("ExceptionType", ExceptionType, typeof(string));
        }
    }
}