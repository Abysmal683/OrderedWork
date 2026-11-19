using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace OrderedWork.Services
{
    /// <summary>
    /// Inverts StatDef skill metadata into WorkType stat lists. RimWorld does not
    /// declare stats on WorkTypeDef; skill-linked stats must be discovered.
    /// </summary>
    public static class StatIndex
    {
        private static readonly Dictionary<string, List<StatDef>> bySkill =
            new Dictionary<string, List<StatDef>>(StringComparer.Ordinal);
        private static readonly Dictionary<string, List<StatDef>> byWorkType =
            new Dictionary<string, List<StatDef>>(StringComparer.Ordinal);
        private static bool built;

        public static List<StatDef> GetFor(WorkTypeDef workType)
        {
            if (workType == null)
            {
                return new List<StatDef>();
            }
            Build();
            List<StatDef> cached;
            if (byWorkType.TryGetValue(workType.defName, out cached))
            {
                return new List<StatDef>(cached);
            }

            HashSet<StatDef> result = new HashSet<StatDef>();
            if (workType.relevantSkills != null)
            {
                foreach (SkillDef skill in workType.relevantSkills)
                {
                    List<StatDef> stats;
                    if (skill != null && bySkill.TryGetValue(skill.defName, out stats))
                    {
                        foreach (StatDef stat in stats)
                        {
                            result.Add(stat);
                        }
                    }
                }
            }
            List<StatDef> ordered = result
                .OrderByDescending(stat => stat.displayPriorityInCategory)
                .ThenBy(stat => stat.label)
                .ToList();
            byWorkType[workType.defName] = ordered;
            return new List<StatDef>(ordered);
        }

        public static void Rebuild()
        {
            built = false;
            bySkill.Clear();
            byWorkType.Clear();
        }

        private static void Build()
        {
            if (built)
            {
                return;
            }
            built = true;
            try
            {
                foreach (StatDef stat in DefDatabase<StatDef>.AllDefsListForReading)
                {
                    if (stat == null || stat.forInformationOnly || stat.alwaysHide)
                    {
                        continue;
                    }

                    HashSet<string> skills = new HashSet<string>(StringComparer.Ordinal);
                    if (stat.disableIfSkillDisabled != null)
                    {
                        skills.Add(stat.disableIfSkillDisabled.defName);
                    }
                    AddSkills(skills, stat.skillNeedFactors);
                    AddSkills(skills, stat.skillNeedOffsets);

                    foreach (string skill in skills)
                    {
                        List<StatDef> stats;
                        if (!bySkill.TryGetValue(skill, out stats))
                        {
                            stats = new List<StatDef>();
                            bySkill[skill] = stats;
                        }
                        if (!stats.Contains(stat))
                        {
                            stats.Add(stat);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Warning("[Ordered Work] StatIndex discovery failed: " + exception.Message);
            }
        }
        private static void AddSkills(HashSet<string> skills, IEnumerable<SkillNeed> needs)
        {
            if (needs == null)
            {
                return;
            }
            foreach (SkillNeed need in needs)
            {
                if (need != null && need.skill != null)
                {
                    skills.Add(need.skill.defName);
                }
            }
        }
    }
}