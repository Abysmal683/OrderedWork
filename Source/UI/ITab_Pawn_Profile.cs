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
            size = new Vector2(400f, 320f);
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
                        Widgets.Label(new Rect(content.x + 4f, y, content.width - 4f, 18f),
                            (i + 1) + ". " + (workType != null ? workType.labelShort : defName));
                        y += 18f;
                    }
                }

                y += 4f;
                Widgets.Label(new Rect(content.x, y, content.width, 18f), "Schedule");
                y += 18f;
                Text.Font = GameFont.Tiny;
                if (SelPawn != null && SelPawn.timetable != null)
                {
                    for (int row = 0; row < 4; row++)
                    {
                        string line = string.Join("  ", Enumerable.Range(row * 6, 6)
                            .Select(hour => hour.ToString("00") + " " + ShortAssignment(SelPawn.timetable.GetAssignment(hour))));
                        Widgets.Label(new Rect(content.x, y, content.width, 14f), line);
                        y += 16f;
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

            if (UiButtonBar.Button(new Rect(area.xMax - 132f, area.yMax - 26f, 132f, 24f), "Open details", true,
                "Open the full Ordered Work editor."))
            {
                OpenDetail();
            }
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
