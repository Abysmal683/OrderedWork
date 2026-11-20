using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OrderedWork.Config;
using OrderedWork.Core;
using OrderedWork.Services;
using OrderedWork.UI;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;
using System.Text;

namespace OrderedWork
{
    public partial class OrderedWorkMod : Mod
    {
        // Merge destination column: existing target givers are drawn first and are fixed; the
        // incoming origin givers follow and can be dragged/reordered. Their resulting order is
        // saved in WorkTypeConfig.Givers and applied from the profile on restart.
        internal static void HandleColumnWheel(Rect area, Rect track, float maxScroll, ref float scroll)
        {
            if (maxScroll <= 0f)
            {
                return;
            }

            Event current = Event.current;
            if (current.type != EventType.ScrollWheel)
            {
                return;
            }

            bool ctrl = current.control || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool overTrack = track.Contains(current.mousePosition);
            bool ctrlOverColumn = ctrl && Mouse.IsOver(area);
            if (!overTrack && !ctrlOverColumn)
            {
                // Not ours: leave the event alone so the outer settings scroll handles the wheel.
                return;
            }

            ApplyWheelStep(current, maxScroll, ref scroll, SplitMergeRowHeight);
            current.Use();
        }

        internal static void HandleCtrlWheel(Rect area, float maxScroll, ref float scroll, float step)
        {
            if (maxScroll <= 0f)
            {
                return;
            }

            Event current = Event.current;
            if (current.type != EventType.ScrollWheel || !Mouse.IsOver(area))
            {
                return;
            }

            bool ctrl = current.control || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (!ctrl)
            {
                return;
            }

            ApplyWheelStep(current, maxScroll, ref scroll, step);
            current.Use();
        }

        private static void ApplyWheelStep(Event current, float maxScroll, ref float scroll, float step)
        {
            float delta = current.delta.y;
            if (Mathf.Abs(delta) <= 0.001f)
            {
                return;
            }
            scroll = Mathf.Clamp(scroll + (delta > 0f ? step : -step), 0f, maxScroll);
        }

        internal void HandleColumnScrollbarInput(int columnId, Rect track, float visibleHeight, float maxScroll, ref float scroll)
        {
            if (maxScroll <= 0f)
            {
                if (splitMerge.ScrollDragColumn == columnId)
                {
                    splitMerge.ScrollDragColumn = 0;
                }
                return;
            }

            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && track.Contains(current.mousePosition))
            {
                splitMerge.ScrollDragColumn = columnId;
                UpdateColumnScrollFromMouse(track, visibleHeight, maxScroll, current.mousePosition.y, ref scroll);
                current.Use();
            }
            else if (splitMerge.ScrollDragColumn == columnId && current.type == EventType.MouseDrag)
            {
                UpdateColumnScrollFromMouse(track, visibleHeight, maxScroll, current.mousePosition.y, ref scroll);
                current.Use();
            }
            else if (splitMerge.ScrollDragColumn == columnId && current.type == EventType.MouseUp)
            {
                splitMerge.ScrollDragColumn = 0;
                current.Use();
            }
        }

        private static void UpdateColumnScrollFromMouse(Rect track, float visibleHeight, float maxScroll, float mouseY, ref float scroll)
        {
            float contentHeight = visibleHeight + maxScroll;
            float thumbHeight = Mathf.Clamp(track.height * (visibleHeight / contentHeight), 20f, track.height);
            float travel = track.height - thumbHeight;
            float t = travel <= 0f ? 0f : (mouseY - track.y - thumbHeight * 0.5f) / travel;
            scroll = Mathf.Clamp01(t) * maxScroll;
        }

        internal static void ClampColumnScroll(float maxScroll, ref float scroll)
        {
            // Keep scroll on whole rows so no row is ever drawn half-clipped.
            scroll = Mathf.Clamp(scroll, 0f, maxScroll);
            scroll = Mathf.Floor(scroll / SplitMergeRowHeight) * SplitMergeRowHeight;
            scroll = Mathf.Clamp(scroll, 0f, maxScroll);
        }

        internal void DrawColumnScrollbar(Rect track, float visibleHeight, float maxScroll, float scroll, int columnId)
        {
            if (maxScroll <= 0f)
            {
                return;
            }

            Widgets.DrawBoxSolid(track, new Color(0.05f, 0.05f, 0.06f, 0.80f));
            float contentHeight = visibleHeight + maxScroll;
            float thumbHeight = Mathf.Clamp(track.height * (visibleHeight / contentHeight), 20f, track.height);
            float travel = track.height - thumbHeight;
            float t = Mathf.Clamp01(scroll / maxScroll);
            Rect thumb = new Rect(track.x + 1f, track.y + travel * t, track.width - 2f, thumbHeight);
            bool active = splitMerge.ScrollDragColumn == columnId;
            Color color = active || Mouse.IsOver(thumb)
                ? new Color(0.72f, 0.80f, 0.92f, 1f)
                : new Color(0.48f, 0.53f, 0.60f, 0.95f);
            Widgets.DrawBoxSolid(thumb, color);
            TooltipHandler.TipRegion(track, ConfigText("OrderedWork.Config.SplitMerge.ScrollbarTip", "Scrollbar: drag it, or Ctrl+wheel over this column. A plain wheel scrolls the settings."));
        }

        internal void DrawSplitMergeRow(Rect row, string giverName, bool inDraft, bool dragged, bool dropTarget, string? draftMarker, bool showHandle = true)
        {
            if (dragged)
            {
                Widgets.DrawBoxSolid(row, new Color(0.22f, 0.42f, 0.62f, 0.60f));
            }
            else if (dropTarget)
            {
                // Blue/green drop tint: reads as "accepts this drag" against the dark list.
                Widgets.DrawBoxSolid(row, new Color(0.28f, 0.74f, 0.58f, 0.45f));
            }
            else if (Mouse.IsOver(row))
            {
                Widgets.DrawHighlight(row);
            }
            else if (inDraft)
            {
                Widgets.DrawBoxSolid(row, new Color(0.30f, 0.30f, 0.30f, 0.30f));
            }

            Color previous = GUI.color;
            if (inDraft && !dragged)
            {
                GUI.color = new Color(0.62f, 0.62f, 0.62f, 0.90f);
            }

            string text = string.IsNullOrEmpty(draftMarker) ? giverName : giverName + "  " + draftMarker;
            Widgets.LabelEllipses(new Rect(row.x + 4f, row.y, row.width - 26f, row.height), text);
            GUI.color = previous;
            if (showHandle)
            {
                Widgets.Label(new Rect(row.xMax - 20f, row.y, 16f, row.height), "≡");
            }
        }

    }
}