using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CodeElements.NetworkCall.Extensions;

namespace CodeElements.NetworkCall.Internal
{
    internal class NetworkEventInfo
    {
        private readonly MethodInfo _addMethod;
        private readonly MethodInfo _removeMethod;
        private readonly Delegate _dynamicHandler;
        private readonly ConcurrentDictionary<object, Action<object>> _subscribers;

        public NetworkEventInfo(EventInfo eventInfo)
        {
            _dynamicHandler = BuildDynamicHandler(eventInfo.EventHandlerType, HandleEvent);

            _addMethod = eventInfo.GetAddMethod();
            _removeMethod = eventInfo.GetRemoveMethod();

            _subscribers = new ConcurrentDictionary<object, Action<object>>();

            EventId = eventInfo.GetEventId();

            var genericArguments = eventInfo.EventHandlerType.GenericTypeArguments;
            if (genericArguments.Any())
                EventArgsType = genericArguments[0];
        }

        public Type EventArgsType { get; }
        public uint EventId { get; }

        public void Subscribe(object obj, Action<object> callAction)
        {
            _subscribers.GetOrAdd(obj, o =>
            {
                _addMethod.Invoke(obj, new object[] {_dynamicHandler});
                return callAction;
            });
        }

        public void Unsubscribe(object obj)
        {
            if (_subscribers.TryRemove(obj, out _))
            {
                _removeMethod.Invoke(obj, new object[] { _dynamicHandler });
            }
        }

        private void HandleEvent(object[] eventParams)
        {
            var sender = eventParams[0];
            if (_subscribers.TryGetValue(sender, out var action))
            {
                //remove sender object from parameters
                action.Invoke(eventParams.Length > 1 ? eventParams[1] : null);
            }
        }

        private static Delegate BuildDynamicHandler(Type delegateType, Action<object[]> func)
        {
            var invokeMethod = delegateType.GetMethod(nameof(EventHandler.Invoke));
            var parms = invokeMethod.GetParameters().Select(parm => Expression.Parameter(parm.ParameterType, parm.Name))
                .ToArray();
            var converted = parms.Select(parm => (Expression) Expression.Convert(parm, typeof(object))).ToList();

            var instance = func.Target == null ? null : Expression.Constant(func.Target);
            var call = Expression.Call(instance, func.GetMethodInfo(),
                Expression.NewArrayInit(typeof(object), converted));
            var expr = Expression.Lambda(delegateType, call, parms);
            return expr.Compile();
        }
    }
}