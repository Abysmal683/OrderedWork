using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Core;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Simple searchable ThingDef picker for clothing rules. One selection per open.
    /// </summary>
    public sealed class Dialog_ThingDefPicker : Window
    {
        private readonly ClothingTarget target;
        private readonly Action<string>? onPick;
        private string search = "";
        private Vector2 scroll;
        private List<ThingDef> cached = new List<ThingDef>();

        public Dialog_ThingDefPicker(ClothingTarget target, Action<string>? onPick)
        {
            this.target = target;
            this.onPick = onPick;
            doCloseX = true;
            doCloseButton = false;
            resizeable = true;
            draggable = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            Rebuild();
        }

        public override Vector2 InitialSize => new Vector2(620f, 560f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 28f), "Pick " + target);
            Text.Font = GameFont.Small;

            Rect searchRect = new Rect(0f, 34f, inRect.width, 26f);
            string next = Widgets.TextField(searchRect, search ?? "");
            if (!string.Equals(next, search, StringComparison.Ordinal))
            {
                search = next;
                Rebuild();
            }

            Rect listRect = new Rect(0f, 66f, inRect.width, inRect.height - 76f);
            UiScrollPane.Draw(listRect, ref scroll, Mathf.Max(listRect.height, cached.Count * 28f + 4f), this, delegate(Rect view)
            {
                for (int i = 0; i < cached.Count; i++)
                {
                    ThingDef def = cached[i];
                    Rect row = new Rect(0f, i * 28f, view.width, 25f);
                    UiZebra.Draw(row, i);
                    string label = def.LabelCap + " · " + def.defName;
                    if (Widgets.ButtonText(row, label))
                    {
                        onPick?.Invoke(def.defName);
                        Close();
                        return;
                    }
                }
            });
        }

        private void Rebuild()
        {
            string term = (search ?? "").Trim();
            cached = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def != null)
                .Where(def => target == ClothingTarget.Apparel
                    ? def.IsApparel
                    : def.HasComp(typeof(CompEquippable)) && !def.IsApparel)
                .Where(def => string.IsNullOrEmpty(term)
                    || def.defName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0
                    || def.label.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(def => def.label)
                .ThenBy(def => def.defName, StringComparer.Ordinal)
                .Take(500)
                .ToList();
        }
    }
}