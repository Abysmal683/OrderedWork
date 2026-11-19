using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>Compact colonist search used from the pawn detail header.</summary>
    public sealed class Dialog_PawnPicker : Window
    {
        private readonly List<Pawn> pawns;
        private readonly Action<Pawn> onSelect;
        private string query = "";
        private Vector2 scroll;

        public Dialog_PawnPicker(List<Pawn> pawns, Action<Pawn> onSelect)
        {
            this.pawns = pawns ?? new List<Pawn>();
            this.onSelect = onSelect;
            doCloseX = true;
            doCloseButton = false;
            resizeable = false;
            draggable = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(380f, 420f); }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 28f), "Search colonist");
            Text.Font = GameFont.Small;
            query = Widgets.TextField(new Rect(0f, 34f, inRect.width, 28f), query ?? "");

            List<Pawn> matches = pawns
                .Where(p => p != null && !p.Dead)
                .Where(p => string.IsNullOrWhiteSpace(query)
                    || (p.Name != null && p.Name.ToStringFull.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    || p.LabelShort.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            Rect list = new Rect(0f, 70f, inRect.width, inRect.height - 76f);
            Rect view = new Rect(0f, 0f, list.width - 16f, Mathf.Max(list.height, matches.Count * 28f));
            Widgets.BeginScrollView(list, ref scroll, view);
            try
            {
                for (int i = 0; i < matches.Count; i++)
                {
                    Pawn pawn = matches[i];
                    Rect row = new Rect(0f, i * 28f, view.width, 25f);
                    string label = pawn.Name != null ? pawn.Name.ToStringFull : pawn.LabelShort;
                    if (Widgets.ButtonText(new Rect(row.x + 4f, row.y, row.width - 8f, 24f), label))
                    {
                        onSelect?.Invoke(pawn);
                        Close();
                        return;
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