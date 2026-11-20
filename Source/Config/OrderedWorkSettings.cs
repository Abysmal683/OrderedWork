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
        public int logCategories;public bool skillFilterLevel = true;
        public bool skillFilterPassion = true;
        public bool skillFilterExpertise;
        public bool skillFilterMods = true;
        public List<string> recommendationPins = new List<string> { "Patient", "PatientBedRest", "Firefighter" };
        public List<PassionWeightEntry> passionWeights = new List<PassionWeightEntry>();
        public int passionDefaultsVersion;
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
            Scribe_Values.Look(ref logCategories, "logCategories", 0);Scribe_Values.Look(ref skillFilterLevel, "skillFilterLevel", true);
            Scribe_Values.Look(ref skillFilterPassion, "skillFilterPassion", true);
            Scribe_Values.Look(ref skillFilterExpertise, "skillFilterExpertise", false);
            Scribe_Values.Look(ref skillFilterMods, "skillFilterMods", true);
            Scribe_Collections.Look(ref recommendationPins, "recommendationPins", LookMode.Value);
            Scribe_Collections.Look(ref passionWeights, "passionWeights", LookMode.Deep);
            Scribe_Values.Look(ref passionDefaultsVersion, "passionDefaultsVersion", 0);
            Scribe_Collections.Look(ref workTypeColors, "workTypeColors", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (verboseLogging && logCategories == 0)
                {
                    logCategories = (int)OrderedWorkLogCategory.All;
                }
                if (recommendationPins == null) recommendationPins = new List<string>();
                if (passionWeights == null) passionWeights = new List<PassionWeightEntry>();
                if (workTypeColors == null) workTypeColors = new List<WorkTypeColorEntry>();
            }
        }
    }

    public class PassionWeightEntry : IExposable
    {
        public string defName;
        public int level0Score;
        public int perLevelBonus;
        public int minLevel;
        public string level0Buffer;
        public string perLevelBuffer;
        public string minLevelBuffer;

        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref level0Score, "level0Score", 0);
            Scribe_Values.Look(ref perLevelBonus, "perLevelBonus", 0);
            Scribe_Values.Look(ref minLevel, "minLevel", 0);
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
