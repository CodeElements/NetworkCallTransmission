using System;
using ZeroFormatter;

namespace CodeElements.NetworkCallTransmission.ZeroFormatter.Exceptions
{
    public abstract class ExceptionInfo
    {
        protected const int NextIndex = 5;

        [Index(0)]
        public virtual string Message { get; set; }

        [Index(1)] //to workaround circular reference
        public virtual byte[] InnerException { get; set; }

        [Index(2)]
        public virtual string StackTrace { get; set; }

        [Index(3)]
        public virtual int HResult { get; set; }

        [Index(4)]
        public virtual string Source { get; set; }

        public virtual void ExportProperties(Exception exception)
        {
            ExceptionFactory.ExportExceptionInformation(exception, this);
        }

        public Exception GetException()
        {
            var exception = InitializeException();
            ApplyProperties(exception);
            return exception;
        }

        protected virtual void ApplyProperties(Exception exception)
        {
            ExceptionFactory.ApplyExceptionInformation(exception, this);
        }

        protected virtual Exception InitializeException()
        {
            return new Exception(Message, GetInnerException()?.GetException());
        }

        protected IExceptionWrapper GetInnerException()
        {
            return InnerException == null
                ? null
                : ZeroFormatterSerializer.Deserialize<IExceptionWrapper>(InnerException);
        }
    }
}