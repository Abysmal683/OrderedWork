using HarmonyLib;
using OrderedWork.Services;
using RimWorld;
using Verse;
using Verse.AI;

namespace OrderedWork.Harmony
{
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    internal static class EquipmentPreWorkPatch
    {
        internal static readonly AccessTools.FieldRef<Pawn_JobTracker, Pawn> PawnRef =
            AccessTools.FieldRefAccess<Pawn_JobTracker, Pawn>("pawn");

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(Pawn_JobTracker __instance, Job newJob, bool fromQueue)
        {
            if (__instance == null || newJob == null || fromQueue)
            {
                return true;
            }

            return !EquipmentPreWorkService.TryPrepare(PawnRef(__instance), newJob);
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.EndCurrentJob))]
    internal static class EquipmentDressCancellationPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Pawn_JobTracker __instance, out Job? __state)
        {
            __state = __instance?.curJob;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Pawn_JobTracker __instance, JobCondition condition, Job? __state)
        {
            Pawn? pawn = __instance != null ? EquipmentPreWorkPatch.PawnRef(__instance) : null;
            if (pawn == null || __state == null)
            {
                return;
            }

            bool interrupted = condition == JobCondition.InterruptForced
                || condition == JobCondition.InterruptOptional;
            if (interrupted && EquipmentDressTracker.IsTracked(pawn, __state))
            {
                int removed = EquipmentDressTracker.CancelQueued(pawn);
                OrderedWorkMod.DebugLog("Dress process cancelled for " + pawn.LabelShort
                    + ": removed " + removed + " queued dress job(s); original work remains queued.",
                    OrderedWorkLogCategory.Equipment);
                return;
            }

            EquipmentDressTracker.Unregister(pawn, __state);
        }
    }
}
