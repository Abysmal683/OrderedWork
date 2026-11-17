using System.Collections.Generic;
using Verse;

namespace OrderedWork
{
    public class OrderedWorkSettings : ModSettings
    {
        public bool autoRecommendNewColony = true;
        public bool autoHideScrollbars = true;
        public bool paletteStressTest;
        public bool keepDestroyedPawnOrders;
        public bool allowEmptyOrigin;
        public float workControlHeight;
        public bool prioritizeLowSkillPassion = true;
        public bool verboseLogging;
        public List<string> recommendationPins = new List<string> { "Patient", "PatientBedRest", "Firefighter" };
        public List<WorkTypeColorEntry> workTypeColors = new List<WorkTypeColorEntry>();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref autoRecommendNewColony, "autoRecommendNewColony", true);
            Scribe_Values.Look(ref autoHideScrollbars, "autoHideScrollbars", true);
            Scribe_Values.Look(ref paletteStressTest, "paletteStressTest", false);
            Scribe_Values.Look(ref keepDestroyedPawnOrders, "keepDestroyedPawnOrders", false);
            Scribe_Values.Look(ref allowEmptyOrigin, "allowEmptyOrigin", false);
            Scribe_Values.Look(ref workControlHeight, "workControlHeight", 0f);
            Scribe_Values.Look(ref prioritizeLowSkillPassion, "prioritizeLowSkillPassion", true);
            Scribe_Values.Look(ref verboseLogging, "verboseLogging", false);
            Scribe_Collections.Look(ref recommendationPins, "recommendationPins", LookMode.Value);
            Scribe_Collections.Look(ref workTypeColors, "workTypeColors", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (recommendationPins == null) recommendationPins = new List<string>();
                if (workTypeColors == null) workTypeColors = new List<WorkTypeColorEntry>();
            }
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
