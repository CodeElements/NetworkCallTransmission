using System;
using System.Linq;
using System.Reflection;

namespace CodeElements.NetworkCallTransmission.Extensions
{
    internal static class ReflectionIdExtensions
    {
        public static uint GetMethodId(this MethodInfo methodInfo)
        {
            return MurmurHash.Hash(methodInfo.Name + methodInfo.ReturnParameter?.ParameterType.FullName +
                                   string.Join("",
                                       methodInfo.GetParameters()
                                           .Select(x => x.Position.ToString() + x.ParameterType.FullName)));
        }

        public static ulong GetEventId(this EventInfo eventInfo, Type interfaceType, uint sessionId)
        {
            return MurmurHash.Hash(eventInfo.Name + eventInfo.EventHandlerType.FullName + interfaceType.Name) << 32 | sessionId;
        }
    }
}