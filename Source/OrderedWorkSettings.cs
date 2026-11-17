using System.Collections.Generic;
using Verse;

namespace OrderedWork
{
    public class OrderedWorkSettings : ModSettings
    {
        public List<string> workTypeOrder = new List<string>();
        public List<GiverOrderEntry> giverOrder = new List<GiverOrderEntry>();
        public List<SplitEntry> splits = new List<SplitEntry>();
        public List<MergeEntry> merges = new List<MergeEntry>();
        public bool initialized;
        public bool autoRecommendNewColony = true;
        public bool autoHideScrollbars = true;
        public bool paletteStressTest;
        public float workControlHeight;
        public List<WorkTypeColorEntry> workTypeColors = new List<WorkTypeColorEntry>();

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref workTypeOrder, "workTypeOrder", LookMode.Value);
            Scribe_Collections.Look(ref giverOrder, "giverOrder", LookMode.Deep);
            Scribe_Collections.Look(ref splits, "splits", LookMode.Deep);
            Scribe_Collections.Look(ref merges, "merges", LookMode.Deep);
            Scribe_Values.Look(ref initialized, "initialized", false);
            Scribe_Values.Look(ref autoRecommendNewColony, "autoRecommendNewColony", true);
            Scribe_Values.Look(ref autoHideScrollbars, "autoHideScrollbars", true);
            Scribe_Values.Look(ref paletteStressTest, "paletteStressTest", false);
            Scribe_Values.Look(ref workControlHeight, "workControlHeight", 0f);
            Scribe_Collections.Look(ref workTypeColors, "workTypeColors", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (workTypeOrder == null) workTypeOrder = new List<string>();
                if (giverOrder == null) giverOrder = new List<GiverOrderEntry>();
                if (splits == null) splits = new List<SplitEntry>();
                if (merges == null) merges = new List<MergeEntry>();
                if (workTypeColors == null) workTypeColors = new List<WorkTypeColorEntry>();
            }
        }
    }

    public class GiverOrderEntry : IExposable
    {
        public string workType;
        public string giver;
        public int order;

        public void ExposeData()
        {
            Scribe_Values.Look(ref workType, "workType");
            Scribe_Values.Look(ref giver, "giver");
            Scribe_Values.Look(ref order, "order", 0);
        }
    }

    public class SplitEntry : IExposable
    {
        public string newDefName;
        public string label;
        public string baseWorkType;
        public List<string> givers = new List<string>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref newDefName, "newDefName");
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref baseWorkType, "baseWorkType");
            Scribe_Collections.Look(ref givers, "givers", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars && givers == null) givers = new List<string>();
        }
    }

    public class MergeEntry : IExposable
    {
        public List<string> givers = new List<string>();
        public string targetWorkType;
        // Final destination order after applying the merge: existing target givers first
        // (fixed), then the incoming givers in the order chosen in the editor. Empty for
        // old saves, which keeps the previous behavior.
        public List<string> targetOrder = new List<string>();

        public void ExposeData()
        {
            Scribe_Collections.Look(ref givers, "givers", LookMode.Value);
            Scribe_Values.Look(ref targetWorkType, "targetWorkType");
            Scribe_Collections.Look(ref targetOrder, "targetOrder", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars && givers == null) givers = new List<string>();
            if (Scribe.mode == LoadSaveMode.LoadingVars && targetOrder == null) targetOrder = new List<string>();
        }
    }

    public class WorkTypeColorEntry : IExposable
    {
        public string defName;
        public float r;
        public float g;
        public float b;

        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref r, "r");
            Scribe_Values.Look(ref g, "g");
            Scribe_Values.Look(ref b, "b");
        }
    }
}
