using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.UI;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork
{
    internal sealed partial class SplitMergePanel
    {
        internal void DrawMergeDestinationColumn(Rect area, List<string>? existingNames, List<string>? incomingNames, ref float scroll)
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

            OrderedWorkMod.HandleColumnWheel(area, track, maxScroll, ref scroll);
            host.HandleColumnScrollbarInput(2, track, area.height, maxScroll, ref scroll);
            OrderedWorkMod.ClampColumnScroll(maxScroll, ref scroll);

            if (total == 0)
            {
                Widgets.Label(
                    new Rect(area.x + 6f, area.y + 4f, contentWidth - 12f, area.height - 8f),
                    OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.DragTarget", "Select a target, then drag origin givers here."));
                host.DrawColumnScrollbar(track, area.height, maxScroll, scroll, 2);
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
                string name = (isFixed ? existingNames![i] : incomingNames![i - fixedCount]) ?? "";
                Rect row = new Rect(area.x + 3f, y, contentWidth - 6f, SplitMergeRowHeight - 2f);
                UiZebra.Draw(row, i);
                bool dragged = !isFixed
                    && DragSide == 2
                    && DragMergeMode
                    && DragGiver == name;
                bool dropTarget = !isFixed
                    && DragSide != 0
                    && DragMergeMode
                    && DragMoved
                    && row.Contains(current.mousePosition);

                if (isFixed)
                {
                    host.DrawSplitMergeRow(row, (i + 1) + ". " + name, true, false, false, OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.Fixed", "(fixed)"), false);
                }
                else
                {
                    int position = fixedCount + (i - fixedCount) + 1;
                    host.DrawSplitMergeRow(row, position + ". " + name, false, dragged, dropTarget, null);
                }

                if (!dragged && DragSide == 0)
                {
                    TooltipHandler.TipRegion(row, OrderedWorkMod.GiverTooltip(DefDatabase<WorkGiverDef>.GetNamedSilentFail(name)));
                }

                if (!isFixed
                    && current.type == EventType.MouseDown
                    && current.button == 0
                    && row.Contains(current.mousePosition))
                {
                    DragSide = 2;
                    DragMergeMode = true;
                    DragGiver = name;
                    DragStartY = current.mousePosition.y;
                    DragMoved = false;
                    current.Use();
                }
            }

            // Work out where a dragged merge row would be inserted, including the empty area of
            // the column. A drop above all incoming rows inserts at 0; below them appends.
            if ((DragSide == 1 || DragSide == 2) && DragMergeMode && DragMoved && Mouse.IsOver(area))
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
                    MergeDropIndex = hovered;
                }
                else
                {
                    float incomingTop = area.y + fixedCount * SplitMergeRowHeight - scroll;
                    MergeDropIndex = current.mousePosition.y < incomingTop ? 0 : incomingCount;
                }
            }

            host.DrawColumnScrollbar(track, area.height, maxScroll, scroll, 2);
        }

        internal void DrawGiverColumn(
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
                ? Mathf.Max(0f, names!.Count * SplitMergeRowHeight - area.height)
                : 0f;
            float contentWidth = area.width - (maxScroll > 0f ? SplitMergeScrollbarWidth + 2f : 0f);
            Rect track = new Rect(area.xMax - SplitMergeScrollbarWidth, area.y, SplitMergeScrollbarWidth, area.height);

            OrderedWorkMod.HandleColumnWheel(area, track, maxScroll, ref scroll);
            host.HandleColumnScrollbarInput(columnId, track, area.height, maxScroll, ref scroll);
            OrderedWorkMod.ClampColumnScroll(maxScroll, ref scroll);

            if (!hasRows)
            {
                Widgets.Label(
                    new Rect(area.x + 6f, area.y + 4f, contentWidth - 12f, area.height - 8f),
                    originSide ? OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.NoGivers", "No givers on this work type.") : OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.DragOrigin", "Drag origin givers here."));
                host.DrawColumnScrollbar(track, area.height, maxScroll, scroll, columnId);
                return;
            }

            Event current = Event.current;
            for (int i = 0; i < names!.Count; i++)
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
                bool dragged = DragSide != 0
                    && DragMergeMode == mergeMode
                    && DragGiver == name
                    && DragSide == (originSide ? 1 : 2);
                bool dropTarget = host.IsValidDropHover(originSide, mergeMode, row);

                host.DrawSplitMergeRow(row, name, inDraft, dragged, dropTarget, inDraft ? draftMarker : null);

                if (!dragged && DragSide == 0)
                {
                    TooltipHandler.TipRegion(row, OrderedWorkMod.GiverTooltip(DefDatabase<WorkGiverDef>.GetNamedSilentFail(name)));
                }

                if (current.type == EventType.MouseDown
                    && current.button == 0
                    && row.Contains(current.mousePosition))
                {
                    DragSide = originSide ? 1 : 2;
                    DragMergeMode = mergeMode;
                    DragGiver = name;
                    DragStartY = current.mousePosition.y;
                    DragMoved = false;
                    current.Use();
                }
            }

            host.DrawColumnScrollbar(track, area.height, maxScroll, scroll, columnId);
        }

    }
}
