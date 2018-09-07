using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CodeElements.NetworkCall.Extensions
{
    internal static class ReflectionIdExtensions
    {
        public static uint GetMethodId(this MethodInfo methodInfo)
        {
            var stringValue = methodInfo.Name + GetInvariantFullName(methodInfo.ReturnParameter?.ParameterType) +
                              string.Join("",
                                  methodInfo.GetParameters().Select(x =>
                                      x.Position.ToString() + GetInvariantFullName(x.ParameterType)));
            return MurmurHash.Hash(stringValue);
        }

        public static uint GetEventId(this EventInfo eventInfo)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(eventInfo.Name);
            stringBuilder.Append(eventInfo.EventHandlerType.Name);
            stringBuilder.Append(eventInfo.EventHandlerType.Namespace);

            foreach (var genericTypeArgument in eventInfo.EventHandlerType.GenericTypeArguments)
                stringBuilder.Append(genericTypeArgument);
            return MurmurHash.Hash(stringBuilder.ToString());
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