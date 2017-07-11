using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CodeElements.NetworkCallTransmissionProtocol.Test.Extensions
{
    public class MurmurHashTests
    {
        [Fact]
        public void TestUniqueness()
        {
            var testSeeds = new[] {"test", "MurmurHashTests.TestUniquenessstringbooleaninteger", "katze"};
            var variantes = new List<string>();

            foreach (var testSeed in testSeeds)
                GetVariants(testSeed, variantes);

            var distinctCount = variantes.Distinct().Count();
            Assert.Equal(variantes.Count, distinctCount);
        }

        private void GetVariants(string seed, List<string> uniqueStrings)
        {
            uniqueStrings.Add(seed);

            for (int i = 0; i < seed.Length; i++)
            {
                var c = seed[i];
                var before = seed.Substring(0, i);
                var after = i == seed.Length - 1 ? "" : seed.Substring(i + 1, seed.Length - 1 - i);

                for (int j = 0; j < 25; j++)
                {
                    c = RotateChar(c);
                    uniqueStrings.Add(before + c + after);
                }
            }
        }

        private static char RotateChar(char c)
        {
            if (c == 'z')
                return 'a';
            if (c == 'Z')
                return 'A';

            return (char) (c + 1);
        }
    }
}