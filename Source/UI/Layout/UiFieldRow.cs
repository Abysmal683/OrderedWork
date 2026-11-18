using UnityEngine;
using Verse;

namespace OrderedWork.UI.Layout
{
    /// <summary>Label + text field row with a consistent label width.</summary>
    public static class UiFieldRow
    {
        public static string Text(Rect row, string label, string value, float labelWidth = 110f)
        {
            Widgets.Label(new Rect(row.x, row.y, labelWidth, row.height), label);
            return Widgets.TextField(
                new Rect(row.x + labelWidth + 4f, row.y, Mathf.Max(40f, row.width - labelWidth - 4f), row.height),
                value ?? "");
        }
    }
}
