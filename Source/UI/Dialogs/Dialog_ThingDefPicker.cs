using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Core;
using OrderedWork.Services;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Searchable multi-select ThingDef picker for clothing rules.
    /// </summary>
    public sealed class Dialog_ThingDefPicker : Window
    {
        private readonly ClothingTarget target;
        private readonly Action<IReadOnlyList<string>>? onPick;
        private readonly HashSet<string> selected = new HashSet<string>(StringComparer.Ordinal);
        private string search = "";
        private Vector2 scroll;
        private List<ThingDef> cached = new List<ThingDef>();

        public Dialog_ThingDefPicker(ClothingTarget target, IEnumerable<string>? initiallySelected, Action<IReadOnlyList<string>>? onPick)
        {
            this.target = target;
            this.onPick = onPick;
            if (initiallySelected != null)
            {
                foreach (string id in initiallySelected)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        selected.Add(id);
                    }
                }
            }
            doCloseX = true;
            doCloseButton = false;
            resizeable = true;
            draggable = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            Rebuild();
        }

        public override Vector2 InitialSize => new Vector2(620f, 600f);

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

            Rect listRect = new Rect(0f, 66f, inRect.width, inRect.height - 112f);
            UiScrollPane.Draw(listRect, ref scroll, Mathf.Max(listRect.height, cached.Count * 28f + 4f), this, delegate(Rect view)
            {
                for (int i = 0; i < cached.Count; i++)
                {
                    ThingDef def = cached[i];
                    Rect row = new Rect(0f, i * 28f, view.width, 25f);
                    UiZebra.Draw(row, i);
                    bool isSelected = selected.Contains(def.defName);
                    if (isSelected)
                    {
                        Widgets.DrawBoxSolid(row, new Color(0.18f, 0.36f, 0.24f, 0.45f));
                    }
                    bool checkbox = isSelected;
                    Widgets.Checkbox(new Vector2(row.x + 3f, row.y + 3f), ref checkbox, 19f);
                    if (checkbox != isSelected)
                    {
                        if (checkbox)
                        {
                            selected.Add(def.defName);
                        }
                        else
                        {
                            selected.Remove(def.defName);
                        }
                    }

                    Texture2D? icon = ClothingIconResolver.IconOf(def.defName);
                    if (icon != null)
                    {
                        GUI.DrawTexture(new Rect(row.x + 28f, row.y + 3f, 19f, 19f), icon);
                    }
                    string label = def.LabelCap + " · " + def.defName;
                    Widgets.LabelEllipses(new Rect(row.x + (icon != null ? 52f : 30f), row.y, row.width - (icon != null ? 58f : 36f), 24f), label);
                    if (Widgets.ButtonInvisible(new Rect(row.x + 26f, row.y, row.width - 26f, 24f)))
                    {
                        bool now = !selected.Contains(def.defName);
                        if (now)
                        {
                            selected.Add(def.defName);
                        }
                        else
                        {
                            selected.Remove(def.defName);
                        }
                    }
                    TooltipHandler.TipRegion(row, ClothingInfoService.TooltipOf(def.defName));
                }
            });

            Rect footer = new Rect(0f, inRect.height - 38f, inRect.width, 30f);
            Widgets.Label(new Rect(0f, footer.y + 4f, 180f, 22f), "Selected: " + selected.Count);
            if (Widgets.ButtonText(new Rect(inRect.width - 188f, footer.y, 180f, 28f), "Add selected", true, true, selected.Count > 0))
            {
                List<string> picked = selected.OrderBy(value => value, StringComparer.Ordinal).ToList();
                onPick?.Invoke(picked);
                Close();
            }
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