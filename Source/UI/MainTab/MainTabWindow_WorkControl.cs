using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OrderedWork.Config;
using OrderedWork.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Full-screen OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.Title", "Work Control") tab: colonist-bar pawn order, a 3-row WorkType palette,
    /// a Pawn/Work search row, and a fixed-name + shared-horizontal-scroll matrix with
    /// thin auto-hiding scrollbars outside the content.
    /// </summary>
    public partial class MainTabWindow_WorkControl : MainTabWindow
    {
        private const float Pad = 4f;
        private const float TitleHeight = 28f;
        private const float PaletteHeight = 84f;
        private const float SearchHeight = 24f;
        private const float HScrollHeight = WorkGridScrollView.Thickness;
        private const float HeaderHeight = 18f;
        private const float LeftColumnMin = 160f;
        private const float LeftColumnMax = 260f;
        private const int DebugPaletteTypeCount = 60;
        private const float MinPanelHeight = 360f; // top UI + 4 pawn rows
        private const float DefaultPanelHeightFactor = 0.5f;

        private Vector2 gridScroll;
        private Vector2 paletteScroll;
        private string searchText = "";
        private string workFilterText = "";
        private WorkTypeDef? workFilter;
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

        private readonly WorkControlInteraction interaction = new WorkControlInteraction();
        private List<Pawn>? cachedFilteredColonists;
        private string? cachedFilterSignature;
        private int cachedFilterTick = -1;

        public override void DoWindowContents(Rect inRect)
        {
            // MainTabWindow normally updates its own bottom-anchored rectangle each frame.
            // Our override must do it too, otherwise the resize grip changes only the setting.
            SetInitialSizeAndPosition();
            Widgets.DrawBoxSolid(inRect, new Color(0.11f, 0.12f, 0.13f, 1f));
            DrawPanelResizeHandle(inRect);

            if (Find.CurrentMap == null)
            {
                DrawCentered(inRect, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.LoadColony", "Load a colony"));
                return;
            }

            List<Pawn> allColonists = GetColonistsInDisplayOrder();
            if (allColonists.Count == 0)
            {
                DrawCentered(inRect, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.NoColonists", "No free colonists"));
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
            Widgets.Label(new Rect(Pad, titleY, Mathf.Max(100f, inRect.width - 330f), TitleHeight), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.Title", "Work Control"));
            Text.Font = GameFont.Small;

            Rect viewRect = new Rect(inRect.xMax - Pad - 120f, titleY + 2f, 120f, 24f);
            Rect detailsRect = new Rect(viewRect.x - 4f - 80f, titleY + 2f, 80f, 24f);
            Rect recommendRect = new Rect(detailsRect.x - 4f - 98f, titleY + 2f, 98f, 24f);
            Rect clothesRect = new Rect(recommendRect.x - 4f - 80f, titleY + 2f, 80f, 24f);
            DrawTopControls(clothesRect, recommendRect, detailsRect, viewRect);

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
                interaction.PaletteWorkType,
                showPaletteScrollbar,
                interaction.IsAnyDragging,
                interaction.BeginPaletteDrag);

            // Search row below the palette, above the matrix.
            DrawSearchRow(searchY);

            bool filtering = !string.IsNullOrWhiteSpace(searchText) || ScopeFiltersLine;
            List<Pawn> colonists = GetFilteredColonists(allColonists, filtering);

            if (filtering)
            {
                TextAnchor previousMatchAnchor = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(
                    new Rect(inRect.xMax - Pad - 180f, searchY, 180f, SearchHeight),
                    colonists.Count + (colonists.Count == 1 ? OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.Match", " match") : OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.Matches", " matches")));
                Text.Anchor = previousMatchAnchor;
            }

            interaction.UpdatePaletteDrag(bodyRect, leftWidth, colonists, gridScroll);

            // Column header.
            Widgets.Label(new Rect(bodyRect.x, headerY, leftWidth - 6f, HeaderHeight), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.Pawn", "Pawn"));
            Widgets.Label(new Rect(bodyRect.x + leftWidth, headerY, bodyRect.width - leftWidth, HeaderHeight),
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.Hint", "Work (left = highest). Drag to reorder, × remove, + add."));

            if (colonists.Count == 0)
            {
                interaction.CancelPaletteDrag();
                DrawCentered(bodyRect, filtering ? OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.NoMatch", "No pawns match") : OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.NoColonists", "No free colonists"));
                return;
            }

            int maxLane = 0;
            foreach (Pawn pawn in colonists)
            {
                maxLane = Mathf.Max(maxLane, WorkControlRows.BuildLane(pawn).types.Count);
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

            float laneContentWidth = Mathf.Max(laneViewport.width, (maxLane + 1) * (WorkControlMetrics.CardWidth + WorkControlMetrics.CardGap) + WorkControlMetrics.CardGap);
            float rowsHeight = colonists.Count * WorkControlMetrics.RowHeight;
            float contentHeight = Mathf.Max(bodyRect.height, rowsHeight);

            WorkControlGrid.Draw(bodyRect, namesViewport, laneViewport, vTrack, hTrack,
                leftWidth, laneContentWidth, contentHeight, colonists, available, autoHide,
                ref gridScroll, interaction);
        }

        /// <summary>Top grip: drag up to grow the panel, down to shrink it; 4 pawn rows minimum.</summary>
        private void DrawPanelResizeHandle(Rect inRect)
        {
            Rect handle = new Rect(inRect.center.x - 80f, 0f, 160f, 12f);
            Widgets.DrawBoxSolid(handle, new Color(0.32f, 0.36f, 0.40f, 1f));
            Widgets.DrawBoxSolid(new Rect(handle.x + 20f, handle.y + 4f, 120f, 2f), new Color(0.62f, 0.68f, 0.72f, 1f));
            TooltipHandler.TipRegion(handle, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.ResizeTip", "Drag to resize Work Control (minimum: 4 pawn rows)"));

            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && handle.Contains(current.mousePosition))
            {
                resizingPanel = true;
                resizeStartMouseY = Verse.UI.MousePositionOnUIInverted.y;
                resizeStartHeight = windowRect.height;
                OrderedWorkMod.DebugLog("[Resize] down mouse=" + current.mousePosition + " height=" + inRect.height, OrderedWorkLogCategory.WorkControl);
                current.Use();
                return;
            }

            if (!resizingPanel)
            {
                return;
            }

            // Poll the physical mouse button so the drag survives leaving the tab/window;
            // waiting for MouseDrag/MouseUp events only inside the window is what cut it off.
            if (!Input.GetMouseButton(0))
            {
                OrderedWorkMod.DebugLog("[Resize] up saved="
                    + (OrderedWorkMod.Settings != null ? OrderedWorkMod.Settings.workControlHeight : -1f), OrderedWorkLogCategory.WorkControl);
                resizingPanel = false;
                return;
            }

            if (OrderedWorkMod.Settings != null)
            {
                float max = Mathf.Max(MinPanelHeight, Verse.UI.screenHeight - 35f);
                float newHeight = resizeStartHeight + (resizeStartMouseY - Verse.UI.MousePositionOnUIInverted.y);
                OrderedWorkMod.Settings.workControlHeight = Mathf.Clamp(newHeight, MinPanelHeight, max);
            }
            if (current.type != EventType.Repaint && current.type != EventType.Layout)
            {
                current.Use();
            }
        }


        private string WorkFilterScopeLabel
        {
            get
            {
                switch (workFilterScope)
                {
                    case WorkFilterScope.Palette:
                        return OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.Scope.Palette", "Palette");
                    case WorkFilterScope.Both:
                        return OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.Scope.Both", "Both");
                    default:
                        return OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.Scope.Line", "Line");
                }
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






























    }
}
