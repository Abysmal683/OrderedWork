using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace OrderedWork.Config
{
    /// <summary>
    /// Captures the current live defs into the canonical profile shape. This is the bridge
    /// between the old settings lists and the JSON profile while the UI is being migrated.
    /// </summary>
    public static class WorkConfigProfileBuilder
    {
        public static WorkConfigProfile Capture(OrderedWorkSettings settings, string profileName)
        {
            WorkConfigProfile profile = new WorkConfigProfile
            {
                Profile = string.IsNullOrWhiteSpace(profileName) ? "default" : profileName.Trim()
            };
            HashSet<string> mods = new HashSet<string>(StringComparer.Ordinal);
            List<WorkTypeDef> workTypes = OrderedWorkTypes(settings);
            int order = 0;
            foreach (WorkTypeDef workType in workTypes)
            {
                SplitEntry split = settings != null && settings.splits != null
                    ? settings.splits.FirstOrDefault(s => s != null && s.newDefName == workType.defName)
                    : null;
                WorkTypeConfig wt = new WorkTypeConfig
                {
                    Vanilla = split == null && !workType.defName.StartsWith("OrderedWork_", StringComparison.Ordinal),
                    Base = split != null ? split.baseWorkType : null,
                    Label = workType.labelShort,
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

        private static List<WorkTypeDef> OrderedWorkTypes(OrderedWorkSettings settings)
        {
            List<WorkTypeDef> all = DefDatabase<WorkTypeDef>.AllDefsListForReading
                .Where(w => w != null && w.visible)
                .ToList();
            List<WorkTypeDef> ordered = new List<WorkTypeDef>();
            HashSet<string> added = new HashSet<string>(StringComparer.Ordinal);
            if (settings != null && settings.workTypeOrder != null)
            {
                foreach (string id in settings.workTypeOrder)
                {
                    WorkTypeDef def = all.FirstOrDefault(w => w.defName == id);
                    if (def != null && added.Add(def.defName))
                    {
                        ordered.Add(def);
                    }
                }
            }
            foreach (WorkTypeDef def in all.OrderByDescending(w => w.naturalPriority))
            {
                if (added.Add(def.defName))
                {
                    ordered.Add(def);
                }
            }
            return ordered;
        }
    }
}
