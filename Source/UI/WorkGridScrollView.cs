using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Thin, optional-auto-hide scrollbars for the Work Control matrix and palette.
    /// The scrollbars live outside the content viewport, so they never cover the last row.
    /// Input handling is split from drawing: call <see cref="HandleVertical"/> /
    /// <see cref="HandleHorizontal"/> before <c>Widgets.BeginScrollView</c> so the content
    /// cannot consume the bar's MouseDown, then draw the bars after the content.
    /// </summary>
    internal static class WorkGridScrollView
    {
        internal const float Thickness = 12f;
        private const float MinThumb = 24f;

        private static bool draggingVertical;
        private static bool draggingHorizontal;
        private static object horizontalOwner;
        private static float dragStartMouseY;
        private static float dragStartScrollY;
        private static float dragStartMouseX;
        private static float dragStartScrollX;
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

            // Shift+wheel is the quick horizontal access; normal wheel remains vertical.
            if (Event.current.shift)
            {
                float maxX = Mathf.Max(0f, content.width - viewport.width);
                if (maxX <= 0f)
                {
                    return false;
                }

                scroll.x = Mathf.Clamp(scroll.x + Event.current.delta.y * 20f, 0f, maxX);
            }
            else
            {
                float maxY = Mathf.Max(0f, content.height - viewport.height);
                if (maxY <= 0f)
                {
                    return false;
                }

                scroll.y = Mathf.Clamp(scroll.y + Event.current.delta.y * 20f, 0f, maxY);
            }

            lastScrollTime = Time.realtimeSinceStartup;
            Event.current.Use();
            return true;
        }

        /// <summary>Consumes vertical-bar input before the scroll view/content is drawn.</summary>
        internal static void HandleVertical(Rect track, Rect viewport, Rect content, ref Vector2 scroll)
        {
            float max = Mathf.Max(0f, content.height - viewport.height);
            if (max <= 0f || track.height <= 0f)
            {
                draggingVertical = false;
                return;
            }

            Rect thumb = VerticalThumb(track, viewport, content, scroll);
            Event current = Event.current;

            if (current.type == EventType.MouseDown && Mouse.IsOver(track))
            {
                LogScrollDown("V", current, track, thumb, scroll.y);
            }

            if (current.type == EventType.MouseDown && current.button == 0 && Mouse.IsOver(thumb))
            {
                draggingVertical = true;
                dragStartMouseY = current.mousePosition.y;
                dragStartScrollY = scroll.y;
                lastScrollTime = Time.realtimeSinceStartup;
                current.Use();
            }
            else if (draggingVertical && current.type == EventType.MouseDrag)
            {
                float delta = current.mousePosition.y - dragStartMouseY;
                float travel = track.height - thumb.height;
                scroll.y = Mathf.Clamp(dragStartScrollY + (travel <= 0f ? 0f : delta / travel * max), 0f, max);
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

        /// <summary>Draws the vertical bar after the scroll view/content.</summary>
        internal static void DrawVertical(Rect track, Rect viewport, Rect content, ref Vector2 scroll)
        {
            float max = Mathf.Max(0f, content.height - viewport.height);
            if (max <= 0f || track.height <= 0f)
            {
                return;
            }

            Rect thumb = VerticalThumb(track, viewport, content, scroll);
            DrawTrackAndThumb(track, thumb);
        }

        /// <summary>Consumes horizontal-bar input before the scroll view/content is drawn.</summary>
        internal static void HandleHorizontal(Rect track, Rect viewport, Rect content, ref Vector2 scroll, object owner)
        {
            float max = Mathf.Max(0f, content.width - viewport.width);
            if (max <= 0f || track.width <= 0f)
            {
                // A non-overflowing palette bar must not cancel the matrix bar's drag.
                if (ReferenceEquals(horizontalOwner, owner))
                {
                    draggingHorizontal = false;
                    horizontalOwner = null;
                }
                return;
            }

            Rect thumb = HorizontalThumb(track, viewport, content, scroll);
            Event current = Event.current;

            if (current.type == EventType.MouseDown && Mouse.IsOver(track))
            {
                LogScrollDown("H", current, track, thumb, scroll.x);
            }

            if (current.type == EventType.MouseDown && current.button == 0 && Mouse.IsOver(thumb))
            {
                draggingHorizontal = true;
                horizontalOwner = owner;
                dragStartMouseX = current.mousePosition.x;
                dragStartScrollX = scroll.x;
                lastScrollTime = Time.realtimeSinceStartup;
                current.Use();
            }
            else if (draggingHorizontal && ReferenceEquals(horizontalOwner, owner) && current.type == EventType.MouseDrag)
            {
                float delta = current.mousePosition.x - dragStartMouseX;
                float travel = track.width - thumb.width;
                scroll.x = Mathf.Clamp(dragStartScrollX + (travel <= 0f ? 0f : delta / travel * max), 0f, max);
                lastScrollTime = Time.realtimeSinceStartup;
                current.Use();
            }
            else if (draggingHorizontal && ReferenceEquals(horizontalOwner, owner) && current.type == EventType.MouseUp)
            {
                draggingHorizontal = false;
                horizontalOwner = null;
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

        /// <summary>Draws the horizontal bar after the scroll view/content.</summary>
        internal static void DrawHorizontal(Rect track, Rect viewport, Rect content, ref Vector2 scroll)
        {
            float max = Mathf.Max(0f, content.width - viewport.width);
            if (max <= 0f || track.width <= 0f)
            {
                return;
            }

            Rect thumb = HorizontalThumb(track, viewport, content, scroll);
            DrawTrackAndThumb(track, thumb);
        }

        private static Rect VerticalThumb(Rect track, Rect viewport, Rect content, Vector2 scroll)
        {
            float max = Mathf.Max(0f, content.height - viewport.height);
            float thumbSize = Mathf.Clamp(
                track.height * (viewport.height / content.height),
                Mathf.Min(MinThumb, track.height),
                track.height);
            float travel = track.height - thumbSize;
            float thumbY = track.y + travel * Mathf.Clamp01(scroll.y / max);
            return new Rect(track.x, thumbY, track.width, thumbSize);
        }

        private static Rect HorizontalThumb(Rect track, Rect viewport, Rect content, Vector2 scroll)
        {
            float max = Mathf.Max(0f, content.width - viewport.width);
            float thumbSize = Mathf.Clamp(
                track.width * (viewport.width / content.width),
                Mathf.Min(MinThumb, track.width),
                track.width);
            float travel = track.width - thumbSize;
            float thumbX = track.x + travel * Mathf.Clamp01(scroll.x / max);
            return new Rect(thumbX, track.y, thumbSize, track.height);
        }

        private static void DrawTrackAndThumb(Rect track, Rect thumb)
        {
            Widgets.DrawBoxSolid(track, new Color(0.07f, 0.07f, 0.08f, 0.75f));
            Widgets.DrawBoxSolid(thumb, Mouse.IsOver(thumb)
                ? new Color(0.62f, 0.70f, 0.78f, 0.95f)
                : new Color(0.45f, 0.50f, 0.56f, 0.90f));
        }

        // Temporary diagnostics: one line per bar MouseDown, never per frame.
        private static void LogScrollDown(string axis, Event current, Rect track, Rect thumb, float scrollValue)
        {
            Log.Message("[OrderedWork][Scroll] " + axis + " down mouse=(" +
                current.mousePosition.x.ToString("F0") + "," + current.mousePosition.y.ToString("F0") + ") track=(" +
                track.x.ToString("F0") + "," + track.y.ToString("F0") + "," +
                track.width.ToString("F0") + "," + track.height.ToString("F0") + ") thumb=(" +
                thumb.x.ToString("F0") + "," + thumb.y.ToString("F0") + "," +
                thumb.width.ToString("F0") + "," + thumb.height.ToString("F0") + ") scroll" +
                axis + "=" + scrollValue.ToString("F0"));
        }
    }
}
