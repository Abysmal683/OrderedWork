using System;
using HarmonyLib;
using OrderedWork.Services;
using RimWorld;
using Verse;

namespace OrderedWork.Patches
{
    /// <summary>
    /// Applies the Harmony patches that capture vanilla Work-tab edits and reapply orders
    /// after a pawn spawns/resurrects.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class OrderedWorkHarmony
    {
        static OrderedWorkHarmony()
        {
            try
            {
                HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("nightmare123487.OrderedWork");
                harmony.PatchAll(typeof(OrderedWorkHarmony).Assembly);
                Log.Message("[Ordered Work] Harmony patches applied (manual Work-tab priority above 4 enabled).");
            }
            catch (Exception ex)
            {
                Log.Error("[Ordered Work] Harmony initialization failed: " + ex);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.EnableAndInitialize))]
    internal static class Patch_PawnWorkSettings_EnableAndInitialize
    {
        private static void Prefix(out bool __state)
        {
            __state = WorkOrderService.IsInitializing;
            WorkOrderService.IsInitializing = true;
        }

        private static void Postfix(bool __state)
        {
            WorkOrderService.IsInitializing = __state;
        }
    }

    [HarmonyPatch(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.EnableAndInitializeIfNotAlreadyInitialized))]
    internal static class Patch_PawnWorkSettings_EnableAndInitializeIfNeeded
    {
        private static void Prefix(out bool __state)
        {
            __state = WorkOrderService.IsInitializing;
            WorkOrderService.IsInitializing = true;
        }

        private static void Postfix(bool __state)
        {
            WorkOrderService.IsInitializing = __state;
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    internal static class Patch_Pawn_SpawnSetup
    {
        private static void Prefix(out bool __state)
        {
            __state = WorkOrderService.IsSpawning;
            WorkOrderService.IsSpawning = true;
        }

        private static void Postfix(Pawn __instance, bool respawningAfterLoad, bool __state)
        {
            WorkOrderService.IsSpawning = __state;
            WorkOrderService.OnPawnSpawned(__instance, respawningAfterLoad);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SetFaction))]
    internal static class Patch_Pawn_SetFaction
    {
        private static void Prefix(out bool __state)
        {
            __state = WorkOrderService.IsSpawning;
            WorkOrderService.IsSpawning = true;
        }

        private static void Postfix(Pawn __instance, bool __state)
        {
            WorkOrderService.IsSpawning = __state;
            WorkOrderService.OnPawnFactionChanged(__instance);
        }
    }
}
