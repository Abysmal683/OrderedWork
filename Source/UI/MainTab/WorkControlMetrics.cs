namespace OrderedWork.UI
{
    internal static class WorkControlMetrics
    {
        internal const float RowHeight = 42f;
        internal const float CardWidth = 135f;
        internal const float CardHeight = 30f;
        internal const float CardGap = 4f;
        internal const float ColumnGutter = 6f;

        internal static int SlotAtX(float x, int count)
        {
            int slot = UnityEngine.Mathf.RoundToInt((x - CardGap) / (CardWidth + CardGap));
            return UnityEngine.Mathf.Clamp(slot, 0, count);
        }
    }
}