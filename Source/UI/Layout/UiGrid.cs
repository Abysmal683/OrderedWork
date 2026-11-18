using UnityEngine;

namespace OrderedWork.UI.Layout
{
    /// <summary>Uniform column grid over a Rect, with explicit row height and gap.</summary>
    public sealed class UiGrid
    {
        private readonly Rect area;
        private readonly int columns;
        private readonly float rowHeight;
        private readonly float columnGap;
        private readonly float rowGap;

        public UiGrid(Rect area, int columns, float rowHeight, float columnGap = 4f, float rowGap = 4f)
        {
            this.area = area;
            this.columns = Mathf.Max(1, columns);
            this.rowHeight = rowHeight;
            this.columnGap = columnGap;
            this.rowGap = rowGap;
        }

        public Rect Cell(int row, int column, float height = -1f)
        {
            float width = Mathf.Max(0f, (area.width - columnGap * (columns - 1)) / columns);
            float actualHeight = height > 0f ? height : rowHeight;
            return new Rect(
                area.x + column * (width + columnGap),
                area.y + row * (actualHeight + rowGap),
                width,
                actualHeight);
        }

        public float RowStride => rowHeight + rowGap;
        public float ColumnWidth => Mathf.Max(0f, (area.width - columnGap * (columns - 1)) / columns);
    }
}
