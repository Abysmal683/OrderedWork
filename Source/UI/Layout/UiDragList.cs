using System;
using OrderedWork.UI;
using UnityEngine;
using Verse;

namespace OrderedWork.UI.Layout
{
    /// <summary>
    /// One scrollable row list with selection and drag/drop. Row hitboxes and drawing
    /// use the same local rect, which keeps selection stable while the list is scrolled.
    /// </summary>
    public sealed class UiDragList
    {
        public const float RowHeight = 24f;

        private Vector2 scroll;
        private int dragIndex = -1;
        private bool dragMoved;
        private float dragStartY;

        public bool IsDragging
        {
            get { return dragIndex >= 0; }
        }

        public bool IsDragMoved
        {
            get { return dragMoved; }
        }

        public int DraggingIndex
        {
            get { return dragIndex; }
        }

        public void Draw(
            Rect viewport,
            int count,
            Action<int, Rect> drawRow,
            Func<int, Rect, Rect> selectionRect,
            Action<int> onSelect,
            Action<int, int> onMove,
            Action onCommit)
        {
            const float gap = 4f;
            float trackWidth = WorkGridScrollView.Thickness;
            Rect contentViewport = new Rect(viewport.x, viewport.y,
                Mathf.Max(0f, viewport.width - trackWidth - gap), viewport.height);
            Rect track = new Rect(viewport.xMax - trackWidth, viewport.y, trackWidth, viewport.height);
            Rect view = new Rect(0f, 0f,
                contentViewport.width,
                Mathf.Max(contentViewport.height, count * RowHeight));
            WorkGridScrollView.HandleVertical(track, contentViewport, view, ref scroll, this);
            Widgets.BeginScrollView(contentViewport, ref scroll, view, false);
            try
            {
                for (int i = 0; i < count; i++)
                {
                    Rect row = new Rect(0f, i * RowHeight, view.width, RowHeight - 2f);
                    if (drawRow != null)
                    {
                        drawRow(i, row);
                    }

                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0
                        && Mouse.IsOver(row))
                    {
                        dragIndex = i;
                        dragMoved = false;
                        dragStartY = Event.current.mousePosition.y;
                    }

                    if (Event.current.type == EventType.MouseDrag && dragIndex >= 0)
                    {
                        if (!dragMoved && Mathf.Abs(Event.current.mousePosition.y - dragStartY) > 4f)
                        {
                            dragMoved = true;
                        }
                        if (dragMoved)
                        {
                            for (int target = 0; target < count; target++)
                            {
                                Rect targetRow = new Rect(0f, target * RowHeight, view.width, RowHeight - 2f);
                                if (target != dragIndex && Mouse.IsOver(targetRow))
                                {
                                    if (onMove != null)
                                    {
                                        onMove(dragIndex, target);
                                    }
                                    dragIndex = target;
                                    break;
                                }
                            }
                        }
                    }

                    if (onSelect != null)
                    {
                        Rect select = selectionRect != null ? selectionRect(i, row) : row;
                        if (Widgets.ButtonInvisible(select))
                        {
                            onSelect(i);
                        }
                    }

                    if (Event.current.type == EventType.MouseUp)
                    {
                        if (dragMoved && onCommit != null)
                        {
                            onCommit();
                        }
                        dragIndex = -1;
                        dragMoved = false;
                    }
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
            WorkGridScrollView.DrawVertical(track, contentViewport, view, ref scroll);
        }
    }
}
