using UnityEngine;
using Verse;

namespace OrderedWork.UI.Layout
{
    /// <summary>
    /// Single action-button implementation. GUI.enabled is what actually gives the
    /// native button its disabled tint; the active argument alone does not.
    /// </summary>
    public static class UiButtonBar
    {
        public static bool Button(Rect rect, string label, bool enabled = true, string tooltip = null)
        {
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }

            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && enabled;
            bool clicked = Widgets.ButtonText(rect, label);
            GUI.enabled = previousEnabled;
            return enabled && clicked;
        }
    }
}
