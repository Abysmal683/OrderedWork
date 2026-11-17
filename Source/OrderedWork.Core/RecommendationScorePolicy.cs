namespace OrderedWork.Core
{
    /// <summary>
    /// Pure recommendation score formula. It deliberately knows nothing about Pawn,
    /// RimWorld defs or UI so it can be tested without loading the game.
    /// </summary>
    public static class RecommendationScorePolicy
    {
        public static int Score(
            int level,
            bool hasPassion,
            int level0Score,
            int perLevelBonus,
            int minLevel,
            bool prioritizeLowSkillPassion)
        {
            if (level < 0)
            {
                return 0;
            }
            if (level == 0 && !prioritizeLowSkillPassion)
            {
                return -3;
            }
            if (!hasPassion || level < minLevel)
            {
                return level == 0 ? -3 : level * 2;
            }
            return level == 0
                ? level0Score
                : level * 2 + perLevelBonus;
        }
    }
}
