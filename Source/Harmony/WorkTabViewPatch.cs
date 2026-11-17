using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.Patches
{
    /// <summary>
    /// Adds a "View: Draw" return button to the vanilla Work tab so the player can switch
    /// back to the Work Control lane from the Numeric view.
    /// </summary>
    [HarmonyPatch(typeof(MainTabWindow_Work), nameof(MainTabWindow_Work.DoWindowContents))]
    internal static class Patch_WorkTab_ViewDrawButton
    {
        private static void Postfix(Rect rect)
        {
            MainButtonDef workControl = DefDatabase<MainButtonDef>.GetNamedSilentFail("WorkControl");
            if (workControl == null)
            {
                return;
            }

            Rect buttonRect = new Rect(rect.xMax - 134f, rect.y + 6f, 124f, 28f);
            if (Widgets.ButtonText(buttonRect, "View: Draw"))
            {
                Find.MainTabsRoot.ToggleTab(workControl, true);
            }
            if (Mouse.IsOver(buttonRect))
            {
                TooltipHandler.TipRegion(buttonRect, "Open Work Control (Draw view)");
            }
        }
    }
}
