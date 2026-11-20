using System.Collections.Generic;
using OrderedWork.Core;
using UnityEngine;
using Verse;

namespace OrderedWork.UI.Clothing
{
    internal static class ClothingSelectionList
    {
        public static void Draw(Rect rect, ClothingResolution resolution)
        {
            float y = rect.y;
            if (resolution.Selections.Count == 0)
            {
                Widgets.Label(new Rect(rect.x, y, rect.width, 20f), "No clothing rules selected.");
                y += 22f;
            }
            foreach (ClothingSelection selection in resolution.Selections)
            {
                Widgets.LabelEllipses(new Rect(rect.x, y, rect.width, 20f),
                    selection.Phase + " · " + selection.DefName + " · " + selection.Slot
                    + (string.IsNullOrEmpty(selection.BodyLayer) ? "" : " / " + selection.BodyLayer));
                y += 20f;
            }
            if (resolution.FreeSlots.Count > 0)
            {
                Widgets.Label(new Rect(rect.x, y + 4f, rect.width, 20f),
                    "Free slots: " + resolution.FreeSlots.Count);
            }
        }
    }
}