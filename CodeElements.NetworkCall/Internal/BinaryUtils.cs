using System.Runtime.CompilerServices;

namespace CodeElements.NetworkCall.Internal
{
    /// <summary>
    ///     Binary utilities for writing data to byte arrays
    /// </summary>
    public static class BinaryUtils
    {
        /// <summary>
        ///     Write the <see cref="int" /> value to a byte array
        /// </summary>
        /// <param name="bytes">The byte array</param>
        /// <param name="offset">The offset in the <see cref="bytes" /> where the value should be written</param>
        /// <param name="value">The <see cref="int" /> that should be encoded</param>
        /// <returns>Return the amount of bytes written to the <see cref="bytes" /></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteInt32(byte[] bytes, int offset, int value)
        {
            fixed (byte* ptr = bytes)
            {
                *(int*) (ptr + offset) = value;
            }

            return 4;
        }

        /// <summary>
        ///     Write the <see cref="uint" /> value to a byte array
        /// </summary>
        /// <param name="bytes">The byte array</param>
        /// <param name="offset">The offset in the <see cref="bytes" /> where the value should be written</param>
        /// <param name="value">The <see cref="uint" /> that should be encoded</param>
        /// <returns>Return the amount of bytes written to the <see cref="bytes" /></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteUInt32(byte[] bytes, int offset, uint value)
        {
            fixed (byte* ptr = bytes)
            {
                *(uint*) (ptr + offset) = value;
            }

            return 4;
        }

        /// <summary>
        ///     Write the <see cref="ushort" /> value to a byte array
        /// </summary>
        /// <param name="bytes">The byte array</param>
        /// <param name="offset">The offset in the <see cref="bytes" /> where the value should be written</param>
        /// <param name="value">The <see cref="ushort" /> that should be encoded</param>
        /// <returns>Return the amount of bytes written to the <see cref="bytes" /></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int WriteUInt16(byte[] bytes, int offset, ushort value)
        {
            fixed (byte* ptr = bytes)
            {
                *(ushort*) (ptr + offset) = value;
            }

            return 2;
        }
    }
}