#nullable disable
using System.Linq;
using HarmonyLib;
using OrderedWork.Services;
using RimWorld;
using Verse;

namespace OrderedWork.Patches
{
    [HarmonyPatch(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.GetPriority))]
    internal static class Patch_WorkSettings_GetPriority_Hidden
    {
        private static void Postfix(Pawn_WorkSettings __instance, WorkTypeDef w, ref int __result)
        {
            int rank;
            if (!HiddenWorkTypeService.TryGetRank(w, out rank))
            {
                return;
            }
            Pawn pawn = WorkOrderService.GetOwningPawn(__instance);
            __result = pawn != null && WorkOrderService.IsIncapable(pawn, w) ? 0 : rank;
        }
    }

    [HarmonyPatch(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.Notify_DisabledWorkTypesChanged))]
    internal static class Patch_WorkSettings_Disabled_Hidden
    {
        private static void Postfix(Pawn_WorkSettings __instance)
        {
            if (!WorkOrderService.IsApplying)
            {
                HiddenWorkTypeService.RefreshPawn(WorkOrderService.GetOwningPawn(__instance));
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_WorkSettings), "CacheWorkGiversInOrder")]
    internal static class Patch_WorkSettings_CacheHidden
    {
        private static void Postfix(Pawn_WorkSettings __instance)
        {
            if (!WorkOrderService.IsApplying)
            {
                Reorder(__instance.WorkGiversInOrderNormal);
                Reorder(__instance.WorkGiversInOrderEmergency);
            }
        }

        private static void Reorder(System.Collections.Generic.List<WorkGiver> givers)
        {
            if (givers == null || givers.Count == 0)
            {
                return;
            }
            System.Collections.Generic.List<WorkGiver> hidden = givers
                .Where(giver => giver != null && giver.def != null
                    && HiddenWorkTypeService.IsHidden(giver.def.workType))
                .OrderBy(giver =>
                {
                    int rank;
                    return HiddenWorkTypeService.TryGetRank(giver.def.workType, out rank) ? rank : int.MaxValue;
                })
                .ToList();
            if (hidden.Count == 0)
            {
                return;
            }
            givers.RemoveAll(giver => giver == null || giver.def == null
                || HiddenWorkTypeService.IsHidden(giver.def.workType));
            givers.InsertRange(0, hidden);
        }
    }
}