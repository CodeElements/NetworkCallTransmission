namespace CodeElements.NetworkCallTransmissionProtocol
{
    public class ResponseData
    {
        public ResponseData(byte[] data, int length)
        {
            Data = data;
            Length = length;
        }

        public ResponseData(byte[] data) : this(data, data.Length)
        {
        }

        public byte[] Data { get; }
        public int Length { get; }
    }
}