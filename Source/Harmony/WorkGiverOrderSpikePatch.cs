using System.Collections.Generic;
using HarmonyLib;
using OrderedWork.Services;
using RimWorld;
using Verse;

namespace OrderedWork.Patches
{
    /// <summary>
    /// Debug-only postfixes: reorder the vanilla WorkGiver list for the selected
    /// pawn without changing any WorkGiverDef or the global def order.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_WorkSettings))]
    internal static class WorkGiverOrderSpikePatch
    {
        private static readonly AccessTools.FieldRef<Pawn_WorkSettings, Pawn> PawnField =
            AccessTools.FieldRefAccess<Pawn_WorkSettings, Pawn>("pawn");

        [HarmonyPostfix]
        [HarmonyPatch("WorkGiversInOrderNormal", MethodType.Getter)]
        private static void NormalPostfix(Pawn_WorkSettings __instance, ref List<WorkGiver> __result)
        {
            Pawn pawn = PawnField(__instance);
            List<WorkGiver> reordered;
            if (WorkGiverOrderSpike.TryReorder(pawn, __result, false, out reordered)
                || WorkGiverOrderService.TryReorder(pawn, __result, out reordered))
            {
                __result = reordered;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("WorkGiversInOrderEmergency", MethodType.Getter)]
        private static void EmergencyPostfix(Pawn_WorkSettings __instance, ref List<WorkGiver> __result)
        {
            Pawn pawn = PawnField(__instance);
            List<WorkGiver> reordered;
            if (WorkGiverOrderSpike.TryReorder(pawn, __result, true, out reordered)
                || WorkGiverOrderService.TryReorder(pawn, __result, out reordered))
            {
                __result = reordered;
            }
        }
    }
}
