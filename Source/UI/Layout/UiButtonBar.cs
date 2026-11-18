using UnityEngine;
using Verse;

namespace OrderedWork.UI.Layout
{
    /// <summary>
    /// Single action-button implementation.
    /// The native button atlas is kept for shape/border; disabled buttons tint the
    /// atlas with GUI.color and redraw the label in normal white so only the body
    /// changes colour.
    /// </summary>
    public static class UiButtonBar
    {
        public static bool Button(Rect rect, string label, bool enabled = true, string tooltip = null)
        {
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }

            Color previousColor = GUI.color;
            if (!enabled)
            {
                GUI.color = new Color(0.55f, 0.55f, 0.55f, 0.70f);
            }

            bool clicked = Widgets.ButtonText(rect, "", true, true, enabled);
            GUI.color = previousColor;

            if (!enabled)
            {
                TextAnchor previousAnchor = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, label);
                Text.Anchor = previousAnchor;
            }

            return enabled && clicked;
        }
    }
}
