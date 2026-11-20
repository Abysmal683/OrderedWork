using OrderedWork.Config;
using RimWorld;
using Verse;

namespace OrderedWork
{
    internal sealed partial class SplitMergePanel
    {
        private void RevertMerge(string defName)
        {
            WorkConfigProfile? profile = host.LoadEditingProfile();
            WorkConfigProfile? reference = WorkConfigStore.LoadReference();
            if (profile == null || reference == null || !WorkConfigReconciler.RevertMerge(profile, reference, defName))
            {
                return;
            }
            host.CommitProfile(profile);
            Messages.Message(string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.MergeReverted",
                "Merge reverted for {0}."), defName), MessageTypeDefOf.TaskCompletion, false);
            mergePendingRevert = "";
        }

        private void RevertOrder(string defName)
        {
            WorkConfigProfile? profile = host.LoadEditingProfile();
            WorkConfigProfile? reference = WorkConfigStore.LoadReference();
            if (profile == null || reference == null || !WorkConfigReconciler.RevertOrder(profile, reference, defName))
            {
                return;
            }
            host.CommitProfile(profile);
            Messages.Message(string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.OrderReverted",
                "Giver order restored for {0}."), defName), MessageTypeDefOf.TaskCompletion, false);
            orderPendingRevert = "";
        }
    }
}