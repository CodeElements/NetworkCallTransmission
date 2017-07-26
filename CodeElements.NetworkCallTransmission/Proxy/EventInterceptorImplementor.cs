using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeElements.NetworkCallTransmission.Proxy
{
    internal class EventInterceptorImplementor
    {
        public FieldBuilder InterceptorField { get; private set; }
        public FieldBuilder EventsField { get; private set; }

        public void ImplementProxy(TypeBuilder typeBuilder)
        {
            // Implement the IAsyncInterceptorProxy interface
            typeBuilder.AddInterfaceImplementation(typeof(IEventInterceptorProxy));

            InterceptorField = ImplementorHelper.ImplementProperty(typeBuilder,
                nameof(IEventInterceptorProxy.Interceptor),
                typeof(IEventInterceptor), typeof(IEventInterceptorProxy));

            EventsField = ImplementorHelper.ImplementProperty(typeBuilder, nameof(IEventInterceptorProxy.Events),
                typeof(EventInfo[]), typeof(IEventInterceptorProxy));
        }

        public void ImplementTriggerEvent(TypeBuilder typeBuilder, FieldBuilder[] fieldBuilders, IList<EventInfo> events)
        {
            var methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual;
            var methodBuilder = typeBuilder.DefineMethod(nameof(IEventInterceptorProxy.TriggerEvent), methodAttributes,
                CallingConventions.HasThis, typeof(void), new[] {typeof(int), typeof(object), typeof(object)});

            var il = methodBuilder.GetILGenerator();
            var jumpTable = new Label[fieldBuilders.Length];

            for (int i = 0; i < fieldBuilders.Length; i++)
                jumpTable[i] = il.DefineLabel();

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Switch, jumpTable);

            //default case
            il.Emit(OpCodes.Ldstr, "The event id was not found.");

            il.Emit(OpCodes.Newobj,
                typeof(ArgumentException).GetConstructors()
                    .First(x => x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == typeof(string)));
            il.Emit(OpCodes.Throw);

            for (int i = 0; i < fieldBuilders.Length; i++)
            {
                var ifNotNullLabel = il.DefineLabel();

                il.MarkLabel(jumpTable[i]);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, fieldBuilders[i]);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Brtrue_S, ifNotNullLabel);

                //if null
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ret);

                //if not null
                il.MarkLabel(ifNotNullLabel);

                var eventInfo = events[i];
                var genericArguments = eventInfo.EventHandlerType.GetGenericArguments();

                il.Emit(OpCodes.Ldarg_2);
                il.Emit(genericArguments[0].IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, genericArguments[0]);

                if (genericArguments.Length == 2)
                {
                    il.Emit(OpCodes.Ldarg_3);
                    il.Emit(genericArguments[1].IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass,
                        genericArguments[1]);
                }

                il.Emit(OpCodes.Callvirt, eventInfo.EventHandlerType.GetMethod(nameof(TransmittedEventHandler<TransmissionInfo>.Invoke)));
                il.Emit(OpCodes.Ret);
            }

            typeBuilder.DefineMethodOverride(methodBuilder,
                typeof(IEventInterceptorProxy).GetMethod(nameof(IEventInterceptorProxy.TriggerEvent)));
        }

        private TransmittedEventHandler<string, int> _asd;

        public void Test(int eventId, object transmissionInfo, object parameter)
        {
            switch (eventId)
            {
                case 1:
                    _asd.Invoke((string) transmissionInfo, (int)parameter);
                    break;
            }
        }
    }
}