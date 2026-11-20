using System;
using OrderedWork.UI;
using UnityEngine;
using Verse;

namespace OrderedWork.UI.Layout
{
    /// <summary>
    /// Tab strip whose body starts at the same Y as the native TabDrawer base.
    /// TabDrawer paints the rounded tabs above that base, so callers pass the
    /// content/body top and keep their own inner inset.
    /// </summary>
    public static class UiTabBar
    {
        public static float TabHeight => TabDrawer.TabHeight;

        public static void Draw(Rect bodyTop, string[] labels, int selected, Action<int> onSelect)
        {
            OrderedTabStrip.Draw(bodyTop, labels, selected, onSelect);
        }
    }
}
