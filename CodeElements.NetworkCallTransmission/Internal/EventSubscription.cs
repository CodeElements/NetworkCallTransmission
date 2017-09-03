using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeElements.NetworkCallTransmission.Internal
{
    internal class EventSubscription
    {
        private readonly MethodInfo _addMethod;
        private readonly Delegate _dynamicHandler;
        private readonly object _obj;
        private readonly MethodInfo _removeMethod;
        private readonly object _subscribeLock = new object();

        public EventSubscription(EventInfo eventInfo, ulong eventId, Action<object[]> handler, object obj)
        {
            _obj = obj;
            EventId = eventId;
            _dynamicHandler = BuildDynamicHandler(eventInfo.EventHandlerType, handler, eventId);

            _addMethod = eventInfo.GetAddMethod();
            _removeMethod = eventInfo.GetRemoveMethod();

            Subscriber = new List<IEventSubscriber>();
            SubscriberLock = new object();

            var permissionsAttribute = eventInfo.GetCustomAttribute<EventPermissionsAttribute>();
            RequiredPermissions = permissionsAttribute?.RequiredPermissions;

            var genericArguments = eventInfo.EventHandlerType.GenericTypeArguments;
            TransmissionInfoType = genericArguments[0];
            if (genericArguments.Length > 1)
                EventArgsType = genericArguments[1];
        }

        public ulong EventId { get; }
        public bool IsSubscribed { get; private set; }
        public List<IEventSubscriber> Subscriber { get; }
        public object SubscriberLock { get; }
        public int[] RequiredPermissions { get; }
        public Type EventArgsType { get; }  
        public Type TransmissionInfoType { get; }

        public void Subscribe()
        {
            if (IsSubscribed)
                return;

            lock (_subscribeLock)
            {
                if (IsSubscribed)
                    return;

                _addMethod.Invoke(_obj, new object[] {_dynamicHandler});
                IsSubscribed = true;
            }
        }

        public void Unsubscribe()
        {
            if (!IsSubscribed)
                return;

            lock (_subscribeLock)
            {
                if (!IsSubscribed)
                    return;

                _removeMethod.Invoke(_obj, new object[] {_dynamicHandler});
                IsSubscribed = false;
            }
        }

        private static Delegate BuildDynamicHandler(Type delegateType, Action<object[]> func, ulong id)
        {
            var invokeMethod = delegateType.GetTypeInfo().GetMethod(nameof(EventHandler.Invoke));
            var parms = invokeMethod.GetParameters().Select(parm => Expression.Parameter(parm.ParameterType, parm.Name))
                .ToArray();
            var instance = func.Target == null ? null : Expression.Constant(func.Target);
            var converted = parms.Select(parm => (Expression) Expression.Convert(parm, typeof(object))).ToList();
            converted.Insert(0, Expression.Convert(Expression.Constant(id), typeof(object)));

            var call = Expression.Call(instance, func.GetMethodInfo(), Expression.NewArrayInit(typeof(object), converted));
            var expr = Expression.Lambda(delegateType, call, parms);
            return expr.Compile();
        }
    }
}