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
    public class ObjectDisposedExceptionWrapper : GenericExceptionInfo<ObjectDisposedException>, IExceptionWrapper
    {
#if ZEROFORMATTER
        [IgnoreFormat]
        public ExceptionType Type { get; } = ExceptionType.ObjectDisposedException;

        [Index(NextIndex)] virtual
#endif
        public string ObjectName { get; set; }

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