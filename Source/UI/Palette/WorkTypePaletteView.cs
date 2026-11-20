using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// VSE/Alpha Skills integration is read-only reflection over their public PassionDef
    /// contract; no third-party source or assets are copied or redistributed. See CREDITS.md.
    /// </summary>
    [StaticConstructorOnStartup]
    internal static partial class WorkTypePaletteView
    {
        internal const float ChipWidth = 110f;
        internal const float ChipHeight = 22f;
        internal const float ChipGap = 3f;
        internal const int Rows = 3;

        // Shared by lane cards, the drag card and Details; change once here.
        internal const float PassionIconSize = 14f;
        private const float HeldCardWidth = 180f;

        // Shared by the vanilla fallback and the VSE/Alpha gray-icon fallback.
        private static readonly Color MinorPassionTint = new Color(1f, 0.86f, 0.30f, 1f);
        private static readonly Color MajorPassionTint = new Color(1f, 0.55f, 0.12f, 1f);
        private static readonly Color DisabledPassionTint = new Color(0.55f, 0.55f, 0.55f, 1f);

        /// <summary>Texture + tint for a passion icon; Texture is null when no icon should be drawn.</summary>
        internal struct PassionVisual
        {
            public Texture2D Texture;
            public Color Tint;
        }

        /// <summary>
        /// Visual-only palette entries for the debug scroll stress test. They are never
        /// registered in DefDatabase and must not be draggable or assignable.
        /// </summary>
        internal static bool IsDebugPaletteType(WorkTypeDef type)
        {
            return type != null
                && !string.IsNullOrEmpty(type.defName)
                && type.defName.StartsWith("OWDebugPalette_", StringComparison.Ordinal);
        }

        private static List<WorkTypeDef> debugPaletteTypes = new List<WorkTypeDef>();
        private static int debugPaletteTypeCount;

        internal static List<WorkTypeDef> BuildDebugPaletteTypes(int count)
        {
            // Called every frame while the stress setting is on; build the visual-only
            // defs once so the debug toggle cannot allocate 60 objects per repaint.
            if (debugPaletteTypes != null && debugPaletteTypeCount == count)
            {
                return debugPaletteTypes;
            }

            debugPaletteTypes = new List<WorkTypeDef>();
            debugPaletteTypeCount = count;
            for (int i = 1; i <= count; i++)
            {
                debugPaletteTypes.Add(new WorkTypeDef
                {
                    defName = "OWDebugPalette_" + i,
                    label = "Debug Work " + i,
                    labelShort = "Debug " + i,
                    description = "Visual-only palette stress entry; not assignable.",
                    naturalPriority = 0,
                    visible = true
                });
            }
            return debugPaletteTypes;
        }

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
            bool showScrollbar,
            bool suppressTooltips,
            Action<WorkTypeDef> beginDrag)
        {
            if (types == null || types.Count == 0)
            {
                return;
            }

            Rect viewport = new Rect(rect.x, rect.y, rect.width, rect.height - WorkGridScrollView.Thickness);
            int perColumn = Mathf.Max(1, Mathf.CeilToInt(types.Count / (float)Rows));
            float contentWidth = Mathf.Max(viewport.width, perColumn * (ChipWidth + ChipGap) + ChipGap);
            Rect content = new Rect(0f, 0f, contentWidth, viewport.height);
            Rect hTrack = new Rect(rect.x, rect.yMax - WorkGridScrollView.Thickness, rect.width, WorkGridScrollView.Thickness);

            // Shift+wheel gives the palette its own horizontal quick scroll.
            WorkGridScrollView.HandleWheel(viewport, viewport, content, ref scroll);

            // Handle the bar before the scroll view so the content cannot eat its MouseDown.
            if (showScrollbar)
            {
                WorkGridScrollView.HandleHorizontal(hTrack, viewport, content, ref scroll, typeof(WorkTypePaletteView));
            }

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

                    // Top-to-bottom inside a column, then left-to-right.
                    int row = i % Rows;
                    int column = i / Rows;
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
                        TooltipHandler.TipRegion(chip, PaletteTooltip(type));
                    }

                    if (!IsDebugPaletteType(type)
                        && Event.current.type == EventType.MouseDown
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
                WorkGridScrollView.DrawHorizontal(hTrack, viewport, content, ref scroll);
            }
        }

        internal static void DrawDragCard(Vector2 mouse, WorkTypeDef type, Pawn hoveredPawn, bool incapable)
        {
            if (type == null)
            {
                return;
            }

            Rect rect = new Rect(mouse.x + 12f, mouse.y + 10f, HeldCardWidth, 28f);
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
                SkillRecord? bestSkill = SafeBestSkill(hoveredPawn, type, out passion);
                int level = bestSkill != null ? bestSkill.Level : -1;
                if (level >= 0)
                {
                    PassionVisual visual = ResolvePassionVisual(passion);
                    if (visual.Texture != null)
                    {
                        DrawPassionIcon(new Rect(x, rect.y + 8f, PassionIconSize, PassionIconSize), visual);
                        x += PassionIconSize + 2f;
                    }

                    GameFont previousFont = Text.Font;
                    Text.Font = GameFont.Tiny;
                    Widgets.Label(new Rect(x, rect.y, 22f, rect.height), level.ToString());
                    Text.Font = previousFont;
                    x += 22f;
                }
            }

            string label = incapable ? DisplayName(type) + OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Palette.IncapableSuffix", " (Incapable)") : DisplayName(type);
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
                    OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Palette.CannotDo", "Ordered Work: ") + skipped + OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Palette.CannotDoSuffix", " selected pawn(s) cannot do ") + DisplayName(type) + OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Palette.IncapableSuffix", " (Incapable)."),
                    MessageTypeDefOf.RejectInput,
                    false);
            }
        }

    }
}
