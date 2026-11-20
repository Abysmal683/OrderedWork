#nullable disable
using UnityEngine;

namespace OrderedWork.UI.Layout
{
    /// <summary>Weighted row columns: the IMGUI equivalent of 1fr / 2fr columns.</summary>
    public static class UiFlex
    {
        public static Rect[] Columns(Rect row, float gap, params float[] weights)
        {
            if (weights == null || weights.Length == 0)
            {
                return new Rect[0];
            }
            float total = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                total += Mathf.Max(0.01f, weights[i]);
            }
            float available = Mathf.Max(0f, row.width - gap * (weights.Length - 1));
            Rect[] result = new Rect[weights.Length];
            float x = row.x;
            for (int i = 0; i < weights.Length; i++)
            {
                float width = available * Mathf.Max(0.01f, weights[i]) / total;
                result[i] = new Rect(x, row.y, width, row.height);
                x += width + gap;
            }
            return result;
        }
    }
}
