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

        private static void ApplySplits(OrderedWorkSettings settings, OrderedWorkMod mod)
        {
            foreach (SplitEntry split in settings.splits)
            {
                if (split == null || string.IsNullOrWhiteSpace(split.newDefName) || string.IsNullOrWhiteSpace(split.baseWorkType))
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
                    description = "Split from " + baseDef.labelShort,
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
