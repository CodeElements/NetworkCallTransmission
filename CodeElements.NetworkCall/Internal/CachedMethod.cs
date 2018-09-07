using System;

namespace CodeElements.NetworkCall.Internal
{
    internal class CachedMethod
    {
        public CachedMethod(uint methodId, Type returnType, Type[] parameterTypes)
        {
            MethodId = methodId;
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
        }

        public uint MethodId { get; }
        public Type ReturnType { get; }
        public Type[] ParameterTypes { get; }
    }
}