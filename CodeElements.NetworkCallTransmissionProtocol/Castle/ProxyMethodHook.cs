using System;
using System.Collections.Generic;
using System.Reflection;
using Castle.DynamicProxy;

namespace CodeElements.NetworkCallTransmissionProtocol.Castle
{
    /// <summary>
    ///     Hook used to tells Castle which methods to proxy in mocked classes.
    ///     Here we proxy the default methods Castle suggests (everything Object's methods)
    ///     plus Object.ToString(), so we can give mocks useful default names.
    ///     This is required to allow Moq to mock ToString on proxy *class* implementations.
    /// </summary>
    internal class ProxyMethodHook : AllMethodsHook
    {
        protected static readonly HashSet<Tuple<Type, string>> GrantedMethods = new HashSet<Tuple<Type, string>>
        {
            Tuple.Create(typeof(object), "ToString"),
            Tuple.Create(typeof(object), "Equals"),
            Tuple.Create(typeof(object), "GetHashCode")
        };

        /// <summary>
        ///     Extends AllMethodsHook.ShouldInterceptMethod to also intercept Object.ToString().
        /// </summary>
        public override bool ShouldInterceptMethod(Type type, MethodInfo methodInfo)
        {
            var isGranted = GrantedMethods.Contains(Tuple.Create(methodInfo.DeclaringType, methodInfo.Name));
            return base.ShouldInterceptMethod(type, methodInfo) || isGranted;
        }
    }
}