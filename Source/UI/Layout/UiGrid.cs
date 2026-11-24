using UnityEngine;

namespace OrderedWork.UI.Layout
{
    /// <summary>
    /// Fixed square-cell grid math. Cells stay square; callers provide the
    /// available rect and gap. No ad-hoc x/y arithmetic at call sites.
    /// </summary>
    public static class UiGrid
    {
        public static int Columns(Rect area, float cellSize, float gap = 6f)
        {
            if (cellSize <= 0f)
            {
                return 1;
            }
            float usable = Mathf.Max(0f, area.width + gap);
            return Mathf.Max(1, Mathf.FloorToInt(usable / (cellSize + gap)));
        }

        public static Rect Cell(Rect area, int index, float cellSize, float gap = 6f, int? columns = null)
        {
            int count = columns ?? Columns(area, cellSize, gap);
            int column = index % Mathf.Max(1, count);
            int row = index / Mathf.Max(1, count);
            return new Rect(
                area.x + column * (cellSize + gap),
                area.y + row * (cellSize + gap),
                cellSize,
                cellSize);
        }

        public static float Height(Rect area, int itemCount, float cellSize, float gap = 6f, int? columns = null)
        {
            int count = columns ?? Columns(area, cellSize, gap);
            int rows = Mathf.CeilToInt(itemCount / (float)Mathf.Max(1, count));
            return Mathf.Max(0f, rows * cellSize + Mathf.Max(0, rows - 1) * gap);
        }
    }
}
