using RimWorld;
using Verse;

namespace OrderedWork.Services
{
    /// <summary>
    /// Pure recommendation scoring for a pawn and a WorkType. Kept separate from
    /// order persistence, applying and UI so the policy can be changed in one place.
    /// </summary>
    public static class RecommendationScorer
    {
        public static int Score(Pawn pawn, WorkTypeDef workType, OrderedWorkSettings settings)
        {
            int level = BestSkillLevel(pawn, workType, out Passion passion);
            if (level < 0)
            {
                return 0;
            }

            bool levelZeroPassion = settings == null || settings.prioritizeLowSkillPassion;
            if (level == 0 && !levelZeroPassion)
            {
                return -3;
            }

            PassionWeightEntry entry = PassionWeightPolicy.Find(settings, passion);
            if (passion == Passion.None || entry == null || level < entry.minLevel)
            {
                return level == 0 ? -3 : level * 2;
            }
            return level == 0
                ? entry.level0Score
                : level * 2 + entry.perLevelBonus;
        }

        public static int BestSkillLevel(Pawn pawn, WorkTypeDef workType, out Passion passion)
        {
            passion = Passion.None;
            if (pawn == null || pawn.skills == null || workType == null || workType.relevantSkills == null)
            {
                return -1;
            }

            int best = -1;
            Passion bestPassion = Passion.None;
            foreach (SkillDef skill in workType.relevantSkills)
            {
                if (skill == null)
                {
                    continue;
                }
                SkillRecord record = pawn.skills.GetSkill(skill);
                if (record == null)
                {
                    continue;
                }
                if (record.Level > best)
                {
                    best = record.Level;
                    bestPassion = record.passion;
                }
                else if (record.Level == best && PassionRank(record.passion) > PassionRank(bestPassion))
                {
                    bestPassion = record.passion;
                }
            }
            passion = bestPassion;
            return best;
        }

        private static int PassionRank(Passion passion)
        {
            if (passion == Passion.Major)
            {
                return 2;
            }
            if (passion == Passion.Minor)
            {
                return 1;
            }
            return 0;
        }
    }
}
