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
    /// Searchable touch-card picker for clothing candidates. Cards are toggled,
    /// not rendered as action buttons.
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

        public override Vector2 InitialSize => new Vector2(700f, 620f);

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
            UiScrollPane.Draw(listRect, ref scroll, Mathf.Max(listRect.height, cached.Count * 52f + 4f), this, delegate(Rect view)
            {
                for (int i = 0; i < cached.Count; i++)
                {
                    DrawCard(view, i, cached[i]);
                }
            });

            Rect footer = new Rect(0f, inRect.height - 38f, inRect.width, 30f);
            Widgets.Label(new Rect(0f, footer.y + 4f, 180f, 22f), "Selected: " + selected.Count);
            if (Widgets.ButtonText(new Rect(inRect.width - 188f, footer.y, 180f, 28f), "Add selected", true, true, selected.Count > 0))
            {
                onPick?.Invoke(selected.OrderBy(value => value, StringComparer.Ordinal).ToList());
                Close();
            }
        }

        private void DrawCard(Rect view, int index, ThingDef def)
        {
            Rect card = new Rect(0f, index * 52f, view.width, 48f);
            bool isSelected = selected.Contains(def.defName);
            Widgets.DrawBoxSolid(card, isSelected
                ? new Color(0.16f, 0.34f, 0.22f, 0.95f)
                : new Color(0.13f, 0.14f, 0.17f, 0.95f));
            Widgets.DrawBox(card, isSelected ? 2 : 1);
            Texture2D? icon = ClothingIconResolver.IconOf(def.defName);
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(card.x + 8f, card.y + 9f, 28f, 28f), icon);
            }
            string label = def.LabelCap + " · " + def.defName;
            Widgets.LabelEllipses(new Rect(card.x + (icon != null ? 44f : 8f), card.y + 6f, card.width - (icon != null ? 52f : 16f), 20f), label);
            ClothingInfo? info = ClothingInfoService.Find(def.defName);
            string subline = info == null
                ? "Pick to add"
                : "Layers: " + (string.IsNullOrEmpty(info.Layers) ? "-" : info.Layers)
                    + " · Body: " + (string.IsNullOrEmpty(info.BodyParts) ? "-" : info.BodyParts);
            Widgets.LabelEllipses(new Rect(card.x + (icon != null ? 44f : 8f), card.y + 25f, card.width - (icon != null ? 52f : 16f), 18f), subline);
            if (Widgets.ButtonInvisible(card))
            {
                if (isSelected)
                {
                    selected.Remove(def.defName);
                }
                else
                {
                    selected.Add(def.defName);
                }
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
