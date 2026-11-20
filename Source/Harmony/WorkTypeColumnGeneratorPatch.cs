using HarmonyLib;
using OrderedWork.Config;
using RimWorld;

namespace OrderedWork.Patches
{
    /// <summary>
    /// Column definitions are generated from WorkType natural priority during def load.
    /// Apply the profile order before that generation, matching XML-driven work-tab mods.
    /// </summary>
    [HarmonyPatch(typeof(PawnColumnDefGenerator), nameof(PawnColumnDefGenerator.ImpliedPawnColumnDefs))]
    internal static class Patch_PawnColumnDefGenerator_Order
    {
        private static bool applied;

        private static void Prefix()
        {
            if (applied)
            {
                return;
            }
            WorkConfigProfile profile = WorkConfigStore.LoadCachedActive()?.Profile;
            if (profile == null)
            {
                return;
            }
            applied = true;
            OrderedWorkStartup.ApplyWorkTypeNaturalPriority(profile);
        }
    }
}