using HarmonyLib;
using RimWorld;
using Verse;

namespace OrderedWork.Patches
{
    /// <summary>
    /// Reapplies the profile WorkType column order at the point the Work tab draws.
    /// PawnColumnDefGenerator owns the original list; the profile owns the final order.
    /// </summary>
    [HarmonyPatch(typeof(MainTabWindow_Work), nameof(MainTabWindow_Work.DoWindowContents))]
    internal static class Patch_WorkTab_ColumnOrder
    {
        private static void Prefix()
        {
            OrderedWorkStartup.RefreshWorkTabColumnOrder();
        }
    }
}