using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CodeElements.NetworkCallTransmissionProtocol.Extensions;
using CodeElements.NetworkCallTransmissionProtocol.Internal;

namespace CodeElements.NetworkCallTransmissionProtocol
{
    /// <summary>
    ///     Provides information about an interface. Thread-safe and can (should!) be reused.
    /// </summary>
    public class ExecuterInterfaceCache
    {
        private ExecuterInterfaceCache(IReadOnlyDictionary<uint, MethodInvoker> methodInvokers)
        {
            MethodInvokers = methodInvokers;
        }

        internal IReadOnlyDictionary<uint, MethodInvoker> MethodInvokers { get; }

        /// <summary>
        ///     Build the cache for a specific interface
        /// </summary>
        /// <typeparam name="TInterface">The interface which should be mirrored in the cache</typeparam>
        /// <returns>Return the thread-safe cache instance</returns>
        public static ExecuterInterfaceCache Build<TInterface>()
        {
            var interfaceType = typeof(TInterface);
            var members = interfaceType.GetMembers();
            if (members.Any(x => x.MemberType != MemberTypes.Method))
                throw new ArgumentException("The interface must only provide methods.", nameof(TInterface));

            var methods = members.Cast<MethodInfo>().ToList();
            if (methods.Count == 0)
                throw new ArgumentException("The interface must at least provide one method.", nameof(TInterface));

            var methodInvokers = new Dictionary<uint, MethodInvoker>();
            foreach (var methodInfo in methods)
            {
                Type actualReturnType;
                if (methodInfo.ReturnType == typeof(Task))
                    actualReturnType = null;
                else if (methodInfo.ReturnType.IsGenericType &&
                         methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    actualReturnType = methodInfo.ReturnType.GetGenericArguments()[0];
                else
                    throw new ArgumentException("Only tasks are supported as return type.", methodInfo.ToString());

                var parameterTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
                methodInvokers.Add(methodInfo.GetMethodId(),
                    new MethodInvoker(methodInfo, parameterTypes, actualReturnType));
            }

            return new ExecuterInterfaceCache(methodInvokers);
        }
    }
}