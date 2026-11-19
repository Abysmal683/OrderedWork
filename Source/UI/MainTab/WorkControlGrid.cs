using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Owns the matrix viewport, external bars and row loop. Row rendering and
    /// interaction are separate collaborators.
    /// </summary>
    internal static class WorkControlGrid
    {
        internal static void Draw(
            Rect bodyRect,
            Rect namesViewport,
            Rect laneViewport,
            Rect vTrack,
            Rect hTrack,
            float leftWidth,
            float laneContentWidth,
            float contentHeight,
            List<Pawn> colonists,
            List<WorkTypeDef> available,
            bool autoHide,
            ref Vector2 gridScroll,
            WorkControlInteraction interaction)
        {
            Rect laneContent = new Rect(0f, 0f, laneContentWidth, contentHeight);

            WorkGridScrollView.HandleWheel(bodyRect, laneViewport, laneContent, ref gridScroll);

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

            if (showVerticalScrollbar)
            {
                WorkGridScrollView.HandleVertical(vTrack, new Rect(bodyRect.x, bodyRect.y,
                    Mathf.Max(0f, bodyRect.width - WorkGridScrollView.Thickness), bodyRect.height),
                    laneContent, ref gridScroll, interaction);
            }
            if (showHorizontalScrollbar)
            {
                WorkGridScrollView.HandleHorizontal(hTrack, laneViewport, laneContent, ref gridScroll, interaction);
            }

            Widgets.BeginScrollView(laneViewport, ref gridScroll, laneContent, false);
            try
            {
                interaction.HandleDrop(gridScroll);

                for (int i = 0; i < colonists.Count; i++)
                {
                    Pawn pawn = colonists[i];
                    float rowY = i * WorkControlMetrics.RowHeight;
                    Rect rowRect = new Rect(0f, rowY, laneContentWidth, WorkControlMetrics.RowHeight);

                    Color rowColor = (i % 2 == 0)
                        ? new Color(0.14f, 0.15f, 0.17f, 0.55f)
                        : new Color(0.16f, 0.17f, 0.19f, 0.55f);
                    Widgets.DrawBoxSolid(rowRect, rowColor);
                    WorkControlRows.DrawRowState(rowRect, pawn);
                    if (interaction.DraggingPalette && interaction.PaletteHoverPawn == pawn)
                    {
                        Widgets.DrawHighlight(rowRect);
                    }

                    WorkControlRows.DrawRowLane(pawn, rowY, available, interaction);
                }
            }
            finally
            {
                Widgets.EndScrollView();
                Text.Font = GameFont.Small;
            }

            WorkControlRows.DrawPawnColumn(colonists, namesViewport, leftWidth, gridScroll.y, interaction);

            if (showVerticalScrollbar)
            {
                WorkGridScrollView.DrawVertical(vTrack, new Rect(bodyRect.x, bodyRect.y,
                    Mathf.Max(0f, bodyRect.width - WorkGridScrollView.Thickness), bodyRect.height),
                    laneContent, ref gridScroll);
            }
            if (showHorizontalScrollbar)
            {
                WorkGridScrollView.DrawHorizontal(hTrack, laneViewport, laneContent, ref gridScroll);
            }

            WorkControlRows.DrawHeldCard(interaction);
        }
    }
}