using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace OrderedWork.Patches
{
    /// <summary>
    /// PawnTable copies the column list when it recaches. Reapply the profile order to
    /// that copy, not only to PawnTableDefOf.Work.columns.
    /// </summary>
    [HarmonyPatch(typeof(PawnTable), "RecacheColumns")]
    internal static class Patch_PawnTable_WorkColumns
    {
        private static void Postfix(PawnTable __instance)
        {
            if (__instance == null)
            {
                return;
            }
            PawnTableDef definition = Traverse.Create(__instance).Field("def").GetValue<PawnTableDef>();
            if (definition != PawnTableDefOf.Work)
            {
                return;
            }
            List<PawnColumnDef> columns = Traverse.Create(__instance)
                .Field("columns")
                .GetValue<List<PawnColumnDef>>();
            OrderedWorkStartup.ReorderWorkTableColumns(columns);
        }
    }
}
namespace OrderedWork.Patches
{
    [HarmonyPatch(typeof(PawnTable), "get_Columns")]
    internal static class Patch_PawnTable_WorkColumnsGetter
    {
        private static void Postfix(PawnTable __instance, ref System.Collections.Generic.List<PawnColumnDef> __result)
        {
            if (__instance == null || __result == null)
            {
                return;
            }
            PawnTableDef definition = Traverse.Create(__instance).Field("def").GetValue<PawnTableDef>();
            if (definition == PawnTableDefOf.Work)
            {
                OrderedWorkStartup.ReorderWorkTableColumns(__result);
            }
        }
    }
}
namespace OrderedWork.Patches
{
    [HarmonyPatch(typeof(PawnTable), nameof(PawnTable.PawnTableOnGUI))]
    internal static class Patch_PawnTable_WorkColumnsBeforeGUI
    {
        private static void Prefix(PawnTable __instance)
        {
            if (__instance == null)
            {
                return;
            }
            PawnTableDef definition = Traverse.Create(__instance).Field("def").GetValue<PawnTableDef>();
            if (definition == PawnTableDefOf.Work)
            {
                OrderedWorkStartup.RefreshWorkTabColumnOrder();
            }
        }
    }
}