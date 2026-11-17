using System;
using System.Collections.Generic;
using System.Linq;
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
        private Vector2 scroll;

        internal WorkTypeDropdownWindow(
            string title,
            IEnumerable<WorkTypeDef> types,
            WorkTypeDef selected,
            Action<WorkTypeDef> onSelect)
        {
            this.title = title;
            this.types = (types ?? Enumerable.Empty<WorkTypeDef>())
                .Where(t => t != null && t != selected)
                .ToList();
            this.onSelect = onSelect;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            resizeable = false;
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(260f, (MaxVisibleRows + 1) * RowHeight + 36f); }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(0f, 0f, inRect.width, 22f), title);

            Rect viewport = new Rect(0f, 24f, inRect.width, inRect.height - 24f);
            Rect content = new Rect(0f, 0f, viewport.width, Mathf.Max(viewport.height, types.Count * RowHeight));

            Widgets.BeginScrollView(viewport, ref scroll, content);
            try
            {
                for (int i = 0; i < types.Count; i++)
                {
                    WorkTypeDef type = types[i];
                    Rect row = new Rect(0f, i * RowHeight, viewport.width - 16f, RowHeight - 2f);
                    if (Widgets.ButtonText(row, WorkTypePaletteView.DisplayName(type)))
                    {
                        if (onSelect != null)
                        {
                            onSelect(type);
                        }
                        Close(true);
                    }
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }
    }
}
