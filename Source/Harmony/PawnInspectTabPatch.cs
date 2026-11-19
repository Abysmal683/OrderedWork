using System.Collections.Generic;
using HarmonyLib;
using OrderedWork.UI;
using Verse;

namespace OrderedWork.Patches
{
    /// <summary>
    /// Adds the Perfil tab to the pawn inspect pane. Only a postfix appends the
    /// tab; vanilla tabs and other mods keep their original order.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetInspectTabs))]
    internal static class Patch_Pawn_GetInspectTabs
    {
        private static readonly ITab_Pawn_Profile ProfileTab = new ITab_Pawn_Profile();

        [HarmonyPostfix]
        private static IEnumerable<InspectTabBase> Postfix(IEnumerable<InspectTabBase> __result)
        {
            if (__result != null)
            {
                foreach (InspectTabBase tab in __result)
                {
                    yield return tab;
                }
            }
            yield return ProfileTab;
        }
    }
}
