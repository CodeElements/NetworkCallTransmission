using System;
#if NETSERIALIZER
using NetSerializer;
using CodeElements.NetworkCallTransmission.ExceptionWrapping.Wrapper;

#endif

#if ZEROFORMATTER
using ZeroFormatter;

#endif

namespace CodeElements.NetworkCallTransmission.ExceptionWrapping
{
    public abstract class ExceptionInfo
    {
#if NETSERIALIZER
        public static Serializer ExceptionWrapperSerializer = new Serializer(typeof(IExceptionWrapper),
            typeof(IExceptionWrapper[]), typeof(ArgumentExceptionWrapper), typeof(ExceptionWrapper),
            typeof(ObjectDisposedExceptionWrapper), typeof(NetworkCallExceptionWrapper),
            typeof(AggregateExceptionWrapper));
#endif

        protected const int NextIndex = 5;

#if ZEROFORMATTER
        [Index(0)] virtual
#endif
        public string Message { get; set; }

        //to workaround circular reference
#if ZEROFORMATTER
        [Index(1)] virtual
#endif
        public byte[] InnerException { get; set; }

#if ZEROFORMATTER
        [Index(2)] virtual
#endif
        public string StackTrace { get; set; }

#if ZEROFORMATTER
        [Index(3)] virtual
#endif
        public int HResult { get; set; }

#if ZEROFORMATTER
        [Index(4)] virtual
#endif
        public string Source { get; set; }

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

#if ZEROFORMATTER
        protected IExceptionWrapper GetInnerException()
        {
            return InnerException == null
                ? null
                : ZeroFormatterSerializer.Deserialize<IExceptionWrapper>(InnerException);
        }
#endif
#if NETSERIALIZER
        protected IExceptionWrapper GetInnerException()
        {
            return InnerException == null
                ? null
                : ExceptionWrapperSerializer.Deserialize<IExceptionWrapper>(InnerException, 0);
        }
#endif

    }
}