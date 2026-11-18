using UnityEngine;

namespace OrderedWork.UI.Layout
{
    /// <summary>Vertical row cursor with a consistent gap.</summary>
    public sealed class UiRows
    {
        private readonly float rowHeight;
        private readonly float gap;
        private Rect remaining;

        public UiRows(Rect rect, float rowHeight, float gap = 4f)
        {
            this.rowHeight = rowHeight;
            this.gap = gap;
            remaining = rect;
        }

        public Rect Next(float height = -1f)
        {
            float actualHeight = height > 0f ? height : rowHeight;
            Rect row = new Rect(remaining.x, remaining.y, remaining.width, actualHeight);
            remaining.y += actualHeight + gap;
            remaining.height = Mathf.Max(0f, remaining.height - actualHeight - gap);
            return row;
        }

        public Rect Remaining => remaining;
    }
}
