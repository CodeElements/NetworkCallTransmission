using System;
using System.IO;
using System.Linq;

#if ZEROFORMATTER
using ZeroFormatter;

#endif
#if NETSERIALIZER
using NetSerializer;

#endif

namespace CodeElements.NetworkCallTransmission.ExceptionWrapping.Wrapper
{
#if ZEROFORMATTER
    [ZeroFormattable]
#endif
#if NETSERIALIZER
    [Serializable]
#endif
    public class AggregateExceptionWrapper : GenericExceptionInfo<AggregateException>, IExceptionWrapper
    {
#if ZEROFORMATTER
        [IgnoreFormat]
        public ExceptionType Type { get; } = ExceptionType.AggregateException;
#endif

        protected override void ApplyProperties(AggregateException exception)
        {
        }

        protected override void ExportProperties(AggregateException exception)
        {
            if (exception.InnerExceptions.Count > 0)
#if ZEROFORMATTER
                InnerException = ZeroFormatterSerializer.Serialize(exception.InnerExceptions
                    .Select(ExceptionFactory.PackException).ToArray());
#endif
#if NETSERIALIZER
                using (var stream = new MemoryStream())
                {
                    ExceptionWrapperSerializer.Serialize(stream,
                        exception.InnerExceptions.Select(ExceptionFactory.PackException).ToArray());
                    InnerException = stream.ToArray();
                }
#endif
        }

        public override void ExportProperties(Exception exception)
        {
            ExceptionFactory.ExportExceptionInformation(exception, this, false);
            ExportProperties((AggregateException) exception);
        }

        protected override AggregateException CreateException()
        {
            var innerExceptions = GetInnerExceptions();
            return innerExceptions == null
                ? new AggregateException(Message)
                : new AggregateException(Message, innerExceptions.Select(x => x.GetException()));
        }

        protected IExceptionWrapper[] GetInnerExceptions()
        {
#if ZEROFORMATTER
            return InnerException == null
                ? null
                : ZeroFormatterSerializer.Deserialize<IExceptionWrapper[]>(InnerException);
#endif
#if NETSERIALIZER
            return InnerException == null
                ? null
                : ExceptionWrapperSerializer.Deserialize<IExceptionWrapper[]>(InnerException, 0);
#endif
        }
    }
}