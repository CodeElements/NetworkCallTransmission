namespace CodeElements.NetworkCallTransmissionProtocol.Internal
{
    internal static class ProtocolInfo
    {
        public const byte Header1 = 0x4D; //Letter N (Network)
        public const byte Header2 = 0x54; //Letter T (Transmission)
        public const byte Header3Call = 0x43; //Letter C (Call)
        public const byte Header3Return = 0x52; //Letter R (Return)
        public const byte Header4 = 1; //Version
    }
}