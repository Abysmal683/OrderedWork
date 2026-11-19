using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Core;
using OrderedWork.Services;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    public sealed partial class WorkPanel
    {        private void DrawTopSkills(Rect rect)
        {
            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (ticks - skillTooltipCacheTick >= 60)
            {
                skillTooltipCache.Clear();
                skillTooltipCacheTick = ticks;
            }
            List<SkillRow> skills = RankedSkills();
            if (skills.Count == 0)
            {
                Widgets.Label(new Rect(rect.x, rect.y + 24f, rect.width, 40f), "No skills.");
                return;
            }
            Rect body = new Rect(rect.x, rect.y + 24f, rect.width, Mathf.Max(0f, rect.height - 24f));
            Rect view = new Rect(0f, 0f, body.width - 16f, skills.Count * 22f);
            Widgets.BeginScrollView(body, ref skillsScroll, view);
            try
            {
                for (int i = 0; i < skills.Count; i++)
                {
                    SkillRow entry = skills[i];
                    Rect row = new Rect(0f, i * 22f, view.width, 20f);
                    UiZebra.Draw(row, i);
                    Widgets.LabelEllipses(new Rect(row.x + 2f, row.y + 2f, row.width - 132f, 18f), entry.Label);
                    Rect icon = new Rect(row.xMax - 110f, row.y + 3f, 16f, 16f);
                    if (entry.IsExpertise)
                    {
                        DrawExpertiseStar(icon);
                    }
                    else
                    {
                        WorkTypePaletteView.DrawPassionIcon(icon, entry.Passion);
                    }
                    Widgets.Label(new Rect(row.xMax - 90f, row.y + 2f, 24f, 18f), entry.Level.ToString());
                    DrawSkillBar(new Rect(row.xMax - 62f, row.y + 7f, 58f, 8f), entry);
                    if (Mouse.IsOver(row)) TooltipHandler.TipRegion(row, SkillTooltip(entry));
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        private List<SkillRow> RankedSkills()
        {
            List<SkillRow> candidates = new List<SkillRow>();
            if (pawn.skills == null)
            {
                return candidates;
            }
            foreach (SkillDef skill in DefDatabase<SkillDef>.AllDefsListForReading)
            {
                SkillRecord record = skill != null ? pawn.skills.GetSkill(skill) : null;
                if (record == null)
                {
                    continue;
                }
                if (!filterMods && skill.modContentPack != null && !skill.modContentPack.IsCoreMod)
                {
                    continue;
                }
                candidates.Add(new SkillRow
                {
                    Label = skill.label,
                    Level = record.Level,
                    Passion = record.passion,
                    XpCurrent = record.xpSinceLastLevel,
                    XpRequired = record.XpRequiredForLevelUp,
                    LearnFactor = record.passion == Passion.Major ? SkillRecord.LearnFactorPassionMajor
                        : record.passion == Passion.Minor ? SkillRecord.LearnFactorPassionMinor
                        : SkillRecord.LearnFactorPassionNone
                });
            }

            if (filterExpertise && VseExpertiseAdapter.IsAvailable)
            {
                foreach (VseExpertiseAdapter.Row row in VseExpertiseAdapter.GetRows(pawn))
                {
                    SkillRecord parent = row.ParentSkill != null && pawn.skills != null
                        ? pawn.skills.GetSkill(row.ParentSkill)
                        : null;
                    candidates.Add(new SkillRow
                    {
                        Label = row.Label,
                        Level = row.Level > 0 ? row.Level : (parent != null ? parent.Level : row.Level),
                        Passion = parent != null ? parent.passion : Passion.None,
                        XpCurrent = row.XpCurrent,
                        XpRequired = row.XpRequired,
                        LearnFactor = 1f,
                        IsExpertise = true,
                        Description = row.Description
                    });
                }
            }

            if (filterLevel && filterPassion)
            {
                return candidates.OrderByDescending(SkillScore)
                    .ThenByDescending(r => r.Level)
                    .ToList();
            }
            if (filterLevel)
            {
                return candidates.OrderByDescending(r => r.Level)
                    .ThenByDescending(r => PassionRank(r.Passion))
                    .ToList();
            }
            if (filterPassion)
            {
                return candidates
                    .OrderBy(r => PassionSource.IsBadOf(r.Passion) ? 2 : r.Passion == Passion.None ? 1 : 0)
                    .ThenByDescending(SkillScore)
                    .ThenByDescending(r => r.Level)
                    .ToList();
            }
            return candidates.OrderBy(r => r.Label, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private int SkillScore(SkillRow row)
        {
            PassionWeightEntry entry = PassionWeightPolicy.Find(OrderedWorkMod.Settings, row.Passion);
            bool prioritizeLowSkill = OrderedWorkMod.Settings == null
                || OrderedWorkMod.Settings.prioritizeLowSkillPassion;
            return RecommendationScorePolicy.Score(
                row.Level,
                entry != null,
                entry != null ? entry.level0Score : 0,
                entry != null ? entry.perLevelBonus : 0,
                entry != null ? entry.minLevel : 0,
                prioritizeLowSkill);
        }

        private static void DrawSkillBar(Rect rect, SkillRow row)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.12f, 0.14f, 0.16f, 0.9f));
            float percent = row.XpRequired > 0.01f
                ? Mathf.Clamp01(row.XpCurrent / row.XpRequired)
                : 1f;
            if (percent > 0f)
            {
                Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width * percent, rect.height),
                    new Color(0.42f, 0.68f, 0.34f, 0.9f));
            }
            Widgets.DrawBox(rect, 1);
        }

        private static void DrawExpertiseStar(Rect rect)
        {
            Texture2D star = ContentFinder<Texture2D>.Get("UI/Passion_Expertise", false);
            if (star != null)
            {
                GUI.DrawTexture(rect, star);
            }
            else
            {
                Widgets.Label(rect, "★");
            }
        }

        private string SkillTooltip(SkillRow row)
        {
            string cached;
            if (skillTooltipCache.TryGetValue(row.Label, out cached))
            {
                return cached;
            }
            if (row.IsExpertise)
            {
                string expertise = row.Label + "\nExpertise";
                if (!string.IsNullOrEmpty(row.Description)) expertise += "\n" + row.Description;
                expertise += row.XpRequired > 0.01f
                    ? "\nXP: " + row.XpCurrent.ToString("0") + " / " + row.XpRequired.ToString("0")
                    : "\nXP: max / uncapped";
                skillTooltipCache[row.Label] = expertise;
                return expertise;
            }
            float multiplier = PassionSource.LearnRateOf(row.Passion);
            if (multiplier <= 0f)
            {
                multiplier = row.LearnFactor;
            }
            string result = row.Label
                + "\nPassion: " + WorkTypePaletteView.PassionText(row.Passion)
                + "\nLearning multiplier: x" + multiplier.ToString("0.00")
                + "\nNegative passion: " + (PassionSource.IsBadOf(row.Passion) ? "yes" : "no")
                + (row.XpRequired > 0.01f
                    ? "\nXP: " + row.XpCurrent.ToString("0") + " / " + row.XpRequired.ToString("0")
                    : "\nXP: max / uncapped");
            PassionWeightEntry weight = PassionWeightPolicy.Find(OrderedWorkMod.Settings, row.Passion);
            if (weight != null)
            {
                result += "\nRecommendation weight:"
                    + "\n  Level 0: " + weight.level0Score
                    + "\n  Per level: " + weight.perLevelBonus
                    + "\n  Min level: " + weight.minLevel;
            }
            skillTooltipCache[row.Label] = result;
            return result;
        }

        private void ApplySkillFilter(bool level, bool passion, bool expertise, bool mods)
        {
            filterLevel = level;
            filterPassion = passion;
            filterExpertise = expertise;
            filterMods = mods;
            if (OrderedWorkMod.Settings != null)
            {
                OrderedWorkMod.Settings.skillFilterLevel = level;
                OrderedWorkMod.Settings.skillFilterPassion = passion;
                OrderedWorkMod.Settings.skillFilterExpertise = expertise;
                OrderedWorkMod.Settings.skillFilterMods = mods;
            }
        }
    }
}
