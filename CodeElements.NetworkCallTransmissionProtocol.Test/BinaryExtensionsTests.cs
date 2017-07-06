using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeElements.NetworkCallTransmissionProtocol.Extensions;
using Xunit;

namespace CodeElements.NetworkCallTransmissionProtocol.Test
{
    public class BinaryExtensionsTests
    {
        private readonly byte[] _testData = {0b10111010, 0b11101111, 0b10011010, 0b00010101};
        private readonly uint _expectedTestResult = 123456789u;

        [Fact]
        public void TestWriteBigEndian7BitEncodedInt()
        {
            using (var memoryStream = new MemoryStream())
            using (var binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.WriteBigEndian7BitEncodedInt(_expectedTestResult);

                var buffer = memoryStream.ToArray();
                Assert.Equal(buffer.Length, 4);
                Assert.True(buffer.SequenceEqual(_testData));

                memoryStream.SetLength(0);

                binaryWriter.WriteBigEndian7BitEncodedInt(127);

                buffer = memoryStream.ToArray();
                Assert.Equal(buffer.Length, 1);
                Assert.Equal(buffer[0], 127);

                memoryStream.SetLength(0);

                binaryWriter.WriteBigEndian7BitEncodedInt(128);

                buffer = memoryStream.ToArray();
                Assert.Equal(buffer.Length, 2);
            }
        }

        [Fact]
        public void TestReadBigEndian7BitEncodedInt()
        {
            using (var memoryStream = new MemoryStream())
            using (var binaryReader = new BinaryReader(memoryStream))
            {
                memoryStream.Write(_testData, 0, _testData.Length);
                memoryStream.Position = 0;

                var result = binaryReader.ReadBigEndian7BitEncodedInt();
                Assert.Equal(result, _expectedTestResult);
            }
        }

        [Fact]
        public void TestReadWriteBigEndian7BitEncodedInt()
        {
            using (var memoryStream = new MemoryStream())
            using (var binaryReader = new BinaryReader(memoryStream))
            using (var binaryWriter = new BinaryWriter(memoryStream))
            {
                var testValues = new List<uint> {uint.MaxValue, uint.MinValue};
                for (int i = 0; i < 10; i++)
                {
                    testValues.Add((uint) (StaticRandom.NextDouble() * uint.MaxValue));
                }

                foreach (var testValue in testValues)
                {
                    binaryWriter.WriteBigEndian7BitEncodedInt(testValue);
                    memoryStream.Position = 0;
                    var number = binaryReader.ReadBigEndian7BitEncodedInt();
                    Assert.Equal(number, testValue);

                    memoryStream.SetLength(0);
                }
            }
        }

        [Fact]
        public void TestCalculateIntegerLength()
        {
            var testValues = new List<uint>
            {
                127,
                128,
                uint.MaxValue,
                uint.MinValue,
                (uint) Math.Pow(127, 2),
                (uint) Math.Pow(127, 2) - 1,
                (uint) Math.Pow(127, 2) + 1,
                (uint) Math.Pow(127, 3),
                (uint) Math.Pow(127, 2) + 1,
                (uint) Math.Pow(127, 2) - 1
            };

            for (int i = 0; i < 10; i++)
            {
                testValues.Add((uint) (StaticRandom.NextDouble() * uint.MaxValue));
            }

            using (var memoryStream = new MemoryStream())
                using(var writer = new BinaryWriter(memoryStream))
            {
                foreach (var testValue in testValues)
                {
                    writer.WriteBigEndian7BitEncodedInt(testValue);
                    Assert.Equal((int) memoryStream.Length, BinaryExtensions.CalculateIntegerLength(testValue));
                    memoryStream.SetLength(0);
                }
            }
        }
    }
}