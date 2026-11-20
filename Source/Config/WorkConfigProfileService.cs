using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace OrderedWork.Config
{
    /// <summary>
    /// Profile domain operations: visible ordering, normalization and the vanilla
    /// reference snapshot. No file IO.
    /// </summary>
    public static class WorkConfigProfileService
    {
        public static List<WorkTypeDef> GetOrderedVisibleWorkTypes(WorkConfigProfile profile)
        {
            List<WorkTypeDef> all = DefDatabase<WorkTypeDef>.AllDefsListForReading
                .Where(w => w != null)
                .ToList();
            if (profile == null)
            {
                return all.OrderByDescending(w => w.naturalPriority).ToList();
            }
            List<WorkTypeDef> ordered = new List<WorkTypeDef>();
            HashSet<string> added = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, WorkTypeConfig> entry in profile.WorkTypes
                .Where(e => e.Value != null)
                .OrderBy(e => e.Value.Order))
            {
                WorkTypeDef def = all.FirstOrDefault(w => w.defName == entry.Key);
                if (def != null && added.Add(def.defName))
                {
                    ordered.Add(def);
                }
            }
            foreach (WorkTypeDef def in all.Where(w => w.visible).OrderByDescending(w => w.naturalPriority))
            {
                if (added.Add(def.defName))
                {
                    ordered.Add(def);
                }
            }
            return ordered;
        }

        /// <summary>
        /// Appends visible live WorkTypes and Givers that are missing from the profile.
        /// This keeps profiles usable after a mod adds a WorkType or a Giver.
        /// </summary>
        public static void ReconcileWithLive(WorkConfigProfile profile)
        {
            if (profile == null)
            {
                return;
            }
            Normalize(profile);
            int nextOrder = profile.WorkTypes.Values
                .Where(value => value != null)
                .Select(value => value.Order)
                .DefaultIfEmpty(-1)
                .Max() + 1;
            HashSet<string> knownMods = new HashSet<string>(
                profile.Mods.Where(mod => mod != null).Select(mod => mod.PackageId), StringComparer.Ordinal);

            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading
                .Where(value => value != null))
            {
                WorkTypeConfig config;
                if (!profile.WorkTypes.TryGetValue(workType.defName, out config) || config == null)
                {
                    if (!workType.visible)
                    {
                        continue;
                    }
                    config = new WorkTypeConfig
                    {
                        Vanilla = !workType.defName.StartsWith("OrderedWork_", StringComparison.Ordinal),
                        Base = null,
                        Label = workType.labelShort,
                        ActionWord = workType.gerundLabel,
                        Description = workType.description,
                        Visible = true,
                        Order = nextOrder++,
                        Mod = workType.modContentPack != null ? workType.modContentPack.PackageId : "unknown"
                    };
                    profile.WorkTypes[workType.defName] = config;
                    if (knownMods.Add(config.Mod))
                    {
                        profile.Mods.Add(new WorkConfigMod { PackageId = config.Mod, Name = config.Mod });
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(config.ActionWord))
                    {
                        config.ActionWord = workType.gerundLabel;
                    }
                    if (config.Vanilla && !config.Hidden && !workType.visible && config.Visible)
                    {
                        config.Visible = false;
                    }
                }

                config.Givers = config.Givers ?? new List<GiverConfig>();
                HashSet<string> knownGivers = new HashSet<string>(
                    config.Givers.Where(giver => giver != null).Select(giver => giver.DefName), StringComparer.Ordinal);
                int giverOrder = config.Givers.Count;
                foreach (WorkGiverDef giver in DefDatabase<WorkGiverDef>.AllDefsListForReading
                    .Where(value => value != null && value.workType == workType)
                    .OrderByDescending(value => value.priorityInType))
                {
                    if (knownGivers.Add(giver.defName))
                    {
                        string packageId = giver.modContentPack != null ? giver.modContentPack.PackageId : "unknown";
                        config.Givers.Add(new GiverConfig
                        {
                            DefName = giver.defName,
                            Order = giverOrder++,
                            Mod = packageId
                        });
                        if (knownMods.Add(packageId))
                        {
                            profile.Mods.Add(new WorkConfigMod { PackageId = packageId, Name = packageId });
                        }
                    }
                }
            }
        }

        public static void Normalize(WorkConfigProfile profile)
        {
            if (profile.WorkTypes == null)
            {
                profile.WorkTypes = new Dictionary<string, WorkTypeConfig>(StringComparer.Ordinal);
            }
            if (profile.Mods == null)
            {
                profile.Mods = new List<WorkConfigMod>();
            }
            foreach (WorkTypeConfig workType in profile.WorkTypes.Values)
            {
                if (workType.Givers == null)
                {
                    workType.Givers = new List<GiverConfig>();
                }
                workType.Givers = workType.Givers
                    .Where(g => g != null && !string.IsNullOrEmpty(g.DefName))
                    .GroupBy(g => g.DefName, StringComparer.Ordinal)
                    .Select(g => g.First())
                    .ToList();
            }
        }

        public static WorkConfigProfile BuildReferenceProfile(string name)
        {
            WorkConfigProfile profile = new WorkConfigProfile { Profile = name };
            HashSet<string> mods = new HashSet<string>(StringComparer.Ordinal);
            int order = 0;
            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading
                .Where(w => w != null && w.visible)
                .OrderByDescending(w => w.naturalPriority))
            {
                WorkTypeConfig wt = new WorkTypeConfig
                {
                    Vanilla = true,
                    Base = null,
                    Label = workType.labelShort,
                    ActionWord = workType.gerundLabel,
                    Description = workType.description,
                    Visible = workType.visible,
                    Order = order++,
                    Mod = workType.modContentPack != null ? workType.modContentPack.PackageId : "unknown"
                };
                int giverOrder = 0;
                foreach (WorkGiverDef giver in DefDatabase<WorkGiverDef>.AllDefsListForReading
                    .Where(g => g != null && g.workType == workType)
                    .OrderByDescending(g => g.priorityInType))
                {
                    string packageId = giver.modContentPack != null ? giver.modContentPack.PackageId : "unknown";
                    wt.Givers.Add(new GiverConfig
                    {
                        DefName = giver.defName,
                        Order = giverOrder++,
                        Mod = packageId
                    });
                    mods.Add(packageId);
                }
                profile.WorkTypes[workType.defName] = wt;
                mods.Add(wt.Mod);
            }
            foreach (string packageId in mods)
            {
                profile.Mods.Add(new WorkConfigMod { PackageId = packageId, Name = packageId });
            }
            return profile;
        }
    }
}
