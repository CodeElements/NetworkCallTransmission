using System;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeElements.NetworkCallTransmission.Proxy
{
    internal static class ImplementorHelper
    {
        public static FieldBuilder ImplementProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType,
            Type overwrittenInterface)
        {
            var field = typeBuilder.DefineField($"__{char.ToLower(propertyName[0]) + propertyName.Substring(1)}",
                propertyType, FieldAttributes.Private);

            // Implement the getter
            var attributes = MethodAttributes.Public | MethodAttributes.HideBySig |
                             MethodAttributes.SpecialName | MethodAttributes.NewSlot |
                             MethodAttributes.Virtual;

            // Implement the getter
            var getterMethod = typeBuilder.DefineMethod($"get_{propertyName}", attributes,
                CallingConventions.HasThis, propertyType,
                new Type[0]);
            getterMethod.SetImplementationFlags(MethodImplAttributes.Managed | MethodImplAttributes.IL);

            var il = getterMethod.GetILGenerator();

            // This is equivalent to:
            // get { return __field;
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);

            // Implement the setter
            var setterMethod = typeBuilder.DefineMethod($"set_{propertyName}", attributes,
                CallingConventions.HasThis, typeof(void),
                new[] {propertyType});

            setterMethod.SetImplementationFlags(MethodImplAttributes.Managed | MethodImplAttributes.IL);
            il = setterMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);

            var overwrittenInteraceTypeInfo = overwrittenInterface.GetTypeInfo();
            var originalSetter = overwrittenInteraceTypeInfo.GetMethod($"set_{propertyName}");
            var originalGetter = overwrittenInteraceTypeInfo.GetMethod($"get_{propertyName}");

            typeBuilder.DefineMethodOverride(setterMethod, originalSetter);
            typeBuilder.DefineMethodOverride(getterMethod, originalGetter);

            return field;
        }
    }
}