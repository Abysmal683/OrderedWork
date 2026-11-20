#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Schedule brush selection and paint operation. The view owns placement.
    /// </summary>
    internal sealed class ScheduleBrush
    {
        internal TimeAssignmentDef Selected;

        internal void Reset()
        {
            Selected = null;
        }

        internal void Ensure()
        {
            if (Selected == null)
            {
                Selected = DefDatabase<TimeAssignmentDef>.AllDefsListForReading
                    .OrderBy(d => d.LabelCap.ToString(), StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
            }
        }

        internal void Paint(SchedulePreset draft, int hour, bool erase)
        {
            if (draft == null || draft.Slots == null || hour < 0 || hour >= draft.Slots.Count)
            {
                return;
            }
            draft.Slots[hour] = erase || Selected == null ? "" : Selected.defName;
        }

        internal void Draw(Rect rect)
        {
            Ensure();
            if (Selected == null)
            {
                Widgets.Label(rect, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Schedule.NoAssignment", "No assignment defs"));
                return;
            }
            Color color = ScheduleVisuals.AssignmentColor(Selected);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y + 4f, 16f, 16f), color);
            if (Widgets.ButtonText(new Rect(rect.x + 20f, rect.y, rect.width - 20f, rect.height), ScheduleVisuals.ShortLabel(Selected)))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (TimeAssignmentDef def in DefDatabase<TimeAssignmentDef>.AllDefsListForReading
                    .OrderBy(d => d.LabelCap.ToString(), StringComparer.OrdinalIgnoreCase))
                {
                    TimeAssignmentDef captured = def;
                    options.Add(new FloatMenuOption(captured.LabelCap.ToString(), delegate { Selected = captured; }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            TooltipHandler.TipRegion(rect, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Schedule.BrushTip", "Active schedule brush. Click or drag over Modif to paint this assignment."));
        }
    }
}