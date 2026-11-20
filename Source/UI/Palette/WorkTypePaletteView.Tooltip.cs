using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using OrderedWork.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    internal static partial class WorkTypePaletteView
    {        private static string PaletteTooltip(WorkTypeDef type)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(DisplayName(type));
            builder.Append(" (").Append(type.defName).Append(")");
            if (!string.IsNullOrEmpty(type.description))
            {
                builder.AppendLine().AppendLine(type.description);
            }

            List<WorkGiverDef> givers = DefDatabase<WorkGiverDef>.AllDefsListForReading
                .Where(g => g.workType == type)
                .OrderByDescending(g => g.priorityInType)
                .ToList();
            builder.AppendLine();
            builder.AppendLine(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Palette.WorkGivers", "Work givers:"));
            if (givers.Count == 0)
            {
                builder.AppendLine(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Palette.NoneShort", "- none"));
            }
            else
            {
                foreach (WorkGiverDef giver in givers)
                {
                    builder.AppendLine("- " + giver.defName);
                }
            }

            builder.AppendLine();
            builder.Append(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Palette.RelevantSkills", "Relevant skills: "));
            if (type.relevantSkills == null || type.relevantSkills.Count == 0)
            {
                builder.Append(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Palette.None", "none"));
            }
            else
            {
                bool first = true;
                for (int i = 0; i < type.relevantSkills.Count; i++)
                {
                    SkillDef skill = type.relevantSkills[i];
                    if (skill == null)
                    {
                        continue;
                    }
                    if (!first)
                    {
                        builder.Append(", ");
                    }
                    builder.Append(skill.label);
                    first = false;
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// Best relevant SkillRecord without calling Pawn_SkillTracker.GetSkill for a missing
        /// skill (that call logs a fallback warning). Returns null when no record is present.
        /// </summary>
        internal static SkillRecord? SafeBestSkill(Pawn? pawn, WorkTypeDef? workType, out Passion passion)
        {
            passion = Passion.None;
            if (pawn == null || pawn.Dead || pawn.skills == null || pawn.skills.skills == null
                || workType == null || workType.relevantSkills == null)
            {
                return null;
            }

            SkillRecord? best = null;
            foreach (SkillDef skill in workType.relevantSkills)
            {
                if (skill == null)
                {
                    continue;
                }

                SkillRecord? record = null;
                List<SkillRecord> records = pawn.skills.skills;
                for (int i = 0; i < records.Count; i++)
                {
                    if (records[i] != null && records[i].def == skill)
                    {
                        record = records[i];
                        break;
                    }
                }
                if (record == null)
                {
                    continue; // do not call GetSkill, which logs a fallback
                }

                if (best == null
                    || record.Level > best.Level
                    || (record.Level == best.Level && PassionRank(record.passion) > PassionRank(best.passion)))
                {
                    best = record;
                }
            }

            if (best != null)
            {
                passion = best.passion;
            }
            return best;
        }

        private static int PassionRank(Passion passion)
        {
            if (passion == Passion.None)
            {
                return 0;
            }
            int colorRank = ColorRankOf(passion);
            if (colorRank > 0)
            {
                return colorRank;
            }
            if (passion == Passion.Major)
            {
                return 4;
            }
            if (passion == Passion.Minor)
            {
                return 3;
            }
            return 0;
        }
    }
}
