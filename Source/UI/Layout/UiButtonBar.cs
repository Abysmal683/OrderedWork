using UnityEngine;
using Verse;

namespace OrderedWork.UI.Layout
{
    /// <summary>Action button with the same disabled treatment used in config panels.</summary>
    public static class UiButtonBar
    {
        public static bool Button(Rect rect, string label, bool enabled = true)
        {
            Color previous = GUI.color;
            if (!enabled)
            {
                GUI.color = new Color(0.55f, 0.55f, 0.55f, 0.7f);
            }
            bool clicked = Widgets.ButtonText(rect, label, true, true, enabled);
            GUI.color = previous;
            return clicked;
        }
    }
}
