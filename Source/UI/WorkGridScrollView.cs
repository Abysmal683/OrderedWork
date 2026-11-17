using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Thin, optional-auto-hide scrollbars for the Work Control matrix and palette.
    /// The scrollbars live outside the content viewport, so they never cover the last row.
    /// </summary>
    internal static class WorkGridScrollView
    {
        internal const float Thickness = 8f;
        private const float MinThumb = 16f;

        private static bool draggingVertical;
        private static bool draggingHorizontal;
        private static float dragStartMouse;
        private static float dragStartScroll;
        private static float lastScrollTime;

        internal static bool IsDragging
        {
            get { return draggingVertical || draggingHorizontal; }
        }

        internal static bool RecentlyScrolled
        {
            get { return Time.realtimeSinceStartup - lastScrollTime < 1.5f; }
        }

        internal static bool HandleWheel(Rect area, Rect viewport, Rect content, ref Vector2 scroll)
        {
            if (Event.current.type != EventType.ScrollWheel || !area.Contains(Event.current.mousePosition))
            {
                return false;
            }

            float max = Mathf.Max(0f, content.height - viewport.height);
            if (max <= 0f)
            {
                return false;
            }

            scroll.y = Mathf.Clamp(scroll.y + Event.current.delta.y * 20f, 0f, max);
            lastScrollTime = Time.realtimeSinceStartup;
            Event.current.Use();
            return true;
        }

        internal static void DrawVertical(Rect track, Rect viewport, Rect content, ref Vector2 scroll)
        {
            float max = Mathf.Max(0f, content.height - viewport.height);
            if (max <= 0f || track.height <= 0f)
            {
                draggingVertical = false;
                return;
            }

            float thumbSize = Mathf.Clamp(track.height * (viewport.height / content.height), MinThumb, track.height);
            float travel = track.height - thumbSize;
            float thumbY = track.y + travel * Mathf.Clamp01(scroll.y / max);
            Rect thumb = new Rect(track.x, thumbY, track.width, thumbSize);

            Widgets.DrawBoxSolid(track, new Color(0.07f, 0.07f, 0.08f, 0.75f));
            Widgets.DrawBoxSolid(thumb, Mouse.IsOver(thumb)
                ? new Color(0.62f, 0.70f, 0.78f, 0.95f)
                : new Color(0.45f, 0.50f, 0.56f, 0.90f));

            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && Mouse.IsOver(thumb))
            {
                draggingVertical = true;
                dragStartMouse = current.mousePosition.y;
                dragStartScroll = scroll.y;
                lastScrollTime = Time.realtimeSinceStartup;
                current.Use();
            }
            else if (draggingVertical && current.type == EventType.MouseDrag)
            {
                float delta = current.mousePosition.y - dragStartMouse;
                scroll.y = Mathf.Clamp(dragStartScroll + (travel <= 0f ? 0f : delta / travel * max), 0f, max);
                lastScrollTime = Time.realtimeSinceStartup;
                current.Use();
            }
            else if (draggingVertical && current.type == EventType.MouseUp)
            {
                draggingVertical = false;
                current.Use();
            }
            else if (current.type == EventType.MouseDown && current.button == 0 && Mouse.IsOver(track) && !Mouse.IsOver(thumb))
            {
                float page = Mathf.Max(MinThumb, viewport.height);
                scroll.y = Mathf.Clamp(scroll.y + (current.mousePosition.y < thumb.y ? -page : page), 0f, max);
                lastScrollTime = Time.realtimeSinceStartup;
                current.Use();
            }
        }

        internal static void DrawHorizontal(Rect track, Rect viewport, Rect content, ref Vector2 scroll)
        {
            float max = Mathf.Max(0f, content.width - viewport.width);
            if (max <= 0f || track.width <= 0f)
            {
                draggingHorizontal = false;
                return;
            }

            float thumbSize = Mathf.Clamp(track.width * (viewport.width / content.width), MinThumb, track.width);
            float travel = track.width - thumbSize;
            float thumbX = track.x + travel * Mathf.Clamp01(scroll.x / max);
            Rect thumb = new Rect(thumbX, track.y, thumbSize, track.height);

            Widgets.DrawBoxSolid(track, new Color(0.07f, 0.07f, 0.08f, 0.75f));
            Widgets.DrawBoxSolid(thumb, Mouse.IsOver(thumb)
                ? new Color(0.62f, 0.70f, 0.78f, 0.95f)
                : new Color(0.45f, 0.50f, 0.56f, 0.90f));

            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && Mouse.IsOver(thumb))
            {
                draggingHorizontal = true;
                dragStartMouse = current.mousePosition.x;
                dragStartScroll = scroll.x;
                lastScrollTime = Time.realtimeSinceStartup;
                current.Use();
            }
            else if (draggingHorizontal && current.type == EventType.MouseDrag)
            {
                float delta = current.mousePosition.x - dragStartMouse;
                scroll.x = Mathf.Clamp(dragStartScroll + (travel <= 0f ? 0f : delta / travel * max), 0f, max);
                lastScrollTime = Time.realtimeSinceStartup;
                current.Use();
            }
            else if (draggingHorizontal && current.type == EventType.MouseUp)
            {
                draggingHorizontal = false;
                current.Use();
            }
            else if (current.type == EventType.MouseDown && current.button == 0 && Mouse.IsOver(track) && !Mouse.IsOver(thumb))
            {
                float page = Mathf.Max(MinThumb, viewport.width);
                scroll.x = Mathf.Clamp(scroll.x + (current.mousePosition.x < thumb.x ? -page : page), 0f, max);
                lastScrollTime = Time.realtimeSinceStartup;
                current.Use();
            }
        }
    }
}
