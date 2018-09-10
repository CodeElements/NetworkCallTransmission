using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CodeElements.NetworkCall.Extensions;
using CodeElements.NetworkCall.Internal;

namespace CodeElements.NetworkCall
{
    /// <summary>
    ///     Provides information about an interface. Thread-safe and can (should!) be reused.
    /// </summary>
    public class NetworkCallServerCache
    {
        private NetworkCallServerCache(IReadOnlyDictionary<uint, MethodInvoker> methodInvokers,
            IReadOnlyDictionary<uint, NetworkEventInfo> networkEvents, ArrayPool<byte> pool)
        {
            MethodInvokers = methodInvokers ?? throw new ArgumentNullException(nameof(methodInvokers));
            NetworkEvents = networkEvents ?? throw new ArgumentNullException(nameof(networkEvents));
            Pool = pool ?? throw new ArgumentNullException(nameof(pool));
        }

        internal IReadOnlyDictionary<uint, MethodInvoker> MethodInvokers { get; }
        internal IReadOnlyDictionary<uint, NetworkEventInfo> NetworkEvents { get; }
        internal ArrayPool<byte> Pool { get; }

        public static NetworkCallServerCache Build<TInterface>()
        {
            var interfaceType = typeof(TInterface);
            if (!interfaceType.IsInterface)
                throw new ArgumentException("The generic type must be an interface", nameof(TInterface));

            return InternalBuild(interfaceType, ArrayPool<byte>.Shared);
        }

        public static NetworkCallServerCache Build<TInterface>(ArrayPool<byte> pool)
        {
            var interfaceType = typeof(TInterface);
            if (!interfaceType.IsInterface)
                throw new ArgumentException("The generic type must be an interface", nameof(TInterface));

            return InternalBuild(interfaceType, pool);
        }

        private static NetworkCallServerCache InternalBuild(Type interfaceType, ArrayPool<byte> pool)
        {
            var members = interfaceType.GetMembers();
            if (members.Any(x => x.MemberType != MemberTypes.Method && x.MemberType != MemberTypes.Event))
                throw new ArgumentException("The interface must only provide methods and events.",
                    nameof(interfaceType));

            var methodInvokers = BuildMethodInvokers(interfaceType);
            var networkEvents = BuildEventInformation(interfaceType);

            return new NetworkCallServerCache(methodInvokers, networkEvents, pool);
        }

        private static IReadOnlyDictionary<uint, MethodInvoker> BuildMethodInvokers(Type interfaceType)
        {
            var methods = interfaceType.GetMethods();
            if (!methods.Any())
                throw new ArgumentException("The interface must provide at least one method", nameof(interfaceType));

            var methodInvokers = new Dictionary<uint, MethodInvoker>(methods.Length);
            foreach (var methodInfo in methods.Where(x => !x.IsSpecialName))
            {
                Type actualReturnType;
                if (methodInfo.ReturnType == typeof(Task))
                    actualReturnType = null;
                else if (methodInfo.ReturnType.IsGenericType &&
                         methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    actualReturnType = methodInfo.ReturnType.GenericTypeArguments[0];
                else
                    throw new ArgumentException("Only Task and Task<> are supported as return type of methods.",
                        methodInfo.ToString());

                var parameterTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
                methodInvokers.Add(methodInfo.GetMethodId(),
                    new MethodInvoker(methodInfo, parameterTypes, actualReturnType));
            }

            return methodInvokers;
        }

        private static IReadOnlyDictionary<uint, NetworkEventInfo> BuildEventInformation(Type interfaceType)
        {
            var events = interfaceType.GetEvents();
            var networkEvents = new Dictionary<uint, NetworkEventInfo>(events.Length);

            foreach (var eventInfo in events)
            {
                var eventHandlerType = eventInfo.EventHandlerType;
                if(!eventHandlerType.IsGenericType)
                    if (eventHandlerType == typeof(EventHandler))
                        continue;

                if (!(eventHandlerType.IsGenericType && eventHandlerType.GetGenericTypeDefinition() == typeof(EventHandler<>)))
                    throw new ArgumentException("All events must be of type EventHandler or EventHandler<>",
                        nameof(interfaceType));

                var networkEvent = new NetworkEventInfo(eventInfo);
                networkEvents.Add(networkEvent.EventId, networkEvent);
            }

            return networkEvents;
        }
    }
}