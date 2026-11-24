using UnityEngine;
using Verse;

namespace OrderedWork.UI.Layout
{
    /// <summary>
    /// Search field gadget with the shared inset and a placeholder tooltip.
    /// Use this instead of raw Widgets.TextField rectangles in list panels.
    /// </summary>
    public static class UiSearchField
    {
        public static string Draw(Rect rect, string value, string? tooltip = null)
        {
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }
            return Widgets.TextField(rect, value ?? "");
        }
    }
}
