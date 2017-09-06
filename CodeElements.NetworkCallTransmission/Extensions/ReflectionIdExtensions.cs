using System;
using System.Linq;
using System.Reflection;

namespace CodeElements.NetworkCallTransmission.Extensions
{
    internal static class ReflectionIdExtensions
    {
        public static uint GetMethodId(this MethodInfo methodInfo)
        {
            var stringValue = methodInfo.Name + GetInvariantFullName(methodInfo.ReturnParameter?.ParameterType) +
                              string.Join("",
                                  methodInfo.GetParameters()
                                      .Select(x => x.Position.ToString() + GetInvariantFullName(x.ParameterType)));
            return MurmurHash.Hash(stringValue);
        }

        public static ulong GetEventId(this EventInfo eventInfo, Type interfaceType, uint sessionId)
        {
            return (MurmurHash.Hash(eventInfo.Name + eventInfo.EventHandlerType.FullName + interfaceType.Name) << 32) |
                   sessionId;
        }

        private static string GetInvariantFullName(Type type)
        {
            if (type == null)
                return null;
            return
                type.Name + type
                    .Namespace; //no assembly because .net Core and .Net Framework have different assembly names (e. g. mscorlib vs CoreLib)
        }
    }
}