using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OrderedWork.Config;
using OrderedWork.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    public partial class MainTabWindow_WorkControl : MainTabWindow
    {
        private void DrawRowState(Rect rowRect, Pawn pawn)
        {
            if (Find.Selector != null && Find.Selector.IsSelected(pawn))
            {
                Widgets.DrawHighlightSelected(rowRect);
                Widgets.DrawBox(rowRect, 2);
            }
        }

        private void DrawCentered(Rect inRect, string message)
        {
            TextAnchor previous = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(inRect, message);
            Text.Anchor = previous;
        }

        private void DrawPawnColumn(List<Pawn> colonists, Rect viewport, float leftWidth)
        {
            GUI.BeginGroup(viewport);
            try
            {
                for (int i = 0; i < colonists.Count; i++)
                {
                    float y = i * RowHeight - gridScroll.y;
                    if (y + RowHeight < 0f || y > viewport.height)
                    {
                        continue;
                    }

                    Pawn pawn = colonists[i];
                    Rect rowRect = new Rect(0f, y, leftWidth, RowHeight);

                    Color rowColor = (i % 2 == 0)
                        ? new Color(0.14f, 0.15f, 0.17f, 0.55f)
                        : new Color(0.16f, 0.17f, 0.19f, 0.55f);
                    Widgets.DrawBoxSolid(rowRect, rowColor);
                    DrawRowState(rowRect, pawn);

                    float portraitSize = Mathf.Min(32f, RowHeight - 6f);
                    Rect portraitRect = new Rect(3f, y + (RowHeight - portraitSize) / 2f, portraitSize, portraitSize);
                    RenderTexture portrait = PortraitsCache.Get(pawn, new Vector2(portraitSize, portraitSize), Rot4.South);
                    if (portrait != null)
                    {
                        GUI.DrawTexture(portraitRect, portrait);
                    }

                    float infoWidth = leftWidth >= 170f ? 46f : 0f;
                    float textX = portraitRect.xMax + 3f;
                    float textWidth = Mathf.Max(16f, leftWidth - textX - infoWidth - ColumnGutter - 2f);
                    string title = pawn.story != null ? pawn.story.Title : null;

                    TextAnchor previous = Text.Anchor;
                    Text.Anchor = TextAnchor.MiddleLeft;
                    bool previousWrap = Text.WordWrap;
                    Text.WordWrap = false;

                    if (!string.IsNullOrEmpty(title))
                    {
                        Widgets.LabelEllipses(new Rect(textX, y, textWidth, RowHeight / 2f), PawnLabel(pawn));

                        GameFont previousFont = Text.Font;
                        Color previousColor = GUI.color;
                        Text.Font = GameFont.Tiny;
                        GUI.color = new Color(0.75f, 0.75f, 0.75f);
                        Widgets.LabelEllipses(new Rect(textX, y + RowHeight / 2f, textWidth, RowHeight / 2f), title);
                        GUI.color = previousColor;
                        Text.Font = previousFont;
                    }
                    else
                    {
                        Widgets.LabelEllipses(new Rect(textX, y, textWidth, RowHeight), PawnLabel(pawn));
                    }
                    Text.WordWrap = previousWrap;
                    Text.Anchor = previous;

                    if (infoWidth > 0f)
                    {
                        GameFont previousInfoFont = Text.Font;
                        Color previousInfoColor = GUI.color;
                        TextAnchor previousInfoAnchor = Text.Anchor;
                        Text.Font = GameFont.Tiny;
                        Text.Anchor = TextAnchor.MiddleRight;
                        GUI.color = new Color(0.72f, 0.72f, 0.72f);
                        Rect infoRect = new Rect(leftWidth - infoWidth - ColumnGutter - 2f, y, infoWidth, RowHeight);
                        Widgets.Label(new Rect(infoRect.x, y, infoRect.width, RowHeight / 2f),
                            pawn.ageTracker != null ? pawn.ageTracker.AgeBiologicalYears + "y" : "");
                        Widgets.Label(new Rect(infoRect.x, y + RowHeight / 2f, infoRect.width, RowHeight / 2f), HealthShort(pawn));
                        GUI.color = previousInfoColor;
                        Text.Anchor = previousInfoAnchor;
                        Text.Font = previousInfoFont;
                    }

                    if (!dragging && !draggingPalette && Mouse.IsOver(rowRect))
                    {
                        TooltipHandler.TipRegion(rowRect, PawnTooltip(pawn));
                    }

                    Rect nameRect = new Rect(0f, y, leftWidth, RowHeight);
                    if (Event.current.type == EventType.MouseDown
                        && Event.current.button == 0
                        && nameRect.Contains(Event.current.mousePosition))
                    {
                        bool shift = Event.current.shift;
                        if (Event.current.clickCount == 2)
                        {
                            Find.Selector.ClearSelection();
                            Find.Selector.Select(pawn, true, false);
                            if (pawn.Spawned && pawn.Map != null)
                            {
                                CameraJumper.TryJump(pawn.Position, pawn.Map);
                            }
                            Find.MainTabsRoot.EscapeCurrentTab(true);
                        }
                        else if (shift)
                        {
                            if (Find.Selector.IsSelected(pawn))
                            {
                                Find.Selector.Deselect(pawn);
                            }
                            else
                            {
                                Find.Selector.Select(pawn, true, false);
                            }
                        }
                        else
                        {
                            Find.Selector.ClearSelection();
                            Find.Selector.Select(pawn, true, false);
                        }
                        Event.current.Use();
                    }
                }
            }
            finally
            {
                GUI.EndGroup();
            }
        }

        private void DrawRowLane(Pawn pawn, float rowY, List<WorkTypeDef> available)
        {
            LaneData lane = BuildLane(pawn);
            float cardY = rowY + (RowHeight - CardHeight) / 2f;
            int removeIndex = -1;

            int ghostIndex = -1;
            int draggedIndex = -1;
            bool hideDragged = false;

            if (draggingPalette && paletteHoverPawn == pawn && paletteWorkType != null)
            {
                draggedIndex = lane.ids.IndexOf(paletteWorkType.defName);
                ghostIndex = paletteDropIndex;
                if (draggedIndex >= 0 && ghostIndex > draggedIndex)
                {
                    ghostIndex--;
                }
                hideDragged = draggedIndex >= 0;
            }
            else if (dragging && dragPawn == pawn && dragSourceIndex >= 0 && dragTargetIndex >= 0)
            {
                draggedIndex = dragSourceIndex;
                ghostIndex = dragTargetIndex;
                if (ghostIndex > draggedIndex)
                {
                    ghostIndex--;
                }
                hideDragged = true;
            }

            int visual = 0;
            for (int i = 0; i < lane.types.Count; i++)
            {
                if (hideDragged && i == draggedIndex)
                {
                    continue;
                }

                if (ghostIndex >= 0 && visual == ghostIndex)
                {
                    WorkTypePaletteView.DrawGhostSlot(new Rect(CardGap + visual * (CardWidth + CardGap), cardY, CardWidth, CardHeight));
                    visual++;
                }

                Rect cardRect = new Rect(CardGap + visual * (CardWidth + CardGap), cardY, CardWidth, CardHeight);
                DrawCard(pawn, cardRect, lane.types[i], lane.ids, i, ref removeIndex);
                visual++;
            }

            if (ghostIndex >= 0 && visual == ghostIndex)
            {
                WorkTypePaletteView.DrawGhostSlot(new Rect(CardGap + visual * (CardWidth + CardGap), cardY, CardWidth, CardHeight));
                visual++;
            }

            Rect addRect = new Rect(CardGap + visual * (CardWidth + CardGap), cardY, CardWidth, CardHeight);
            if (Widgets.ButtonText(addRect, "+"))
            {
                OpenAddMenu(pawn, lane.ids, available);
            }

            if (removeIndex >= 0 && removeIndex < lane.ids.Count)
            {
                List<string> reduced = new List<string>(lane.ids);
                reduced.RemoveAt(removeIndex);
                WorkOrderService.SaveAndApply(pawn, reduced);
            }
        }

        private void DrawCard(Pawn pawn, Rect cardRect, WorkTypeDef workType, List<string> laneIds, int index, ref int removeIndex)
        {
            Color typeColor = WorkTypeColorRegistry.GetColor(workType);
            Widgets.DrawBoxSolid(cardRect, new Color(typeColor.r, typeColor.g, typeColor.b, 0.28f));
            Widgets.DrawBoxSolid(
                new Rect(cardRect.x, cardRect.y, 2f, cardRect.height),
                new Color(typeColor.r, typeColor.g, typeColor.b, 0.95f));

            if (!dragging && !draggingPalette && Mouse.IsOver(cardRect))
            {
                Widgets.DrawHighlight(cardRect);
            }

            Rect removeRect = new Rect(cardRect.xMax - 20f, cardRect.y + (cardRect.height - 18f) / 2f, 18f, 18f);
            Rect bodyRect = new Rect(cardRect.x, cardRect.y, cardRect.width - 20f, cardRect.height);

            Passion passion;
            SkillRecord bestSkill = WorkTypePaletteView.SafeBestSkill(pawn, workType, out passion);
            int level = bestSkill != null ? bestSkill.Level : -1;

            float x = cardRect.x + 4f;
            if (level >= 0)
            {
                WorkTypePaletteView.PassionVisual visual = WorkTypePaletteView.ResolvePassionVisual(passion);
                if (visual.Texture != null)
                {
                    WorkTypePaletteView.DrawPassionIcon(
                        new Rect(x, cardRect.y + (cardRect.height - WorkTypePaletteView.PassionIconSize) / 2f, WorkTypePaletteView.PassionIconSize, WorkTypePaletteView.PassionIconSize),
                        visual);
                    x += WorkTypePaletteView.PassionIconSize + 2f;
                }

                GameFont previousFont = Text.Font;
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(x, cardRect.y + (cardRect.height - 16f) / 2f, 22f, 16f), level.ToString());
                Text.Font = previousFont;
                x += 22f;
            }

            bool previousWrap = Text.WordWrap;
            Text.WordWrap = false;
            Widgets.LabelEllipses(
                new Rect(x, cardRect.y + (cardRect.height - 20f) / 2f, Mathf.Max(10f, removeRect.x - x - 4f), 20f),
                WorkTypePaletteView.DisplayName(workType));
            Text.WordWrap = previousWrap;

            if (!dragging && !draggingPalette && Mouse.IsOver(cardRect))
            {
                TooltipHandler.TipRegion(cardRect, LaneTooltip(pawn, workType));
            }

            if (Widgets.ButtonText(removeRect, "×"))
            {
                removeIndex = index;
            }

            bool overRemove = removeRect.Contains(Event.current.mousePosition);
            if (!overRemove
                && Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && bodyRect.Contains(Event.current.mousePosition))
            {
                dragging = true;
                dragPawn = pawn;
                dragSourceIndex = index;
                dragTargetIndex = index;
                dragCount = laneIds.Count;
                Event.current.Use();
            }

            if (Widgets.ButtonInvisible(bodyRect))
            {
                // Click only; reordering is driven by the drag events above.
            }
        }

        private void DrawHeldCard()
        {
            Vector2 mouse = Event.current.mousePosition;
            if (draggingPalette && paletteWorkType != null)
            {
                bool incapable = paletteHoverPawn != null && WorkOrderService.IsIncapable(paletteHoverPawn, paletteWorkType);
                WorkTypePaletteView.DrawDragCard(mouse, paletteWorkType, paletteHoverPawn, incapable);
                return;
            }

            if (dragging && dragPawn != null && dragSourceIndex >= 0)
            {
                LaneData lane = BuildLane(dragPawn);
                if (dragSourceIndex < lane.types.Count)
                {
                    WorkTypePaletteView.DrawDragCard(mouse, lane.types[dragSourceIndex], dragPawn, false);
                }
            }
        }

        private static string LaneTooltip(Pawn pawn, WorkTypeDef workType)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(WorkTypePaletteView.DisplayName(workType));
            if (!string.IsNullOrEmpty(workType.description))
            {
                builder.AppendLine().AppendLine(workType.description);
            }

            List<WorkGiverDef> givers = DefDatabase<WorkGiverDef>.AllDefsListForReading
                .Where(g => g.workType == workType)
                .OrderByDescending(g => g.priorityInType)
                .ToList();
            builder.AppendLine();
            builder.AppendLine(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Palette.WorkGivers", "Work givers:"));
            if (givers.Count == 0)
            {
                builder.AppendLine(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Palette.NoneShort", "- none"));
            }
            else
            {
                foreach (WorkGiverDef giver in givers)
                {
                    builder.AppendLine("- " + giver.defName);
                }
            }

            Passion passion;
            SkillRecord bestSkill = WorkTypePaletteView.SafeBestSkill(pawn, workType, out passion);
            builder.AppendLine();
            builder.AppendLine(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.Skill", "Skill: ") + (bestSkill != null ? bestSkill.def.label : "—"));
            builder.AppendLine(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.Passion", "Passion: ") + WorkTypePaletteView.PassionText(bestSkill != null ? passion : Passion.None));
            builder.AppendLine(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.Level", "Level: ") + (bestSkill != null ? bestSkill.Level.ToString() : "—"));

            int rank = WorkOrderService.GetOrder(pawn).IndexOf(workType.defName) + 1;
            builder.AppendLine(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.Priority", "Priority: ") + (rank > 0 ? rank.ToString() : OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.Off", "Off")));

            AppendPawnSpecificOverrides(pawn, workType, builder);
            return builder.ToString();
        }

        private static void AppendPawnSpecificOverrides(Pawn pawn, WorkTypeDef workType, StringBuilder builder)
        {
            // Hook for future per-pawn/per-giver overrides. None implemented yet.
            PawnSpecificOverrideHook?.Invoke(pawn, workType, builder);
        }

        private static string HealthShort(Pawn pawn)
        {
            if (pawn == null || pawn.health == null)
            {
                return "";
            }
            if (pawn.health.State == PawnHealthState.Dead)
            {
                return OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Status.Dead", "Dead");
            }
            if (pawn.health.State == PawnHealthState.Down)
            {
                return OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Status.Down", "Down");
            }
            float percent = pawn.health.summaryHealth != null
                ? pawn.health.summaryHealth.SummaryHealthPercent
                : 1f;
            return percent < 0.999f ? Mathf.RoundToInt(percent * 100f) + "%" : OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Status.Ok", "OK");
        }

        private static string PawnTooltip(Pawn pawn)
        {
            string text = pawn.Name != null ? pawn.Name.ToStringFull : pawn.LabelShort;
            if (pawn.ageTracker != null)
            {
                text += "\n" + OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.Age", "Age: ") + pawn.ageTracker.AgeBiologicalYears + "y";
            }
            if (pawn.story != null && !string.IsNullOrEmpty(pawn.story.Title))
            {
                text += "\n" + OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.TitleLabel", "Title: ") + pawn.story.Title;
            }
            string health = HealthShort(pawn);
            if (!string.IsNullOrEmpty(health))
            {
                text += "\n" + OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.Health", "Health: ") + health;
            }
            text += "\n" + OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.WorkTypes", "Work types: ") + WorkOrderService.GetOrder(pawn).Count;
            return text;
        }

        private static string PawnLabel(Pawn pawn)
        {
            string label = pawn.NameFullColored;
            return string.IsNullOrEmpty(label) ? pawn.LabelShort : label;
        }

        private static LaneData BuildLane(Pawn pawn)
        {
            LaneData lane = new LaneData();
            foreach (string id in WorkOrderService.GetOrder(pawn))
            {
                WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(id);
                if (workType != null)
                {
                    lane.types.Add(workType);
                    lane.ids.Add(id);
                }
            }
            return lane;
        }

        private static int SlotAtX(float x, int count)
        {
            int slot = Mathf.RoundToInt((x - CardGap) / (CardWidth + CardGap));
            return Mathf.Clamp(slot, 0, count);
        }

        private sealed class LaneData
        {
            public readonly List<WorkTypeDef> types = new List<WorkTypeDef>();
            public readonly List<string> ids = new List<string>();
        }

    }
}
