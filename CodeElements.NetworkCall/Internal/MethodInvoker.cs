using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace CodeElements.NetworkCall.Internal
{
    internal class MethodInvoker
    {
        private delegate Task ReturnValueDelegate(object instance, object[] arguments);

        private readonly ReturnValueDelegate _delegate;

        public MethodInvoker(MethodInfo methodInfo, Type[] parameterTypes, Type returnType)
        {
            ParameterTypes = parameterTypes;
            ReturnsResult = returnType != null;
            ReturnType = returnType;
            _delegate = BuildDelegate(methodInfo);

            if (ReturnsResult)
                TaskReturnPropertyInfo = methodInfo.ReturnType.GetProperty("Result");
        }

        public Type ReturnType { get; }
        public int ParameterCount => ParameterTypes.Length;
        public Type[] ParameterTypes { get; }
        public bool ReturnsResult { get; }
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
                argumentExpressions.Add(Expression.Convert(
                    Expression.ArrayIndex(argumentsExpression, Expression.Constant(i)), parameterInfo.ParameterType));
            }

            var callExpression = Expression.Call(Expression.Convert(instanceExpression, methodInfo.DeclaringType),
                methodInfo, argumentExpressions);
            return Expression.Lambda<ReturnValueDelegate>(Expression.Convert(callExpression, typeof(Task)),
                instanceExpression, argumentsExpression).Compile();
        }
    }
}