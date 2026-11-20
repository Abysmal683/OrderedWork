using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OrderedWork.Core;
using OrderedWork.Config;

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
            Run("HourFocusStack prepends focus without replacing base", TestFocusPrepends);
            Run("HourFocusStack falls back to next available focus", TestFocusFallback);
            Run("HourFocusStack keeps base when all focus is unavailable", TestFocusAllUnavailable);
            Run("HourFocusStack ignores focus outside base order", TestFocusOutsideBase);
            Run("HourFocusStack normalizes max three and deduplicates", TestFocusNormalize);
            Run("HourFocusStack leaves base order unchanged", TestFocusBaseUnchanged);
            Run("AtomicJsonRecordStore roundtrips and backs up", TestAtomicStoreRoundtrip);
            Run("AtomicJsonRecordStore rejects unsupported schema", TestAtomicStoreSchema);
            Run("Profile normalize drops null, empty and duplicate givers", TestProfileNormalize);
            Run("Profile clone rebuilds an independent reference copy", TestProfileClone);
            Run("Profile create split moves selected givers", TestProfileCreateSplit);
            Run("Profile reconcile restores merge ownership", TestProfileRevertMerge);
            Run("Profile reconcile restores order sequence", TestProfileRevertOrder);

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

        private static void TestProfileNormalize()
        {
            WorkConfigProfile profile = new WorkConfigProfile();
            profile.WorkTypes["A"] = new WorkTypeConfig
            {
                Givers = new List<GiverConfig>
                {
                    null,
                    new GiverConfig { DefName = "G", Order = 2 },
                    new GiverConfig { DefName = "G", Order = 0 },
                    new GiverConfig { DefName = "" }
                }
            };
            WorkConfigProfileService.Normalize(profile);
            AssertEqual(1, profile.WorkTypes["A"].Givers.Count);
            Assert(profile.WorkTypes["A"].Givers[0].DefName == "G", "normalized giver name");
        }

        private static void TestProfileClone()
        {
            WorkConfigProfile source = new WorkConfigProfile
            {
                Profile = "reference",
                WorkTypes =
                {
                    ["A"] = new WorkTypeConfig
                    {
                        Label = "A",
                        Givers = new List<GiverConfig> { new GiverConfig { DefName = "G", Order = 0 } }
                    }
                }
            };
            WorkConfigProfile clone = WorkConfigProfileService.CloneForProfile(source, "active");
            Assert(clone.Profile == "active", "clone profile name");
            Assert(clone.WorkTypes["A"].Givers[0].DefName == "G", "clone giver");
            clone.WorkTypes["A"].Givers[0].DefName = "Changed";
            Assert(source.WorkTypes["A"].Givers[0].DefName == "G", "clone is independent");
        }
        private static void TestProfileCreateSplit()
        {
            WorkConfigProfile profile = new WorkConfigProfile();
            profile.WorkTypes["Origin"] = new WorkTypeConfig
            {
                Givers = new List<GiverConfig>
                {
                    new GiverConfig { DefName = "G", Order = 0 },
                    new GiverConfig { DefName = "H", Order = 1 }
                }
            };
            WorkTypeConfig split;
            Assert(WorkConfigReconciler.CreateSplit(profile, "Origin", "OrderedWork_New", new[] { "G" },
                "New", "new", "", "test", out split), "split reports mutation");
            Assert(split != null && split.Base == "Origin", "split base");
            AssertSequence(new[] { "G" }, split.Givers.Select(g => g.DefName).ToList());
            AssertSequence(new[] { "H" }, profile.WorkTypes["Origin"].Givers.Select(g => g.DefName).ToList());
        }
        private static void TestProfileRevertMerge()
        {
            WorkConfigProfile reference = new WorkConfigProfile();
            reference.WorkTypes["Target"] = new WorkTypeConfig { Givers = new List<GiverConfig> { new GiverConfig { DefName = "G", Order = 0 } } };
            reference.WorkTypes["Origin"] = new WorkTypeConfig { Givers = new List<GiverConfig> { new GiverConfig { DefName = "H", Order = 0 } } };

            WorkConfigProfile profile = new WorkConfigProfile();
            profile.WorkTypes["Target"] = new WorkTypeConfig
            {
                Givers = new List<GiverConfig>
                {
                    new GiverConfig { DefName = "G", Order = 0 },
                    new GiverConfig { DefName = "H", Order = 1 }
                }
            };
            profile.WorkTypes["Origin"] = new WorkTypeConfig { Givers = new List<GiverConfig>() };

            Assert(WorkConfigReconciler.RevertMerge(profile, reference, "Target"), "merge revert reports mutation");
            AssertSequence(new[] { "G" }, profile.WorkTypes["Target"].Givers.Select(g => g.DefName).ToList());
            AssertSequence(new[] { "H" }, profile.WorkTypes["Origin"].Givers.Select(g => g.DefName).ToList());
        }

        private static void TestProfileRevertOrder()
        {
            WorkConfigProfile reference = new WorkConfigProfile();
            reference.WorkTypes["A"] = new WorkTypeConfig
            {
                Givers = new List<GiverConfig>
                {
                    new GiverConfig { DefName = "Second", Order = 0 },
                    new GiverConfig { DefName = "First", Order = 1 }
                }
            };
            WorkConfigProfile profile = new WorkConfigProfile();
            profile.WorkTypes["A"] = new WorkTypeConfig
            {
                Givers = new List<GiverConfig>
                {
                    new GiverConfig { DefName = "First", Order = 0 },
                    new GiverConfig { DefName = "Second", Order = 1 }
                }
            };

            Assert(WorkConfigReconciler.RevertOrder(profile, reference, "A"), "order revert reports mutation");
            AssertSequence(new[] { "Second", "First" }, profile.WorkTypes["A"].Givers.Select(g => g.DefName).ToList());
        }        private sealed class AtomicTestRecord
        {
            public int SchemaVersion { get; set; } = 1;
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
        }

        private static void TestAtomicStoreRoundtrip()
        {
            string root = Path.Combine(Path.GetTempPath(), "orderedwork-store-" + Guid.NewGuid().ToString("N"));
            try
            {
                AtomicJsonRecordStore<AtomicTestRecord> store = new AtomicJsonRecordStore<AtomicTestRecord>(
                    root,
                    value => value.SchemaVersion,
                    1,
                    1,
                    value => value.Name = value.Name ?? "",
                    null,
                    null,
                    value => value.Id,
                    value => AtomicJsonRecordStore<AtomicTestRecord>.SanitizeFileName(value, "record", false),
                    value => value.Name,
                    false,
                    null,
                    null);

                AtomicTestRecord record = new AtomicTestRecord { Id = "one", Name = "Alpha" };
                Assert(store.Save(record), "save first record");
                Assert(File.Exists(Path.Combine(root, "one.json")), "record file exists");
                record.Name = "Beta";
                Assert(store.Save(record), "save second record");
                Assert(File.Exists(Path.Combine(root, "one.json.bak")), "backup exists");
                AtomicTestRecord loaded = store.TryLoadById("one");
                Assert(loaded != null && loaded.Name == "Beta", "roundtrip");
                File.WriteAllText(Path.Combine(root, "one.json"), "{ corrupt");
                loaded = store.TryLoadById("one");
                Assert(loaded != null && loaded.Name == "Alpha", "backup fallback");
                Assert(File.ReadAllText(Path.Combine(root, "one.json")).Contains("Alpha"), "main repaired from backup");
                Assert(store.LoadAll().Count == 1, "load all");
                Assert(store.Delete("one"), "delete");
                Assert(!File.Exists(Path.Combine(root, "one.json")), "delete removes file");
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        private static void TestAtomicStoreSchema()
        {
            string root = Path.Combine(Path.GetTempPath(), "orderedwork-store-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(root);
                File.WriteAllText(Path.Combine(root, "bad.json"),
                    "{\"SchemaVersion\":2,\"Id\":\"bad\",\"Name\":\"bad\"}");
                AtomicJsonRecordStore<AtomicTestRecord> store = new AtomicJsonRecordStore<AtomicTestRecord>(
                    root,
                    value => value.SchemaVersion,
                    1,
                    1,
                    null,
                    null,
                    null,
                    value => value.Id,
                    value => value,
                    value => value.Name,
                    false,
                    null,
                    null);
                Assert(store.TryLoadById("bad") == null, "unsupported schema rejected");
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }
        private static void TestFocusPrepends()
        {
            List<string> effective = HourFocusStack.BuildEffectiveOrder(
                new[] { "Hunter", "Doctor", "Cook", "Fish" },
                new[] { "Fish", "Cook" });
            AssertSequence(new[] { "Fish", "Cook", "Hunter", "Doctor" }, effective);
        }

        private static void TestFocusFallback()
        {
            List<string> effective = HourFocusStack.BuildEffectiveOrder(
                new[] { "Hunter", "Doctor", "Cook", "Fish" },
                new[] { "Fish", "Cook" },
                id => id != "Fish");
            AssertSequence(new[] { "Cook", "Hunter", "Doctor" }, effective);
        }

        private static void TestFocusAllUnavailable()
        {
            List<string> effective = HourFocusStack.BuildEffectiveOrder(
                new[] { "Hunter", "Doctor" },
                new[] { "Fish", "Cook" },
                id => id == "Hunter" || id == "Doctor");
            AssertSequence(new[] { "Hunter", "Doctor" }, effective);
        }

        private static void TestFocusOutsideBase()
        {
            List<string> effective = HourFocusStack.BuildEffectiveOrder(
                new[] { "Hunter", "Doctor" },
                new[] { "Artist" });
            AssertSequence(new[] { "Hunter", "Doctor" }, effective);
        }

        private static void TestFocusNormalize()
        {
            List<string> focus = HourFocusStack.NormalizeFocus(new[] { "Fish", "Cook", "Fish", "Doctor", "Mine" });
            AssertSequence(new[] { "Fish", "Cook", "Doctor" }, focus);
        }

        private static void TestFocusBaseUnchanged()
        {
            List<string> baseOrder = new List<string> { "Hunter", "Doctor", "Cook" };
            HourFocusStack.BuildEffectiveOrder(baseOrder, new[] { "Cook" });
            AssertSequence(new[] { "Hunter", "Doctor", "Cook" }, baseOrder);
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

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
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
