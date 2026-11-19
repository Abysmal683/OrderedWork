using System;
using System.Collections.Generic;
using OrderedWork.Services;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>Read-only Profile tab body; the full editor stays in the dialog.</summary>
    public static class ProfileSummaryPanel
    {
        public static void Draw(Pawn pawn, Rect area, Action openDetails)
        {
            Rect content = UiInset.All(area, 12f);
            Rect body = new Rect(content.x, content.y, content.width, Mathf.Max(0f, content.height - 32f));
            try
            {
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(body.x, body.y, body.width, 24f), "Ordered Work");
                Text.Font = GameFont.Small;

                List<string> order = WorkOrderService.GetOrder(pawn) ?? new List<string>();
                float y = body.y + 30f;
                Widgets.Label(new Rect(body.x, y, body.width, 18f), "Assigned work");
                y += 20f;
                if (order.Count == 0)
                {
                    Widgets.Label(new Rect(body.x, y, body.width, 18f), "No active WorkTypes.");
                    y += 18f;
                }
                else
                {
                    for (int i = 0; i < order.Count && i < 9; i++)
                    {
                        WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(order[i]);
                        Rect row = new Rect(body.x, y, body.width, 18f);
                        UiZebra.Draw(row, i);
                        Widgets.LabelEllipses(new Rect(row.x + 4f, row.y + 1f, row.width - 82f, 18f),
                            (i + 1) + ". " + (workType != null ? workType.labelShort : order[i]));
                        SkillDef skill = BestSkill(pawn, workType);
                        SkillRecord record = skill != null && pawn.skills != null ? pawn.skills.GetSkill(skill) : null;
                        WorkTypePaletteView.DrawPassionIcon(new Rect(row.xMax - 66f, row.y + 2f, 14f, 14f),
                            record != null ? record.passion : Passion.None);
                        Widgets.Label(new Rect(row.xMax - 46f, row.y + 1f, 26f, 18f),
                            record != null ? record.Level.ToString() : "—");
                        y += 18f;
                    }
                }

                y += 4f;
                Widgets.Label(new Rect(body.x, y, body.width, 18f), "Schedule");
                y += 18f;
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                if (pawn != null && pawn.timetable != null)
                {
                    float cellWidth = (body.width - 10f) / 6f;
                    for (int row = 0; row < 4; row++)
                    {
                        for (int column = 0; column < 6; column++)
                        {
                            int hour = row * 6 + column;
                            TimeAssignmentDef assignment = pawn.timetable.GetAssignment(hour);
                            Rect cell = new Rect(body.x + column * (cellWidth + 2f), y, cellWidth, 17f);
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
                    Widgets.Label(new Rect(body.x, y, body.width, 18f), "No timetable.");
                }
            }
            finally
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
            }

            if (UiButtonBar.Button(new Rect(content.xMax - 150f, content.y + 2f, 132f, 24f),
                "Open details", true, "Open the full Ordered Work editor."))
            {
                openDetails?.Invoke();
            }
        }

        private static SkillDef BestSkill(Pawn pawn, WorkTypeDef workType)
        {
            if (pawn == null || pawn.skills == null || workType == null || workType.relevantSkills == null)
            {
                return null;
            }
            SkillDef best = null;
            int bestLevel = -1;
            foreach (SkillDef skill in workType.relevantSkills)
            {
                SkillRecord record = skill != null ? pawn.skills.GetSkill(skill) : null;
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
    }
}