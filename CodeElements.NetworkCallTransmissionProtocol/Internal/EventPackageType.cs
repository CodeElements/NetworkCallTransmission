namespace CodeElements.NetworkCallTransmissionProtocol.Internal
{
    public enum EventPackageType
    {
        SubscribeEvent,
        UnsubscribeEvent,
        Clear
    }

    public enum EventResponseType
    {
        TriggerEvent,
        TriggerEventWithParameter
    }
}