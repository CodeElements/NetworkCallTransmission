using System;

namespace CodeElements.NetworkCallTransmissionProtocol
{
    public interface IEventProvider<out TEvents> : IDisposable
    {
        TEvents Events { get; }

        void SuspendSubscribing();
        void ResumeSubscribing();
    }
}