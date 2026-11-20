using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.UI.Layout;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Compact reusable WorkType dropdown for Mod Settings. The selected type is excluded
    /// from the list and the panel is capped at 10 rows, matching the Work Control combo.
    /// </summary>
    internal sealed class WorkTypeDropdownWindow : Window
    {
        private const float RowHeight = 24f;
        private const int MaxVisibleRows = 10;

        private readonly string title;
        private readonly List<WorkTypeDef> types;
        private readonly Action<WorkTypeDef> onSelect;
        private readonly Func<WorkTypeDef, string>? label;
        private Vector2 scroll;

        internal WorkTypeDropdownWindow(
            string title,
            IEnumerable<WorkTypeDef> types,
            WorkTypeDef? selected,
            Action<WorkTypeDef> onSelect,
            Func<WorkTypeDef, string>? label = null)
        {
            this.title = title;
            this.types = (types ?? Enumerable.Empty<WorkTypeDef>())
                .Where(t => t != null && t != selected)
                .ToList();
            this.onSelect = onSelect;
            this.label = label;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            resizeable = false;
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(340f, (MaxVisibleRows + 1) * RowHeight + 36f); }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(0f, 0f, inRect.width, 22f), title);

            Rect viewport = new Rect(0f, 24f, inRect.width, inRect.height - 24f);
            UiScrollPane.Draw(viewport, ref scroll, Mathf.Max(viewport.height, types.Count * RowHeight), this, delegate(Rect view)
            {
                for (int i = 0; i < types.Count; i++)
                {
                    WorkTypeDef type = types[i];
                    Rect row = new Rect(0f, i * RowHeight, view.width, RowHeight - 2f);
                    UiZebra.Draw(row, i);
                    if (Mouse.IsOver(row))
                    {
                        Widgets.DrawBoxSolid(row, new Color(1f, 1f, 1f, 0.08f));
                    }
                    string text = label != null ? label(type) : WorkTypePaletteView.DisplayName(type);
                    Widgets.LabelEllipses(row, text);
                    TooltipHandler.TipRegion(row, text);
                    if (Widgets.ButtonInvisible(row))
                    {
                        if (onSelect != null)
                        {
                            onSelect(type);
                        }
                        Close(true);
                    }
                }
            });
        }
    }
}
