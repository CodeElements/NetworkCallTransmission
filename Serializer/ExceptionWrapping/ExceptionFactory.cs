using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using CodeElements.NetworkCallTransmission.ExceptionWrapping.Wrapper;

#if ZEROFORMATTER
using ZeroFormatter;

#endif

#if NETSERIALIZER
using System.IO;

#endif

namespace CodeElements.NetworkCallTransmission.ExceptionWrapping
{
    internal static class ExceptionFactory
    {
        private static readonly FieldInfo MessageField;
        private static readonly PropertyInfo HResultProperty;
        private static readonly FieldInfo RemoteStackTraceField;
        private static readonly IReadOnlyDictionary<Type, Func<IExceptionWrapper>> ExceptionToExceptionInfo;

        static ExceptionFactory()
        {
            var exceptionType = typeof(Exception).GetTypeInfo();
            HResultProperty = exceptionType.GetProperty(nameof(Exception.HResult));
            MessageField = exceptionType.GetDeclaredField("_message");
            RemoteStackTraceField = exceptionType.GetDeclaredField("_remoteStackTraceString");

            ExceptionToExceptionInfo = new ReadOnlyDictionary<Type, Func<IExceptionWrapper>>(
                new Dictionary<Type, Func<IExceptionWrapper>>
                {
                    {typeof(ArgumentException), () => new ArgumentExceptionWrapper()},
                    {typeof(Exception), () => new ExceptionWrapper()},
                    {typeof(ObjectDisposedException), () => new ObjectDisposedExceptionWrapper()},
                    {typeof(AggregateException), () => new AggregateExceptionWrapper()}
                });
        }

        public static IExceptionWrapper PackException(Exception exception)
        {
            var exceptionType = exception.GetType();
            var exceptionInfo = ExceptionToExceptionInfo.TryGetValue(exceptionType, out var wrapperInitalizer)
                ? wrapperInitalizer()
                : new NetworkCallExceptionWrapper();

            exceptionInfo.ExportProperties(exception);
            return exceptionInfo;
        }

        public static void ExportExceptionInformation(Exception exception, ExceptionInfo exceptionInfo, bool serializeInnerException = true)
        {
            var stackTrace = exception.StackTrace;

            exceptionInfo.Message = MessageField == null
                ? exception.Message
                : (string) MessageField.GetValue(exception);
            exceptionInfo.StackTrace = stackTrace;
            exceptionInfo.HResult = exception.HResult;
            exceptionInfo.Source = exception.Source;

#if ZEROFORMATTER
            if (serializeInnerException && exception.InnerException != null)
                exceptionInfo.InnerException =
                    ZeroFormatterSerializer.Serialize(PackException(exception.InnerException));
#endif
#if NETSERIALIZER
            if(serializeInnerException && exception.InnerException != null)
                using (var memoryStream = new MemoryStream())
                {
                    ExceptionInfo.ExceptionWrapperSerializer.Serialize(memoryStream,
                        PackException(exception.InnerException));
                    exceptionInfo.InnerException = memoryStream.ToArray();
                }
#endif

        }

        public static void ApplyExceptionInformation(Exception exception, ExceptionInfo exceptionInfo)
        {
            RemoteStackTraceField?.SetValue(exception, exceptionInfo.StackTrace);
            HResultProperty?.SetValue(exception, exception.HResult);
            exception.Source = exceptionInfo.Source;
        }
    }
}