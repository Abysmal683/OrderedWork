using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using OrderedWork.UI;
using Verse;

namespace OrderedWork.Patches
{
    /// <summary>
    /// Adds the Perfil tab to the pawn inspect pane. Only a postfix appends the
    /// tab; vanilla tabs and other mods keep their original order.
    /// </summary>
    [HarmonyPatch(typeof(Thing), nameof(Thing.GetInspectTabs))]
    internal static class Patch_Pawn_GetInspectTabs
    {
        private static readonly ITab_Pawn_Profile ProfileTab = new ITab_Pawn_Profile();

        [HarmonyPostfix]
        private static void Postfix(Thing __instance, ref IEnumerable<InspectTabBase> __result)
        {
            if (__instance is Pawn pawn && pawn.IsFreeColonist && !pawn.Dead)
            {
                IEnumerable<InspectTabBase> original = __result ?? Enumerable.Empty<InspectTabBase>();
                __result = original.Concat(new[] { ProfileTab });
            }
        }
    }
}
