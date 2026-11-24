using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Core;
using OrderedWork.Config;
using OrderedWork.Data;
using RimWorld;
using Verse;
using Verse.AI;

namespace OrderedWork.Services
{
    internal static class EquipmentExecutionService
    {
        public static void EquipNow(
            Pawn pawn,
            PawnWorkOrder? entry,
            EquipmentCatalogSnapshot catalog,
            List<EquipmentSet> sets)
        {
            if (pawn == null || entry == null || pawn.Map == null || pawn.jobs == null
                || OrderedWorkMod.Settings == null || !OrderedWorkMod.Settings.equipmentEnabled)
            {
                return;
            }

            try
            {
                EquipmentPlan plan = BuildPlan(pawn, entry, catalog, sets);
                int queued = 0;
                bool first = true;
                foreach (EquipmentSelection selection in plan.Selections)
                {
                    if (selection.Source == EquipmentSelectionSource.Forced ||
                        selection.Source == EquipmentSelectionSource.Principal ||
                        selection.Source == EquipmentSelectionSource.WorkType ||
                        selection.Source == EquipmentSelectionSource.Mandatory ||
                        selection.Source == EquipmentSelectionSource.Emergency)
                    {
                        if (pawn.apparel.WornApparel.Any(value => value.def.defName == selection.DefName))
                        {
                            continue;
                        }
                        Apparel? apparel = FindAvailableApparel(pawn, selection.DefName);
                        if (apparel == null)
                        {
                            continue;
                        }
                        Job job = JobMaker.MakeJob(JobDefOf.Wear, apparel);
                        Queue(pawn, job, ref first);
                        queued++;
                    }
                }

                if (!string.IsNullOrWhiteSpace(plan.PrimaryEquipmentDefName)
                    && (pawn.equipment?.Primary == null
                        || pawn.equipment.Primary.def.defName != plan.PrimaryEquipmentDefName))
                {
                    ThingWithComps? equipment = FindAvailableEquipment(pawn, plan.PrimaryEquipmentDefName);
                    if (equipment != null)
                    {
                        Job job = JobMaker.MakeJob(JobDefOf.Equip, equipment);
                        Queue(pawn, job, ref first);
                        queued++;
                    }
                }

                OrderedWorkMod.DebugLog("Equip now for " + pawn.LabelShort + ": queued " + queued
                    + " job(s), conflicts " + plan.Conflicts.Count + ".", OrderedWorkLogCategory.Equipment);
            }
            catch (Exception exception)
            {
                Log.ErrorOnce("[Ordered Work] Equip now failed: " + exception, 817237);
            }
        }

        public static EquipmentPlan BuildPlan(
            Pawn pawn,
            PawnWorkOrder entry,
            EquipmentCatalogSnapshot catalog,
            List<EquipmentSet> sets)
        {
            Dictionary<string, EquipmentSet> byId = sets.ToDictionary(value => value.Id, StringComparer.Ordinal);
            List<EquipmentSet> active = new List<EquipmentSet>();
            AddAssigned(entry, EquipmentSetRole.Mandatory, byId, active);
            AddAssigned(entry, EquipmentSetRole.Emergency, byId, active);
            AddAssigned(entry, EquipmentSetRole.Principal, byId, active);

            WorkTypeDef? activeWork = ActiveWorkService.GetActiveWorkType(pawn);
            string? workTypeDefName = activeWork?.defName;
            WorkConfigProfile? profile = WorkConfigStore.LoadCachedActive().Profile;
            if (workTypeDefName != null && profile != null
                && profile.WorkTypes.TryGetValue(workTypeDefName, out WorkTypeConfig? config)
                && config != null && !string.IsNullOrWhiteSpace(config.EquipmentSetId)
                && byId.TryGetValue(config.EquipmentSetId, out EquipmentSet? workSet))
            {
                active.Add(workSet);
            }

            EquipmentOverrides overrides = BuildOverrides(entry);
            EquipmentResolutionContext context = new EquipmentResolutionContext(
                catalog.Footprints,
                catalog.AllowedApparel,
                active,
                workTypeDefName,
                pawn.Drafted,
                overrides);
            return EquipmentResolver.Resolve(context);
        }

        private static void AddAssigned(
            PawnWorkOrder entry,
            EquipmentSetRole role,
            Dictionary<string, EquipmentSet> byId,
            List<EquipmentSet> active)
        {
            List<string>? ids = role switch
            {
                EquipmentSetRole.Mandatory => entry.mandatoryEquipmentSetIds,
                EquipmentSetRole.Emergency => entry.emergencyEquipmentSetIds,
                EquipmentSetRole.Principal => entry.principalEquipmentSetIds,
                _ => null
            };
            if (ids == null) return;
            foreach (string id in ids)
            {
                if (byId.TryGetValue(id, out EquipmentSet? set) && set != null)
                {
                    active.Add(set);
                }
            }
        }

        private static EquipmentOverrides BuildOverrides(PawnWorkOrder entry)
        {
            EquipmentOverrides result = new EquipmentOverrides();
            foreach (PawnEquipmentOverride value in entry.equipmentOverrides)
            {
                if (value == null) continue;
                switch (value.kind)
                {
                    case "forced_apparel":
                        result.ForcedApparel[new ApparelSlotKey(value.bodyDefName, value.bodyPartGroupDefName, value.apparelLayerDefName)] = value.defName;
                        break;
                    case "primary_equipment":
                        result.PrimaryEquipmentDefName = value.defName;
                        break;
                    case "inventory":
                        result.Inventory[value.defName] = value.forbidden ? -1 : value.count;
                        break;
                }
            }
            return result;
        }

        private static Apparel? FindAvailableApparel(Pawn pawn, string defName)
        {
            ThingDef? def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null || pawn.Map == null) return null;
            return pawn.Map.listerThings.ThingsOfDef(def)
                .OfType<Apparel>()
                .Where(value => value.Spawned && !value.IsForbidden(pawn))
                .Where(value => pawn.CanReserve(value))
                .Where(value => pawn.CanReach(value, PathEndMode.ClosestTouch, Danger.Deadly))
                .Where(value => InSearchArea(pawn, value))
                .OrderBy(value => value.Position.DistanceToSquared(pawn.Position))
                .FirstOrDefault();
        }

        private static ThingWithComps? FindAvailableEquipment(Pawn pawn, string defName)
        {
            ThingDef? def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null || pawn.Map == null) return null;
            return pawn.Map.listerThings.ThingsOfDef(def)
                .OfType<ThingWithComps>()
                .Where(value => value.Spawned && !value.IsForbidden(pawn))
                .Where(value => pawn.CanReserve(value))
                .Where(value => pawn.CanReach(value, PathEndMode.ClosestTouch, Danger.Deadly))
                .Where(value => InSearchArea(pawn, value))
                .OrderBy(value => value.Position.DistanceToSquared(pawn.Position))
                .FirstOrDefault();
        }

        private static bool InSearchArea(Pawn pawn, Thing thing)
        {
            if (OrderedWorkMod.Settings == null || !OrderedWorkMod.Settings.equipmentSearchAreaEnabled)
            {
                return true;
            }
            IntVec3 center = SearchCenter(pawn);
            return thing.Position.DistanceTo(center) <= OrderedWorkMod.Settings.equipmentSearchRadius;
        }

        private static IntVec3 SearchCenter(Pawn pawn)
        {
            Thing? target = pawn.CurJob?.targetA.Thing ?? pawn.CurJob?.targetB.Thing;
            return target is Building_WorkTable ? target.Position : pawn.Position;
        }
        private static void Queue(Pawn pawn, Job job, ref bool first)
        {
            if (first)
            {
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                first = false;
            }
            else
            {
                pawn.jobs.jobQueue.EnqueueLast(job, JobTag.Misc);
            }
        }
    }
}
