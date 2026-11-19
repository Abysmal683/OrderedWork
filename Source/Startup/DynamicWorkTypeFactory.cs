using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using OrderedWork.Config;
using OrderedWork.Data;
using RimWorld;
using Verse;

namespace OrderedWork.Startup
{
    public static class DynamicWorkTypeFactory
    {        public static void EnsureSplitWorkTypes(
            IEnumerable<KeyValuePair<string, WorkTypeConfig>> ordered,
            OrderedWorkMod mod)
        {
            foreach (KeyValuePair<string, WorkTypeConfig> entry in ordered)
            {
                WorkTypeConfig split = entry.Value;
                if (split == null || split.Vanilla || string.IsNullOrWhiteSpace(entry.Key)
                    || string.IsNullOrWhiteSpace(split.Base)
                    || split.Givers == null || split.Givers.Count == 0)
                {
                    continue;
                }
                if (DefDatabase<WorkTypeDef>.GetNamedSilentFail(entry.Key) != null)
                {
                    continue;
                }
                WorkTypeDef baseDef = DefDatabase<WorkTypeDef>.GetNamedSilentFail(split.Base);
                if (baseDef == null)
                {
                    continue;
                }

                string label = string.IsNullOrWhiteSpace(split.Label) ? entry.Key : split.Label;
                WorkTypeDef def = new WorkTypeDef
                {
                    defName = entry.Key,
                    labelShort = label,
                    pawnLabel = label,
                    gerundLabel = label,
                    description = string.IsNullOrWhiteSpace(split.Description)
                        ? "Split from " + baseDef.labelShort
                        : split.Description,
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

                foreach (GiverConfig giverConfig in split.Givers.Where(g => g != null))
                {
                    WorkGiverDef giver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(giverConfig.DefName);
                    if (giver != null)
                    {
                        giver.workType = def;
                    }
                }
                def.PostLoad();
                def.workGiversByPriority = DefDatabase<WorkGiverDef>.AllDefsListForReading
                    .Where(g => g != null && g.workType == def)
                    .OrderByDescending(g => g.priorityInType)
                    .ToList();
                EnsureWorkTabColumn(def);
            }
        }

        private static void EnsureWorkTabColumn(WorkTypeDef workType)
        {
            if (workType == null || PawnTableDefOf.Work == null || PawnTableDefOf.Work.columns == null)
            {
                return;
            }
            if (PawnTableDefOf.Work.columns.Any(c => c != null && c.workType == workType))
            {
                return;
            }
            try
            {
                Type worker = AccessTools.TypeByName("WorkTab.PawnColumnWorker_WorkType")
                    ?? typeof(PawnColumnWorker_WorkPriority);
                PawnColumnDef column = new PawnColumnDef
                {
                    defName = "WorkPriority_" + workType.defName,
                    workType = workType,
                    workerClass = worker,
                    sortable = true,
                    generated = true,
                    modContentPack = workType.modContentPack
                };
                column.PostLoad();
                DefDatabase<PawnColumnDef>.Add(column);
                int index = PawnTableDefOf.Work.columns.FindIndex(c => c != null && c.workType != null
                    && c.workType.naturalPriority < workType.naturalPriority);
                if (index < 0) index = PawnTableDefOf.Work.columns.Count;
                PawnTableDefOf.Work.columns.Insert(index, column);
                Type controller = AccessTools.TypeByName("WorkTab.Controller");
                if (controller != null)
                {
                    FieldInfo allColumns = controller.GetField("allColumns",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (allColumns != null)
                    {
                        allColumns.SetValue(null, new List<PawnColumnDef>(PawnTableDefOf.Work.columns));
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Warning("[Ordered Work] Could not add Work tab column for " + workType.defName
                    + ": " + exception.Message);
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
    }
}
