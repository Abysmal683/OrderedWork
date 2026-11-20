using OrderedWork.Config;
using RimWorld;
using Verse;

namespace OrderedWork
{
    public partial class OrderedWorkMod : Mod
    {
        private void ResetSplitMetadataState()
        {
            splitMerge.ResetMetadata();
        }
    }
}