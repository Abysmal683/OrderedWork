#nullable disable
using UnityEngine;
using Verse;

namespace OrderedWork.UI.Layout
{
    /// <summary>
    /// Small padding helpers for IMGUI Rect layout. RimWorld has no CSS-style
    /// box model; callers use these instead of ad-hoc x/y arithmetic.
    /// </summary>
    public static class UiInset
    {
        public static Rect All(Rect rect, float value)
        {
            return rect.ContractedBy(value);
        }

        public static Rect Horizontal(Rect rect, float value)
        {
            return new Rect(rect.x + value, rect.y, Mathf.Max(0f, rect.width - value * 2f), rect.height);
        }

        public static Rect Vertical(Rect rect, float value)
        {
            return new Rect(rect.x, rect.y + value, rect.width, Mathf.Max(0f, rect.height - value * 2f));
        }

        public static Rect Top(Rect rect, float value)
        {
            return new Rect(rect.x, rect.y + value, rect.width, Mathf.Max(0f, rect.height - value));
        }

        public static Rect Bottom(Rect rect, float value)
        {
            return new Rect(rect.x, rect.y, rect.width, Mathf.Max(0f, rect.height - value));
        }
    }
}