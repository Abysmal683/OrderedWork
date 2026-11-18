using System.Collections.Generic;
using System.Linq;
using OrderedWork.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Advanced per-pawn panel. Sections share the pawn header and expose schedule,
    /// zone and (later) clothing/override editing without replacing vanilla tabs.
    /// </summary>
    public partial class Dialog_WorkControlDetails : Window
    {
        private enum DetailTab
        {
            Overview,
            Schedule,
            Zone
        }

        private readonly Pawn pawn;
        private DetailTab detailTab;
        private Vector2 overviewScroll;

        public Dialog_WorkControlDetails(Pawn pawn)
        {
            this.pawn = pawn;
            doCloseX = true;
            doCloseButton = false;
            resizeable = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            forcePause = false;
        }

        public override Vector2 InitialSize => new Vector2(1100f, 720f);

        public override void DoWindowContents(Rect inRect)
        {
            if (pawn == null || pawn.Destroyed)
            {
                Widgets.Label(inRect, "Pawn unavailable.");
                DrawCloseButton(inRect);
                return;
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width - 40f, 30f), PawnLabel(pawn));
            Text.Font = GameFont.Small;

            Rect tabRow = new Rect(0f, 34f, inRect.width, 26f);
            DrawDetailTab(new Rect(tabRow.x, tabRow.y, 110f, 24f), DetailTab.Overview, "Overview");
            DrawDetailTab(new Rect(tabRow.x + 114f, tabRow.y, 100f, 24f), DetailTab.Schedule, "Horario");
            DrawDetailTab(new Rect(tabRow.x + 218f, tabRow.y, 90f, 24f), DetailTab.Zone, "Zona");

            Rect content = new Rect(0f, 66f, inRect.width, inRect.height - 98f);
            switch (detailTab)
            {
                case DetailTab.Schedule:
                    DrawScheduleTab(content);
                    break;
                case DetailTab.Zone:
                    DrawZoneTab(content);
                    break;
                default:
                    DrawOverview(content);
                    break;
            }
            DrawCloseButton(inRect);
        }

        private void DrawDetailTab(Rect rect, DetailTab tab, string label)
        {
            if (Widgets.ButtonText(rect, label) && detailTab != tab)
            {
                detailTab = tab;
            }
        }

        private void DrawCloseButton(Rect inRect)
        {
            if (Widgets.ButtonText(new Rect(inRect.xMax - 130f, inRect.yMax - 34f, 120f, 28f), "Close"))
            {
                Close(true);
            }
        }

        private void DrawOverview(Rect content)
        {
            List<SkillRecord> topSkills = pawn.skills != null && pawn.skills.skills != null
                ? pawn.skills.skills.Where(s => s != null).OrderByDescending(s => s.Level).Take(8).ToList()
                : new List<SkillRecord>();
            List<string> order = WorkOrderService.GetOrder(pawn);

            float contentHeight = 32f + 22f + 22f + (topSkills.Count * 20f) + 10f + 22f
                + (Mathf.Max(1, order.Count) * 20f) + 10f;
            Rect scrollContent = new Rect(0f, 0f, content.width - 20f, Mathf.Max(content.height, contentHeight));

            Widgets.BeginScrollView(content, ref overviewScroll, scrollContent);
            try
            {
                float y = 0f;
                if (pawn.story != null && !string.IsNullOrEmpty(pawn.story.Title))
                {
                    Widgets.Label(new Rect(0f, y, scrollContent.width, 20f), pawn.story.Title);
                }
                y += 22f;

                Widgets.Label(new Rect(0f, y, scrollContent.width, 20f), "Top skills");
                y += 22f;
                if (topSkills.Count == 0)
                {
                    Widgets.Label(new Rect(6f, y, scrollContent.width - 6f, 20f), "No skills.");
                    y += 20f;
                }
                else
                {
                    foreach (SkillRecord skill in topSkills)
                    {
                        WorkTypePaletteView.DrawPassionIcon(
                            new Rect(4f, y + 4f, WorkTypePaletteView.PassionIconSize, WorkTypePaletteView.PassionIconSize),
                            skill.passion);
                        Widgets.Label(new Rect(20f, y, scrollContent.width - 20f, 20f),
                            skill.def.label + ": " + skill.Level);
                        y += 20f;
                    }
                }

                y += 10f;
                Widgets.Label(new Rect(0f, y, scrollContent.width, 20f), "Work order");
                y += 22f;
                if (order.Count == 0)
                {
                    Widgets.Label(new Rect(6f, y, scrollContent.width - 6f, 20f), "No active WorkTypes.");
                }
                else
                {
                    float rankWidth = 44f;
                    float statusWidth = 110f;
                    float skillWidth = 90f;
                    float nameWidth = scrollContent.width - rankWidth - statusWidth - skillWidth - 12f;
                    Widgets.Label(new Rect(0f, y, rankWidth, 18f), "Priority");
                    Widgets.Label(new Rect(rankWidth, y, nameWidth, 18f), "Work type");
                    Widgets.Label(new Rect(rankWidth + nameWidth, y, skillWidth, 18f), "Skill");
                    Widgets.Label(new Rect(rankWidth + nameWidth + skillWidth, y, statusWidth, 18f), "Status");
                    y += 20f;

                    for (int i = 0; i < order.Count; i++)
                    {
                        WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(order[i]);
                        if (workType == null)
                        {
                            continue;
                        }
                        Passion passion;
                        SkillRecord bestSkill = WorkTypePaletteView.SafeBestSkill(pawn, workType, out passion);
                        string levelText = bestSkill != null ? bestSkill.Level.ToString() : "—";
                        int priority = pawn.workSettings != null ? pawn.workSettings.GetPriority(workType) : 0;
                        string status = WorkOrderService.IsIncapable(pawn, workType)
                            ? "Incapable"
                            : priority > 0 ? "Active" : "Off";
                        Widgets.Label(new Rect(0f, y, rankWidth, 18f), (i + 1).ToString());
                        Widgets.Label(new Rect(rankWidth, y, nameWidth, 18f), WorkTypePaletteView.DisplayName(workType));
                        WorkTypePaletteView.DrawPassionIcon(
                            new Rect(rankWidth + nameWidth, y + 3f, WorkTypePaletteView.PassionIconSize, WorkTypePaletteView.PassionIconSize),
                            passion);
                        Widgets.Label(new Rect(rankWidth + nameWidth + 16f, y, skillWidth - 16f, 18f), levelText);
                        Widgets.Label(new Rect(rankWidth + nameWidth + skillWidth, y, statusWidth, 18f), status);
                        y += 20f;
                    }
                }
            }
            finally
            {
                Widgets.EndScrollView();
                Text.Font = GameFont.Small;
            }
        }

        private static string PawnLabel(Pawn pawn)
        {
            string label = pawn.NameFullColored;
            return string.IsNullOrEmpty(label) ? pawn.LabelShort : label;
        }
    }
}
