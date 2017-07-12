using System;

namespace CodeElements.NetworkCallTransmissionProtocol
{
    public interface IEventProvider : IDisposable
    {
        void SuspendSubscribing();
        void ResumeSubscribing();
    }
}