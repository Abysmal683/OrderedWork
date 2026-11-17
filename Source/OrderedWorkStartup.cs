using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

                ApplySplits(settings, mod);
                ApplyMerges(settings);
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
        public static bool ApplyPendingChanges(OrderedWorkSettings settings, OrderedWorkMod mod)
        {
            if (settings == null || mod == null)
            {
                return false;
            }
            try
            {
                ApplySplits(settings, mod);
                ApplyMerges(settings);
                ApplyOrder(settings);
                RebuildCaches();
                OrderedWork.Services.WorkOrderService.InvalidateAvailableWorkTypes();
                return true;
            }
            catch (Exception exception)
            {
                Log.Error("[Ordered Work] Failed to apply pending work changes: " + exception);
                return false;
            }
        }

        private static void ApplySplits(OrderedWorkSettings settings, OrderedWorkMod mod)
        {
            foreach (SplitEntry split in settings.splits)
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

        private static void ApplyMerges(OrderedWorkSettings settings)
        {
            // Pass 1: move giver ownership into each target work type.
            foreach (MergeEntry merge in settings.merges)
            {
                if (merge == null || string.IsNullOrWhiteSpace(merge.targetWorkType))
                {
                    continue;
                }
                WorkTypeDef target = DefDatabase<WorkTypeDef>.GetNamedSilentFail(merge.targetWorkType);
                if (target == null)
                {
                    continue;
                }
                foreach (string giverName in merge.givers)
                {
                    WorkGiverDef giver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(giverName);
                    if (giver != null)
                    {
                        giver.workType = target;
                    }
                }
            }

            // Pass 2: honor a saved target order. Existing target givers stay first, then the
            // incoming givers in their editor order. Old saves have no targetOrder and keep
            // the previous behavior (ApplyOrder decides the final order).
            List<string> targets = settings.merges
                .Where(m => m != null && !string.IsNullOrWhiteSpace(m.targetWorkType))
                .Select(m => m.targetWorkType)
                .Distinct()
                .ToList();

            foreach (string targetName in targets)
            {
                WorkTypeDef target = DefDatabase<WorkTypeDef>.GetNamedSilentFail(targetName);
                if (target == null)
                {
                    continue;
                }

                List<WorkGiverDef> targetGivers = DefDatabase<WorkGiverDef>.AllDefsListForReading
                    .Where(g => g != null && g.workType == target)
                    .ToList();
                if (targetGivers.Count == 0)
                {
                    continue;
                }

                List<WorkGiverDef> ordered = new List<WorkGiverDef>();
                bool hasExplicitOrder = false;
                foreach (MergeEntry merge in settings.merges)
                {
                    if (merge == null || merge.targetWorkType != targetName || merge.targetOrder == null || merge.targetOrder.Count == 0)
                    {
                        continue;
                    }
                    hasExplicitOrder = true;
                    foreach (string giverName in merge.targetOrder)
                    {
                        WorkGiverDef giver = targetGivers.FirstOrDefault(g => g.defName == giverName);
                        if (giver != null && !ordered.Contains(giver))
                        {
                            ordered.Add(giver);
                        }
                    }
                }

                if (!hasExplicitOrder)
                {
                    continue;
                }

                foreach (WorkGiverDef giver in targetGivers.OrderByDescending(g => g.priorityInType))
                {
                    if (!ordered.Contains(giver))
                    {
                        ordered.Add(giver);
                    }
                }
                for (int i = 0; i < ordered.Count; i++)
                {
                    ordered[i].priorityInType = 10000 - i * 10;
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
                foreach (GiverOrderEntry entry in settings.giverOrder.Where(e => e.workType == workType.defName).OrderBy(e => e.order))
                {
                    WorkGiverDef giver = givers.FirstOrDefault(g => g.defName == entry.giver);
                    if (giver != null && !sorted.Contains(giver))
                    {
                        sorted.Add(giver);
                    }
                }
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
