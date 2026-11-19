using System.Collections.Generic;
using System.Linq;
using OrderedWork.Services;
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

        public override void OnOpen()
        {
            OpenDetail();
        }

        protected override void FillTab()
        {
            Rect area = new Rect(0f, 0f, size.x, size.y).ContractedBy(12f);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(area.x, area.y, area.width, 24f), "Ordered Work");
            Text.Font = GameFont.Small;

            List<string> order = WorkOrderService.GetOrder(SelPawn) ?? new List<string>();
            float y = area.y + 32f;
            Widgets.Label(new Rect(area.x, y, area.width, 20f), "Assigned work");
            y += 20f;
            if (order.Count == 0)
            {
                Widgets.Label(new Rect(area.x, y, area.width, 18f), "No active WorkTypes.");
                y += 18f;
            }
            else
            {
                foreach (string defName in order.Take(10))
                {
                    WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(defName);
                    Widgets.Label(new Rect(area.x + 4f, y, area.width - 4f, 18f),
                        (order.IndexOf(defName) + 1) + ". " + (workType != null ? workType.labelShort : defName));
                    y += 18f;
                }
            }

            y += 6f;
            Widgets.Label(new Rect(area.x, y, area.width, 20f), "Schedule");
            y += 20f;
            if (SelPawn != null && SelPawn.timetable != null)
            {
                string schedule = string.Join("  ", Enumerable.Range(0, 24)
                    .Select(hour => hour.ToString("00") + " " + ShortAssignment(SelPawn.timetable.GetAssignment(hour))));
                Widgets.Label(new Rect(area.x, y, area.width, 42f), schedule);
            }
            else
            {
                Widgets.Label(new Rect(area.x, y, area.width, 18f), "No timetable.");
            }

            if (Widgets.ButtonText(new Rect(area.x, area.yMax - 28f, 150f, 26f), "Open details"))
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
