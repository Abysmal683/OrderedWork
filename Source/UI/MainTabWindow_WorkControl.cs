using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OrderedWork.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Full-screen "Work Control" tab: colonist-bar pawn order, a 3-row WorkType palette,
    /// a Pawn/Work search row, and a fixed-name + shared-horizontal-scroll matrix with
    /// thin auto-hiding scrollbars outside the content.
    /// </summary>
    public class MainTabWindow_WorkControl : MainTabWindow
    {
        private const float Pad = 4f;
        private const float TitleHeight = 28f;
        private const float PaletteHeight = 84f;
        private const float SearchHeight = 24f;
        private const float HScrollHeight = WorkGridScrollView.Thickness;
        private const float HeaderHeight = 18f;
        private const float RowHeight = 42f;
        private const float CardWidth = 135f;
        private const float CardHeight = 30f;
        private const float CardGap = 4f;
        private const float LeftColumnMin = 160f;
        private const float LeftColumnMax = 260f;
        private const float ColumnGutter = 6f;
        private const int DebugPaletteTypeCount = 60;
        private const float MinPanelHeight = 360f; // top UI + 4 pawn rows
        private const float DefaultPanelHeightFactor = 0.5f;

        private Vector2 gridScroll;
        private Vector2 paletteScroll;
        private string searchText = "";
        private string workFilterText = "";
        private WorkTypeDef workFilter;
        private bool invertWorkFilter;
        private WorkFilterScope workFilterScope = WorkFilterScope.Line;

        /// <summary>Where the active Work filter applies: pawn rows, palette chips, or both.</summary>
        private enum WorkFilterScope
        {
            Line,
            Palette,
            Both
        }

        public override Vector2 RequestedTabSize
        {
            get
            {
                float max = Mathf.Max(MinPanelHeight, Verse.UI.screenHeight - 35f);
                float desired = OrderedWorkMod.Settings != null && OrderedWorkMod.Settings.workControlHeight > 0f
                    ? OrderedWorkMod.Settings.workControlHeight
                    : max * DefaultPanelHeightFactor;
                return new Vector2(Verse.UI.screenWidth, Mathf.Clamp(desired, MinPanelHeight, max));
            }
        }

        private bool resizingPanel;
        private float resizeStartMouseY;
        private float resizeStartHeight;

        private bool draggingPalette;
        private WorkTypeDef paletteWorkType;
        private Pawn paletteHoverPawn;
        private int paletteDropIndex;

        private bool dragging;
        private Pawn dragPawn;
        private int dragSourceIndex = -1;
        private int dragTargetIndex = -1;
        private int dragCount;

        /// <summary>Future per-pawn/per-giver override hook (not implemented yet).</summary>
        internal static Action<Pawn, WorkTypeDef, StringBuilder> PawnSpecificOverrideHook { get; set; }

        public override void DoWindowContents(Rect inRect)
        {
            // MainTabWindow normally updates its own bottom-anchored rectangle each frame.
            // Our override must do it too, otherwise the resize grip changes only the setting.
            SetInitialSizeAndPosition();
            Widgets.DrawBoxSolid(inRect, new Color(0.11f, 0.12f, 0.13f, 1f));
            DrawPanelResizeHandle(inRect);

            if (Find.CurrentMap == null)
            {
                DrawCentered(inRect, "Load a colony");
                return;
            }

            List<Pawn> allColonists = GetColonistsInDisplayOrder();
            if (allColonists.Count == 0)
            {
                DrawCentered(inRect, "No free colonists");
                return;
            }

            WorkOrderService.EnsureColonyInitialized();

            float leftWidth = Mathf.Clamp(inRect.width * 0.22f, LeftColumnMin, LeftColumnMax);
            float titleY = Pad;
            float paletteY = titleY + TitleHeight;
            float searchY = paletteY + PaletteHeight + 2f;
            float hScrollY = searchY + SearchHeight + 2f;
            float headerY = hScrollY + HScrollHeight + 2f;
            float bodyTop = headerY + HeaderHeight + 2f;
            float bodyHeight = Mathf.Max(40f, inRect.yMax - Pad - bodyTop);
            Rect bodyRect = new Rect(Pad, bodyTop, inRect.width - Pad * 2f, bodyHeight);
            Widgets.DrawBoxSolid(
                new Rect(bodyRect.x + leftWidth - 1f, headerY, 2f, bodyRect.yMax - headerY),
                new Color(0.32f, 0.36f, 0.40f, 1f));
            Rect paletteRect = new Rect(Pad, paletteY, inRect.width - Pad * 2f, PaletteHeight);

            // Title + top controls.
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(Pad, titleY, Mathf.Max(100f, inRect.width - 330f), TitleHeight), "Work Control");
            Text.Font = GameFont.Small;

            Rect viewRect = new Rect(inRect.xMax - Pad - 120f, titleY + 2f, 120f, 24f);
            Rect detailsRect = new Rect(viewRect.x - 4f - 80f, titleY + 2f, 80f, 24f);
            Rect recommendRect = new Rect(detailsRect.x - 4f - 98f, titleY + 2f, 98f, 24f);
            DrawTopControls(recommendRect, detailsRect, viewRect);

            List<WorkTypeDef> available = WorkOrderService.GetAvailableWorkTypes();
            List<WorkTypeDef> paletteTypes = GetPaletteOrder(available)
                .Where(MatchesPaletteFilter)
                .ToList();
            if (OrderedWorkMod.Settings != null && OrderedWorkMod.Settings.paletteStressTest)
            {
                paletteTypes.AddRange(WorkTypePaletteView.BuildDebugPaletteTypes(DebugPaletteTypeCount));
            }
            bool autoHide = OrderedWorkMod.Settings == null || OrderedWorkMod.Settings.autoHideScrollbars;
            bool showPaletteScrollbar = !autoHide
                || Mouse.IsOver(paletteRect)
                || WorkGridScrollView.IsDragging
                || WorkGridScrollView.RecentlyScrolled;

            // 3-row WorkType palette (top).
            WorkTypePaletteView.DrawPalette(
                paletteRect,
                ref paletteScroll,
                paletteTypes,
                paletteWorkType,
                showPaletteScrollbar,
                dragging || draggingPalette,
                BeginPaletteDrag);

            // Search row below the palette, above the matrix.
            DrawSearchRow(searchY);

            bool filtering = !string.IsNullOrWhiteSpace(searchText) || ScopeFiltersLine;
            List<Pawn> colonists = allColonists
                .Where(p => IsSearchMatch(p) && MatchesLineWorkFilter(p))
                .ToList();

            if (filtering)
            {
                TextAnchor previousMatchAnchor = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(
                    new Rect(inRect.xMax - Pad - 180f, searchY, 180f, SearchHeight),
                    colonists.Count + (colonists.Count == 1 ? " match" : " matches"));
                Text.Anchor = previousMatchAnchor;
            }

            UpdatePaletteDrag(bodyRect, leftWidth, colonists);

            // Column header.
            Widgets.Label(new Rect(bodyRect.x, headerY, leftWidth - 6f, HeaderHeight), "Pawn");
            Widgets.Label(new Rect(bodyRect.x + leftWidth, headerY, bodyRect.width - leftWidth, HeaderHeight),
                "Work (left = highest). Drag to reorder, × remove, + add.");

            if (colonists.Count == 0)
            {
                CancelPaletteDrag();
                DrawCentered(bodyRect, filtering ? "No pawns match" : "No free colonists");
                return;
            }

            float vThickness = WorkGridScrollView.Thickness;
            Rect gridViewport = new Rect(
                bodyRect.x,
                bodyRect.y,
                Mathf.Max(0f, bodyRect.width - vThickness),
                bodyRect.height);
            Rect namesViewport = new Rect(bodyRect.x, bodyRect.y, leftWidth, bodyRect.height);
            Rect laneViewport = new Rect(
                bodyRect.x + leftWidth,
                bodyRect.y,
                Mathf.Max(0f, gridViewport.width - leftWidth),
                bodyRect.height);
            Rect vTrack = new Rect(bodyRect.xMax - vThickness, bodyRect.y, vThickness, bodyRect.height);
            Rect hTrack = new Rect(
                bodyRect.x + leftWidth,
                hScrollY,
                Mathf.Max(0f, gridViewport.width - leftWidth),
                HScrollHeight);

            int maxLane = 0;
            foreach (Pawn pawn in colonists)
            {
                maxLane = Mathf.Max(maxLane, BuildLane(pawn).types.Count);
            }

            float laneContentWidth = Mathf.Max(laneViewport.width, (maxLane + 1) * (CardWidth + CardGap) + CardGap);
            float rowsHeight = colonists.Count * RowHeight;
            float contentHeight = Mathf.Max(bodyRect.height, rowsHeight);
            Rect laneContent = new Rect(0f, 0f, laneContentWidth, contentHeight);

            // Wheel works over both the fixed names and the lanes.
            WorkGridScrollView.HandleWheel(bodyRect, gridViewport, laneContent, ref gridScroll);
            if (Event.current.type == EventType.ScrollWheel && Event.current.shift)
            {
                Log.Message("[OrderedWork][Scroll] shiftGeometry maxLane=" + maxLane
                    + " content=" + laneContentWidth + " laneViewport=" + laneViewport.width
                    + " scrollX=" + gridScroll.x);
            }

            bool showVerticalScrollbar = !autoHide
                || Mouse.IsOver(bodyRect)
                || Mouse.IsOver(vTrack)
                || Mouse.IsOver(hTrack)
                || WorkGridScrollView.IsDragging
                || WorkGridScrollView.RecentlyScrolled;
            bool showHorizontalScrollbar = !autoHide
                || Mouse.IsOver(bodyRect)
                || Mouse.IsOver(hTrack)
                || Mouse.IsOver(vTrack)
                || WorkGridScrollView.IsDragging
                || WorkGridScrollView.RecentlyScrolled;

            // Handle bar input before the scroll view so the content cannot eat MouseDown.
            if (showVerticalScrollbar)
            {
                WorkGridScrollView.HandleVertical(vTrack, gridViewport, laneContent, ref gridScroll);
            }
            if (showHorizontalScrollbar)
            {
                WorkGridScrollView.HandleHorizontal(hTrack, laneViewport, laneContent, ref gridScroll, this);
            }

            Widgets.BeginScrollView(laneViewport, ref gridScroll, laneContent, false);
            try
            {
                HandleDrop();

                for (int i = 0; i < colonists.Count; i++)
                {
                    Pawn pawn = colonists[i];
                    float rowY = i * RowHeight;
                    Rect rowRect = new Rect(0f, rowY, laneContentWidth, RowHeight);

                    Color rowColor = (i % 2 == 0)
                        ? new Color(0.14f, 0.15f, 0.17f, 0.55f)
                        : new Color(0.16f, 0.17f, 0.19f, 0.55f);
                    Widgets.DrawBoxSolid(rowRect, rowColor);
                    DrawRowState(rowRect, pawn);
                    if (draggingPalette && paletteHoverPawn == pawn)
                    {
                        Widgets.DrawHighlight(rowRect);
                    }

                    DrawRowLane(pawn, rowY, available);
                }
            }
            finally
            {
                Widgets.EndScrollView();
                Text.Font = GameFont.Small;
            }

            DrawPawnColumn(colonists, namesViewport, leftWidth);

            if (showVerticalScrollbar)
            {
                WorkGridScrollView.DrawVertical(vTrack, gridViewport, laneContent, ref gridScroll);
            }
            if (showHorizontalScrollbar)
            {
                WorkGridScrollView.DrawHorizontal(hTrack, laneViewport, laneContent, ref gridScroll);
            }

            DrawHeldCard();
        }

        /// <summary>Top grip: drag up to grow the panel, down to shrink it; 4 pawn rows minimum.</summary>
        private void DrawPanelResizeHandle(Rect inRect)
        {
            Rect handle = new Rect(inRect.center.x - 80f, 0f, 160f, 12f);
            Widgets.DrawBoxSolid(handle, new Color(0.32f, 0.36f, 0.40f, 1f));
            Widgets.DrawBoxSolid(new Rect(handle.x + 20f, handle.y + 4f, 120f, 2f), new Color(0.62f, 0.68f, 0.72f, 1f));
            TooltipHandler.TipRegion(handle, "Drag to resize Work Control (minimum: 4 pawn rows)");

            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && handle.Contains(current.mousePosition))
            {
                resizingPanel = true;
                resizeStartMouseY = Verse.UI.MousePositionOnUIInverted.y;
                resizeStartHeight = windowRect.height;
                Log.Message("[OrderedWork][Resize] down mouse=" + current.mousePosition + " height=" + inRect.height);
                current.Use();
            }
            else if (resizingPanel && current.type == EventType.MouseDrag)
            {
                if (OrderedWorkMod.Settings != null)
                {
                    float max = Mathf.Max(MinPanelHeight, Verse.UI.screenHeight - 35f);
                    float newHeight = resizeStartHeight + (resizeStartMouseY - Verse.UI.MousePositionOnUIInverted.y);
                    OrderedWorkMod.Settings.workControlHeight = Mathf.Clamp(newHeight, MinPanelHeight, max);
                    Log.Message("[OrderedWork][Resize] drag mouse=" + current.mousePosition
                        + " requested=" + OrderedWorkMod.Settings.workControlHeight);
                }
                current.Use();
            }
            else if (resizingPanel && current.type == EventType.MouseUp)
            {
                Log.Message("[OrderedWork][Resize] up saved=" + (OrderedWorkMod.Settings != null ? OrderedWorkMod.Settings.workControlHeight : -1f));
                resizingPanel = false;
                current.Use();
            }
        }

        private void DrawSearchRow(float searchY)
        {
            float x = Pad;
            Widgets.Label(new Rect(x, searchY, 40f, SearchHeight), "Pawn");
            x += 42f;
            searchText = Widgets.TextField(new Rect(x, searchY, 168f, SearchHeight - 2f), searchText ?? "");
            x += 174f;

            Rect workButton = new Rect(x, searchY, 190f, SearchHeight - 2f);
            x += 194f;
            string workLabel = !WorkFilterActive
                ? "Any"
                : (workFilter != null ? WorkTypePaletteView.DisplayName(workFilter) : workFilterText);
            if (Widgets.ButtonText(workButton, workLabel))
            {
                Find.WindowStack.Add(new WorkTypeComboWindow(this));
            }
            if (Mouse.IsOver(workButton))
            {
                TooltipHandler.TipRegion(workButton, "Filter WorkTypes");
            }

            Rect notRect = new Rect(x, searchY, 64f, SearchHeight - 2f);
            Widgets.CheckboxLabeled(notRect, "Not", ref invertWorkFilter, !WorkFilterActive);
            if (Mouse.IsOver(notRect))
            {
                TooltipHandler.TipRegion(notRect, "Invert the Work filter where it applies");
            }
            x += 68f;

            Rect scopeRect = new Rect(x, searchY, 118f, SearchHeight - 2f);
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && WorkFilterActive;
            if (Widgets.ButtonText(scopeRect, "Filter: " + WorkFilterScopeLabel))
            {
                workFilterScope = NextScope(workFilterScope);
            }
            GUI.enabled = previousEnabled;
            if (Mouse.IsOver(scopeRect))
            {
                TooltipHandler.TipRegion(scopeRect, "Where the Work filter applies: pawn rows, palette, or both");
            }
            x += 122f;

            Rect clearRect = new Rect(x, searchY, 56f, SearchHeight - 2f);
            bool canClear = !string.IsNullOrWhiteSpace(searchText)
                || WorkFilterActive
                || invertWorkFilter
                || workFilterScope != WorkFilterScope.Line;
            GUI.enabled = previousEnabled && canClear;
            if (Widgets.ButtonText(clearRect, "Clear"))
            {
                searchText = "";
                workFilterText = "";
                workFilter = null;
                invertWorkFilter = false;
                workFilterScope = WorkFilterScope.Line;
            }
            GUI.enabled = previousEnabled;
            if (Mouse.IsOver(clearRect))
            {
                TooltipHandler.TipRegion(clearRect, "Clear the Pawn and Work filters");
            }
        }

        private string WorkFilterScopeLabel
        {
            get
            {
                switch (workFilterScope)
                {
                    case WorkFilterScope.Palette:
                        return "Palette";
                    case WorkFilterScope.Both:
                        return "Both";
                    default:
                        return "Line";
                }
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

        private bool WorkFilterActive
        {
            get { return workFilter != null || !string.IsNullOrEmpty(workFilterText); }
        }

        private bool ScopeFiltersLine
        {
            get
            {
                return WorkFilterActive
                    && (workFilterScope == WorkFilterScope.Line || workFilterScope == WorkFilterScope.Both);
            }
        }

        private bool ScopeFiltersPalette
        {
            get
            {
                return WorkFilterActive
                    && (workFilterScope == WorkFilterScope.Palette || workFilterScope == WorkFilterScope.Both);
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
                Widgets.Label(new Rect(0f, 0f, inRect.width, 22f), "Filter Work");

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

        private void BeginPaletteDrag(WorkTypeDef type)
        {
            draggingPalette = true;
            paletteWorkType = type;
            paletteHoverPawn = null;
            paletteDropIndex = 0;
        }

        private void CancelPaletteDrag()
        {
            draggingPalette = false;
            paletteWorkType = null;
            paletteHoverPawn = null;
        }

        private void UpdatePaletteDrag(Rect bodyRect, float leftWidth, List<Pawn> colonists)
        {
            if (!draggingPalette || paletteWorkType == null)
            {
                return;
            }

            Vector2 mouse = Event.current.mousePosition;
            paletteHoverPawn = null;
            paletteDropIndex = 0;

            if (bodyRect.Contains(mouse))
            {
                float contentY = mouse.y - bodyRect.y + gridScroll.y;
                int row = Mathf.FloorToInt(contentY / RowHeight);
                if (row >= 0 && row < colonists.Count)
                {
                    paletteHoverPawn = colonists[row];
                    int laneCount = BuildLane(paletteHoverPawn).types.Count;
                    float contentX = mouse.x - (bodyRect.x + leftWidth) + gridScroll.x;
                    paletteDropIndex = SlotAtX(contentX, laneCount);
                }
            }

            if (Event.current.type == EventType.MouseUp)
            {
                if (paletteHoverPawn != null)
                {
                    List<Pawn> selected = GetSelectedColonists();
                    List<Pawn> targets;
                    if (selected.Count >= 2 && selected.Contains(paletteHoverPawn))
                    {
                        // Drop inside the selected group: apply to every selected pawn.
                        targets = selected;
                    }
                    else
                    {
                        // No group, or drop on an unselected row: the hovered pawn only.
                        targets = new List<Pawn> { paletteHoverPawn };
                    }
                    WorkTypePaletteView.ApplyDrop(paletteWorkType, paletteHoverPawn, paletteDropIndex, targets);
                }
                CancelPaletteDrag();
                Event.current.Use();
            }
        }

        private void DrawTopControls(Rect recommendRect, Rect detailsRect, Rect viewRect)
        {
            List<Pawn> selected = GetSelectedColonists();
            bool hasSelection = selected.Count > 0;

            if (Widgets.ButtonText(recommendRect, "Recommend", true, true, hasSelection))
            {
                for (int i = 0; i < selected.Count; i++)
                {
                    WorkOrderService.GenerateAndApplyRecommendation(selected[i]);
                }
            }
            if (Mouse.IsOver(recommendRect))
            {
                TooltipHandler.TipRegion(recommendRect, "Generate recommendation for selected pawn(s)");
            }

            if (Widgets.ButtonText(detailsRect, "Details", true, true, hasSelection))
            {
                Find.WindowStack.Add(new Dialog_WorkControlDetails(selected[0]));
            }
            if (Mouse.IsOver(detailsRect))
            {
                TooltipHandler.TipRegion(detailsRect, "More details for the first selected pawn");
            }

            if (Widgets.ButtonText(viewRect, "View: Numeric"))
            {
                MainButtonDef workTab = DefDatabase<MainButtonDef>.GetNamedSilentFail("Work");
                if (workTab != null)
                {
                    Find.MainTabsRoot.ToggleTab(workTab, true);
                }
            }
            if (Mouse.IsOver(viewRect))
            {
                TooltipHandler.TipRegion(viewRect, "Open the vanilla Work tab (Numeric view)");
            }
        }

        private static List<Pawn> GetSelectedColonists()
        {
            if (Find.Selector == null)
            {
                return new List<Pawn>();
            }
            return Find.Selector.SelectedPawns
                .Where(p => p != null && !p.Destroyed && p.IsFreeColonist)
                .ToList();
        }

        /// <summary>
        /// Palette order follows the user's Work-type order (the same list shown in Mod
        /// Settings and used by the Work tab), not naturalPriority. Unknown/hidden ids are
        /// skipped, and any visible type missing from the list is appended at the end.
        /// </summary>
        private static List<WorkTypeDef> GetPaletteOrder(List<WorkTypeDef> available)
        {
            if (available == null || available.Count == 0)
            {
                return new List<WorkTypeDef>();
            }
            if (OrderedWorkMod.Settings == null
                || OrderedWorkMod.Settings.workTypeOrder == null
                || OrderedWorkMod.Settings.workTypeOrder.Count == 0)
            {
                return available;
            }

            Dictionary<string, WorkTypeDef> byName = new Dictionary<string, WorkTypeDef>(StringComparer.Ordinal);
            foreach (WorkTypeDef type in available)
            {
                if (type != null && !string.IsNullOrEmpty(type.defName))
                {
                    byName[type.defName] = type;
                }
            }

            List<WorkTypeDef> ordered = new List<WorkTypeDef>();
            HashSet<string> added = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in OrderedWorkMod.Settings.workTypeOrder)
            {
                WorkTypeDef type;
                if (!string.IsNullOrEmpty(id) && byName.TryGetValue(id, out type) && added.Add(id))
                {
                    ordered.Add(type);
                }
            }
            foreach (WorkTypeDef type in available)
            {
                if (type != null && added.Add(type.defName))
                {
                    ordered.Add(type);
                }
            }
            return ordered;
        }

        private static List<Pawn> GetColonistsInDisplayOrder()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return new List<Pawn>();
            }

            List<Pawn> bar = null;
            try
            {
                bar = Find.ColonistBar != null ? Find.ColonistBar.GetColonistsInOrder() : null;
            }
            catch (Exception)
            {
                bar = null;
            }

            List<Pawn> result = new List<Pawn>();
            if (bar != null)
            {
                foreach (Pawn pawn in bar)
                {
                    if (pawn != null && !pawn.Dead && pawn.IsFreeColonist && pawn.Spawned && pawn.Map == map)
                    {
                        result.Add(pawn);
                    }
                }
            }

            if (result.Count == 0)
            {
                result = map.mapPawns.FreeColonists.Where(p => p != null && !p.Dead).ToList();
            }
            return result;
        }

        private void DrawRowState(Rect rowRect, Pawn pawn)
        {
            if (Find.Selector != null && Find.Selector.IsSelected(pawn))
            {
                Widgets.DrawHighlightSelected(rowRect);
                Widgets.DrawBox(rowRect, 2);
            }
        }

        private void DrawCentered(Rect inRect, string message)
        {
            TextAnchor previous = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(inRect, message);
            Text.Anchor = previous;
        }

        private void DrawPawnColumn(List<Pawn> colonists, Rect viewport, float leftWidth)
        {
            GUI.BeginGroup(viewport);
            try
            {
                for (int i = 0; i < colonists.Count; i++)
                {
                    float y = i * RowHeight - gridScroll.y;
                    if (y + RowHeight < 0f || y > viewport.height)
                    {
                        continue;
                    }

                    Pawn pawn = colonists[i];
                    Rect rowRect = new Rect(0f, y, leftWidth, RowHeight);

                    Color rowColor = (i % 2 == 0)
                        ? new Color(0.14f, 0.15f, 0.17f, 0.55f)
                        : new Color(0.16f, 0.17f, 0.19f, 0.55f);
                    Widgets.DrawBoxSolid(rowRect, rowColor);
                    DrawRowState(rowRect, pawn);

                    float portraitSize = Mathf.Min(32f, RowHeight - 6f);
                    Rect portraitRect = new Rect(3f, y + (RowHeight - portraitSize) / 2f, portraitSize, portraitSize);
                    RenderTexture portrait = PortraitsCache.Get(pawn, new Vector2(portraitSize, portraitSize), Rot4.South);
                    if (portrait != null)
                    {
                        GUI.DrawTexture(portraitRect, portrait);
                    }

                    float infoWidth = leftWidth >= 170f ? 46f : 0f;
                    float textX = portraitRect.xMax + 3f;
                    float textWidth = Mathf.Max(16f, leftWidth - textX - infoWidth - ColumnGutter - 2f);
                    string title = pawn.story != null ? pawn.story.Title : null;

                    TextAnchor previous = Text.Anchor;
                    Text.Anchor = TextAnchor.MiddleLeft;
                    bool previousWrap = Text.WordWrap;
                    Text.WordWrap = false;

                    if (!string.IsNullOrEmpty(title))
                    {
                        Widgets.LabelEllipses(new Rect(textX, y, textWidth, RowHeight / 2f), PawnLabel(pawn));

                        GameFont previousFont = Text.Font;
                        Color previousColor = GUI.color;
                        Text.Font = GameFont.Tiny;
                        GUI.color = new Color(0.75f, 0.75f, 0.75f);
                        Widgets.LabelEllipses(new Rect(textX, y + RowHeight / 2f, textWidth, RowHeight / 2f), title);
                        GUI.color = previousColor;
                        Text.Font = previousFont;
                    }
                    else
                    {
                        Widgets.LabelEllipses(new Rect(textX, y, textWidth, RowHeight), PawnLabel(pawn));
                    }
                    Text.WordWrap = previousWrap;
                    Text.Anchor = previous;

                    if (infoWidth > 0f)
                    {
                        GameFont previousInfoFont = Text.Font;
                        Color previousInfoColor = GUI.color;
                        TextAnchor previousInfoAnchor = Text.Anchor;
                        Text.Font = GameFont.Tiny;
                        Text.Anchor = TextAnchor.MiddleRight;
                        GUI.color = new Color(0.72f, 0.72f, 0.72f);
                        Rect infoRect = new Rect(leftWidth - infoWidth - ColumnGutter - 2f, y, infoWidth, RowHeight);
                        Widgets.Label(new Rect(infoRect.x, y, infoRect.width, RowHeight / 2f),
                            pawn.ageTracker != null ? pawn.ageTracker.AgeBiologicalYears + "y" : "");
                        Widgets.Label(new Rect(infoRect.x, y + RowHeight / 2f, infoRect.width, RowHeight / 2f), HealthShort(pawn));
                        GUI.color = previousInfoColor;
                        Text.Anchor = previousInfoAnchor;
                        Text.Font = previousInfoFont;
                    }

                    if (!dragging && !draggingPalette && Mouse.IsOver(rowRect))
                    {
                        TooltipHandler.TipRegion(rowRect, PawnTooltip(pawn));
                    }

                    Rect nameRect = new Rect(0f, y, leftWidth, RowHeight);
                    if (Event.current.type == EventType.MouseDown
                        && Event.current.button == 0
                        && nameRect.Contains(Event.current.mousePosition))
                    {
                        bool shift = Event.current.shift;
                        if (Event.current.clickCount == 2)
                        {
                            Find.Selector.ClearSelection();
                            Find.Selector.Select(pawn, true, false);
                            if (pawn.Spawned && pawn.Map != null)
                            {
                                CameraJumper.TryJump(pawn.Position, pawn.Map);
                            }
                            Find.MainTabsRoot.EscapeCurrentTab(true);
                        }
                        else if (shift)
                        {
                            if (Find.Selector.IsSelected(pawn))
                            {
                                Find.Selector.Deselect(pawn);
                            }
                            else
                            {
                                Find.Selector.Select(pawn, true, false);
                            }
                        }
                        else
                        {
                            Find.Selector.ClearSelection();
                            Find.Selector.Select(pawn, true, false);
                        }
                        Event.current.Use();
                    }
                }
            }
            finally
            {
                GUI.EndGroup();
            }
        }

        private void DrawRowLane(Pawn pawn, float rowY, List<WorkTypeDef> available)
        {
            LaneData lane = BuildLane(pawn);
            float cardY = rowY + (RowHeight - CardHeight) / 2f;
            int removeIndex = -1;

            int ghostIndex = -1;
            int draggedIndex = -1;
            bool hideDragged = false;

            if (draggingPalette && paletteHoverPawn == pawn && paletteWorkType != null)
            {
                draggedIndex = lane.ids.IndexOf(paletteWorkType.defName);
                ghostIndex = paletteDropIndex;
                if (draggedIndex >= 0 && ghostIndex > draggedIndex)
                {
                    ghostIndex--;
                }
                hideDragged = draggedIndex >= 0;
            }
            else if (dragging && dragPawn == pawn && dragSourceIndex >= 0 && dragTargetIndex >= 0)
            {
                draggedIndex = dragSourceIndex;
                ghostIndex = dragTargetIndex;
                if (ghostIndex > draggedIndex)
                {
                    ghostIndex--;
                }
                hideDragged = true;
            }

            int visual = 0;
            for (int i = 0; i < lane.types.Count; i++)
            {
                if (hideDragged && i == draggedIndex)
                {
                    continue;
                }

                if (ghostIndex >= 0 && visual == ghostIndex)
                {
                    WorkTypePaletteView.DrawGhostSlot(new Rect(CardGap + visual * (CardWidth + CardGap), cardY, CardWidth, CardHeight));
                    visual++;
                }

                Rect cardRect = new Rect(CardGap + visual * (CardWidth + CardGap), cardY, CardWidth, CardHeight);
                DrawCard(pawn, cardRect, lane.types[i], lane.ids, i, ref removeIndex);
                visual++;
            }

            if (ghostIndex >= 0 && visual == ghostIndex)
            {
                WorkTypePaletteView.DrawGhostSlot(new Rect(CardGap + visual * (CardWidth + CardGap), cardY, CardWidth, CardHeight));
                visual++;
            }

            Rect addRect = new Rect(CardGap + visual * (CardWidth + CardGap), cardY, CardWidth, CardHeight);
            if (Widgets.ButtonText(addRect, "+"))
            {
                OpenAddMenu(pawn, lane.ids, available);
            }

            if (removeIndex >= 0 && removeIndex < lane.ids.Count)
            {
                List<string> reduced = new List<string>(lane.ids);
                reduced.RemoveAt(removeIndex);
                WorkOrderService.SaveAndApply(pawn, reduced);
            }
        }

        private void DrawCard(Pawn pawn, Rect cardRect, WorkTypeDef workType, List<string> laneIds, int index, ref int removeIndex)
        {
            Color typeColor = WorkTypeColorRegistry.GetColor(workType);
            Widgets.DrawBoxSolid(cardRect, new Color(typeColor.r, typeColor.g, typeColor.b, 0.28f));
            Widgets.DrawBoxSolid(
                new Rect(cardRect.x, cardRect.y, 2f, cardRect.height),
                new Color(typeColor.r, typeColor.g, typeColor.b, 0.95f));

            if (!dragging && !draggingPalette && Mouse.IsOver(cardRect))
            {
                Widgets.DrawHighlight(cardRect);
            }

            Rect removeRect = new Rect(cardRect.xMax - 20f, cardRect.y + (cardRect.height - 18f) / 2f, 18f, 18f);
            Rect bodyRect = new Rect(cardRect.x, cardRect.y, cardRect.width - 20f, cardRect.height);

            Passion passion;
            SkillRecord bestSkill = WorkTypePaletteView.SafeBestSkill(pawn, workType, out passion);
            int level = bestSkill != null ? bestSkill.Level : -1;

            float x = cardRect.x + 4f;
            if (level >= 0)
            {
                WorkTypePaletteView.PassionVisual visual = WorkTypePaletteView.ResolvePassionVisual(passion);
                if (visual.Texture != null)
                {
                    WorkTypePaletteView.DrawPassionIcon(
                        new Rect(x, cardRect.y + (cardRect.height - WorkTypePaletteView.PassionIconSize) / 2f, WorkTypePaletteView.PassionIconSize, WorkTypePaletteView.PassionIconSize),
                        visual);
                    x += WorkTypePaletteView.PassionIconSize + 2f;
                }

                GameFont previousFont = Text.Font;
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(x, cardRect.y + (cardRect.height - 16f) / 2f, 22f, 16f), level.ToString());
                Text.Font = previousFont;
                x += 22f;
            }

            bool previousWrap = Text.WordWrap;
            Text.WordWrap = false;
            Widgets.LabelEllipses(
                new Rect(x, cardRect.y + (cardRect.height - 20f) / 2f, Mathf.Max(10f, removeRect.x - x - 4f), 20f),
                WorkTypePaletteView.DisplayName(workType));
            Text.WordWrap = previousWrap;

            if (!dragging && !draggingPalette && Mouse.IsOver(cardRect))
            {
                TooltipHandler.TipRegion(cardRect, LaneTooltip(pawn, workType));
            }

            if (Widgets.ButtonText(removeRect, "×"))
            {
                removeIndex = index;
            }

            bool overRemove = removeRect.Contains(Event.current.mousePosition);
            if (!overRemove
                && Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && bodyRect.Contains(Event.current.mousePosition))
            {
                dragging = true;
                dragPawn = pawn;
                dragSourceIndex = index;
                dragTargetIndex = index;
                dragCount = laneIds.Count;
                Event.current.Use();
            }

            if (Widgets.ButtonInvisible(bodyRect))
            {
                // Click only; reordering is driven by the drag events above.
            }
        }

        private void DrawHeldCard()
        {
            Vector2 mouse = Event.current.mousePosition;
            if (draggingPalette && paletteWorkType != null)
            {
                bool incapable = paletteHoverPawn != null && WorkOrderService.IsIncapable(paletteHoverPawn, paletteWorkType);
                WorkTypePaletteView.DrawDragCard(mouse, paletteWorkType, paletteHoverPawn, incapable);
                return;
            }

            if (dragging && dragPawn != null && dragSourceIndex >= 0)
            {
                LaneData lane = BuildLane(dragPawn);
                if (dragSourceIndex < lane.types.Count)
                {
                    WorkTypePaletteView.DrawDragCard(mouse, lane.types[dragSourceIndex], dragPawn, false);
                }
            }
        }

        private void HandleDrop()
        {
            if (!dragging)
            {
                return;
            }

            int slot = SlotAtX(Event.current.mousePosition.x, dragCount);
            if (Event.current.type != EventType.MouseUp)
            {
                dragTargetIndex = slot;
                return;
            }

            int from = dragSourceIndex;
            Pawn pawn = dragPawn;

            dragging = false;
            dragPawn = null;
            dragSourceIndex = -1;
            dragTargetIndex = -1;

            if (pawn != null && from >= 0 && slot >= 0 && !(slot == from || slot == from + 1))
            {
                List<string> ids = BuildLane(pawn).ids;
                if (from < ids.Count)
                {
                    string moved = ids[from];
                    ids.RemoveAt(from);
                    int insert = slot;
                    if (insert > from)
                    {
                        insert--;
                    }
                    ids.Insert(Mathf.Clamp(insert, 0, ids.Count), moved);
                    WorkOrderService.SaveAndApply(pawn, ids);
                }
            }
            Event.current.Use();
        }

        private void OpenAddMenu(Pawn pawn, List<string> currentOrder, List<WorkTypeDef> available)
        {
            HashSet<string> inOrder = new HashSet<string>(currentOrder);
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            foreach (WorkTypeDef workType in available)
            {
                if (inOrder.Contains(workType.defName))
                {
                    continue;
                }

                SkillRecord bestSkill = WorkTypePaletteView.SafeBestSkill(pawn, workType, out _);
                string levelText = bestSkill != null ? bestSkill.Level.ToString() : "-";
                string label = WorkTypePaletteView.DisplayName(workType) + " [" + levelText + "]";

                if (WorkOrderService.IsIncapable(pawn, workType))
                {
                    options.Add(new FloatMenuOption(label + " - Incapaz", null));
                    continue;
                }

                WorkTypeDef local = workType;
                options.Add(new FloatMenuOption(label, delegate
                {
                    List<string> updated = WorkOrderService.GetOrder(pawn);
                    if (!updated.Contains(local.defName))
                    {
                        updated.Add(local.defName);
                        WorkOrderService.SaveAndApply(pawn, updated);
                    }
                }));
            }

            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption("Nothing available to add", null));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static string LaneTooltip(Pawn pawn, WorkTypeDef workType)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(WorkTypePaletteView.DisplayName(workType));
            if (!string.IsNullOrEmpty(workType.description))
            {
                builder.AppendLine().AppendLine(workType.description);
            }

            List<WorkGiverDef> givers = DefDatabase<WorkGiverDef>.AllDefsListForReading
                .Where(g => g.workType == workType)
                .OrderByDescending(g => g.priorityInType)
                .ToList();
            builder.AppendLine();
            builder.AppendLine("Work givers:");
            if (givers.Count == 0)
            {
                builder.AppendLine("- none");
            }
            else
            {
                foreach (WorkGiverDef giver in givers)
                {
                    builder.AppendLine("- " + giver.defName);
                }
            }

            Passion passion;
            SkillRecord bestSkill = WorkTypePaletteView.SafeBestSkill(pawn, workType, out passion);
            builder.AppendLine();
            builder.AppendLine("Skill: " + (bestSkill != null ? bestSkill.def.label : "—"));
            builder.AppendLine("Passion: " + WorkTypePaletteView.PassionText(bestSkill != null ? passion : Passion.None));
            builder.AppendLine("Level: " + (bestSkill != null ? bestSkill.Level.ToString() : "—"));

            int rank = WorkOrderService.GetOrder(pawn).IndexOf(workType.defName) + 1;
            builder.AppendLine("Priority: " + (rank > 0 ? rank.ToString() : "Off"));

            AppendPawnSpecificOverrides(pawn, workType, builder);
            return builder.ToString();
        }

        private static void AppendPawnSpecificOverrides(Pawn pawn, WorkTypeDef workType, StringBuilder builder)
        {
            // Hook for future per-pawn/per-giver overrides. None implemented yet.
            PawnSpecificOverrideHook?.Invoke(pawn, workType, builder);
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

        private static string HealthShort(Pawn pawn)
        {
            if (pawn == null || pawn.health == null)
            {
                return "";
            }
            if (pawn.health.State == PawnHealthState.Dead)
            {
                return "Dead";
            }
            if (pawn.health.State == PawnHealthState.Down)
            {
                return "Down";
            }
            float percent = pawn.health.summaryHealth != null
                ? pawn.health.summaryHealth.SummaryHealthPercent
                : 1f;
            return percent < 0.999f ? Mathf.RoundToInt(percent * 100f) + "%" : "OK";
        }

        private static string PawnTooltip(Pawn pawn)
        {
            string text = pawn.Name != null ? pawn.Name.ToStringFull : pawn.LabelShort;
            if (pawn.ageTracker != null)
            {
                text += "\nAge: " + pawn.ageTracker.AgeBiologicalYears + "y";
            }
            if (pawn.story != null && !string.IsNullOrEmpty(pawn.story.Title))
            {
                text += "\nTitle: " + pawn.story.Title;
            }
            string health = HealthShort(pawn);
            if (!string.IsNullOrEmpty(health))
            {
                text += "\nHealth: " + health;
            }
            text += "\nWork types: " + WorkOrderService.GetOrder(pawn).Count;
            return text;
        }

        private static string PawnLabel(Pawn pawn)
        {
            string label = pawn.NameFullColored;
            return string.IsNullOrEmpty(label) ? pawn.LabelShort : label;
        }

        private static LaneData BuildLane(Pawn pawn)
        {
            LaneData lane = new LaneData();
            foreach (string id in WorkOrderService.GetOrder(pawn))
            {
                WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(id);
                if (workType != null)
                {
                    lane.types.Add(workType);
                    lane.ids.Add(id);
                }
            }
            return lane;
        }

        private static int SlotAtX(float x, int count)
        {
            int slot = Mathf.RoundToInt((x - CardGap) / (CardWidth + CardGap));
            return Mathf.Clamp(slot, 0, count);
        }

        private sealed class LaneData
        {
            public readonly List<WorkTypeDef> types = new List<WorkTypeDef>();
            public readonly List<string> ids = new List<string>();
        }
    }
}
