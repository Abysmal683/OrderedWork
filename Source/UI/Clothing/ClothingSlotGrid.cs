using System.Collections.Generic;
using OrderedWork.Core;
using OrderedWork.UI;
using OrderedWork.UI.Layout;
using UnityEngine;
using Verse;

namespace OrderedWork.UI.Clothing
{
    internal static class ClothingSlotGrid
    {
        private const int Columns = 4;
        private const int MaxSlots = 28;
        private const float CellHeight = 42f;
        private const float Gap = 4f;

        public static float Height(int slotCount)
        {
            int visible = Mathf.Min(slotCount, MaxSlots);
            int rows = Mathf.Max(1, Mathf.CeilToInt(visible / (float)Columns));
            return rows * (CellHeight + Gap);
        }

        public static void Draw(Rect rect, IReadOnlyList<ClothingSlot> slots, ClothingResolution resolution)
        {
            if (slots == null || slots.Count == 0)
            {
                Widgets.Label(rect, "No apparel slots are known for this pawn.");
                return;
            }

            Dictionary<string, ClothingSelection> selected = new Dictionary<string, ClothingSelection>();
            foreach (ClothingSelection selection in resolution.Selections)
            {
                selected[Key(selection.Target, selection.Slot, selection.BodyLayer)] = selection;
            }

            float cellWidth = Mathf.Max(80f, (rect.width - Gap * (Columns - 1)) / Columns);
            int visible = Mathf.Min(slots.Count, MaxSlots);
            for (int i = 0; i < visible; i++)
            {
                int column = i % Columns;
                int row = i / Columns;
                Rect cell = new Rect(
                    rect.x + column * (cellWidth + Gap),
                    rect.y + row * (CellHeight + Gap),
                    cellWidth,
                    CellHeight);
                ClothingSlot slot = slots[i];
                ClothingSelection? selection;
                selected.TryGetValue(Key(slot.Target, slot.Slot, slot.BodyLayer), out selection);

                Color color = selection == null ? UiColors.EmptyColumn : ColorFor(selection.Phase);
                Widgets.DrawBoxSolid(cell, color);
                Widgets.LabelEllipses(new Rect(cell.x + 4f, cell.y + 3f, cell.width - 8f, 17f),
                    SlotLabel(slot));
                Widgets.LabelEllipses(new Rect(cell.x + 4f, cell.y + 20f, cell.width - 8f, 18f),
                    selection == null ? "free" : selection.DefName);
            }
        }

        private static Color ColorFor(ClothingRulePhase phase)
        {
            switch (phase)
            {
                case ClothingRulePhase.Locked: return new Color(0.22f, 0.34f, 0.55f, 0.95f);
                case ClothingRulePhase.Emergency: return new Color(0.55f, 0.38f, 0.16f, 0.95f);
                default: return new Color(0.22f, 0.46f, 0.28f, 0.95f);
            }
        }

        private static string SlotLabel(ClothingSlot slot)
        {
            return slot.Target == ClothingTarget.Tool
                ? "Tool: " + slot.Slot
                : slot.Slot + " / " + slot.BodyLayer;
        }

        private static string Key(ClothingTarget target, string slot, string? bodyLayer)
        {
            return target == ClothingTarget.Tool
                ? "Tool|" + slot
                : "Apparel|" + slot + "|" + (bodyLayer ?? "");
        }
    }
}