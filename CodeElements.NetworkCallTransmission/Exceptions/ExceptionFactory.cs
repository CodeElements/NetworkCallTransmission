using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using CodeElements.NetworkCallTransmission.Exceptions.Wrapper;
using ZeroFormatter;

namespace CodeElements.NetworkCallTransmission.Exceptions
{
    internal static class ExceptionFactory
    {
        private static readonly FieldInfo StackTraceStringProperty;
        private static readonly FieldInfo MessageField;
        private static readonly PropertyInfo HResultProperty;
        private static readonly FieldInfo RemoteStackTraceField;
        private static readonly IReadOnlyDictionary<Type, Func<IExceptionWrapper>> ExceptionToExceptionInfo;

        static ExceptionFactory()
        {
            var exceptionType = typeof(Exception).GetTypeInfo();
            StackTraceStringProperty = exceptionType.GetDeclaredField("_stackTraceString");
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
                : new RemoteCallExceptionWrapper();

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

            if (serializeInnerException && exception.InnerException != null)
                exceptionInfo.InnerException =
                    ZeroFormatterSerializer.Serialize(PackException(exception.InnerException));
        }

        public static void ApplyExceptionInformation(Exception exception, ExceptionInfo exceptionInfo)
        {
            RemoteStackTraceField?.SetValue(exception, exceptionInfo.StackTrace);
            HResultProperty?.SetValue(exception, exception.HResult);
            exception.Source = exceptionInfo.Source;
        }
    }
}