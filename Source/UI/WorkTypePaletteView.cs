using System.Collections.Generic;
using OrderedWork.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Compact horizontal WorkType palette and its drag ghost. The main tab owns the drag
    /// state; this helper draws the palette/card and applies a drop to the target pawns.
    /// </summary>
    internal static class WorkTypePaletteView
    {
        internal const float CardWidth = 120f;
        internal const float CardHeight = 26f;
        internal const float Gap = 4f;

        private const float PassionIconSize = 12f;

        internal static void DrawPalette(
            Rect rect,
            ref Vector2 scroll,
            List<WorkTypeDef> types,
            WorkTypeDef draggingType,
            System.Action<WorkTypeDef> beginDrag)
        {
            if (types == null || types.Count == 0)
            {
                return;
            }

            float contentWidth = Mathf.Max(rect.width, types.Count * (CardWidth + Gap) + Gap);
            Rect content = new Rect(0f, 0f, contentWidth, rect.height);

            Widgets.BeginScrollView(rect, ref scroll, content);
            try
            {
                for (int i = 0; i < types.Count; i++)
                {
                    WorkTypeDef type = types[i];
                    if (type == null)
                    {
                        continue;
                    }

                    Rect card = new Rect(Gap + i * (CardWidth + Gap), 2f, CardWidth, CardHeight);
                    Widgets.DrawBoxSolid(
                        card,
                        type == draggingType
                            ? new Color(0.20f, 0.32f, 0.42f, 0.95f)
                            : new Color(0.17f, 0.18f, 0.20f, 0.75f));

                    if (Mouse.IsOver(card))
                    {
                        Widgets.DrawHighlight(card);
                    }

                    bool previousWrap = Text.WordWrap;
                    Text.WordWrap = false;
                    Widgets.LabelEllipses(new Rect(card.x + 4f, card.y, card.width - 8f, card.height), type.labelShort);
                    Text.WordWrap = previousWrap;

                    if (Event.current.type == EventType.MouseDown
                        && Event.current.button == 0
                        && card.Contains(Event.current.mousePosition))
                    {
                        beginDrag(type);
                        Event.current.Use();
                    }
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        internal static void DrawDragCard(Vector2 mouse, WorkTypeDef type, Pawn hoveredPawn, bool incapable)
        {
            if (type == null)
            {
                return;
            }

            Rect rect = new Rect(mouse.x + 12f, mouse.y + 10f, 200f, 28f);
            Widgets.DrawBoxSolid(
                rect,
                incapable
                    ? new Color(0.36f, 0.12f, 0.12f, 0.95f)
                    : new Color(0.10f, 0.24f, 0.32f, 0.95f));

            float x = rect.x + 4f;
            if (hoveredPawn != null && !incapable)
            {
                Passion passion;
                int level = WorkOrderService.BestSkillLevel(hoveredPawn, type, out passion);
                if (level >= 0)
                {
                    if (passion == Passion.Major)
                    {
                        GUI.DrawTexture(new Rect(x, rect.y + 8f, PassionIconSize, PassionIconSize), WidgetsWork.PassionWorkboxMajorIcon);
                    }
                    else if (passion == Passion.Minor)
                    {
                        GUI.DrawTexture(new Rect(x, rect.y + 8f, PassionIconSize, PassionIconSize), WidgetsWork.PassionWorkboxMinorIcon);
                    }
                    x += PassionIconSize + 2f;

                    GameFont previousFont = Text.Font;
                    Text.Font = GameFont.Tiny;
                    Widgets.Label(new Rect(x, rect.y, 22f, rect.height), level.ToString());
                    Text.Font = previousFont;
                    x += 22f;
                }
            }

            string label = incapable ? type.labelShort + " (Incapaz)" : type.labelShort;
            bool previousWrap = Text.WordWrap;
            Text.WordWrap = false;
            Widgets.LabelEllipses(new Rect(x, rect.y, Mathf.Max(10f, rect.xMax - x - 4f), rect.height), label);
            Text.WordWrap = previousWrap;
        }

        internal static void ApplyDrop(WorkTypeDef type, Pawn hoveredPawn, int dropIndex, List<Pawn> selected)
        {
            if (type == null)
            {
                return;
            }

            List<Pawn> targets = selected != null && selected.Count > 0
                ? selected
                : (hoveredPawn != null ? new List<Pawn> { hoveredPawn } : new List<Pawn>());
            if (targets.Count == 0)
            {
                return;
            }

            int skipped = 0;
            foreach (Pawn pawn in targets)
            {
                if (pawn == null || pawn.Destroyed || !pawn.IsFreeColonist)
                {
                    skipped++;
                    continue;
                }
                if (WorkOrderService.IsIncapable(pawn, type))
                {
                    skipped++;
                    continue;
                }

                List<string> ids = WorkOrderService.GetOrder(pawn);
                int index = ids.IndexOf(type.defName);
                int target = Mathf.Clamp(dropIndex, 0, ids.Count);

                if (index >= 0)
                {
                    // Already present: no-op when the ghost is adjacent to its current slot.
                    if (target == index || target == index + 1)
                    {
                        continue;
                    }
                    ids.RemoveAt(index);
                    if (target > index)
                    {
                        target--;
                    }
                    ids.Insert(Mathf.Clamp(target, 0, ids.Count), type.defName);
                }
                else
                {
                    ids.Insert(target, type.defName);
                }

                WorkOrderService.SaveAndApply(pawn, ids);
            }

            if (skipped > 0)
            {
                Messages.Message(
                    "Ordered Work: " + skipped + " selected pawn(s) cannot do " + type.labelShort + " (Incapaz).",
                    MessageTypeDefOf.RejectInput,
                    false);
            }
        }
    }
}
