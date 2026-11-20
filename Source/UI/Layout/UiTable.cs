using System.Collections.Generic;
using UnityEngine;

namespace OrderedWork.UI.Layout
{
    /// <summary>Fixed-width table columns with gaps and an optional right inset.</summary>
    public sealed class UiTable
    {
        private readonly Rect area;
        private readonly List<float> widths = new List<float>();
        private readonly float gap;
        private readonly float rightInset;

        public UiTable(Rect area, float gap = 6f, float rightInset = 0f)
        {
            this.area = area;
            this.gap = gap;
            this.rightInset = rightInset;
        }

        public UiTable Add(float width)
        {
            widths.Add(width);
            return this;
        }

        public UiTable Flex()
        {
            widths.Add(0f);
            return this;
        }

        public Rect Column(int index, float? height = null)
        {
            float fixedWidth = 0f;
            int flexCount = 0;
            foreach (float width in widths)
            {
                if (width <= 0f) flexCount++;
                else fixedWidth += width;
            }
            float available = Mathf.Max(0f, area.width - rightInset - gap * (widths.Count - 1));
            float flexWidth = flexCount > 0 ? Mathf.Max(0f, (available - fixedWidth) / flexCount) : 0f;

            float x = area.x;
            for (int i = 0; i < index; i++)
            {
                x += (widths[i] > 0f ? widths[i] : flexWidth) + gap;
            }
            float columnWidth = widths[index] > 0f ? widths[index] : flexWidth;
            return new Rect(x, area.y, columnWidth, height ?? area.height);
        }
    }
}
