using System.Collections.Generic;
using OrderedWork.Services;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Renders the global passion-weight table. The viewport is clipped manually and
    /// the scrollbar is drag-only, so it never owns the settings wheel event.
    /// </summary>
    internal static class PassionWeightsView
    {
        private const int VisiblePassionRows = 10;
        private const float PassionRowHeight = 26f;

        public static void Draw(Listing_Standard list, OrderedWorkSettings settings, ref Vector2 scroll, bool drawHeader = true)
        {
            if (drawHeader)
            {
                list.Label("Recommendation passion weights (global). Level 0 / per level / min level:");
            }
            PassionWeightPolicy.EnsureDefaults(settings);
            List<PassionSource.PassionInfo> registered = PassionSource.Registered;
            float viewHeight = Mathf.Min(registered.Count, VisiblePassionRows) * PassionRowHeight;
            Rect outRect = list.GetRect(viewHeight);
            float barWidth = WorkGridScrollView.Thickness;
            Rect viewport = new Rect(outRect.x, outRect.y, outRect.width - barWidth - 2f, outRect.height);
            Rect content = new Rect(0f, 0f, viewport.width, registered.Count * PassionRowHeight);
            Rect track = new Rect(viewport.xMax + 2f, outRect.y, barWidth, outRect.height);
            float maxScroll = Mathf.Max(0f, content.height - viewport.height);
            scroll.y = Mathf.Clamp(scroll.y, 0f, maxScroll);
            WorkGridScrollView.HandleVertical(track, viewport, content, ref scroll);

            GUI.BeginGroup(viewport);
            string pendingTooltip = null;
            Rect pendingTooltipRect = default(Rect);
            for (int i = 0; i < registered.Count; i++)
            {
                DrawRow(viewport, scroll.y, i, registered[i], settings,
                    ref pendingTooltip, ref pendingTooltipRect);
            }
            GUI.EndGroup();
            if (!string.IsNullOrEmpty(pendingTooltip))
            {
                TooltipHandler.TipRegion(pendingTooltipRect, pendingTooltip);
            }
            WorkGridScrollView.DrawVertical(track, viewport, content, ref scroll);

            if (list.ButtonText("Reset recommendation weights"))
            {
                PassionWeightPolicy.ResetToDefaults(settings);
            }
        }

        private static void DrawRow(
            Rect viewport,
            float scroll,
            int index,
            PassionSource.PassionInfo info,
            OrderedWorkSettings settings,
            ref string pendingTooltip,
            ref Rect pendingTooltipRect)
        {
            PassionWeightEntry entry = PassionWeightPolicy.Find(settings, info.Passion);
            if (entry == null)
            {
                return;
            }

            Rect row = new Rect(0f, index * PassionRowHeight - scroll, viewport.width, 24f);
            UiZebra.Draw(row, index);
            WorkTypePaletteView.PassionVisual visual = WorkTypePaletteView.ResolvePassionVisual(info.Passion);
            if (visual.Texture != null)
            {
                Color previous = GUI.color;
                GUI.color = visual.Tint;
                GUI.DrawTexture(new Rect(row.x, row.y + 5f, 16f, 16f), visual.Texture);
                GUI.color = previous;
            }

            Widgets.Label(new Rect(row.x + 20f, row.y, 140f, 24f), info.Label);
            string description = PassionSource.DescriptionOf(info.Passion);
            float learnRate = PassionSource.LearnRateOf(info.Passion);
            if (learnRate > 0f)
            {
                string rateLine = "Learn rate: x" + learnRate.ToString("0.##");
                description = string.IsNullOrEmpty(description)
                    ? rateLine
                    : description + "\n\n" + rateLine;
            }
            if (!string.IsNullOrEmpty(description) && Mouse.IsOver(row))
            {
                pendingTooltip = description;
                pendingTooltipRect = new Rect(viewport.x + row.x, viewport.y + row.y, 170f, 24f);
            }

            float fieldWidth = Mathf.Max(54f, (row.width - 170f - 24f) / 3f);
            Widgets.TextFieldNumeric(new Rect(row.x + 174f, row.y, fieldWidth, 24f),
                ref entry.level0Score, ref entry.level0Buffer, -20, 20);
            Widgets.TextFieldNumeric(new Rect(row.x + 178f + fieldWidth, row.y, fieldWidth, 24f),
                ref entry.perLevelBonus, ref entry.perLevelBuffer, -20, 20);
            Widgets.TextFieldNumeric(new Rect(row.x + 182f + fieldWidth * 2f, row.y, fieldWidth, 24f),
                ref entry.minLevel, ref entry.minLevelBuffer, 0, 20);
        }
    }
}
