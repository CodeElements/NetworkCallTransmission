using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CodeElements.NetworkCallTransmissionProtocol.Extensions;
using CodeElements.NetworkCallTransmissionProtocol.NetSerializer;

namespace CodeElements.NetworkCallTransmissionProtocol
{
    /// <summary>
    /// Provides information about an interface. Thread-safe and can (should!) be reused.
    /// </summary>
    public class ExecuterInterfaceCache
    {
        private ExecuterInterfaceCache(IReadOnlyDictionary<string, MethodInvoker> methodInvokers)
        {
            MethodInvokers = methodInvokers;
        }

        internal IReadOnlyDictionary<string, MethodInvoker> MethodInvokers { get; }

        public static ExecuterInterfaceCache Build<TInterface>()
        {
            var interfaceType = typeof(TInterface);
            var members = interfaceType.GetMembers();
            if (members.Any(x => x.MemberType != MemberTypes.Method))
                throw new ArgumentException("The interface must only provide methods.", nameof(TInterface));

            var methods = members.Cast<MethodInfo>().ToList();
            if (methods.Count == 0)
                throw new ArgumentException("The interface must at least provide one method.", nameof(TInterface));

            var serializerDictionary = new Dictionary<Type[], Serializer>(new TypeArrayEqualityComparer());
            var methodInvokers = new Dictionary<string, MethodInvoker>();
            var md5 = MD5.Create();

            foreach (var methodInfo in methods)
            {
                Type actualReturnType;
                if (methodInfo.ReturnType == typeof(Task))
                    actualReturnType = null;
                else if (methodInfo.ReturnType.IsGenericType &&
                         methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    actualReturnType = methodInfo.ReturnType.GetGenericArguments()[0];
                else
                    throw new ArgumentException("Only tasks are supported as return type.", methodInfo.ToString());

                Serializer returnSerializer;
                if (actualReturnType != null)
                {
                    var returnTypesAttribute = methodInfo.GetCustomAttribute<AdditionalTypesAttribute>();
                    var types = new Type[1 + (returnTypesAttribute?.Types.Length ?? 0)];
                    types[0] = actualReturnType;

                    if (returnTypesAttribute?.Types.Length > 0)
                        Array.Copy(returnTypesAttribute.Types, 0, types, 1, returnTypesAttribute.Types.Length);

                    if (!serializerDictionary.TryGetValue(types, out returnSerializer))
                        serializerDictionary.Add(types, returnSerializer = new Serializer(types));
                }
                else
                    returnSerializer = null;

                var parameters = methodInfo.GetParameters();
                var parameterSerializers = new Serializer[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var additionalTypes = parameter.GetCustomAttribute<AdditionalTypesAttribute>();

                    var types = new Type[1 + (additionalTypes?.Types.Length ?? 0)];
                    types[0] = parameter.ParameterType;

                    if (additionalTypes?.Types.Length > 0)
                        Array.Copy(additionalTypes.Types, 0, types, 1, additionalTypes.Types.Length);

                    if (!serializerDictionary.TryGetValue(types, out var parameterSerializer))
                        serializerDictionary.Add(types, parameterSerializer = new Serializer(types));

                    parameterSerializers[i] = parameterSerializer;
                }

                methodInvokers.Add(Encoding.ASCII.GetString(methodInfo.GetMethodId(md5)),
                    new MethodInvoker(methodInfo, parameterSerializers, returnSerializer, parameters.Length));
            }

            return new ExecuterInterfaceCache(methodInvokers);
        }
    }
}