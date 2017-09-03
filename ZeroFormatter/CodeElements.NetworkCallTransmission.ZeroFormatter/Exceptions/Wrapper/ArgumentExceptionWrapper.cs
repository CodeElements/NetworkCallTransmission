using System;
using ZeroFormatter;

namespace CodeElements.NetworkCallTransmission.ZeroFormatter.Exceptions.Wrapper
{
    [ZeroFormattable]
    public class ArgumentExceptionWrapper : GenericExceptionInfo<ArgumentException>, IExceptionWrapper
    {
        [IgnoreFormat]
        public ExceptionType Type { get; } = ExceptionType.ArgumentException;

        [Index(NextIndex)]
        public virtual string ParamName { get; set; }

        protected override void ApplyProperties(ArgumentException exception)
        {
            //Applied in CreateException
        }

        protected override void ExportProperties(ArgumentException exception)
        {
            ParamName = exception.ParamName;
        }

        protected override ArgumentException CreateException()
        {
            return new ArgumentException(Message, ParamName, GetInnerException()?.GetException());
        }
    }
}