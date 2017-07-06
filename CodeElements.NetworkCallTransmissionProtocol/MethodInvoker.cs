using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using CodeElements.NetworkCallTransmissionProtocol.NetSerializer;

namespace CodeElements.NetworkCallTransmissionProtocol
{
    internal class MethodInvoker
    {
        private delegate Task ReturnValueDelegate(object instance, object[] arguments);

        private readonly ReturnValueDelegate _delegate;

        public MethodInvoker(MethodInfo methodInfo, Serializer[] parameterSerializers, Serializer returnSerializer, int parametersCount)
        {
            ParameterSerializers = parameterSerializers;
            ReturnSerializer = returnSerializer;
            ParametersCount = parametersCount;
            _delegate = BuildDelegate(methodInfo);

            if (returnSerializer != null)
                TaskReturnPropertyInfo = methodInfo.ReturnType.GetProperty("Result");
        }

        public Serializer ReturnSerializer { get; }
        public Serializer[] ParameterSerializers { get; }
        public int ParametersCount { get; }
        public PropertyInfo TaskReturnPropertyInfo { get; }

        public Task Invoke(object instance, object[] arguments)
        {
            return _delegate(instance, arguments);
        }

        private static ReturnValueDelegate BuildDelegate(MethodInfo methodInfo)
        {
            var instanceExpression = Expression.Parameter(typeof(object), "instance");
            var argumentsExpression = Expression.Parameter(typeof(object[]), "arguments");
            var argumentExpressions = new List<Expression>();
            var parameterInfos = methodInfo.GetParameters();

            for (var i = 0; i < parameterInfos.Length; ++i)
            {
                var parameterInfo = parameterInfos[i];
                argumentExpressions.Add(Expression.Convert(Expression.ArrayIndex(argumentsExpression, Expression.Constant(i)), parameterInfo.ParameterType));
            }

            var callExpression = Expression.Call(Expression.Convert(instanceExpression, methodInfo.ReflectedType), methodInfo, argumentExpressions);
            return
                Expression.Lambda<ReturnValueDelegate>(Expression.Convert(callExpression, typeof(Task)),
                    instanceExpression, argumentsExpression).Compile();
        }
    }
}