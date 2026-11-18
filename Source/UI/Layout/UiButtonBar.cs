using UnityEngine;
using Verse;

namespace OrderedWork.UI.Layout
{
    /// <summary>
    /// Single action-button implementation. Enabled buttons use the vanilla atlas.
    /// Disabled buttons use CustomButtonText so the body itself is darkened while
    /// the label stays readable.
    /// </summary>
    public static class UiButtonBar
    {
        public static bool Button(Rect rect, string label, bool enabled = true, string tooltip = null)
        {
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }

            if (!enabled)
            {
                Widgets.CustomButtonText(
                    ref rect,
                    label,
                    UiColors.DisabledButtonBackground,
                    Color.white,
                    UiColors.DisabledButtonBorder,
                    UiColors.DisabledButtonBackground,
                    false,
                    0f,
                    false,
                    false,
                    1f);
                return false;
            }

            return Widgets.ButtonText(rect, label, true, true, true);
        }
    }
}
