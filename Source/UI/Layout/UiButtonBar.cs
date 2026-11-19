using UnityEngine;
using Verse;

namespace OrderedWork.UI.Layout
{
    /// <summary>
    /// Single action-button implementation.
    /// Enabled: vanilla atlas and label.
    /// Disabled: vanilla atlas tinted dark; the native label is left empty and a
    /// normal-colour label is drawn on top so the body changes, not the letters.
    /// </summary>
    public static class UiButtonBar
    {
        public static bool Button(Rect rect, string label, bool enabled = true, string tooltip = null,
            Color? tint = null)
        {
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }

            if (enabled)
            {
                if (!tint.HasValue)
                {
                    return Widgets.ButtonText(rect, label, true, true, true);
                }
                Color previous = GUI.color;
                GUI.color = tint.Value;
                bool clicked = Widgets.ButtonText(rect, label, true, true, true);
                GUI.color = previous;
                return clicked;
            }

            Color previousColor = GUI.color;
            GUI.color = new Color(0.55f, 0.55f, 0.55f, 0.70f);
            Widgets.ButtonText(rect, "", true, true, false);
            GUI.color = previousColor;

            TextAnchor previousAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, label);
            Text.Anchor = previousAnchor;
            return false;
        }
    }
}
