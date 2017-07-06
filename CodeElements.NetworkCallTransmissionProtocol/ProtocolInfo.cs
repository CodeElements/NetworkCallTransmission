using System;
using System.IO;
using System.Net;

namespace CodeElements.NetworkCallTransmissionProtocol
{
    internal static class ProtocolInfo
    {
        public const byte Header1 = 0x4D; //Letter N (Network)
        public const byte Header2 = 0x54; //Letter T (Transmission)
        public const byte Header3Call = 0x43; //Letter C (Call)
        public const byte Header3Return = 0x52; //Letter R (Return)
        public const byte Header4 = 1; //Version

        public static readonly Type[] SupportedExceptionTypes =
        {
            typeof(SystemException),
            typeof(UnauthorizedAccessException), typeof(ArgumentException), typeof(ArgumentNullException),
            typeof(ArgumentOutOfRangeException), typeof(ArithmeticException), typeof(BadImageFormatException),
            typeof(FormatException), typeof(InvalidCastException), typeof(InvalidOperationException),
            typeof(NotSupportedException), typeof(NullReferenceException), typeof(IOException), typeof(WebException),
            typeof(RemoteException)
        };
    }
}