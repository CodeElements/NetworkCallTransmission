namespace CodeElements.NetworkCallTransmissionProtocol.Proxy
{
    public interface IAsyncInterceptor
    {
        /// <summary>
        ///     Intercepts an asynchronous method <paramref name="invocation" /> with return type of
        ///     <see cref="T:System.Threading.Tasks.Task" />.
        /// </summary>
        /// <param name="invocation">The method invocation.</param>
        void InterceptAsynchronous(IInvocation invocation);

        /// <summary>
        ///     Intercepts an asynchronous method <paramref name="invocation" /> with return type of
        ///     <see cref="T:System.Threading.Tasks.Task`1" />.
        /// </summary>
        /// <typeparam name="TResult">
        ///     The type of the <see cref="T:System.Threading.Tasks.Task`1" />
        ///     <see cref="P:System.Threading.Tasks.Task`1.Result" />.
        /// </typeparam>
        /// <param name="invocation">The method invocation.</param>
        void InterceptAsynchronous<TResult>(IInvocation invocation);
    }
}