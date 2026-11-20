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
            List<WorkTypeDef> workTypes = WorkConfigStore.GetOrderedVisibleWorkTypes(
                WorkConfigStore.LoadEditingProfile(WorkConfigStore.LoadSelectedProfileName()));
            WorkConfigProfile? existingProfile = WorkConfigStore.LoadPending(profile.Profile)
                ?? (WorkConfigStore.LoadActiveWithFallback(profile.Profile)?.Profile);
            if (existingProfile != null)
            {
                HashSet<string> captured = new HashSet<string>(
                    workTypes.Select(workType => workType.defName), StringComparer.Ordinal);
                foreach (string defName in existingProfile.WorkTypes.Keys)
                {
                    WorkTypeDef hidden = DefDatabase<WorkTypeDef>.GetNamedSilentFail(defName);
                    if (hidden != null && captured.Add(hidden.defName))
                    {
                        workTypes.Add(hidden);
                    }
                }
            }
            int order = 0;
            foreach (WorkTypeDef workType in workTypes)
            {
                WorkTypeConfig? existingType = null;
                if (existingProfile != null)
                {
                    existingProfile.WorkTypes.TryGetValue(workType.defName, out existingType);
                }
                bool isSplit = existingType != null && !existingType.Vanilla;
                WorkTypeConfig wt = new WorkTypeConfig
                {
                    Vanilla = !isSplit && !workType.defName.StartsWith("OrderedWork_", StringComparison.Ordinal),
                    Base = isSplit ? existingType?.Base : null,
                    Label = workType.labelShort,
                    ActionWord = workType.gerundLabel,
                    Description = workType.description,
                    Visible = existingProfile != null && existingProfile.WorkTypes.TryGetValue(workType.defName, out WorkTypeConfig? existing) && existing != null ? existing.Visible : workType.visible,
                    Hidden = existingProfile != null && existingProfile.WorkTypes.TryGetValue(workType.defName, out WorkTypeConfig? existingHidden) && existingHidden != null && existingHidden.Hidden,
                    ClothingPresetId = existingProfile != null && existingProfile.WorkTypes.TryGetValue(workType.defName, out WorkTypeConfig? existingClothing) && existingClothing != null ? existingClothing.ClothingPresetId : "",
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
