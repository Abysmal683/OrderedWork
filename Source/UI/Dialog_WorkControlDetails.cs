using System.Collections.Generic;
using System.Linq;
using OrderedWork.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Read-only foundation for a future advanced pawn tab. Shows the pawn's name/title, its
    /// best skills and the stored WorkType order with rank, skill/passion and status.
    /// </summary>
    public class Dialog_WorkControlDetails : Window
    {
        private const float PassionIconSize = 12f;

        private readonly Pawn pawn;
        private Vector2 scroll;

        public Dialog_WorkControlDetails(Pawn pawn)
        {
            this.pawn = pawn;
            doCloseX = true;
            doCloseButton = false;
            resizeable = false;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            forcePause = false;
        }

        public override Vector2 InitialSize => new Vector2(680f, 560f);

        public override void DoWindowContents(Rect inRect)
        {
            if (pawn == null || pawn.Destroyed)
            {
                Widgets.Label(inRect, "Pawn unavailable.");
                if (Widgets.ButtonText(new Rect(inRect.xMax - 130f, inRect.yMax - 34f, 120f, 28f), "Close"))
                {
                    Close(true);
                }
                return;
            }

            List<SkillRecord> topSkills = pawn.skills != null && pawn.skills.skills != null
                ? pawn.skills.skills.Where(s => s != null).OrderByDescending(s => s.Level).Take(8).ToList()
                : new List<SkillRecord>();

            List<string> order = WorkOrderService.GetOrder(pawn);

            float contentHeight = 32f + 22f + 22f + (topSkills.Count * 20f) + 10f + 22f
                + (Mathf.Max(1, order.Count) * 20f) + 10f;
            Rect scrollOuter = new Rect(0f, 0f, inRect.width, inRect.height - 40f);
            Rect scrollContent = new Rect(0f, 0f, inRect.width - 20f, Mathf.Max(scrollOuter.height, contentHeight));

            Widgets.BeginScrollView(scrollOuter, ref scroll, scrollContent);
            try
            {
                float y = 0f;

                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(0f, y, scrollContent.width, 30f), PawnLabel(pawn));
                Text.Font = GameFont.Small;
                y += 30f;

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
                        DrawPassionIcon(new Rect(4f, y + 4f, PassionIconSize, PassionIconSize), skill.passion);
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

                    Widgets.Label(new Rect(0f, y, rankWidth, 18f), "Rank");
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
                        int level = WorkOrderService.BestSkillLevel(pawn, workType, out passion);
                        string levelText = level >= 0 ? level.ToString() : "—";
                        int priority = pawn.workSettings != null ? pawn.workSettings.GetPriority(workType) : 0;
                        string status = WorkOrderService.IsIncapable(pawn, workType)
                            ? "Incapable"
                            : priority > 0 ? "Active" : "Off";

                        Widgets.Label(new Rect(0f, y, rankWidth, 18f), (i + 1).ToString());
                        Widgets.Label(new Rect(rankWidth, y, nameWidth, 18f), workType.labelShort);
                        DrawPassionIcon(new Rect(rankWidth + nameWidth, y + 3f, PassionIconSize, PassionIconSize), passion);
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

            Rect closeRect = new Rect(inRect.xMax - 130f, inRect.yMax - 34f, 120f, 28f);
            if (Widgets.ButtonText(closeRect, "Close"))
            {
                Close(true);
            }
        }

        private static void DrawPassionIcon(Rect rect, Passion passion)
        {
            Texture2D icon = null;
            if (passion == Passion.Major)
            {
                icon = WidgetsWork.PassionWorkboxMajorIcon;
            }
            else if (passion == Passion.Minor)
            {
                icon = WidgetsWork.PassionWorkboxMinorIcon;
            }

            if (icon != null)
            {
                GUI.DrawTexture(rect, icon);
            }
        }

        private static string PawnLabel(Pawn pawn)
        {
            string label = pawn.NameFullColored;
            return string.IsNullOrEmpty(label) ? pawn.LabelShort : label;
        }
    }
}
