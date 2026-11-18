using System;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Large vanilla-like tab strip used by the pawn detail panel and mod settings.
    /// It deliberately only draws and selects; callers own the content state.
    /// </summary>
    internal static class OrderedTabStrip
    {
        public static void Draw(Rect rect, string[] labels, int selected, Action<int> onSelect)
        {
            if (labels == null || labels.Length == 0)
            {
                return;
            }
            float gap = 4f;
            float width = (rect.width - (labels.Length - 1) * gap) / labels.Length;
            for (int i = 0; i < labels.Length; i++)
            {
                Rect tab = new Rect(rect.x + i * (width + gap), rect.y, width, rect.height);
                bool active = i == selected;
                Widgets.DrawBoxSolid(tab, active
                    ? new Color(0.18f, 0.20f, 0.24f, 1f)
                    : new Color(0.10f, 0.10f, 0.12f, 1f));
                if (active)
                {
                    Widgets.DrawBoxSolid(new Rect(tab.x + 3f, tab.yMax - 2f, tab.width - 6f, 2f),
                        new Color(0.35f, 0.55f, 0.85f, 1f));
                }
                if (Widgets.ButtonInvisible(tab))
                {
                    onSelect?.Invoke(i);
                }
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(tab, labels[i]);
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }
    }
}
