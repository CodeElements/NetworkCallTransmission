using System;

namespace CodeElements.NetworkCallTransmissionProtocol
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method)]
    public class AdditionalTypesAttribute : Attribute
    {
        public AdditionalTypesAttribute(params Type[] types)
        {
            Types = types;
        }

        public Type[] Types { get; }
    }
}