using UnityEngine;
using Verse;

namespace OrderedWork.UI.Layout
{
    /// <summary>
    /// Selectable card used by the equipment cells, candidate rows and set rows.
    /// Centralizes card colors, insets, icon placement and click handling.
    /// </summary>
    public static class UiCard
    {
        public static bool Draw(
            Rect rect,
            string label,
            string? subLabel = null,
            bool selected = false,
            Texture2D? icon = null,
            string? tooltip = null)
        {
            Widgets.DrawBoxSolid(rect, selected ? UiColors.CardSelected : UiColors.Card);
            Widgets.DrawBox(rect, selected ? 2 : 1);
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }

            Rect content = UiInset.All(rect, 6f);
            float x = content.x;
            if (icon != null)
            {
                float iconSize = Mathf.Min(content.height - 4f, 28f);
                GUI.DrawTexture(new Rect(content.x, content.y + (content.height - iconSize) / 2f, iconSize, iconSize), icon);
                x += iconSize + 6f;
            }

            float textWidth = Mathf.Max(0f, content.xMax - x);
            if (string.IsNullOrEmpty(subLabel))
            {
                Widgets.LabelEllipses(new Rect(x, content.y, textWidth, content.height), label);
            }
            else
            {
                Widgets.LabelEllipses(new Rect(x, content.y, textWidth, content.height / 2f), label);
                Widgets.LabelEllipses(new Rect(x, content.y + content.height / 2f, textWidth, content.height / 2f), subLabel);
            }

            return Widgets.ButtonInvisible(rect);
        }
    }
}
