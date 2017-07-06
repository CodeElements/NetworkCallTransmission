using System;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeElements.NetworkCallTransmissionProtocol.Proxy
{
	internal class InterceptorImplementor
	{
		public FieldBuilder InterceptorField { get; private set; }
		public FieldBuilder MethodsField { get; private set; }

		private void ImplementInterceptorField(TypeBuilder typeBuilder)
		{
			InterceptorField = typeBuilder.DefineField("__interceptor", typeof(IAsyncInterceptor),
				FieldAttributes.Private);

			// Implement the getter
			var attributes = MethodAttributes.Public | MethodAttributes.HideBySig |
			                 MethodAttributes.SpecialName | MethodAttributes.NewSlot |
			                 MethodAttributes.Virtual;

			// Implement the getter
			var getterMethod = typeBuilder.DefineMethod("get_Interceptor", attributes,
				CallingConventions.HasThis, typeof(IAsyncInterceptor),
				new Type[0]);
			getterMethod.SetImplementationFlags(MethodImplAttributes.Managed | MethodImplAttributes.IL);

			var il = getterMethod.GetILGenerator();

			// This is equivalent to:
			// get { return __interceptor;
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, InterceptorField);
			il.Emit(OpCodes.Ret);

			// Implement the setter
			var setterMethod = typeBuilder.DefineMethod("set_Interceptor", attributes,
				CallingConventions.HasThis, typeof(void),
				new[] {typeof(IAsyncInterceptor)});

			setterMethod.SetImplementationFlags(MethodImplAttributes.Managed | MethodImplAttributes.IL);
			il = setterMethod.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Stfld, InterceptorField);
			il.Emit(OpCodes.Ret);

			var originalSetter = typeof(IProxy).GetMethod("set_Interceptor");
			var originalGetter = typeof(IProxy).GetMethod("get_Interceptor");

			typeBuilder.DefineMethodOverride(setterMethod, originalSetter);
			typeBuilder.DefineMethodOverride(getterMethod, originalGetter);
		}

		private void ImplementMethodInfoArray(TypeBuilder typeBuilder)
		{
			MethodsField = typeBuilder.DefineField("__methods", typeof(MethodInfo[]),
				FieldAttributes.Private);

			// Implement the getter
			var attributes = MethodAttributes.Public | MethodAttributes.HideBySig |
			                 MethodAttributes.SpecialName | MethodAttributes.NewSlot |
			                 MethodAttributes.Virtual;

			// Implement the getter
			var getterMethod = typeBuilder.DefineMethod("get_Methods", attributes,
				CallingConventions.HasThis, typeof(MethodInfo[]),
				new Type[0]);
			getterMethod.SetImplementationFlags(MethodImplAttributes.Managed | MethodImplAttributes.IL);

			var il = getterMethod.GetILGenerator();

			// This is equivalent to:
			// get { return __interceptor;
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, MethodsField);
			il.Emit(OpCodes.Ret);

			// Implement the setter
			var setterMethod = typeBuilder.DefineMethod("set_Methods", attributes,
				CallingConventions.HasThis, typeof(void),
				new[] { typeof(MethodInfo[]) });

			setterMethod.SetImplementationFlags(MethodImplAttributes.Managed | MethodImplAttributes.IL);
			il = setterMethod.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Stfld, MethodsField);
			il.Emit(OpCodes.Ret);

			var originalSetter = typeof(IProxy).GetMethod("set_Methods");
			var originalGetter = typeof(IProxy).GetMethod("get_Methods");

			typeBuilder.DefineMethodOverride(setterMethod, originalSetter);
			typeBuilder.DefineMethodOverride(getterMethod, originalGetter);
		}

		public void ImplementProxy(TypeBuilder typeBuilder)
		{
			// Implement the IProxy interface
			typeBuilder.AddInterfaceImplementation(typeof(IProxy));

			ImplementMethodInfoArray(typeBuilder);
			ImplementInterceptorField(typeBuilder);
		}
	}
}