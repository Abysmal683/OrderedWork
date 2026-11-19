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
    {        private void DrawGiverColumn(
            Rect area,
            List<string> names,
            HashSet<string> draftNames,
            bool originSide,
            bool mergeMode,
            ref float scroll,
            string draftMarker,
            int columnId)
        {
            Widgets.DrawBoxSolid(area, UiColors.ColumnBackground);

            bool hasRows = names != null && names.Count > 0;
            float maxScroll = hasRows
                ? Mathf.Max(0f, names.Count * SplitMergeRowHeight - area.height)
                : 0f;
            float contentWidth = area.width - (maxScroll > 0f ? SplitMergeScrollbarWidth + 2f : 0f);
            Rect track = new Rect(area.xMax - SplitMergeScrollbarWidth, area.y, SplitMergeScrollbarWidth, area.height);

            HandleColumnWheel(area, track, maxScroll, ref scroll);
            HandleColumnScrollbarInput(columnId, track, area.height, maxScroll, ref scroll);
            ClampColumnScroll(maxScroll, ref scroll);

            if (!hasRows)
            {
                Widgets.Label(
                    new Rect(area.x + 6f, area.y + 4f, contentWidth - 12f, area.height - 8f),
                    originSide ? "No givers on this work type." : "Drag origin givers here.");
                DrawColumnScrollbar(track, area.height, maxScroll, scroll, columnId);
                return;
            }

            Event current = Event.current;
            for (int i = 0; i < names.Count; i++)
            {
                float y = area.y + i * SplitMergeRowHeight - scroll;
                if (y + SplitMergeRowHeight <= area.y || y >= area.yMax)
                {
                    continue;
                }

                string name = names[i];
                Rect row = new Rect(area.x + 3f, y, contentWidth - 6f, SplitMergeRowHeight - 2f);
                UiZebra.Draw(row, i);
                bool inDraft = originSide && draftNames.Contains(name);
                bool dragged = smDragSide != 0
                    && smDragMergeMode == mergeMode
                    && smDragGiver == name
                    && smDragSide == (originSide ? 1 : 2);
                bool dropTarget = IsValidDropHover(originSide, mergeMode, row);

                DrawSplitMergeRow(row, name, inDraft, dragged, dropTarget, inDraft ? draftMarker : null);

                if (!dragged && smDragSide == 0)
                {
                    TooltipHandler.TipRegion(row, GiverTooltip(DefDatabase<WorkGiverDef>.GetNamedSilentFail(name)));
                }

                if (current.type == EventType.MouseDown
                    && current.button == 0
                    && row.Contains(current.mousePosition))
                {
                    smDragSide = originSide ? 1 : 2;
                    smDragMergeMode = mergeMode;
                    smDragGiver = name;
                    smDragStartY = current.mousePosition.y;
                    smDragMoved = false;
                    current.Use();
                }
            }

            DrawColumnScrollbar(track, area.height, maxScroll, scroll, columnId);
        }

        // Merge destination column: existing target givers are drawn first and are fixed; the
        // incoming origin givers follow and can be dragged/reordered. Their resulting order is
        // saved in WorkTypeConfig.Givers and applied from the profile on restart.
        private void DrawMergeDestinationColumn(Rect area, List<string> existingNames, List<string> incomingNames, ref float scroll)
        {
            Widgets.DrawBoxSolid(area, UiColors.ColumnBackground);

            int fixedCount = existingNames != null ? existingNames.Count : 0;
            int incomingCount = incomingNames != null ? incomingNames.Count : 0;
            int total = fixedCount + incomingCount;
            float maxScroll = total > 0
                ? Mathf.Max(0f, total * SplitMergeRowHeight - area.height)
                : 0f;
            float contentWidth = area.width - (maxScroll > 0f ? SplitMergeScrollbarWidth + 2f : 0f);
            Rect track = new Rect(area.xMax - SplitMergeScrollbarWidth, area.y, SplitMergeScrollbarWidth, area.height);

            HandleColumnWheel(area, track, maxScroll, ref scroll);
            HandleColumnScrollbarInput(2, track, area.height, maxScroll, ref scroll);
            ClampColumnScroll(maxScroll, ref scroll);

            if (total == 0)
            {
                Widgets.Label(
                    new Rect(area.x + 6f, area.y + 4f, contentWidth - 12f, area.height - 8f),
                    "Select a target, then drag origin givers here.");
                DrawColumnScrollbar(track, area.height, maxScroll, scroll, 2);
                return;
            }

            Event current = Event.current;
            for (int i = 0; i < total; i++)
            {
                float y = area.y + i * SplitMergeRowHeight - scroll;
                if (y + SplitMergeRowHeight <= area.y || y >= area.yMax)
                {
                    continue;
                }

                bool isFixed = i < fixedCount;
                string name = isFixed ? existingNames[i] : incomingNames[i - fixedCount];
                Rect row = new Rect(area.x + 3f, y, contentWidth - 6f, SplitMergeRowHeight - 2f);
                UiZebra.Draw(row, i);
                bool dragged = !isFixed
                    && smDragSide == 2
                    && smDragMergeMode
                    && smDragGiver == name;
                bool dropTarget = !isFixed
                    && smDragSide != 0
                    && smDragMergeMode
                    && smDragMoved
                    && row.Contains(current.mousePosition);

                if (isFixed)
                {
                    DrawSplitMergeRow(row, (i + 1) + ". " + name, true, false, false, "(fixed)", false);
                }
                else
                {
                    int position = fixedCount + (i - fixedCount) + 1;
                    DrawSplitMergeRow(row, position + ". " + name, false, dragged, dropTarget, null);
                }

                if (!dragged && smDragSide == 0)
                {
                    TooltipHandler.TipRegion(row, GiverTooltip(DefDatabase<WorkGiverDef>.GetNamedSilentFail(name)));
                }

                if (!isFixed
                    && current.type == EventType.MouseDown
                    && current.button == 0
                    && row.Contains(current.mousePosition))
                {
                    smDragSide = 2;
                    smDragMergeMode = true;
                    smDragGiver = name;
                    smDragStartY = current.mousePosition.y;
                    smDragMoved = false;
                    current.Use();
                }
            }

            // Work out where a dragged merge row would be inserted, including the empty area of
            // the column. A drop above all incoming rows inserts at 0; below them appends.
            if ((smDragSide == 1 || smDragSide == 2) && smDragMergeMode && smDragMoved && Mouse.IsOver(area))
            {
                int hovered = -1;
                for (int j = 0; j < incomingCount; j++)
                {
                    float y = area.y + (fixedCount + j) * SplitMergeRowHeight - scroll;
                    Rect row = new Rect(area.x + 3f, y, contentWidth - 6f, SplitMergeRowHeight - 2f);
                    if (row.Contains(current.mousePosition))
                    {
                        hovered = j;
                        break;
                    }
                }
                if (hovered >= 0)
                {
                    smMergeDropIndex = hovered;
                }
                else
                {
                    float incomingTop = area.y + fixedCount * SplitMergeRowHeight - scroll;
                    smMergeDropIndex = current.mousePosition.y < incomingTop ? 0 : incomingCount;
                }
            }

            DrawColumnScrollbar(track, area.height, maxScroll, scroll, 2);
        }

        private static void HandleColumnWheel(Rect area, Rect track, float maxScroll, ref float scroll)
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

            float delta = current.delta.y;
            if (Mathf.Abs(delta) > 0.001f)
            {
                scroll += delta > 0f ? -SplitMergeRowHeight : SplitMergeRowHeight;
            }
            current.Use();
        }

        private void HandleColumnScrollbarInput(int columnId, Rect track, float visibleHeight, float maxScroll, ref float scroll)
        {
            if (maxScroll <= 0f)
            {
                if (smScrollDragColumn == columnId)
                {
                    smScrollDragColumn = 0;
                }
                return;
            }

            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && track.Contains(current.mousePosition))
            {
                smScrollDragColumn = columnId;
                UpdateColumnScrollFromMouse(track, visibleHeight, maxScroll, current.mousePosition.y, ref scroll);
                current.Use();
            }
            else if (smScrollDragColumn == columnId && current.type == EventType.MouseDrag)
            {
                UpdateColumnScrollFromMouse(track, visibleHeight, maxScroll, current.mousePosition.y, ref scroll);
                current.Use();
            }
            else if (smScrollDragColumn == columnId && current.type == EventType.MouseUp)
            {
                smScrollDragColumn = 0;
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

        private static void ClampColumnScroll(float maxScroll, ref float scroll)
        {
            // Keep scroll on whole rows so no row is ever drawn half-clipped.
            scroll = Mathf.Clamp(scroll, 0f, maxScroll);
            scroll = Mathf.Floor(scroll / SplitMergeRowHeight) * SplitMergeRowHeight;
            scroll = Mathf.Clamp(scroll, 0f, maxScroll);
        }

        private void DrawColumnScrollbar(Rect track, float visibleHeight, float maxScroll, float scroll, int columnId)
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
            bool active = smScrollDragColumn == columnId;
            Color color = active || Mouse.IsOver(thumb)
                ? new Color(0.72f, 0.80f, 0.92f, 1f)
                : new Color(0.48f, 0.53f, 0.60f, 0.95f);
            Widgets.DrawBoxSolid(thumb, color);
            TooltipHandler.TipRegion(track, "Scrollbar: drag it, or Ctrl+wheel over this column. A plain wheel scrolls the settings.");
        }

        private void DrawSplitMergeRow(Rect row, string giverName, bool inDraft, bool dragged, bool dropTarget, string draftMarker, bool showHandle = true)
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