using UnityEngine;

namespace OrderedWork.UI.Layout
{
    /// <summary>
    /// Shared UI colors for Ordered Work panels. Keep visual decisions here rather
    /// than duplicating literal Color values across partial views.
    /// </summary>
    public static class UiColors
    {
        public static readonly Color PanelBody = new Color(0.13f, 0.14f, 0.17f, 1f);
        public static readonly Color ColumnBackground = new Color(0.14f, 0.16f, 0.20f, 0.55f);
        public static readonly Color ColumnHeaderBackground = new Color(0.20f, 0.25f, 0.30f, 0.45f);
        public static readonly Color DisabledText = new Color(0.55f, 0.55f, 0.55f, 0.7f);
        public static readonly Color WarningText = new Color(1f, 0.75f, 0.35f, 1f);
        public static readonly Color EmptyColumn = new Color(0.18f, 0.18f, 0.20f, 1f);
    }
}
