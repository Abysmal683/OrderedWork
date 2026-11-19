using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OrderedWork.Config;
using OrderedWork.Services;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    public partial class MainTabWindow_WorkControl : MainTabWindow
    {
        private void DrawSearchRow(float searchY)
        {
            float x = Pad;
            Widgets.Label(new Rect(x, searchY, 40f, SearchHeight), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.Pawn", "Pawn"));
            x += 42f;
            Rect searchField = new Rect(x, searchY, 168f, SearchHeight - 2f);
            if (Event.current.type == EventType.MouseDown && !searchField.Contains(Event.current.mousePosition))
            {
                GUI.FocusControl(null);
            }
            searchText = Widgets.TextField(searchField, searchText ?? "");
            x += 174f;

            Rect workButton = new Rect(x, searchY, 190f, SearchHeight - 2f);
            x += 194f;
            string workLabel = !WorkFilterActive
                ? OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.Any", "Any")
                : (workFilter != null ? WorkTypePaletteView.DisplayName(workFilter) : workFilterText);
            if (Widgets.ButtonText(workButton, workLabel))
            {
                Find.WindowStack.Add(new WorkTypeComboWindow(this));
            }
            if (Mouse.IsOver(workButton))
            {
                TooltipHandler.TipRegion(workButton, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.FilterWorkTypes", "Filter WorkTypes"));
            }

            Rect notRect = new Rect(x, searchY, 64f, SearchHeight - 2f);
            Widgets.CheckboxLabeled(notRect, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.Not", "Not"), ref invertWorkFilter, !WorkFilterActive);
            if (Mouse.IsOver(notRect))
            {
                TooltipHandler.TipRegion(notRect, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.InvertTip", "Invert the Work filter where it applies"));
            }
            x += 68f;

            Rect scopeRect = new Rect(x, searchY, 118f, SearchHeight - 2f);
            if (UiButtonBar.Button(scopeRect, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.FilterPrefix", "Filter: ") + WorkFilterScopeLabel, WorkFilterActive,
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.FilterScopeTip", "Where the Work filter applies: pawn rows, palette, or both")))
            {
                workFilterScope = NextScope(workFilterScope);
            }
            x += 122f;

            Rect clearRect = new Rect(x, searchY, 56f, SearchHeight - 2f);
            bool canClear = !string.IsNullOrWhiteSpace(searchText)
                || WorkFilterActive
                || invertWorkFilter
                || workFilterScope != WorkFilterScope.Line;
            if (UiButtonBar.Button(clearRect, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.Clear", "Clear"), canClear, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.ClearTip", "Clear the Pawn and Work filters")))
            {
                searchText = "";
                workFilterText = "";
                workFilter = null;
                invertWorkFilter = false;
                workFilterScope = WorkFilterScope.Line;
            }
        }

        private static WorkFilterScope NextScope(WorkFilterScope scope)
        {
            switch (scope)
            {
                case WorkFilterScope.Line:
                    return WorkFilterScope.Palette;
                case WorkFilterScope.Palette:
                    return WorkFilterScope.Both;
                default:
                    return WorkFilterScope.Line;
            }
        }

        private bool IsCurrentWorkFilter(WorkTypeDef workType)
        {
            if (workType == null)
            {
                return false;
            }
            if (workFilter != null)
            {
                return ReferenceEquals(workFilter, workType)
                    || string.Equals(workFilter.defName, workType.defName, StringComparison.Ordinal);
            }
            return !string.IsNullOrEmpty(workFilterText)
                && string.Equals(workFilterText, workType.defName, StringComparison.Ordinal);
        }

        private bool IsCurrentWorkFilterId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }
            if (workFilter != null)
            {
                return string.Equals(workFilter.defName, id, StringComparison.Ordinal);
            }
            return string.Equals(workFilterText, id, StringComparison.Ordinal);
        }

        private bool MatchesPaletteFilter(WorkTypeDef workType)
        {
            if (!ScopeFiltersPalette)
            {
                return true;
            }
            bool match = IsCurrentWorkFilter(workType);
            return invertWorkFilter ? !match : match;
        }

        private bool MatchesLineWorkFilter(Pawn pawn)
        {
            if (!ScopeFiltersLine)
            {
                return true;
            }

            bool active = false;
            foreach (string id in WorkOrderService.GetOrder(pawn))
            {
                if (IsCurrentWorkFilterId(id))
                {
                    active = true;
                    break;
                }
            }
            return invertWorkFilter ? !active : active;
        }

        /// <summary>
        /// WorkType combo with at most 10 visible rows and a thin integrated scrollbar.
        /// The selected WorkType is the filter; there is no separate text field.
        /// </summary>
        private sealed class WorkTypeComboWindow : Window
        {
            private const float RowHeight = 24f;
            private const int MaxVisibleRows = 10;
            private const float BarWidth = WorkGridScrollView.Thickness;

            private readonly MainTabWindow_WorkControl owner;
            private Vector2 scroll;
            private bool draggingBar;
            private float barDragStartMouse;
            private float barDragStartScroll;

            public WorkTypeComboWindow(MainTabWindow_WorkControl owner)
            {
                this.owner = owner;
                doCloseX = true;
                closeOnClickedOutside = true;
                absorbInputAroundWindow = true;
                resizeable = false;
            }

            public override Vector2 InitialSize
            {
                get { return new Vector2(260f, (MaxVisibleRows + 1) * RowHeight + 36f); }
            }

            public override void DoWindowContents(Rect inRect)
            {
                Widgets.Label(new Rect(0f, 0f, inRect.width, 22f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.FilterWork", "Filter Work"));

                // Any only appears while a Work filter is active; the selected WorkType is excluded.
                List<WorkTypeDef> matches = WorkOrderService.GetAvailableWorkTypes()
                    .Where(w => w != null && !owner.IsCurrentWorkFilter(w))
                    .ToList();
                bool showAny = owner.WorkFilterActive;

                Rect listOuter = new Rect(0f, 24f, inRect.width, inRect.height - 24f);
                Rect viewport = new Rect(listOuter.x, listOuter.y, listOuter.width - BarWidth, listOuter.height);
                int rows = matches.Count + (showAny ? 1 : 0);
                Rect content = new Rect(0f, 0f, viewport.width, Mathf.Max(viewport.height, rows * RowHeight));

                Widgets.BeginScrollView(viewport, ref scroll, content, false);
                try
                {
                    int rowIndex = 0;
                    if (showAny)
                    {
                        Rect anyRect = new Rect(0f, 0f, viewport.width, RowHeight - 2f);
                        if (Widgets.ButtonText(anyRect, "Any"))
                        {
                            owner.workFilterText = "";
                            owner.workFilter = null;
                            Close(true);
                        }
                        rowIndex = 1;
                    }

                    for (int i = 0; i < matches.Count; i++)
                    {
                        WorkTypeDef workType = matches[i];
                        Rect row = new Rect(0f, (i + rowIndex) * RowHeight, viewport.width, RowHeight - 2f);
                        if (Widgets.ButtonText(row, WorkTypePaletteView.DisplayName(workType)))
                        {
                            owner.workFilterText = workType.defName;
                            owner.workFilter = workType;
                            Close(true);
                        }
                    }

                }
                finally
                {
                    Widgets.EndScrollView();
                }

                DrawThinScrollbar(
                    new Rect(listOuter.xMax - BarWidth, listOuter.y, BarWidth, listOuter.height),
                    viewport,
                    content);
            }

            private void DrawThinScrollbar(Rect track, Rect viewport, Rect content)
            {
                float max = Mathf.Max(0f, content.height - viewport.height);
                if (max <= 0f)
                {
                    draggingBar = false;
                    return;
                }

                float thumbSize = Mathf.Clamp(track.height * (viewport.height / content.height), 24f, track.height);
                float travel = track.height - thumbSize;
                float thumbY = track.y + travel * Mathf.Clamp01(scroll.y / max);
                Rect thumb = new Rect(track.x, thumbY, track.width, thumbSize);

                Widgets.DrawBoxSolid(track, new Color(0.07f, 0.07f, 0.08f, 0.75f));
                Widgets.DrawBoxSolid(thumb, Mouse.IsOver(thumb)
                    ? new Color(0.62f, 0.70f, 0.78f, 0.95f)
                    : new Color(0.45f, 0.50f, 0.56f, 0.90f));

                Event current = Event.current;
                if (current.type == EventType.MouseDown && current.button == 0 && Mouse.IsOver(thumb))
                {
                    draggingBar = true;
                    barDragStartMouse = current.mousePosition.y;
                    barDragStartScroll = scroll.y;
                    current.Use();
                }
                else if (draggingBar && current.type == EventType.MouseDrag)
                {
                    float delta = current.mousePosition.y - barDragStartMouse;
                    scroll.y = Mathf.Clamp(barDragStartScroll + (travel <= 0f ? 0f : delta / travel * max), 0f, max);
                    current.Use();
                }
                else if (draggingBar && current.type == EventType.MouseUp)
                {
                    draggingBar = false;
                    current.Use();
                }
                else if (current.type == EventType.MouseDown && current.button == 0 && Mouse.IsOver(track) && !Mouse.IsOver(thumb))
                {
                    scroll.y = Mathf.Clamp(scroll.y + (current.mousePosition.y < thumb.y ? -viewport.height : viewport.height), 0f, max);
                    current.Use();
                }
            }
        }

        private bool IsSearchMatch(Pawn pawn)
        {
            string term = searchText?.Trim();
            if (string.IsNullOrEmpty(term))
            {
                return true;
            }
            return SearchBlob(pawn).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string SearchBlob(Pawn pawn)
        {
            string blob = pawn.LabelShort + "\n" + pawn.Label;
            if (pawn.Name != null)
            {
                blob += "\n" + pawn.Name.ToStringFull + "\n" + pawn.Name.ToStringShort;
            }
            blob += "\n" + (string)pawn.NameFullColored;
            if (pawn.story != null)
            {
                blob += "\n" + pawn.story.Title;
            }

            if (pawn.Name is NameTriple triple)
            {
                blob += "\n" + triple.First + "\n" + triple.Nick + "\n" + triple.Last;
            }
            else if (pawn.Name is NameSingle singleName)
            {
                blob += "\n" + singleName.Name;
            }
            return blob;
        }

    }
}