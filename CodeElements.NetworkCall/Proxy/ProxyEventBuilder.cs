﻿using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

// ReSharper disable PossibleNullReferenceException

namespace CodeElements.NetworkCall.Proxy
{
    internal class ProxyEventBuilder
    {
        private readonly MethodInfo _getInterceptor;
        private readonly MethodInfo _getEvents;
        private readonly MethodInfo _eventInterceptorEventSubscribed;
        private readonly MethodInfo _eventInterceptorEventUnsubscribed;

        public ProxyEventBuilder()
        {
            var eventInterceptorProxyType = typeof(IEventInterceptorProxy).GetTypeInfo();
            _getInterceptor = eventInterceptorProxyType
                .GetProperty(nameof(IEventInterceptorProxy.Interceptor)).GetGetMethod();
            _getEvents = eventInterceptorProxyType.GetProperty(nameof(IEventInterceptorProxy.Events))
                .GetGetMethod();

            var eventInterceptorType = typeof(IEventInterceptor).GetTypeInfo();
            _eventInterceptorEventSubscribed =
                eventInterceptorType.GetMethod(nameof(IEventInterceptor.EventSubscribed));
            _eventInterceptorEventUnsubscribed =
                eventInterceptorType.GetMethod(nameof(IEventInterceptor.EventUnsubscribed));
        }

        public FieldBuilder CreateEvent(int eventIndex, Type interfaceType, FieldInfo interceptorField, EventInfo eventInfo, TypeBuilder typeBuilder)
        {
            MethodAttributes methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig |
                                                MethodAttributes.SpecialName | MethodAttributes.Virtual;

            string qualifiedEventName = $"{interfaceType.Name}.{eventInfo.Name}";
            string addMethodName = $"add_{eventInfo.Name}";
            string remMethodName = $"remove_{eventInfo.Name}";

            var eventField = typeBuilder.DefineField(qualifiedEventName, eventInfo.EventHandlerType,
                FieldAttributes.Private);

            var builder = typeBuilder.DefineEvent(qualifiedEventName, EventAttributes.None, eventInfo.EventHandlerType);

            var addMethod = typeBuilder.DefineMethod("add_" + eventInfo.Name, methodAttributes, CallingConventions.HasThis, null,
                new[] {eventInfo.EventHandlerType});
            addMethod.SetImplementationFlags(MethodImplAttributes.Managed | MethodImplAttributes.Synchronized);

            var removeMethod = typeBuilder.DefineMethod("remove_" + eventInfo.Name, methodAttributes, CallingConventions.HasThis, null,
                new[] {eventInfo.EventHandlerType});
            removeMethod.SetImplementationFlags(MethodImplAttributes.Managed | MethodImplAttributes.Synchronized);

            EmitEventMethodBody(addMethod, eventIndex, eventInfo, eventField, false);
            EmitEventMethodBody(removeMethod, eventIndex, eventInfo, eventField, true);

            builder.SetAddOnMethod(addMethod);
            builder.SetRemoveOnMethod(removeMethod);

            var interfaceTypeInfo = interfaceType.GetTypeInfo();
            typeBuilder.DefineMethodOverride(addMethod, interfaceTypeInfo.GetMethod(addMethodName));
            typeBuilder.DefineMethodOverride(removeMethod, interfaceTypeInfo.GetMethod(remMethodName));

            /*var raiseMethod = typeBuilder.DefineMethod("On" + eventInfo.Name,
                MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.HasThis, null,
                new[] {typeof(object)});

            var il = raiseMethod.GetILGenerator();
            var notNullLabel = il.DefineLabel();
            var returnLabel = il.DefineLabel();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, eventField);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brtrue_S, notNullLabel);

            //if the value is null
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Br_S, returnLabel);

            //if the value is not null
            il.MarkLabel(notNullLabel);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            if (eventInfo.EventHandlerType.IsGenericType)
            {
                var argumentType = eventInfo.EventHandlerType.GetGenericArguments()[0];
                il.Emit(OpCodes.Castclass, argumentType);
            }
            il.Emit(OpCodes.Callvirt, eventInfo.EventHandlerType.GetMethod(nameof(EventHandler.Invoke)));

            il.MarkLabel(returnLabel);
            il.Emit(OpCodes.Ret);*/

            return eventField;
        }

        private void EmitEventMethodBody(MethodBuilder methodBuilder, int eventIndex, EventInfo eventInfo, FieldBuilder eventField, bool remove)
        {
            var action = typeof(Delegate).GetTypeInfo().GetMethod(remove ? nameof(Delegate.Remove) : nameof(Delegate.Combine),
                new[] {typeof(Delegate), typeof(Delegate)});

            var compareExchange = typeof(Interlocked).GetTypeInfo().GetMethods()
                .First(x => x.IsGenericMethod && x.Name == nameof(Interlocked.CompareExchange))
                .MakeGenericMethod(eventInfo.EventHandlerType);

            methodBuilder.DefineParameter(0, ParameterAttributes.Retval, null);
            methodBuilder.DefineParameter(1, ParameterAttributes.In, "value");

            var il = methodBuilder.GetILGenerator();
            il.DeclareLocal(eventInfo.EventHandlerType);
            il.DeclareLocal(eventInfo.EventHandlerType);
            il.DeclareLocal(eventInfo.EventHandlerType);

            Label loop = il.DefineLabel();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, eventField);
            il.Emit(OpCodes.Stloc_0);

            il.MarkLabel(loop);

            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Stloc_1);
            il.Emit(OpCodes.Ldloc_1);
            il.Emit(OpCodes.Ldarg_1);

            il.Emit(OpCodes.Call, action);
            il.Emit(OpCodes.Castclass, eventInfo.EventHandlerType);

            il.Emit(OpCodes.Stloc_2);
            il.Emit(OpCodes.Ldarg_0);

            il.Emit(OpCodes.Ldflda, eventField);

            il.Emit(OpCodes.Ldloc_2);
            il.Emit(OpCodes.Ldloc_1);

            il.Emit(OpCodes.Call, compareExchange);

            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldloc_1);
            il.Emit(OpCodes.Bne_Un_S, loop);

            var endMethod = il.DefineLabel();
            if (remove)
            {
                //if the new event handler is null
                il.Emit(OpCodes.Ldloc_2); //the new event handler
            }
            else
            {
                //if the previous event handler was null
                il.Emit(OpCodes.Ldloc_0); //the inital event handler
            }

            il.Emit(OpCodes.Brtrue, endMethod); //null

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, _getInterceptor);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, _getEvents);
            il.Emit(OpCodes.Ldc_I4, eventIndex);
            il.Emit(OpCodes.Ldelem_Ref); //load array[index]
            il.Emit(OpCodes.Callvirt, remove ? _eventInterceptorEventUnsubscribed : _eventInterceptorEventSubscribed);

            il.MarkLabel(endMethod);
            // end loop
            il.Emit(OpCodes.Ret);
        }
    }
}