using ZeroFormatter;

namespace CodeElements.NetworkCallTransmission.ZeroFormatter.Exceptions.Wrapper
{
    [ZeroFormattable]
    public class ExceptionWrapper : ExceptionInfo, IExceptionWrapper
    {
        public ExceptionType Type { get; } = ExceptionType.Exception;
    }
}