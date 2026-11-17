using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using OrderedWork.Config;
using RimWorld;
using Verse;

namespace OrderedWork
{
    [StaticConstructorOnStartup]
    public static class OrderedWorkStartup
    {
        static OrderedWorkStartup()
        {
            try
            {
                OrderedWorkMod mod = LoadedModManager.GetMod<OrderedWorkMod>();
                OrderedWorkSettings settings = mod?.GetSettings<OrderedWorkSettings>();
                if (settings == null)
                {
                    return;
                }

                string selectedProfile = WorkConfigStore.LoadSelectedProfileName();
                WorkConfigStore.PromotePending(selectedProfile);
                WorkConfigLoadResult loaded = WorkConfigStore.LoadActiveWithFallback(selectedProfile);
                if (loaded != null && loaded.Profile != null)
                {
                    ApplyProfile(loaded.Profile, settings, mod);
                    return;
                }

                ApplySplits(new List<SplitEntry>(), mod);
                ApplyOrder(settings);
                RebuildCaches();
            }
            catch (Exception exception)
            {
                Log.Error("[Ordered Work] Failed to apply work changes: " + exception);
            }
        }

        /// <summary>
        /// Applies saved work changes immediately for settings feedback. Safe to call after
        /// adding a split or merge; it is the same pipeline used at startup and remains
        /// restart-backed because DefDatabase mutations cannot be cleanly rolled back.
        /// </summary>
        public static void ApplyProfileNow(WorkConfigProfile profile, OrderedWorkSettings settings, OrderedWorkMod mod)
        {
            ApplyProfile(profile, settings, mod);
        }

        private static void ApplyProfile(WorkConfigProfile profile, OrderedWorkSettings settings, OrderedWorkMod mod)
        {
            if (profile == null || profile.WorkTypes == null || profile.WorkTypes.Count == 0)
            {
                return;
            }

            settings.workTypeOrder.Clear();
            List<SplitEntry> splitEntries = new List<SplitEntry>();

            List<KeyValuePair<string, WorkTypeConfig>> ordered = profile.WorkTypes
                .Where(e => e.Value != null)
                .OrderBy(e => e.Value.Order)
                .ToList();

            foreach (KeyValuePair<string, WorkTypeConfig> entry in ordered)
            {
                WorkTypeConfig wt = entry.Value;
                if (!wt.Vanilla)
                {
                    splitEntries.Add(new SplitEntry
                    {
                        newDefName = entry.Key,
                        label = wt.Label,
                        description = wt.Description,
                        baseWorkType = wt.Base,
                        givers = wt.Givers != null
                            ? wt.Givers.Where(g => g != null).Select(g => g.DefName).Distinct().ToList()
                            : new List<string>()
                    });
                }
            }

            ApplySplits(splitEntries, mod);

            int workOrder = 0;
            foreach (KeyValuePair<string, WorkTypeConfig> entry in ordered)
            {
                WorkTypeDef def = DefDatabase<WorkTypeDef>.GetNamedSilentFail(entry.Key);
                if (def == null)
                {
                    continue;
                }
                WorkTypeConfig wt = entry.Value;
                string label = string.IsNullOrWhiteSpace(wt.Label) ? def.defName : wt.Label;
                def.labelShort = label;
                def.pawnLabel = label;
                def.gerundLabel = label;
                if (!string.IsNullOrWhiteSpace(wt.Description))
                {
                    def.description = wt.Description;
                }
                def.visible = wt.Visible;
                def.naturalPriority = 10000 - workOrder * 10;
                settings.workTypeOrder.Add(entry.Key);

                int giverOrder = 0;
                if (wt.Givers != null)
                {
                    foreach (GiverConfig giverConfig in wt.Givers.Where(g => g != null).OrderBy(g => g.Order))
                    {
                        WorkGiverDef giver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(giverConfig.DefName);
                        if (giver == null)
                        {
                            continue;
                        }
                        giver.workType = def;
                        giver.priorityInType = 10000 - giverOrder * 10;
                        giverOrder++;
                    }
                }
                workOrder++;
            }

            HashSet<string> profileNames = new HashSet<string>(profile.WorkTypes.Keys, StringComparer.Ordinal);
            foreach (WorkTypeDef stale in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (stale != null
                    && stale.defName.StartsWith("OrderedWork_", StringComparison.Ordinal)
                    && !profileNames.Contains(stale.defName))
                {
                    stale.visible = false;
                }
            }
            RebuildCaches();
            OrderedWork.Services.WorkOrderService.InvalidateAvailableWorkTypes();
        }

        private static void ApplySplits(IEnumerable<SplitEntry> splitEntries, OrderedWorkMod mod)
        {
            foreach (SplitEntry split in splitEntries)
            {
                if (split == null || string.IsNullOrWhiteSpace(split.newDefName)
                    || string.IsNullOrWhiteSpace(split.baseWorkType)
                    || split.givers == null || split.givers.Count == 0)
                {
                    continue;
                }
                if (DefDatabase<WorkTypeDef>.GetNamedSilentFail(split.newDefName) != null)
                {
                    continue;
                }
                WorkTypeDef baseDef = DefDatabase<WorkTypeDef>.GetNamedSilentFail(split.baseWorkType);
                if (baseDef == null)
                {
                    continue;
                }

                WorkTypeDef def = new WorkTypeDef
                {
                    defName = split.newDefName,
                    labelShort = split.label ?? split.newDefName,
                    pawnLabel = split.label ?? split.newDefName,
                    gerundLabel = split.label ?? split.newDefName,
                    description = string.IsNullOrWhiteSpace(split.description)
                        ? "Split from " + baseDef.labelShort
                        : split.description,
                    verb = baseDef.verb,
                    naturalPriority = baseDef.naturalPriority,
                    alwaysStartActive = baseDef.alwaysStartActive,
                    requireCapableColonist = baseDef.requireCapableColonist,
                    relevantSkills = baseDef.relevantSkills != null ? new List<SkillDef>(baseDef.relevantSkills) : new List<SkillDef>(),
                    workTags = baseDef.workTags,
                    disabledForSlaves = baseDef.disabledForSlaves,
                    visible = baseDef.visible,
                    visibleOnlyWithChildrenInColony = baseDef.visibleOnlyWithChildrenInColony,
                    modContentPack = mod.Content
                };

                DefDatabase<WorkTypeDef>.Add(def);
                CopyLifeStageAndMech(def, baseDef);
                CopyTraitWorkTypeReferences(def, baseDef);

                foreach (string giverName in split.givers)
                {
                    WorkGiverDef giver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(giverName);
                    if (giver != null)
                    {
                        giver.workType = def;
                    }
                }
                def.PostLoad();
            }
        }

        private static void CopyLifeStageAndMech(WorkTypeDef newDef, WorkTypeDef baseDef)
        {
            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                RaceProperties race = thingDef.race;
                if (race == null)
                {
                    continue;
                }
                if (race.lifeStageWorkSettings != null)
                {
                    List<LifeStageWorkSettings> baseEntries = race.lifeStageWorkSettings.Where(e => e.workType == baseDef).ToList();
                    foreach (LifeStageWorkSettings entry in baseEntries)
                    {
                        race.lifeStageWorkSettings.Add(new LifeStageWorkSettings
                        {
                            workType = newDef,
                            minAge = entry.minAge
                        });
                    }
                }
                if (race.mechEnabledWorkTypes != null && race.mechEnabledWorkTypes.Contains(baseDef))
                {
                    race.mechEnabledWorkTypes.Add(newDef);
                }
            }
        }

        private static void CopyTraitWorkTypeReferences(WorkTypeDef newDef, WorkTypeDef baseDef)
        {
            foreach (TraitDef trait in DefDatabase<TraitDef>.AllDefsListForReading)
            {
                if (trait.disabledWorkTypes != null && trait.disabledWorkTypes.Contains(baseDef) && !trait.disabledWorkTypes.Contains(newDef))
                {
                    trait.disabledWorkTypes.Add(newDef);
                }
                if (trait.requiredWorkTypes != null && trait.requiredWorkTypes.Contains(baseDef) && !trait.requiredWorkTypes.Contains(newDef))
                {
                    trait.requiredWorkTypes.Add(newDef);
                }
            }
        }

        private static void ApplyOrder(OrderedWorkSettings settings)
        {
            if (settings.workTypeOrder == null || settings.workTypeOrder.Count == 0)
            {
                return;
            }
            List<WorkTypeDef> all = DefDatabase<WorkTypeDef>.AllDefsListForReading;
            List<WorkTypeDef> ordered = new List<WorkTypeDef>();
            foreach (string defName in settings.workTypeOrder)
            {
                WorkTypeDef def = all.FirstOrDefault(d => d.defName == defName);
                if (def != null && !ordered.Contains(def))
                {
                    ordered.Add(def);
                }
            }
            foreach (WorkTypeDef def in all.OrderByDescending(d => d.naturalPriority))
            {
                if (!ordered.Contains(def))
                {
                    ordered.Add(def);
                }
            }
            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].naturalPriority = 10000 - i * 10;
            }

            foreach (WorkTypeDef workType in ordered)
            {
                List<WorkGiverDef> givers = DefDatabase<WorkGiverDef>.AllDefsListForReading
                    .Where(g => g.workType == workType)
                    .ToList();
                List<WorkGiverDef> sorted = new List<WorkGiverDef>();
                foreach (WorkGiverDef giver in givers.OrderByDescending(g => g.priorityInType))
                {
                    if (!sorted.Contains(giver))
                    {
                        sorted.Add(giver);
                    }
                }
                for (int i = 0; i < sorted.Count; i++)
                {
                    sorted[i].priorityInType = 10000 - i * 10;
                }
            }
        }

        private static void RebuildCaches()
        {
            FieldInfo field = typeof(WorkTypeDef).GetField("workGiversByPriority", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                return;
            }
            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                List<WorkGiverDef> list = DefDatabase<WorkGiverDef>.AllDefsListForReading
                    .Where(g => g.workType == workType)
                    .OrderByDescending(g => g.priorityInType)
                    .ToList();
                field.SetValue(workType, list);
            }
        }
    }
}
