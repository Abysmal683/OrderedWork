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
            Run("RecommendationScorePolicy handles no relevant skill", TestScoreNoSkill);
            Run("RecommendationScorePolicy handles None", TestScoreNone);
            Run("RecommendationScorePolicy applies Minor weights", TestScoreMinor);
            Run("RecommendationScorePolicy applies Major weights", TestScoreMajor);
            Run("RecommendationScorePolicy respects min level", TestScoreMinLevel);
            Run("RecommendationScorePolicy supports negative weights", TestScoreNegative);
            Run("RecommendationScorePolicy disables level-zero passion", TestScoreLevelZeroDisabled);
            Run("RecommendationScorePolicy lets a bad slow passion grow", TestScoreBadSlowPassion);

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

        private static void TestScoreNoSkill()
        {
            AssertEqual(0, RecommendationScorePolicy.Score(-1, false, -3, 0, 0, true));
        }

        private static void TestScoreNone()
        {
            AssertEqual(-3, RecommendationScorePolicy.Score(0, false, -3, 0, 0, true));
            AssertEqual(6, RecommendationScorePolicy.Score(3, false, -3, 0, 0, true));
        }

        private static void TestScoreMinor()
        {
            AssertEqual(1, RecommendationScorePolicy.Score(0, true, 1, 2, 0, true));
            AssertEqual(4, RecommendationScorePolicy.Score(1, true, 1, 2, 0, true));
            AssertEqual(6, RecommendationScorePolicy.Score(2, true, 1, 2, 0, true));
        }

        private static void TestScoreMajor()
        {
            AssertEqual(3, RecommendationScorePolicy.Score(0, true, 3, 4, 0, true));
            AssertEqual(10, RecommendationScorePolicy.Score(3, true, 3, 4, 0, true));
        }

        private static void TestScoreMinLevel()
        {
            AssertEqual(-3, RecommendationScorePolicy.Score(0, true, 5, 6, 5, true));
            AssertEqual(8, RecommendationScorePolicy.Score(4, true, 5, 6, 5, true));
            AssertEqual(16, RecommendationScorePolicy.Score(5, true, 5, 6, 5, true));
        }

        private static void TestScoreNegative()
        {
            AssertEqual(-5, RecommendationScorePolicy.Score(0, true, -5, -2, 0, true));
            AssertEqual(0, RecommendationScorePolicy.Score(1, true, -5, -2, 0, true));
        }

        private static void TestScoreLevelZeroDisabled()
        {
            AssertEqual(-3, RecommendationScorePolicy.Score(0, true, 3, 4, 0, false));
            AssertEqual(10, RecommendationScorePolicy.Score(3, true, 3, 4, 0, false));
        }

        private static void TestScoreBadSlowPassion()
        {
            AssertEqual(-4, RecommendationScorePolicy.Score(0, true, -4, 1, 0, true));
            AssertEqual(21, RecommendationScorePolicy.Score(10, true, -4, 1, 0, true));
            AssertEqual(20, RecommendationScorePolicy.Score(10, false, -3, 0, 0, true));
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
