using System.Collections.Generic;
using System.Linq;
using OrderedWork.Data;
using OrderedWork.Services;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    public partial class Dialog_WorkControlDetails
    {
        private void DrawOverridesTab(Rect content)
        {
            PawnWorkOrder entry = OrderedWorkGameComponent.GetOrCreate()?.GetEntry(pawn);
            int count = CountOverrides(entry);
            float height = 80f + (count * 22f) + 120f;
            Rect view = new Rect(0f, 0f,
                Mathf.Max(content.width, 760f),
                Mathf.Max(content.height, height));

            Widgets.BeginScrollView(content, ref detailTabScroll, view);
            try
            {
                UiRows rows = new UiRows(view, 20f, 4f);
                if (entry == null)
                {
                    Widgets.Label(rows.Next(24f), "No Ordered Work policy saved for this pawn.");
                    return;
                }

                DrawSectionTitle(rows, "Work overrides");
                int rowIndex = 0;
                if (entry.workOverrides.Count == 0)
                {
                    Widgets.Label(rows.Next(), "None.");
                }
                else
                {
                    foreach (PawnWorkOverride value in entry.workOverrides)
                    {
                        string state = value.enabled ? "enabled" : "disabled";
                        string locked = value.locked ? ", locked" : "";
                        DrawRow(rows, "WorkType " + WorkTypeLabel(value.workTypeDefName)
                            + " | rank " + value.rank + " | " + state + locked
                            + " | " + value.source, rowIndex++);
                    }
                }

                DrawSectionTitle(rows, "Schedule overrides");
                rowIndex = 0;
                if (entry.scheduleOverrides.Count == 0)
                {
                    Widgets.Label(rows.Next(), "None.");
                }
                else
                {
                    foreach (PawnScheduleOverride value in entry.scheduleOverrides)
                    {
                        TimeAssignmentDef assignment = SchedulePolicyResolver.ResolveDef(value.assignmentDefName);
                        DrawRow(rows, "Hour " + value.hour + " | "
                            + (assignment != null ? assignment.label : value.assignmentDefName)
                            + " | " + value.source, rowIndex++);
                    }
                }

                DrawSectionTitle(rows, "Hourly work overrides");
                rowIndex = 0;
                if (entry.scheduleWorkOverrides.Count == 0)
                {
                    Widgets.Label(rows.Next(), "None.");
                }
                else
                {
                    foreach (PawnScheduleWorkOverride value in entry.scheduleWorkOverrides)
                    {
                        DrawRow(rows, "Hour " + value.hour + " | "
                            + WorkTypeLabel(value.workTypeDefName)
                            + " | " + value.source, rowIndex++);
                    }
                }

                DrawSectionTitle(rows, "Clothing overrides");
                rowIndex = 0;
                if (entry.clothingOverrides.Count == 0)
                {
                    Widgets.Label(rows.Next(), "None.");
                }
                else
                {
                    foreach (PawnClothingOverride value in entry.clothingOverrides)
                    {
                        DrawRow(rows, "WorkType " + WorkTypeLabel(value.workTypeDefName)
                            + " | apparel policy " + (string.IsNullOrEmpty(value.apparelPolicyId) ? "none" : value.apparelPolicyId)
                            + " | " + value.source, rowIndex++);
                    }
                }
            }
            finally
            {
                Widgets.EndScrollView();
                Text.Font = GameFont.Small;
            }
        }

        private static int CountOverrides(PawnWorkOrder entry)
        {
            if (entry == null)
            {
                return 0;
            }
            return entry.workOverrides.Count
                + entry.scheduleOverrides.Count
                + entry.scheduleWorkOverrides.Count
                + entry.clothingOverrides.Count;
        }

        private static void DrawSectionTitle(UiRows rows, string title)
        {
            Rect row = rows.Next(26f);
            Widgets.Label(row, title);
        }

        private static void DrawRow(UiRows rows, string text, int index)
        {
            Rect row = rows.Next();
            UiZebra.Draw(row, index);
            Widgets.Label(row, text);
        }

        private static string WorkTypeLabel(string defName)
        {
            WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(defName);
            return workType != null ? workType.label : (defName ?? "unknown");
        }
    }
}
