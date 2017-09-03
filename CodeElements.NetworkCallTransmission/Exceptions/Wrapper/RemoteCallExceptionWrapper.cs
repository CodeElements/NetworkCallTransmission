using System;
using System.Reflection;
using ZeroFormatter;

namespace CodeElements.NetworkCallTransmission.Exceptions.Wrapper
{
    [ZeroFormattable]
    public class RemoteCallExceptionWrapper : ExceptionInfo, IExceptionWrapper
    {
        [IgnoreFormat]
        public ExceptionType Type { get; } = ExceptionType.NetworkCallException;

        [Index(NextIndex)]
        public virtual string ExceptionClassName { get; set; }

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

            return new RemoteCallException(Message, GetInnerException()?.GetException(), ExceptionClassName);
        }
    }
}