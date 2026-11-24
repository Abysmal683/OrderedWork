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
            Run("Clothing lock outranks work", TestClothingLockWins);
            Run("Clothing denied mandatory reports policy contradiction", TestClothingPolicyContradiction);
            Run("Clothing emergency outranks work", TestClothingEmergencyWins);
            Run("Clothing work fills an empty slot", TestClothingWorkFills);
            Run("Clothing rules fill different body layers", TestClothingLayers);
            Run("Clothing lower rule cannot replace occupied slot", TestClothingSlotConflict);
            Run("Clothing tool selection is separate", TestClothingTool);
            Run("Clothing leaves unmatched known slots free", TestClothingFreeSlots);
            Run("Clothing resolver tolerates null inputs", TestClothingNullInputs);
            Run("Clothing preset normalize cleans rules and candidates", TestClothingPresetNormalize);
            Run("Clothing preset normalize restores schema and id", TestClothingPresetIdentity);
            Run("Apparel slot key includes body, group and layer", TestApparelSlotKeyIdentity);
            Run("Apparel footprint normalizes and deduplicates cells", TestApparelFootprintNormalize);
            Run("Apparel catalog maps cells to possible defs", TestApparelCatalog);
            Run("Cell rule normalizes alternatives by local priority", TestCellRuleNormalize);
            Run("Equipment set normalizes cells and inventory", TestEquipmentSetNormalize);
            Run("Equipment resolver keeps mandatory over lower sets", TestEquipmentResolverMandatoryWins);
            Run("Equipment resolver lets WorkType replace Principal", TestEquipmentResolverWorkTypeWins);
            Run("Equipment resolver claims multi-cell footprints", TestEquipmentResolverMultiCell);
            Run("Equipment resolver unions inventory and overflows primary", TestEquipmentResolverInventory);
            Run("Equipment resolver preserves forced cells", TestEquipmentResolverForced);
            Run("Equipment resolver reports mandatory coverage", TestEquipmentResolverMandatoryCoverage);
            Run("Equipment resolver falls back to Principal", TestEquipmentResolverPrincipalFallback);

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
        private static void TestClothingLockWins()
        {
            ClothingPreset preset = new ClothingPreset
            {
                Rules = new List<ClothingRule>
                {
                    new ClothingRule { Phase = ClothingRulePhase.Work, Target = ClothingTarget.Apparel, Slot = "Torso", BodyLayer = "Shell", Candidates = new List<string> { "WorkCoat" } },
                    new ClothingRule { Phase = ClothingRulePhase.Locked, Target = ClothingTarget.Apparel, Slot = "Torso", BodyLayer = "Shell", Candidates = new List<string> { "RoyalCoat" }, Mandatory = true }
                }
            };
            ClothingContext context = new ClothingContext(new[] { "WorkCoat", "RoyalCoat" });
            ClothingResolution result = ClothingResolver.Resolve(preset, context);
            Assert(result.Selections.Count == 1, "lock selection count");
            Assert(result.Selections[0].DefName == "RoyalCoat", "lock wins");
        }

        private static void TestClothingPolicyContradiction()
        {
            ClothingPreset preset = new ClothingPreset
            {
                Rules = new List<ClothingRule>
                {
                    new ClothingRule { Phase = ClothingRulePhase.Locked, Target = ClothingTarget.Apparel, Slot = "Torso", BodyLayer = "Shell", Candidates = new List<string> { "RoyalCoat" }, Mandatory = true }
                }
            };
            ClothingResolution result = ClothingResolver.Resolve(preset, new ClothingContext(Array.Empty<string>()));
            Assert(result.Selections.Count == 0, "denied selection");
            Assert(result.Conflicts.Count == 1, "denied conflict");
            Assert(result.Conflicts[0].Reason == ClothingConflictReason.PolicyContradiction, "policy contradiction reason");
        }

        private static void TestClothingEmergencyWins()
        {
            ClothingPreset preset = new ClothingPreset
            {
                Rules = new List<ClothingRule>
                {
                    new ClothingRule { Phase = ClothingRulePhase.Work, Target = ClothingTarget.Apparel, Slot = "Torso", BodyLayer = "Shell", Candidates = new List<string> { "WorkCoat" } },
                    new ClothingRule { Phase = ClothingRulePhase.Emergency, Target = ClothingTarget.Apparel, Slot = "Torso", BodyLayer = "Shell", Candidates = new List<string> { "EmergencyCoat" } }
                }
            };
            ClothingResolution result = ClothingResolver.Resolve(preset, new ClothingContext(new[] { "WorkCoat", "EmergencyCoat" }));
            Assert(result.Selections.Count == 1, "emergency selection count");
            Assert(result.Selections[0].DefName == "EmergencyCoat", "emergency outranks work");
        }
        private static void TestClothingWorkFills()
        {
            ClothingPreset preset = new ClothingPreset
            {
                Rules = new List<ClothingRule>
                {
                    new ClothingRule { Phase = ClothingRulePhase.Work, Target = ClothingTarget.Apparel, Slot = "Torso", BodyLayer = "Shell", Candidates = new List<string> { "WorkCoat" } }
                }
            };
            ClothingContext context = new ClothingContext(new[] { "WorkCoat" }, null,
                new[] { new ClothingSlot("Torso", "Shell", ClothingTarget.Apparel) });
            ClothingResolution result = ClothingResolver.Resolve(preset, context);
            Assert(result.Selections.Count == 1, "work selection");
            Assert(result.FreeSlots.Count == 0, "work fills free slot");
        }

        private static void TestClothingLayers()
        {
            ClothingPreset preset = new ClothingPreset
            {
                Rules = new List<ClothingRule>
                {
                    new ClothingRule { Phase = ClothingRulePhase.Work, Target = ClothingTarget.Apparel, Slot = "Torso", BodyLayer = "OnSkin", Candidates = new List<string> { "Shirt" } },
                    new ClothingRule { Phase = ClothingRulePhase.Work, Target = ClothingTarget.Apparel, Slot = "Torso", BodyLayer = "Shell", Candidates = new List<string> { "Coat" } }
                }
            };
            ClothingResolution result = ClothingResolver.Resolve(preset, new ClothingContext(new[] { "Shirt", "Coat" }));
            Assert(result.Selections.Count == 2, "different layers selected");
        }

        private static void TestClothingSlotConflict()
        {
            ClothingPreset preset = new ClothingPreset
            {
                Rules = new List<ClothingRule>
                {
                    new ClothingRule { Phase = ClothingRulePhase.Work, Priority = 1, Target = ClothingTarget.Apparel, Slot = "Torso", BodyLayer = "Shell", Candidates = new List<string> { "CoatA" } },
                    new ClothingRule { Phase = ClothingRulePhase.Work, Priority = 2, Target = ClothingTarget.Apparel, Slot = "Torso", BodyLayer = "Shell", Candidates = new List<string> { "CoatB" } }
                }
            };
            ClothingResolution result = ClothingResolver.Resolve(preset, new ClothingContext(new[] { "CoatA", "CoatB" }));
            Assert(result.Selections.Count == 1, "only one slot selection");
            Assert(result.Selections[0].DefName == "CoatA", "priority selection");
            Assert(result.Conflicts.Any(conflict => conflict.Reason == ClothingConflictReason.SlotOccupied), "occupied conflict");
        }

        private static void TestClothingTool()
        {
            ClothingPreset preset = new ClothingPreset
            {
                Rules = new List<ClothingRule>
                {
                    new ClothingRule { Phase = ClothingRulePhase.Work, Target = ClothingTarget.Tool, Slot = "Primary", Candidates = new List<string> { "Hammer" } }
                }
            };
            ClothingResolution result = ClothingResolver.Resolve(preset, new ClothingContext(null, new[] { "Hammer" }));
            Assert(result.Selections.Count == 1, "tool selection");
            Assert(result.Selections[0].Target == ClothingTarget.Tool, "tool target");
        }

        private static void TestClothingFreeSlots()
        {
            ClothingPreset preset = new ClothingPreset { Rules = new List<ClothingRule>() };
            ClothingResolution result = ClothingResolver.Resolve(preset, new ClothingContext(null, null,
                new[] { new ClothingSlot("Feet", "OnSkin", ClothingTarget.Apparel) }));
            Assert(result.FreeSlots.Count == 1, "free slot count");
            Assert(result.FreeSlots[0].Slot == "Feet", "free slot identity");
        }

        private static void TestClothingNullInputs()
        {
            ClothingResolution result = ClothingResolver.Resolve(null, null);
            Assert(result.Selections.Count == 0, "null preset selections");
            Assert(result.Conflicts.Count == 0, "null preset conflicts");
        }
        private static void TestClothingPresetNormalize()
        {
            ClothingPreset preset = new ClothingPreset
            {
                Rules = new List<ClothingRule>
                {
                    null,
                    new ClothingRule
                    {
                        Slot = " Torso ",
                        BodyLayer = " Shell ",
                        Group = " ",
                        Candidates = new List<string> { "Coat", " Coat ", "", null }
                    }
                }
            };
            preset.Normalize();
            Assert(preset.Rules != null && preset.Rules.Count == 1, "normalized rule count");
            Assert(preset.Rules[0].Slot == "Torso", "normalized slot");
            Assert(preset.Rules[0].BodyLayer == "Shell", "normalized layer");
            Assert(preset.Rules[0].Group == null, "normalized empty group");
            AssertSequence(new[] { "Coat" }, preset.Rules[0].Candidates);
        }

        private static void TestClothingPresetIdentity()
        {
            ClothingPreset preset = new ClothingPreset { Id = "", Name = "", SchemaVersion = 0, Rules = null };
            preset.Normalize();
            Assert(!string.IsNullOrEmpty(preset.Id), "generated id");
            Assert(preset.Name == "new clothing", "default name");
            AssertEqual(ClothingPreset.CurrentSchemaVersion, preset.SchemaVersion);
            Assert(preset.Rules != null && preset.Rules.Count == 0, "empty rule list");
        }
        private static ApparelSlotKey TorsoShell()
        {
            return new ApparelSlotKey("Human", "Torso", "Shell");
        }

        private static ApparelSlotKey LegsShell()
        {
            return new ApparelSlotKey("Human", "Legs", "Shell");
        }

        private static CellRule Rule(ApparelSlotKey cell, params string[] alternatives)
        {
            return new CellRule
            {
                Cell = cell,
                Alternatives = alternatives
                    .Select((defName, index) => new CellAlternative { DefName = defName, Priority = index })
                    .ToList()
            };
        }

        private static ApparelFootprint Footprint(string defName, params ApparelSlotKey[] cells)
        {
            return new ApparelFootprint { DefName = defName, Cells = cells.ToList() };
        }

        private static Dictionary<string, ApparelFootprint> Footprints(params ApparelFootprint[] footprints)
        {
            return footprints.ToDictionary(footprint => footprint.DefName, StringComparer.Ordinal);
        }

        private static EquipmentResolutionContext Context(
            IEnumerable<EquipmentSet> sets,
            IEnumerable<string> allowed,
            Dictionary<string, ApparelFootprint> footprints,
            string activeWorkType = null,
            bool emergencyActive = false,
            EquipmentOverrides overrides = null)
        {
            return new EquipmentResolutionContext(
                footprints,
                allowed,
                sets,
                activeWorkType,
                emergencyActive,
                overrides);
        }

        private static void TestEquipmentResolverMandatoryWins()
        {
            ApparelSlotKey torso = TorsoShell();
            EquipmentSet mandatory = new EquipmentSet
            {
                Id = "mandatory",
                Role = EquipmentSetRole.Mandatory,
                Cells = new List<CellRule> { Rule(torso, "MandatoryArmor") }
            };
            EquipmentSet principal = new EquipmentSet
            {
                Id = "principal",
                Role = EquipmentSetRole.Principal,
                Cells = new List<CellRule> { Rule(torso, "CasualCoat") }
            };
            EquipmentResolutionContext context = Context(
                new[] { mandatory, principal },
                new[] { "MandatoryArmor", "CasualCoat" },
                Footprints(Footprint("MandatoryArmor", torso), Footprint("CasualCoat", torso)));

            EquipmentPlan plan = EquipmentResolver.Resolve(context);

            Assert(plan.ByCell.ContainsKey(torso), "mandatory cell selected");
            Assert(plan.ByCell[torso].DefName == "MandatoryArmor", "mandatory wins");
            AssertEqual(0, plan.MandatoryUncovered.Count);
        }

        private static void TestEquipmentResolverWorkTypeWins()
        {
            ApparelSlotKey torso = TorsoShell();
            EquipmentSet work = new EquipmentSet
            {
                Id = "work",
                Role = EquipmentSetRole.WorkType,
                WorkTypeDefName = "Cook",
                Cells = new List<CellRule> { Rule(torso, "CookApron") }
            };
            EquipmentSet principal = new EquipmentSet
            {
                Id = "principal",
                Role = EquipmentSetRole.Principal,
                Cells = new List<CellRule> { Rule(torso, "CasualCoat") }
            };
            EquipmentResolutionContext context = Context(
                new[] { principal, work },
                new[] { "CookApron", "CasualCoat" },
                Footprints(Footprint("CookApron", torso), Footprint("CasualCoat", torso)),
                activeWorkType: "Cook");

            EquipmentPlan plan = EquipmentResolver.Resolve(context);

            Assert(plan.ByCell[torso].DefName == "CookApron", "work type replaces principal");
            Assert(plan.ByCell[torso].Source == EquipmentSelectionSource.WorkType, "work source");
        }

        private static void TestEquipmentResolverMultiCell()
        {
            ApparelSlotKey torso = TorsoShell();
            ApparelSlotKey legs = LegsShell();
            EquipmentSet principal = new EquipmentSet
            {
                Id = "principal",
                Role = EquipmentSetRole.Principal,
                Cells = new List<CellRule>
                {
                    Rule(torso, "Robe"),
                    Rule(legs, "Pants")
                }
            };
            EquipmentResolutionContext context = Context(
                new[] { principal },
                new[] { "Robe", "Pants" },
                Footprints(Footprint("Robe", torso, legs), Footprint("Pants", legs)));

            EquipmentPlan plan = EquipmentResolver.Resolve(context);

            AssertEqual(1, plan.Selections.Count);
            Assert(plan.ByCell[torso].DefName == "Robe", "robe claims torso");
            Assert(plan.ByCell[legs].DefName == "Robe", "robe claims legs");
        }

        private static void TestEquipmentResolverInventory()
        {
            EquipmentSet mandatory = new EquipmentSet
            {
                Id = "mandatory",
                Role = EquipmentSetRole.Mandatory,
                Order = 1,
                PrimaryEquipmentDefName = "Rifle",
                Inventory = new List<InventoryTarget> { new InventoryTarget { DefName = "Food", Expected = 2 } }
            };
            EquipmentSet principal = new EquipmentSet
            {
                Id = "principal",
                Role = EquipmentSetRole.Principal,
                Order = 2,
                PrimaryEquipmentDefName = "Pistol",
                Inventory = new List<InventoryTarget> { new InventoryTarget { DefName = "Food", Expected = 3 } }
            };
            EquipmentResolutionContext context = Context(
                new[] { principal, mandatory },
                Array.Empty<string>(),
                new Dictionary<string, ApparelFootprint>(StringComparer.Ordinal));

            EquipmentPlan plan = EquipmentResolver.Resolve(context);

            AssertEqual(5, plan.Inventory["Food"]);
            Assert(plan.PrimaryEquipmentDefName == "Rifle", "first primary wins");
            AssertEqual(1, plan.Inventory["Pistol"]);
        }

        private static void TestEquipmentResolverForced()
        {
            ApparelSlotKey torso = TorsoShell();
            EquipmentSet mandatory = new EquipmentSet
            {
                Id = "mandatory",
                Role = EquipmentSetRole.Mandatory,
                Cells = new List<CellRule> { Rule(torso, "MandatoryArmor") }
            };
            EquipmentOverrides overrides = new EquipmentOverrides();
            overrides.ForcedApparel[torso] = "ForcedCoat";
            EquipmentResolutionContext context = Context(
                new[] { mandatory },
                new[] { "MandatoryArmor", "ForcedCoat" },
                Footprints(Footprint("MandatoryArmor", torso), Footprint("ForcedCoat", torso)),
                overrides: overrides);

            EquipmentPlan plan = EquipmentResolver.Resolve(context);

            Assert(plan.ByCell[torso].DefName == "ForcedCoat", "forced wins");
            Assert(plan.ByCell[torso].Source == EquipmentSelectionSource.Forced, "forced source");
            AssertEqual(0, plan.MandatoryUncovered.Count);
        }

        private static void TestEquipmentResolverMandatoryCoverage()
        {
            ApparelSlotKey torso = TorsoShell();
            EquipmentSet mandatory = new EquipmentSet
            {
                Id = "mandatory",
                Role = EquipmentSetRole.Mandatory,
                Cells = new List<CellRule> { Rule(torso, "MissingArmor") }
            };
            EquipmentResolutionContext context = Context(
                new[] { mandatory },
                new[] { "MissingArmor" },
                new Dictionary<string, ApparelFootprint>(StringComparer.Ordinal));

            EquipmentPlan plan = EquipmentResolver.Resolve(context);

            AssertEqual(1, plan.MandatoryUncovered.Count);
            Assert(plan.MandatoryUncovered[0] == torso, "mandatory cell uncovered");
        }

        private static void TestEquipmentResolverPrincipalFallback()
        {
            ApparelSlotKey torso = TorsoShell();
            EquipmentSet principal = new EquipmentSet
            {
                Id = "principal",
                Role = EquipmentSetRole.Principal,
                Cells = new List<CellRule> { Rule(torso, "CasualCoat") }
            };
            EquipmentResolutionContext context = Context(
                new[] { principal },
                new[] { "CasualCoat" },
                Footprints(Footprint("CasualCoat", torso)),
                emergencyActive: true);

            EquipmentPlan plan = EquipmentResolver.Resolve(context);

            Assert(plan.ByCell[torso].DefName == "CasualCoat", "principal fallback");
            Assert(plan.ByCell[torso].Source == EquipmentSelectionSource.Principal, "principal source");
        }
        private static void TestApparelSlotKeyIdentity()
        {
            ApparelSlotKey torsoShell = new ApparelSlotKey("Human", "Torso", "Shell");
            ApparelSlotKey same = new ApparelSlotKey("Human", "Torso", "Shell");
            ApparelSlotKey otherLayer = new ApparelSlotKey("Human", "Torso", "Middle");
            ApparelSlotKey otherBody = new ApparelSlotKey("Mech", "Torso", "Shell");

            Assert(torsoShell == same, "slot key equality");
            Assert(torsoShell != otherLayer, "slot key layer differs");
            Assert(torsoShell != otherBody, "slot key body differs");
            Assert(torsoShell.IsValid, "valid slot key");
            Assert(!new ApparelSlotKey("", "Torso", "Shell").IsValid, "invalid slot key");
        }

        private static void TestApparelFootprintNormalize()
        {
            ApparelFootprint footprint = new ApparelFootprint
            {
                DefName = " Robe ",
                Cells = new List<ApparelSlotKey>
                {
                    new ApparelSlotKey("Human", "Torso", "Shell"),
                    new ApparelSlotKey("Human", "Torso", "Shell"),
                    new ApparelSlotKey("", "Torso", "Shell")
                }
            };

            footprint.Normalize();

            Assert(footprint.DefName == "Robe", "footprint def trimmed");
            AssertEqual(1, footprint.Cells.Count);
            Assert(footprint.Cells[0] == new ApparelSlotKey("Human", "Torso", "Shell"), "footprint cell retained");
        }

        private static void TestApparelCatalog()
        {
            ApparelSlotCatalog catalog = new ApparelSlotCatalog();
            catalog.Add(new ApparelFootprint
            {
                DefName = "Robe",
                Cells = new List<ApparelSlotKey>
                {
                    new ApparelSlotKey("Human", "Torso", "Shell"),
                    new ApparelSlotKey("Human", "Legs", "Shell")
                }
            });
            catalog.Add(new ApparelFootprint
            {
                DefName = "Duster",
                Cells = new List<ApparelSlotKey>
                {
                    new ApparelSlotKey("Human", "Torso", "Shell")
                }
            });

            IReadOnlyCollection<string> torso = catalog.DefsFor(new ApparelSlotKey("Human", "Torso", "Shell"));
            IReadOnlyCollection<string> legs = catalog.DefsFor(new ApparelSlotKey("Human", "Legs", "Shell"));

            AssertSequence(new[] { "Duster", "Robe" }, torso);
            AssertSequence(new[] { "Robe" }, legs);
            AssertEqual(2, catalog.Cells.Count);
        }

        private static void TestCellRuleNormalize()
        {
            CellRule rule = new CellRule
            {
                Cell = new ApparelSlotKey("Human", "Torso", "Shell"),
                Alternatives = new List<CellAlternative>
                {
                    null,
                    new CellAlternative { DefName = "Robe", Priority = 2 },
                    new CellAlternative { DefName = "Duster", Priority = 1 },
                    new CellAlternative { DefName = " Robe ", Priority = 0 },
                    new CellAlternative { DefName = "", Priority = 3 }
                }
            };

            rule.Normalize();

            AssertEqual(2, rule.Alternatives.Count);
            AssertSequence(new[] { "Robe", "Duster" }, rule.Alternatives.Select(alternative => alternative.DefName));
            AssertEqual(0, rule.Alternatives[0].Priority);
            AssertEqual(1, rule.Alternatives[1].Priority);
        }

        private static void TestEquipmentSetNormalize()
        {
            ApparelSlotKey torsoShell = new ApparelSlotKey("Human", "Torso", "Shell");
            EquipmentSet set = new EquipmentSet
            {
                Id = "",
                Name = " ",
                Role = EquipmentSetRole.Mandatory,
                Cells = new List<CellRule>
                {
                    null,
                    new CellRule { Cell = torsoShell, Alternatives = new List<CellAlternative> { new CellAlternative { DefName = "Robe" } } },
                    new CellRule { Cell = torsoShell, Alternatives = new List<CellAlternative> { new CellAlternative { DefName = "Duster" } } },
                    new CellRule { Cell = default, Alternatives = new List<CellAlternative>() }
                },
                Inventory = new List<InventoryTarget>
                {
                    new InventoryTarget { DefName = " Food ", Expected = 5 },
                    new InventoryTarget { DefName = "Food", Expected = 8 },
                    new InventoryTarget { DefName = "", Expected = 1 }
                }
            };

            set.Normalize();

            Assert(!string.IsNullOrEmpty(set.Id), "set id generated");
            Assert(set.Name == "new set", "set default name");
            AssertEqual(1, set.Cells.Count);
            AssertEqual(2, set.Cells[0].Alternatives.Count);
            AssertSequence(new[] { "Robe", "Duster" }, set.Cells[0].Alternatives.Select(alternative => alternative.DefName));
            AssertEqual(1, set.Inventory.Count);
            Assert(set.Inventory[0].DefName == "Food", "inventory trimmed");
            AssertEqual(8, set.Inventory[0].Expected);
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
