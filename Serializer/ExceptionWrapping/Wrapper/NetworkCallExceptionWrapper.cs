using System;
using System.Reflection;
using CodeElements.NetworkCall;

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
    public class NetworkCallExceptionWrapper : ExceptionInfo, IExceptionWrapper
    {
#if ZEROFORMATTER
        [IgnoreFormat]
        public ExceptionType Type { get; } = ExceptionType.NetworkCallException;

        [Index(NextIndex)] virtual
#endif
        public string ExceptionClassName { get; set; }

        public override void ExportProperties(Exception exception)
        {
            base.ExportProperties(exception);
            ExceptionClassName = exception.GetType().AssemblyQualifiedName;
        }

        protected override Exception InitializeException()
        {
            var type = System.Type.GetType(ExceptionClassName, false);
            if (type != null)
            {
                if (InnerException != null)
                {
                    var constructor = type.GetTypeInfo().GetConstructor(new[] { typeof(string), typeof(Exception) });
                    if (constructor != null)
                        return (Exception)Activator.CreateInstance(type, Message, GetInnerException()?.GetException());
                }
                if (Message != null)
                {
                    var constructor = type.GetTypeInfo().GetConstructor(new[] { typeof(string) });
                    if (constructor != null)
                        return (Exception)Activator.CreateInstance(type, Message);
                }
                try
                {
                    return (Exception)Activator.CreateInstance(type);
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            return new NetworkCallException(Message, ExceptionClassName, GetInnerException()?.GetException());
        }
    }
}