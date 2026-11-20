#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.Services;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Draws the Schedule tab. State, brush and persistence are collaborators.
    /// </summary>
    internal sealed class ScheduleEditorView
    {
        private readonly ScheduleEditorState state;
        private readonly ScheduleBrush brush;
        private readonly ScheduleApplyController controller;

        internal ScheduleEditorView(ScheduleEditorState state, ScheduleBrush brush, ScheduleApplyController controller)
        {
            this.state = state;
            this.brush = brush;
            this.controller = controller;
        }

        internal void Draw(Rect content, Pawn pawn, Action<Rect, float, float> drawFocus)
        {
            state.Ensure(pawn);
            brush.Ensure();

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, 100f, 24f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Schedule.Title", "Schedule"));
            Text.Font = GameFont.Small;
            Rect infoRect = new Rect(104f, 2f, 18f, 20f);
            Widgets.Label(infoRect, "?");
            TooltipHandler.TipRegion(infoRect,
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Schedule.Info", "Origin: current vanilla timetable, read-only. Edit: editable preset; the brush paints hours. Priority: reserved for WorkType overrides."));

            Rect panel = new Rect(0f, 28f, content.width, Mathf.Max(0f, content.height - 28f));
            GUI.BeginGroup(panel);
            try
            {
                Widgets.DrawBoxSolid(new Rect(0f, 0f, panel.width, panel.height), UiColors.PanelBody);
                Rect inner = UiInset.All(new Rect(0f, 0f, panel.width, panel.height), 6f);
                GUI.BeginGroup(inner);
                try
                {
                    DrawPanel(new Rect(0f, 0f, inner.width, inner.height), pawn, drawFocus);
                }
                finally
                {
                    GUI.EndGroup();
                }
            }
            finally
            {
                GUI.EndGroup();
                Text.Font = GameFont.Small;
            }
        }

        private void DrawPanel(Rect content, Pawn pawn, Action<Rect, float, float> drawFocus)
        {
            float y = 0f;
            float line = 24f;
            if (UiButtonBar.Button(new Rect(0f, y, 64f, line), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.Load", "Load")))
            {
                controller.OpenLoad();
            }
            if (UiButtonBar.Button(new Rect(68f, y, 56f, line), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Schedule.New", "New")))
            {
                controller.New();
            }
            if (UiButtonBar.Button(new Rect(128f, y, 100f, line), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Schedule.CopyCurrent", "Copy current")))
            {
                controller.CopyCurrent();
            }
            if (UiButtonBar.Button(new Rect(232f, y, 64f, line), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.Save", "Save")))
            {
                controller.OpenSave();
            }
            bool hasChanges = controller.HasChanges;
            if (UiButtonBar.Button(new Rect(300f, y, 116f, line), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.ApplyToPawn", "Apply to pawn"), hasChanges))
            {
                controller.Apply();
            }
            if (Mouse.IsOver(new Rect(300f, y, 116f, line)) && !hasChanges)
            {
                TooltipHandler.TipRegion(new Rect(300f, y, 116f, line),
                    OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Schedule.ApplyTip", "Apply is enabled only when a non-empty hour differs from the current timetable."));
            }
            y += line + 8f;

            Rect brushRect = new Rect(0f, y, 210f, line);
            brush.Draw(brushRect);
            Widgets.LabelEllipses(new Rect(brushRect.xMax + 8f, y, content.width - brushRect.width - 8f, line),
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Schedule.PaintHint", "Left click: paint · Right click: clear"));
            y += line + 8f;

            float hourWidth = Mathf.Max(22f, (content.width - 76f) / SchedulePreset.SlotCount);
            float hourHeaderTop = y;
            Widgets.Label(new Rect(0f, y, 72f, 18f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Schedule.Hour", "Hour"));
            for (int hour = 0; hour < SchedulePreset.SlotCount; hour++)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(new Rect(76f + hour * hourWidth, y, hourWidth, 18f), hour.ToString("00"));
                Text.Anchor = TextAnchor.UpperLeft;
            }
            y += 20f;
            Widgets.DrawBoxSolid(new Rect(0f, y - 2f, content.width, 2f), UiColors.SectionSeparator);
            DrawScheduleRow(content, y, hourWidth, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Schedule.Origin", "Origin"), pawn, false, true);
            y += 42f;
            DrawScheduleRow(content, y, hourWidth, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Schedule.Edit", "Edit"), pawn, true, false);
            y += 42f;
            float focusTop = y;
            Widgets.DrawBoxSolid(new Rect(0f, y - 5f, content.width, 2f), UiColors.SectionSeparator);
            drawFocus?.Invoke(content, y, hourWidth);
            if (pawn != null && pawn.Map != null)
            {
                ScheduleVisuals.DrawCurrentHourHighlight(hourHeaderTop, focusTop + 34f, hourWidth, GenLocalDate.HourOfDay(pawn.Map));
            }
        }

        private void DrawScheduleRow(Rect content, float y, float hourWidth, string label, Pawn pawn,
            bool editable, bool origin)
        {
            Widgets.Label(new Rect(0f, y, 72f, 24f), label);
            for (int hour = 0; hour < SchedulePreset.SlotCount; hour++)
            {
                Rect cell = new Rect(76f + hour * hourWidth, y, hourWidth - 1f, 30f);
                TimeAssignmentDef def = origin
                    ? (pawn != null && pawn.timetable != null ? pawn.timetable.GetAssignment(hour) : null)
                    : (editable ? state.ResolveDraft(hour) : null);
                if (editable && Mouse.IsOver(cell)
                    && (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag))
                {
                    brush.Paint(state.Draft, hour, Event.current.button == 1);
                    if (Event.current.button == 1 || Event.current.button == 0)
                    {
                        Event.current.Use();
                    }
                }
                Color color = def != null ? ScheduleVisuals.AssignmentColor(def) : new Color(0.18f, 0.18f, 0.20f, 1f);
                if ((hour & 1) == 1)
                {
                    color = new Color(color.r * 0.82f, color.g * 0.82f, color.b * 0.82f, color.a);
                }
                if (origin)
                {
                    color = new Color(color.r, color.g, color.b, 0.55f);
                }
                Widgets.DrawBoxSolid(cell, color);
                if (def != null)
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(cell, ScheduleVisuals.ShortLabel(def, hourWidth));
                    Text.Anchor = TextAnchor.UpperLeft;
                }
                TooltipHandler.TipRegion(cell, def != null
                    ? def.LabelCap + "\n" + def.defName
                    : (editable
                        ? OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Schedule.ClickPaint", "Click to paint ")
                            + (brush.Selected != null ? brush.Selected.LabelCap.ToString() : "—")
                        : "—"));
            }
        }
    }
}