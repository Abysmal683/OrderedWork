using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OrderedWork.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Three-row WorkType palette, drag ghost and drop application. The main tab owns the
    /// drag state; this helper draws the chips/ghost and applies a drop to the target pawns.
    /// </summary>
    internal static class WorkTypePaletteView
    {
        internal const float ChipWidth = 110f;
        internal const float ChipHeight = 22f;
        internal const float ChipGap = 3f;
        internal const int Rows = 3;

        private const float PassionIconSize = 12f;

        internal static string DisplayName(WorkTypeDef type)
        {
            if (type == null)
            {
                return "";
            }
            string name = !string.IsNullOrEmpty(type.labelShort) ? type.labelShort : type.label;
            if (string.IsNullOrEmpty(name))
            {
                return type.defName;
            }
            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        internal static void DrawPalette(
            Rect rect,
            ref Vector2 scroll,
            List<WorkTypeDef> types,
            WorkTypeDef draggingType,
            Pawn contextPawn,
            bool showScrollbar,
            bool suppressTooltips,
            Action<WorkTypeDef> beginDrag)
        {
            if (types == null || types.Count == 0)
            {
                return;
            }

            Rect viewport = new Rect(rect.x, rect.y, rect.width, rect.height - WorkGridScrollView.Thickness);
            int perRow = Mathf.Max(1, Mathf.CeilToInt(types.Count / (float)Rows));
            float contentWidth = Mathf.Max(viewport.width, perRow * (ChipWidth + ChipGap) + ChipGap);
            Rect content = new Rect(0f, 0f, contentWidth, viewport.height);

            Widgets.BeginScrollView(viewport, ref scroll, content, false);
            try
            {
                for (int i = 0; i < types.Count; i++)
                {
                    WorkTypeDef type = types[i];
                    if (type == null)
                    {
                        continue;
                    }

                    int row = i / perRow;
                    int column = i % perRow;
                    Rect chip = new Rect(
                        ChipGap + column * (ChipWidth + ChipGap),
                        row * (ChipHeight + ChipGap),
                        ChipWidth,
                        ChipHeight);

                    Color typeColor = WorkTypeColorRegistry.GetColor(type);
                    Widgets.DrawBoxSolid(
                        chip,
                        type == draggingType
                            ? new Color(typeColor.r, typeColor.g, typeColor.b, 0.55f)
                            : new Color(typeColor.r, typeColor.g, typeColor.b, 0.30f));
                    Widgets.DrawBoxSolid(new Rect(chip.x, chip.y, 2f, chip.height), new Color(typeColor.r, typeColor.g, typeColor.b, 0.95f));

                    if (Mouse.IsOver(chip))
                    {
                        Widgets.DrawHighlight(chip);
                    }

                    bool previousWrap = Text.WordWrap;
                    Text.WordWrap = false;
                    Widgets.LabelEllipses(new Rect(chip.x + 5f, chip.y, chip.width - 9f, chip.height), DisplayName(type));
                    Text.WordWrap = previousWrap;

                    if (!suppressTooltips && draggingType == null && Mouse.IsOver(chip))
                    {
                        TooltipHandler.TipRegion(chip, PaletteTooltip(type, contextPawn));
                    }

                    if (Event.current.type == EventType.MouseDown
                        && Event.current.button == 0
                        && chip.Contains(Event.current.mousePosition))
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

            if (showScrollbar)
            {
                WorkGridScrollView.DrawHorizontal(
                    new Rect(rect.x, rect.yMax - WorkGridScrollView.Thickness, rect.width, WorkGridScrollView.Thickness),
                    viewport,
                    content,
                    ref scroll);
            }
        }

        internal static void DrawDragCard(Vector2 mouse, WorkTypeDef type, Pawn hoveredPawn, bool incapable)
        {
            if (type == null)
            {
                return;
            }

            Rect rect = new Rect(mouse.x + 12f, mouse.y + 10f, 200f, 28f);
            Color typeColor = WorkTypeColorRegistry.GetColor(type);
            Widgets.DrawBoxSolid(
                rect,
                incapable
                    ? new Color(0.36f, 0.12f, 0.12f, 0.95f)
                    : new Color(typeColor.r * 0.55f, typeColor.g * 0.55f, typeColor.b * 0.55f, 0.95f));

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

            string label = incapable ? DisplayName(type) + " (Incapaz)" : DisplayName(type);
            bool previousWrap = Text.WordWrap;
            Text.WordWrap = false;
            Widgets.LabelEllipses(new Rect(x, rect.y, Mathf.Max(10f, rect.xMax - x - 4f), rect.height), label);
            Text.WordWrap = previousWrap;
        }

        internal static void DrawGhostSlot(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.16f, 0.28f, 0.34f, 0.55f));
            Color previous = GUI.color;
            GUI.color = new Color(0.35f, 0.85f, 1f, 0.95f);
            Widgets.DrawBox(rect, 1);
            GUI.color = previous;
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
                    "Ordered Work: " + skipped + " selected pawn(s) cannot do " + DisplayName(type) + " (Incapaz).",
                    MessageTypeDefOf.RejectInput,
                    false);
            }
        }

        private static string PaletteTooltip(WorkTypeDef type, Pawn contextPawn)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(DisplayName(type));
            builder.Append(" (").Append(type.defName).Append(")");
            if (!string.IsNullOrEmpty(type.description))
            {
                builder.AppendLine().AppendLine(type.description);
            }

            List<WorkGiverDef> givers = DefDatabase<WorkGiverDef>.AllDefsListForReading
                .Where(g => g.workType == type)
                .OrderByDescending(g => g.priorityInType)
                .ToList();
            builder.AppendLine();
            builder.AppendLine("Work givers:");
            if (givers.Count == 0)
            {
                builder.AppendLine("- none");
            }
            else
            {
                foreach (WorkGiverDef giver in givers)
                {
                    builder.AppendLine("- " + giver.defName);
                }
            }

            Passion passion = Passion.None;
            int level = contextPawn != null ? WorkOrderService.BestSkillLevel(contextPawn, type, out passion) : -1;
            builder.AppendLine();
            builder.Append("Skill");
            if (contextPawn != null)
            {
                builder.Append(" (").Append(contextPawn.LabelShort).Append(")");
            }
            builder.Append(": ").Append(level >= 0 ? level + " " + PassionText(passion) : "—");
            return builder.ToString();
        }

        internal static string PassionText(Passion passion)
        {
            if (passion == Passion.Major)
            {
                return "major";
            }
            if (passion == Passion.Minor)
            {
                return "minor";
            }
            return "none";
        }
    }
}
