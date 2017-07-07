using System;

namespace CodeElements.NetworkCallTransmissionProtocol.Internal
{
    internal class MethodCache
    {
        public MethodCache(byte[] methodId, Type returnType, Type[] parameterTypes)
        {
            MethodId = methodId;
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
        }

        public byte[] MethodId { get; }
        public Type ReturnType { get; }
        public Type[] ParameterTypes { get; }
    }
}