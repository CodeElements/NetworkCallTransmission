using System;
#if ZEROFORMATTER
using ZeroFormatter;

#endif

namespace CodeElements.NetworkCallTransmission.ExceptionWrapping.Wrapper
{
#if ZEROFORMATTER
    [ZeroFormattable]
#endif
#if NETSERIALIZER
    [Serializable]
#endif
    public class ArgumentExceptionWrapper : GenericExceptionInfo<ArgumentException>, IExceptionWrapper
    {
#if ZEROFORMATTER
        [IgnoreFormat]
        public ExceptionType Type { get; } = ExceptionType.ArgumentException;

        [Index(NextIndex)] virtual
#endif
        public string ParamName { get; set; }

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