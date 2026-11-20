#nullable disable
using UnityEngine;
using Verse;

namespace OrderedWork.UI.Layout
{
    /// <summary>
    /// Collapsible section header with a visual divider. State belongs to the
    /// caller; this only draws and reports the requested expansion state.
    /// </summary>
    public static class UiCollapsibleSection
    {
        public static bool Header(Listing_Standard list, string title, bool expanded)
        {
            Rect row = list.GetRect(24f);
            if (Widgets.ButtonInvisible(row))
            {
                expanded = !expanded;
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(row.x, row.y, 22f, row.height), expanded ? "−" : "+");
            Widgets.Label(new Rect(row.x + 24f, row.y, row.width - 24f, row.height), title);
            Widgets.DrawBoxSolid(
                new Rect(row.x, row.yMax - 1f, row.width, 1f),
                new Color(0.35f, 0.42f, 0.50f, 0.75f));
            Text.Anchor = TextAnchor.UpperLeft;
            list.Gap(4f);
            return expanded;
        }
    }
}
