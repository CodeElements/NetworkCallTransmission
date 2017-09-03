using System;
using ZeroFormatter;

namespace CodeElements.NetworkCallTransmission.Exceptions.Wrapper
{
    [ZeroFormattable]
    public class ObjectDisposedExceptionWrapper : GenericExceptionInfo<ObjectDisposedException>, IExceptionWrapper
    {
        [IgnoreFormat]
        public ExceptionType Type { get; } = ExceptionType.ObjectDisposedException;

        [Index(NextIndex)]
        public virtual string ObjectName { get; set; }

        protected override void ApplyProperties(ObjectDisposedException exception)
        {
            //Applied in CreateException
        }

        protected override void ExportProperties(ObjectDisposedException exception)
        {
            ObjectName = exception.ObjectName;
        }

        protected override ObjectDisposedException CreateException()
        {
            return new ObjectDisposedException(ObjectName, Message);
        }
    }
}