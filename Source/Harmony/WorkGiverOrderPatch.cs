using System;
using System.Collections.Generic;
using HarmonyLib;
using OrderedWork.Services;
using RimWorld;
using Verse;

namespace OrderedWork.Patches
{
    /// <summary>
    /// Per-pawn WorkGiver order postfixes. Reorder the runtime list without changing
    /// any WorkGiverDef or the global def order.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_WorkSettings))]
    internal static class WorkGiverOrderPatch
    {
        private static readonly AccessTools.FieldRef<Pawn_WorkSettings, Pawn> PawnField =
            AccessTools.FieldRefAccess<Pawn_WorkSettings, Pawn>("pawn");

        [HarmonyPostfix]
        [HarmonyPatch("WorkGiversInOrderNormal", MethodType.Getter)]
        private static void NormalPostfix(Pawn_WorkSettings __instance, ref List<WorkGiver> __result)
        {
            SafeApply(__instance, ref __result, false);
        }

        [HarmonyPostfix]
        [HarmonyPatch("WorkGiversInOrderEmergency", MethodType.Getter)]
        private static void EmergencyPostfix(Pawn_WorkSettings __instance, ref List<WorkGiver> __result)
        {
            SafeApply(__instance, ref __result, true);
        }

        private static void SafeApply(Pawn_WorkSettings instance, ref List<WorkGiver> result, bool emergency)
        {
            try
            {
                Pawn pawn = PawnField(instance);
                List<WorkGiver> reordered;
                if (WorkGiverOrderService.TryReorder(pawn, result, out reordered))
                {
                    result = reordered;
                }
            }
            catch (Exception exception)
            {
                Log.Warning("[Ordered Work] WorkGiver ordering failed: " + exception.Message);
            }
        }
    }
}
