#nullable disable
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
                    Widgets.Label(rows.Next(24f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overrides.NoPolicy", "No Ordered Work policy saved for this pawn."));
                    return;
                }

                DrawSectionTitle(rows, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overrides.Work", "Work overrides"));
                int rowIndex = 0;
                if (entry.workOverrides.Count == 0)
                {
                    Widgets.Label(rows.Next(), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.None", "None."));
                }
                else
                {
                    foreach (PawnWorkOverride value in entry.workOverrides)
                    {
                        string state = value.enabled ? OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overrides.Enabled", "enabled") : OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overrides.Disabled", "disabled");
                        string locked = value.locked ? OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overrides.LockedSuffix", ", locked") : "";
                        DrawRow(rows, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overrides.WorkType", "WorkType ") + WorkTypeLabel(value.workTypeDefName)
                            + OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overrides.Rank", " | rank ") + value.rank + " | " + state + locked
                            + " | " + value.source, rowIndex++);
                    }
                }

                DrawSectionTitle(rows, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overrides.Schedule", "Schedule overrides"));
                rowIndex = 0;
                if (entry.scheduleOverrides.Count == 0)
                {
                    Widgets.Label(rows.Next(), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.None", "None."));
                }
                else
                {
                    foreach (PawnScheduleOverride value in entry.scheduleOverrides)
                    {
                        TimeAssignmentDef assignment = SchedulePolicyResolver.ResolveDef(value.assignmentDefName);
                        DrawRow(rows, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overrides.Hour", "Hour ") + value.hour + " | "
                            + (assignment != null ? assignment.label : value.assignmentDefName)
                            + " | " + value.source, rowIndex++);
                    }
                }

                DrawSectionTitle(rows, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overrides.Hourly", "Hourly work overrides"));
                rowIndex = 0;
                if (entry.hourFocusOverrides.Count == 0)
                {
                    Widgets.Label(rows.Next(), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.None", "None."));
                }
                else
                {
                    foreach (PawnHourFocusOverride value in entry.hourFocusOverrides)
                    {
                        DrawRow(rows, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overrides.Hour", "Hour ") + value.hour + " | "
                            + string.Join(", ", value.workTypeDefNames.Select(WorkTypeLabel))
                            + " | " + value.source, rowIndex++);
                    }
                }

                DrawSectionTitle(rows, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overrides.Clothing", "Clothing overrides"));
                rowIndex = 0;
                if (entry.clothingOverrides.Count == 0)
                {
                    Widgets.Label(rows.Next(), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.None", "None."));
                }
                else
                {
                    foreach (PawnClothingOverride value in entry.clothingOverrides)
                    {
                        DrawRow(rows, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overrides.WorkType", "WorkType ") + WorkTypeLabel(value.workTypeDefName)
                            + OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overrides.ApparelPolicy", " | apparel policy ") + (string.IsNullOrEmpty(value.apparelPolicyId) ? OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Palette.None", "none") : value.apparelPolicyId)
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
                + entry.hourFocusOverrides.Count
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
            return workType != null ? workType.label : (defName ?? OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.Unknown", "unknown"));
        }
    }
}
