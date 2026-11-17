using System.Collections.Generic;
using Verse;

namespace OrderedWork
{
    public class OrderedWorkSettings : ModSettings
    {
        public List<string> workTypeOrder = new List<string>();
        public List<GiverOrderEntry> giverOrder = new List<GiverOrderEntry>();
        // Captured before the first apply so Order -> Reset can restore def priorities.
        public List<GiverOrderEntry> originalGiverOrder = new List<GiverOrderEntry>();
        public List<SplitEntry> splits = new List<SplitEntry>();
        public bool initialized;
        public bool autoRecommendNewColony = true;
        public bool autoHideScrollbars = true;
        public bool paletteStressTest;
        public bool keepDestroyedPawnOrders;
        public bool allowEmptyOrigin;
        public float workControlHeight;
        public List<WorkTypeColorEntry> workTypeColors = new List<WorkTypeColorEntry>();

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref workTypeOrder, "workTypeOrder", LookMode.Value);
            Scribe_Collections.Look(ref giverOrder, "giverOrder", LookMode.Deep);
            Scribe_Collections.Look(ref originalGiverOrder, "originalGiverOrder", LookMode.Deep);
            Scribe_Collections.Look(ref splits, "splits", LookMode.Deep);
            Scribe_Values.Look(ref initialized, "initialized", false);
            Scribe_Values.Look(ref autoRecommendNewColony, "autoRecommendNewColony", true);
            Scribe_Values.Look(ref autoHideScrollbars, "autoHideScrollbars", true);
            Scribe_Values.Look(ref paletteStressTest, "paletteStressTest", false);
            Scribe_Values.Look(ref keepDestroyedPawnOrders, "keepDestroyedPawnOrders", false);
            Scribe_Values.Look(ref allowEmptyOrigin, "allowEmptyOrigin", false);
            Scribe_Values.Look(ref workControlHeight, "workControlHeight", 0f);
            Scribe_Collections.Look(ref workTypeColors, "workTypeColors", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (workTypeOrder == null) workTypeOrder = new List<string>();
                if (giverOrder == null) giverOrder = new List<GiverOrderEntry>();
                if (originalGiverOrder == null) originalGiverOrder = new List<GiverOrderEntry>();
                if (splits == null) splits = new List<SplitEntry>();
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
        public string description;
        public string baseWorkType;
        public List<string> givers = new List<string>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref newDefName, "newDefName");
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref description, "description");
            Scribe_Values.Look(ref baseWorkType, "baseWorkType");
            Scribe_Collections.Look(ref givers, "givers", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars && givers == null) givers = new List<string>();
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
