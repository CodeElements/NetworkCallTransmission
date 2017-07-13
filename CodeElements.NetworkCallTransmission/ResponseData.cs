namespace CodeElements.NetworkCallTransmission
{
    /// <summary>
    ///     Contains information which must be sent to the counterpart for the services to work
    /// </summary>
    public class ResponseData
    {
        internal ResponseData(byte[] data, int length)
        {
            Data = data;
            Length = length;
        }

        internal ResponseData(byte[] data) : this(data, data.Length)
        {
        }

        /// <summary>
        ///     The data which should be sent
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        ///     The length of the data that should be sent. Please note that the length of <see cref="Data" /> may be greater due
        ///     to smaller data than expected
        /// </summary>
        public int Length { get; }
    }
}