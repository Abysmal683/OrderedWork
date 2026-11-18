using System;
using UnityEngine;
using Verse;

namespace OrderedWork.UI.Layout
{
    /// <summary>
    /// Section header + inset body. This replaces repeated title/background/body
    /// arithmetic without forcing a scroll or a fixed height.
    /// </summary>
    public static class UiSection
    {
        public const float HeaderHeight = 24f;

        public static Rect Draw(Rect rect, string title, Action<Rect> body, float inset = 6f, bool background = true)
        {
            if (background)
            {
                Widgets.DrawBoxSolid(rect, new Color(0.10f, 0.11f, 0.13f, 0.65f));
            }
            Widgets.Label(new Rect(rect.x + inset, rect.y + 2f, rect.width - inset * 2f, HeaderHeight), title);
            Rect content = new Rect(
                rect.x + inset,
                rect.y + HeaderHeight + 2f,
                Mathf.Max(0f, rect.width - inset * 2f),
                Mathf.Max(0f, rect.height - HeaderHeight - inset - 2f));
            if (body != null)
            {
                body(content);
            }
            return content;
        }
    }
}
