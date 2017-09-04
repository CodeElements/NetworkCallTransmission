#if ZEROFORMATTER
using ZeroFormatter;

#endif
#if NETSERIALIZER
using System;
#endif

namespace CodeElements.NetworkCallTransmission.ExceptionWrapping.Wrapper
{
#if ZEROFORMATTER
    [ZeroFormattable]
#endif
#if NETSERIALIZER
    [Serializable]
#endif
    public class ExceptionWrapper : ExceptionInfo, IExceptionWrapper
    {
#if ZEROFORMATTER
        public ExceptionType Type { get; } = ExceptionType.Exception;
#endif
    }
}