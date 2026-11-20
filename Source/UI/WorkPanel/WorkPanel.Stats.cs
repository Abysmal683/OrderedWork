#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Services;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    public sealed partial class WorkPanel
    {        private void DrawStats(Rect rect, ref Vector2 scroll)
        {
            statTooltips.Tick();
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 20f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Stats.Specialized", "Specialized stats"));
            Rect body = new Rect(rect.x, rect.y + 24f, rect.width, Mathf.Max(0f, rect.height - 24f));
            WorkTypeDef selected = SelectedWorkType();
            List<StatDef> selectedStats = selected != null ? StatIndex.GetFor(selected) : new List<StatDef>();
            int contentRows = selectedStats.Count + (selected != null && BestSkillDef(selected) != null ? 1 : 0);
            UiScrollPane.Draw(body, ref scroll, Mathf.Max(body.height, contentRows * 20f + 4f), this, delegate(Rect view)
            {
                float y = 0f;
                if (selected == null)
                {
                    Widgets.Label(new Rect(0f, y + 6f, view.width, 20f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Work.SelectType", "Select a WorkType."));
                }
                else
                {
                    List<StatDef> stats = selectedStats;
                    if (stats.Count == 0)
                    {
                        StatDef fallback = RelevantWorkStat(selected);
                        if (fallback != null)
                        {
                            DrawStatLine(view.width, ref y, fallback, SafeStat(fallback), false, 0);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < stats.Count; i++)
                        {
                            DrawStatLine(view.width, ref y, stats[i], SafeStat(stats[i]), false, i);
                        }
                    }

                    SkillDef skill = BestSkillDef(selected);
                    SkillRecord record = skill != null && pawn.skills != null ? pawn.skills.GetSkill(skill) : null;
                    if (skill != null && record != null)
                    {
                        UiZebra.Draw(new Rect(0f, y, view.width, 20f), stats.Count);
                        Widgets.Label(new Rect(0f, y, view.width, 20f),
                            OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Stats.SkillLabel", "Skill ") + skill.label + ": " + record.Level);
                    }
                }
            });
        }

        private void DrawGenericStats(Rect rect)
        {
            statTooltips.Tick();
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 20f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Stats.Base", "Base stats"));
            Rect body = new Rect(rect.x, rect.y + 24f, rect.width, Mathf.Max(0f, rect.height - 24f));
            UiScrollPane.Draw(body, ref genericStatsScroll, Mathf.Max(body.height, 140f), this, delegate(Rect view)
            {
                float y = 0f;
                DrawStatLine(view.width, ref y, StatDefOf.WorkSpeedGlobal, SafeStat(StatDefOf.WorkSpeedGlobal), false, 0);
                DrawStatLine(view.width, ref y, StatDefOf.MoveSpeed, SafeStat(StatDefOf.MoveSpeed), false, 1);
                DrawCapacityLine(view.width, ref y, PawnCapacityDefOf.Manipulation, SafeCapacity(PawnCapacityDefOf.Manipulation), 2);
                DrawCapacityLine(view.width, ref y, PawnCapacityDefOf.Sight, SafeCapacity(PawnCapacityDefOf.Sight), 3);
                DrawCapacityLine(view.width, ref y, PawnCapacityDefOf.Consciousness, SafeCapacity(PawnCapacityDefOf.Consciousness), 4);
                DrawStatLine(view.width, ref y, StatDefOf.GlobalLearningFactor, SafeStat(StatDefOf.GlobalLearningFactor), false, 5);
            });
        }

        private void DrawStatLine(float width, ref float y, StatDef stat, float value, bool percent, int index)
        {
            Rect row = new Rect(0f, y, width, 20f);
            UiZebra.Draw(row, index);
            string text = stat != null ? stat.label : OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Stats.Stat", "Stat");
            string amount = percent ? (value * 100f).ToString("0") + "%" : value.ToString("0.##");
            Widgets.Label(row, text + ": " + amount);
            if (stat != null && Mouse.IsOver(row))
            {
                TooltipHandler.TipRegion(row, statTooltips.Get(stat));
            }
            y += 20f;
        }

        private static void DrawCapacityLine(float width, ref float y, PawnCapacityDef capacity, float value, int index)
        {
            Rect row = new Rect(0f, y, width, 20f);
            UiZebra.Draw(row, index);
            string label = capacity != null ? capacity.LabelCap.ToString() : OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Stats.Capacity", "Capacity");
            Widgets.Label(row, label + ": " + (value * 100f).ToString("0") + "%");
            if (Mouse.IsOver(row))
            {
                TooltipHandler.TipRegion(row, CapacityTooltip(capacity));
            }
            y += 20f;
        }

        private float SafeStat(StatDef stat)
        {
            if (stat == null)
            {
                return 0f;
            }
            try
            {
                return pawn.GetStatValue(stat);
            }
            catch (Exception)
            {
                return 0f;
            }
        }

        private float SafeCapacity(PawnCapacityDef capacity)
        {
            if (pawn == null || pawn.health == null || pawn.health.capacities == null || capacity == null)
            {
                return 0f;
            }
            try
            {
                return pawn.health.capacities.GetLevel(capacity);
            }
            catch (Exception)
            {
                return 0f;
            }
        }

        private static string CapacityTooltip(PawnCapacityDef capacity)
        {
            if (capacity == null)
            {
                return OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Stats.Capacity", "Capacity");
            }
            return capacity.LabelCap + "\n" + OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Stats.BodyCapacity", "A body capacity affected by health, injuries, bionics and drugs.");
        }

        private static StatDef RelevantWorkStat(WorkTypeDef workType)
        {
            if (workType != null)
            {
                switch (workType.defName)
                {
                    case "Construction": return StatDefOf.ConstructionSpeed;
                    case "Mining": return StatDefOf.MiningSpeed;
                    case "Growing": return StatDefOf.PlantWorkSpeed;
                    case "Cooking": return StatDefOf.GeneralLaborSpeed;
                    case "Research": return StatDefOf.ResearchSpeed;
                    case "Doctor": return StatDefOf.MedicalTendSpeed;
                    case "Cleaning": return StatDefOf.CleaningSpeed;
                    case "Fishing": return StatDefOf.FishingSpeed;
                }
            }
            return StatDefOf.GeneralLaborSpeed;
        }

        private static StatDef NamedStat(string defName)
        {
            return DefDatabase<StatDef>.GetNamedSilentFail(defName);
        }
    }
}