using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Services;
using UnityEngine;
using Verse;

namespace OrderedWork.UI.Layout
{
    /// <summary>
    /// Right-aligned source icons with a fixed limit and a +N overflow tooltip.
    /// Use beside a label row when several items contribute the same value.
    /// </summary>
    public static class UiSourceIcons
    {
        public static void Draw(Rect rect, IReadOnlyList<string>? sources, int maxVisible = 3, float iconSize = 16f)
        {
            if (sources == null || sources.Count == 0)
            {
                return;
            }

            List<string> unique = sources
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (unique.Count == 0)
            {
                return;
            }

            int visible = Mathf.Min(maxVisible, unique.Count);
            float x = rect.xMax - (visible * (iconSize + 2f));
            for (int i = 0; i < visible; i++)
            {
                Texture2D? icon = ClothingIconResolver.IconOf(unique[i]);
                if (icon != null)
                {
                    GUI.DrawTexture(new Rect(x, rect.y + (rect.height - iconSize) / 2f, iconSize, iconSize), icon, ScaleMode.ScaleToFit);
                }
                x += iconSize + 2f;
            }

            int overflow = unique.Count - visible;
            if (overflow > 0)
            {
                Widgets.Label(new Rect(rect.xMax - iconSize - 4f, rect.y, iconSize + 4f, rect.height),
                    "+" + overflow);
            }

            string tooltip = string.Join("\n", unique);
            TooltipHandler.TipRegion(rect, tooltip);
        }
    }
}
