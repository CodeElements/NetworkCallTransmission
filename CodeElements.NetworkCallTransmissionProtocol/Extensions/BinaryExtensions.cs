using System;
using System.IO;

namespace CodeElements.NetworkCallTransmissionProtocol.Extensions
{
    /// <summary>
    ///     Provide extensions for <see cref="BinaryReader" /> and <see cref="BinaryWriter" />
    /// </summary>
    internal static class BinaryExtensions
    {
        private const byte ValueRange = 0b0111_1111;
        private const byte Msb = 0b1000_0000; //most significant bit

        /// <summary>
        ///     Get the length of the integer the value will produce in bytes
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Return the length in bytes of the data</returns>
        public static int CalculateIntegerLength(uint value)
        {
            //if the value is zero, we still need one byte to represent the zero value
            if (value == 0)
                return 1;

            var counter = 0;
            while (value != 0)
            {
                value = value >> 1;
                counter++;
            }

            return (int) Math.Ceiling(counter / 7f);
        }

        /// <summary>
        ///     Write an integer value using a portable, variable-sized format
        /// </summary>
        /// <param name="binaryWriter">The binary writer</param>
        /// <param name="value">The value which should be encoded</param>
        public static void WriteBigEndian7BitEncodedInt(this BinaryWriter binaryWriter, uint value)
        {
            if (value == 0u)
            {
                binaryWriter.Write((byte) 0x0);
                return;
            }

            //the bytes must be written in reverse order, the LSB (least significant byte) must be the first one
            var tempBuffer = new byte[5];
            var index = 4;

            while (value != 0)
            {
                var b = (byte) (value & ValueRange);
                value = value >> 7;

                //only the last byte (with index = 4) must have the MSB turned off (the bit indicates that it was the last byte which belongs to the number)
                if (index != 4)
                    b |= Msb;

                tempBuffer[index--] = b;
            }

            binaryWriter.Write(tempBuffer, index + 1, 4 - index);
        }

        /// <summary>
        ///     Read an integer value using a portable, variable-sized format
        /// </summary>
        /// <param name="binaryReader">The binary reader</param>
        /// <returns>The decoded integer value</returns>
        public static uint ReadBigEndian7BitEncodedInt(this BinaryReader binaryReader)
        {
            var result = 0u;

            //we iterate 5 times because the integer could be 5 bytes (because we are only using )
            for (int i = 0; i < 5; i++)
            {
                var b = binaryReader.ReadByte();
                result = (result << 7) | (uint) (b & ValueRange); //we append the first 7 bits to the result

                if ((b & Msb) == 0) //when the 8th bit is turned off, we are done
                    return result;
            }

            // Still haven't seen a byte with the high bit unset? Dodgy data.
            throw new IOException("Invalid 7-bit encoded integer in stream.");
        }

        /// <summary>
        /// Read bytes from a stream and gurantee that the bytes were all read
        /// </summary>
        /// <param name="stream">The source stream</param>
        /// <param name="length">The amount of bytes to read</param>
        /// <returns>Returns the array of bytes which was read</returns>
        public static byte[] CheckedReadBytes(this Stream stream, int length)
        {
            byte[] ret = new byte[length];
            int index = 0;
            while (index < length)
            {
                int read = stream.Read(ret, index, length - index);
                if (read == 0)
                {
                    throw new EndOfStreamException
                    ($"End of stream reached with {length - index} byte{(length - index == 1 ? "s" : "")} left to read.");
                }
                index += read;
            }
            return ret;
        }
    }
}