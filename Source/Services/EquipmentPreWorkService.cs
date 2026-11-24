using System;
using System.Collections.Generic;
using OrderedWork.Core;
using OrderedWork.Config;
using OrderedWork.Data;
using RimWorld;
using Verse;
using Verse.AI;

namespace OrderedWork.Services
{
    internal static class EquipmentPreWorkService
    {
        public static bool TryPrepare(Pawn? pawn, Job? newJob)
        {
            if (pawn == null || newJob?.def == null || newJob.workGiverDef == null
                || OrderedWorkMod.Settings == null || !OrderedWorkMod.Settings.equipmentEnabled
                || !pawn.IsColonistPlayerControlled || pawn.Downed || pawn.Drafted || pawn.InMentalState
                || pawn.jobs?.jobQueue == null || pawn.jobs.jobQueue.AnyPlayerForced)
            {
                return false;
            }

            if (!EquipmentPreWorkRules.ShouldPrepare(
                newJob.workGiverDef.defName,
                newJob.def.defName,
                newJob.playerForced,
                fromQueue: false))
            {
                return false;
            }

            if (EquipmentDressTracker.HasQueued(pawn))
            {
                return false;
            }

            try
            {
                PawnWorkOrder? entry = OrderedWorkGameComponent.GetOrCreate()?.GetEntry(pawn);
                if (entry == null)
                {
                    return false;
                }

                EquipmentCatalogSnapshot catalog = EquipmentCatalogBuilder.Build(pawn);
                List<EquipmentSet> sets = EquipmentSetStore.LoadCached();
                EquipmentPlan plan;
                List<Job> jobs = EquipmentExecutionService.BuildDressJobs(
                    pawn,
                    entry,
                    catalog,
                    sets,
                    newJob.workGiverDef.workType,
                    null,
                    false,
                    out plan);
                if (jobs.Count == 0)
                {
                    return false;
                }

                EquipmentDressTracker.Register(pawn, jobs);
                pawn.jobs.jobQueue.EnqueueLast(newJob, null);
                for (int index = jobs.Count - 1; index >= 0; index--)
                {
                    pawn.jobs.jobQueue.EnqueueFirst(jobs[index], JobTag.Misc);
                }

                OrderedWorkMod.DebugLog("Pre-work dressing for " + pawn.LabelShort + " before "
                    + newJob.workGiverDef.defName + ": " + jobs.Count + " dress job(s), "
                    + pawn.jobs.jobQueue.Count + " queued total.", OrderedWorkLogCategory.Equipment);
                return true;
            }
            catch (Exception exception)
            {
                EquipmentDressTracker.CancelQueued(pawn);
                pawn.jobs?.jobQueue?.RemoveAll(pawn, job => ReferenceEquals(job, newJob));
                Log.ErrorOnce("[Ordered Work] Pre-work dressing failed: " + exception, 817239);
                return false;
            }
        }
    }
}
