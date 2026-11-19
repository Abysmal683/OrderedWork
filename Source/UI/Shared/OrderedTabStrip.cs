using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Thin wrapper around RimWorld's native TabDrawer so ordered Work uses the
    /// same rounded atlas, colors and selected state as vanilla inspect dialogs.
    /// </summary>
    internal static class OrderedTabStrip
    {
        public static void Draw(Rect rect, string[] labels, int selected, Action<int> onSelect)
        {
            if (labels == null || labels.Length == 0)
            {
                return;
            }
            List<TabRecord> tabs = new List<TabRecord>(labels.Length);
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                tabs.Add(new TabRecord(labels[i], delegate { onSelect?.Invoke(index); }, i == selected));
            }
            TabDrawer.DrawTabs(rect, tabs, 200f);
        }
    }
}