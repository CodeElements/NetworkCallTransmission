using System;
using CodeElements.NetworkCallTransmission.ZeroFormatter.Exceptions.Wrapper;
using ZeroFormatter;

namespace CodeElements.NetworkCallTransmission.ZeroFormatter.Exceptions
{
    [Union(new[]
    {
        typeof(ArgumentExceptionWrapper), typeof(ExceptionWrapper), typeof(ObjectDisposedExceptionWrapper),
        typeof(RemoteCallExceptionWrapper), typeof(AggregateExceptionWrapper)
    }, typeof(ExceptionWrapper))]
    public interface IExceptionWrapper
    {
        [UnionKey]
        ExceptionType Type { get; }

        Exception GetException();
        void ExportProperties(Exception exception);
    }
}