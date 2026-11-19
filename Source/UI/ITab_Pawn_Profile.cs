using System.Collections.Generic;
using System.Linq;
using OrderedWork.Services;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Direct access tab for the per-pawn Ordered Work detail. The tab itself is
    /// intentionally just a launcher; the full editor lives in the existing dialog.
    /// </summary>
    public sealed class ITab_Pawn_Profile : ITab
    {
        public ITab_Pawn_Profile()
        {
            labelKey = "Profile";
            size = new Vector2(400f, 340f);
            tutorTag = "OrderedWorkProfile";
        }

        public override bool IsVisible
        {
            get { return SelPawn != null && SelPawn.IsFreeColonist && !SelPawn.Dead; }
        }

        protected override void FillTab()
        {
            Rect area = UiInset.All(new Rect(0f, 0f, size.x, size.y), 12f);
            Rect content = new Rect(area.x, area.y, area.width, Mathf.Max(0f, area.height - 32f));
            try
            {
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(content.x, content.y, content.width, 24f), "Ordered Work");
                Text.Font = GameFont.Small;

                List<string> order = WorkOrderService.GetOrder(SelPawn) ?? new List<string>();
                float y = content.y + 30f;
                Widgets.Label(new Rect(content.x, y, content.width, 18f), "Assigned work");
                y += 20f;
                if (order.Count == 0)
                {
                    Widgets.Label(new Rect(content.x, y, content.width, 18f), "No active WorkTypes.");
                    y += 18f;
                }
                else
                {
                    for (int i = 0; i < order.Count && i < 9; i++)
                    {
                        string defName = order[i];
                        WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(defName);
                        Rect row = new Rect(content.x, y, content.width, 18f);
                        UiZebra.Draw(row, i);
                        Widgets.LabelEllipses(new Rect(row.x + 4f, row.y + 1f, row.width - 82f, 18f),
                            (i + 1) + ". " + (workType != null ? workType.labelShort : defName));
                        SkillDef skill = BestSkill(workType);
                        SkillRecord record = skill != null && SelPawn.skills != null ? SelPawn.skills.GetSkill(skill) : null;
                        WorkTypePaletteView.DrawPassionIcon(new Rect(row.xMax - 66f, row.y + 2f, 14f, 14f),
                            record != null ? record.passion : Passion.None);
                        Widgets.Label(new Rect(row.xMax - 46f, row.y + 1f, 26f, 18f),
                            record != null ? record.Level.ToString() : "—");
                        y += 18f;
                    }
                }

                y += 4f;
                Widgets.Label(new Rect(content.x, y, content.width, 18f), "Schedule");
                y += 18f;
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                if (SelPawn != null && SelPawn.timetable != null)
                {
                    float cellWidth = (content.width - 10f) / 6f;
                    for (int row = 0; row < 4; row++)
                    {
                        for (int column = 0; column < 6; column++)
                        {
                            int hour = row * 6 + column;
                            TimeAssignmentDef assignment = SelPawn.timetable.GetAssignment(hour);
                            Rect cell = new Rect(content.x + column * (cellWidth + 2f), y,
                                cellWidth, 17f);
                            Widgets.DrawBoxSolid(cell, AssignmentColor(assignment));
                            Widgets.Label(cell, hour.ToString("00"));
                            TooltipHandler.TipRegion(cell,
                                hour.ToString("00") + ": " + (assignment != null ? assignment.LabelCap : "None"));
                        }
                        y += 19f;
                    }
                }
                else
                {
                    Widgets.Label(new Rect(content.x, y, content.width, 18f), "No timetable.");
                }
            }
            finally
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
            }

            if (UiButtonBar.Button(new Rect(area.xMax - 150f, area.y + 2f, 132f, 24f), "Open details", true,
                "Open the full Ordered Work editor."))
            {
                OpenDetail();
            }
        }
        private SkillDef BestSkill(WorkTypeDef workType)
        {
            if (workType == null || workType.relevantSkills == null || SelPawn == null || SelPawn.skills == null)
            {
                return null;
            }
            SkillDef best = null;
            int bestLevel = -1;
            foreach (SkillDef skill in workType.relevantSkills)
            {
                SkillRecord record = skill != null ? SelPawn.skills.GetSkill(skill) : null;
                if (record != null && record.Level > bestLevel)
                {
                    best = skill;
                    bestLevel = record.Level;
                }
            }
            return best;
        }

        private static Color AssignmentColor(TimeAssignmentDef def)
        {
            if (def == null) return new Color(0.18f, 0.18f, 0.20f, 1f);
            Color color = def.color;
            return color.a <= 0f ? new Color(0.25f, 0.30f, 0.36f, 1f) : color;
        }

        private static string ShortAssignment(TimeAssignmentDef def)
        {
            if (def == null) return "—";
            if (def.defName == "Sleep") return "S";
            if (def.defName == "Work") return "W";
            if (def.defName == "Joy") return "J";
            if (def.defName == "Meditate") return "M";
            if (def.defName == "Anything") return "A";
            return def.label;
        }
        private void OpenDetail()
        {
            if (SelPawn == null || SelPawn.Dead)
            {
                return;
            }
            if (Find.WindowStack.IsOpen<Dialog_WorkControlDetails>())
            {
                return;
            }
            Find.WindowStack.Add(new Dialog_WorkControlDetails(SelPawn));
        }
    }
}
