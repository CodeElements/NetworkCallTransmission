using System;
using System.Linq;
using ZeroFormatter;

namespace CodeElements.NetworkCallTransmission.Exceptions.Wrapper
{
    [ZeroFormattable]
    public class AggregateExceptionWrapper : GenericExceptionInfo<AggregateException>, IExceptionWrapper
    {
        [IgnoreFormat]
        public ExceptionType Type { get; } = ExceptionType.AggregateException;

        protected override void ApplyProperties(AggregateException exception)
        {
        }

        protected override void ExportProperties(AggregateException exception)
        {
            if (exception.InnerExceptions.Count > 0)
                InnerException = ZeroFormatterSerializer.Serialize(exception.InnerExceptions
                    .Select(ExceptionFactory.PackException).ToArray());
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
            return InnerException == null
                ? null
                : ZeroFormatterSerializer.Deserialize<IExceptionWrapper[]>(InnerException);
        }
    }
}