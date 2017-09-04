using System;
using CodeElements.NetworkCallTransmission.ExceptionWrapping.Wrapper;
#if ZEROFORMATTER
using ZeroFormatter;

#endif

namespace CodeElements.NetworkCallTransmission.ExceptionWrapping
{
#if ZEROFORMATTER
    [Union(new[]
    {
        typeof(ArgumentExceptionWrapper), typeof(ExceptionWrapper), typeof(ObjectDisposedExceptionWrapper),
        typeof(NetworkCallExceptionWrapper), typeof(AggregateExceptionWrapper)
    }, typeof(ExceptionWrapper))]
#endif
    public interface IExceptionWrapper
    {
#if ZEROFORMATTER
        [UnionKey]
        ExceptionType Type { get; }
#endif

        Exception GetException();
        void ExportProperties(Exception exception);
    }
}