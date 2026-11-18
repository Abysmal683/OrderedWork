using UnityEngine;
using Verse;

namespace OrderedWork.UI.Layout
{
    /// <summary>Alternating row tint for long option lists.</summary>
    public static class UiZebra
    {
        public static void Draw(Rect row, int index)
        {
            if ((index & 1) == 0)
            {
                Widgets.DrawBoxSolid(row, new Color(1f, 1f, 1f, 0.035f));
            }
        }
    }
}
