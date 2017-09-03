using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CodeElements.NetworkCallTransmission.Extensions;
using CodeElements.NetworkCallTransmission.Internal;
using CodeElements.NetworkCallTransmission.Memory;

namespace CodeElements.NetworkCallTransmission
{
    /// <summary>
    ///     Provides information about an interface. Thread-safe and can (should!) be reused.
    /// </summary>
    public class ExecuterInterfaceCache
    {
        internal const int MaxBufferSize = 2000;
        internal const int DefaultPoolSize = MaxBufferSize * 524; // ~1 MiB

        private ExecuterInterfaceCache(IReadOnlyDictionary<uint, MethodInvoker> methodInvokers,
            BufferManager bufferManager)
        {
            MethodInvokers = methodInvokers;
            BufferManager = bufferManager;
        }

        internal IReadOnlyDictionary<uint, MethodInvoker> MethodInvokers { get; }
        internal BufferManager BufferManager { get; }

        public static ExecuterInterfaceCache Build<TInterface>()
        {
            return Build<TInterface>(DefaultPoolSize);
        }

        /// <summary>
        ///     Build the cache for a specific interface
        /// </summary>
        /// <typeparam name="TInterface">The interface which should be mirrored in the cache</typeparam>
        /// <param name="totalBufferCacheSize">The size of the thread-shared buffer for object serialization. Submit zero if you dont want a global buffer</param>
        /// <returns>Return the thread-safe cache instance</returns>
        public static ExecuterInterfaceCache Build<TInterface>(long totalBufferCacheSize)
        {
            var interfaceType = typeof(TInterface).GetTypeInfo();
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
                else if (methodInfo.ReturnType.GetTypeInfo().IsGenericType &&
                         methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    actualReturnType = methodInfo.ReturnType.GenericTypeArguments[0];
                else
                    throw new ArgumentException("Only tasks are supported as return type.", methodInfo.ToString());

                var parameterTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
                methodInvokers.Add(methodInfo.GetMethodId(),
                    new MethodInvoker(methodInfo, parameterTypes, actualReturnType));
            }

            BufferManager bufferManager;
            if (totalBufferCacheSize == 0)
                bufferManager = BufferManager.CreateBufferManager(0, 0); //No buffers
            else if (totalBufferCacheSize < MaxBufferSize)
                throw new ArgumentException(
                    $"The total buffer size must be greater than the size of one buffer {MaxBufferSize}. Submit zero if you don't want a buffer cache",
                    nameof(bufferManager));
            else
                bufferManager = BufferManager.CreateBufferManager(totalBufferCacheSize, MaxBufferSize);

            return new ExecuterInterfaceCache(methodInvokers, bufferManager);
        }
    }
}