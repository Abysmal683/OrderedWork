#nullable disable
using System;
using UnityEngine;
using Verse;

namespace OrderedWork.UI.Layout
{
    /// <summary>
    /// Shared vertical scroll pane with an external track. Callers receive the local
    /// content rect; the track cannot overlap rows.
    /// </summary>
    internal static class UiScrollPane
    {
        internal static void Draw(Rect outer, ref Vector2 scroll, float contentHeight,
            object owner, Action<Rect> draw, bool handleWheel = true)
        {
            float trackWidth = WorkGridScrollView.Thickness;
            const float gap = 4f;
            Rect viewport = new Rect(outer.x, outer.y,
                Mathf.Max(0f, outer.width - trackWidth - gap), outer.height);
            Rect track = new Rect(outer.xMax - trackWidth, outer.y, trackWidth, outer.height);
            Rect view = new Rect(0f, 0f, viewport.width, Mathf.Max(viewport.height, contentHeight));

            if (handleWheel)
            {
                WorkGridScrollView.HandleWheel(outer, viewport, view, ref scroll);
            }
            WorkGridScrollView.HandleVertical(track, viewport, view, ref scroll, owner);
            Widgets.BeginScrollView(viewport, ref scroll, view, false);
            try
            {
                draw?.Invoke(view);
            }
            finally
            {
                Widgets.EndScrollView();
            }
            WorkGridScrollView.DrawVertical(track, viewport, view, ref scroll);
        }
    }
}