using System;

namespace CodeElements.NetworkCallTransmission.ZeroFormatter.Exceptions
{
    public abstract class GenericExceptionInfo<TException> : ExceptionInfo where TException : Exception
    {
        protected override void ApplyProperties(Exception exception)
        {
            base.ApplyProperties(exception);
            ApplyProperties((TException)exception);
        }

        protected abstract void ApplyProperties(TException exception);
        protected abstract void ExportProperties(TException exception);
        protected abstract TException CreateException();

        public override void ExportProperties(Exception exception)
        {
            base.ExportProperties(exception);
            ExportProperties((TException) exception);
        }

        protected sealed override Exception InitializeException()
        {
            return CreateException();
        }
    }
}