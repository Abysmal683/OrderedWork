using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Core;

namespace OrderedWork.Tests
{
    internal static class Program
    {
        private static int failures;

        private static int Main()
        {
            Run("NormalizeOrder appends all when order is empty", TestEmptyOrderAppendsAll);
            Run("NormalizeOrder keeps custom order first", TestCustomOrderWins);
            Run("NormalizeOrder drops unknown ids and duplicates", TestUnknownAndDuplicates);
            Run("BuildRanks assigns dense 1..N without gaps", TestDenseRanks);
            Run("BuildRanks appends missing available ids", TestMissingAppended);
            Run("BuildRanks handles empty available", TestEmptyAvailable);
            Run("NormalizeOrder tolerates null inputs", TestNullInputs);

            Console.WriteLine(failures == 0 ? "All tests passed." : failures + " test(s) failed.");
            return failures == 0 ? 0 : 1;
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                Console.WriteLine("PASS " + name);
            }
            catch (Exception ex)
            {
                failures++;
                Console.WriteLine("FAIL " + name + ": " + ex.Message);
            }
        }

        private static void TestEmptyOrderAppendsAll()
        {
            List<string> result = DenseRank.NormalizeOrder(Array.Empty<string>(), new[] { "Doctor", "Cook", "Haul" });
            AssertSequence(new[] { "Doctor", "Cook", "Haul" }, result);
        }

        private static void TestCustomOrderWins()
        {
            List<string> result = DenseRank.NormalizeOrder(new[] { "Haul", "Doctor" }, new[] { "Doctor", "Cook", "Haul" });
            AssertSequence(new[] { "Haul", "Doctor", "Cook" }, result);
        }

        private static void TestUnknownAndDuplicates()
        {
            List<string> result = DenseRank.NormalizeOrder(
                new[] { "Doctor", "Missing", "Doctor", "Cook" },
                new[] { "Doctor", "Cook" });
            AssertSequence(new[] { "Doctor", "Cook" }, result);
        }

        private static void TestDenseRanks()
        {
            Dictionary<string, int> ranks = DenseRank.BuildRanks(
                new[] { "Cook", "Doctor" },
                new[] { "Doctor", "Cook", "Haul" });
            AssertEqual(1, ranks["Cook"]);
            AssertEqual(2, ranks["Doctor"]);
            AssertEqual(3, ranks["Haul"]);
            AssertEqual(3, ranks.Count);
        }

        private static void TestMissingAppended()
        {
            Dictionary<string, int> ranks = DenseRank.BuildRanks(
                new[] { "Doctor" },
                new[] { "Doctor", "Cook", "Haul" });
            AssertEqual(1, ranks["Doctor"]);
            AssertEqual(2, ranks["Cook"]);
            AssertEqual(3, ranks["Haul"]);
        }

        private static void TestEmptyAvailable()
        {
            Dictionary<string, int> ranks = DenseRank.BuildRanks(new[] { "Doctor" }, Array.Empty<string>());
            AssertEqual(0, ranks.Count);
        }

        private static void TestNullInputs()
        {
            List<string> result = DenseRank.NormalizeOrder(null, null);
            AssertEqual(0, result.Count);
        }

        private static void AssertSequence(IEnumerable<string> expected, IEnumerable<string> actual)
        {
            string[] expectedArray = expected.ToArray();
            string[] actualArray = actual.ToArray();
            if (!expectedArray.SequenceEqual(actualArray, StringComparer.Ordinal))
            {
                throw new Exception("Expected [" + string.Join(", ", expectedArray) + "] but got [" + string.Join(", ", actualArray) + "].");
            }
        }

        private static void AssertEqual(int expected, int actual)
        {
            if (expected != actual)
            {
                throw new Exception("Expected " + expected + " but got " + actual + ".");
            }
        }
    }
}
