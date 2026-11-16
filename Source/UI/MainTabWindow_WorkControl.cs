using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Data;
using OrderedWork.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Full-screen "Work Control" tab. The shell mirrors the vanilla Work tab: one row per
    /// free colonist, a fixed pawn column on the left and a horizontal lane of flat WorkType
    /// cards on the right. A single scroll view supplies the outer vertical scroll and the
    /// shared horizontal scroll, so moving it moves every lane together.
    /// </summary>
    public class MainTabWindow_WorkControl : MainTabWindow
    {
        private const float Pad = 4f;
        private const float TitleHeight = 28f;
        private const float SearchHeight = 24f;
        private const float HeaderHeight = 18f;
        private const float RowHeight = 42f;
        private const float CardWidth = 190f;
        private const float CardHeight = 30f;
        private const float CardGap = 4f;
        private const float LeftColumnMin = 160f;
        private const float LeftColumnMax = 260f;
        private const float PassionIconSize = 12f;

        private Vector2 gridScroll;
        private Pawn selectedPawn;
        private string searchText = "";

        private bool dragging;
        private Pawn dragPawn;
        private int dragSourceIndex = -1;
        private int dragTargetIndex = -1;
        private int dragCount;

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.DrawBoxSolid(inRect, new Color(0.11f, 0.12f, 0.13f, 1f));

            if (Find.CurrentMap == null)
            {
                DrawCentered(inRect, "Load a colony");
                return;
            }

            List<Pawn> allColonists = Find.CurrentMap.mapPawns.FreeColonists.ToList();
            if (allColonists.Count == 0)
            {
                DrawCentered(inRect, "No free colonists");
                return;
            }

            WorkOrderService.EnsureColonyInitialized();
            if (selectedPawn != null && (selectedPawn.Destroyed || !allColonists.Contains(selectedPawn)))
            {
                selectedPawn = null;
            }

            float leftWidth = Mathf.Clamp(inRect.width * 0.22f, LeftColumnMin, LeftColumnMax);
            float titleY = Pad;
            float searchY = titleY + TitleHeight;
            float headerY = searchY + SearchHeight + 2f;
            float bodyTop = headerY + HeaderHeight + 2f;
            float bodyHeight = Mathf.Max(40f, inRect.yMax - Pad - bodyTop);
            Rect bodyRect = new Rect(Pad, bodyTop, inRect.width - Pad * 2f, bodyHeight);
            Rect laneViewport = new Rect(bodyRect.x + leftWidth, bodyRect.y, bodyRect.width - leftWidth, bodyRect.height);

            // Title + top controls.
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(Pad, titleY, Mathf.Max(100f, inRect.width - 420f), TitleHeight), "Work Control");
            Text.Font = GameFont.Small;

            Rect openRect = new Rect(inRect.xMax - Pad - 116f, titleY + 2f, 116f, 24f);
            Rect modeRect = new Rect(openRect.x - 4f - 150f, titleY + 2f, 150f, 24f);
            Rect detailsRect = new Rect(modeRect.x - 4f - 80f, titleY + 2f, 80f, 24f);
            Rect recommendRect = new Rect(detailsRect.x - 4f - 28f, titleY + 2f, 28f, 24f);
            DrawTopControls(recommendRect, detailsRect, modeRect, openRect);

            // Search row (pawn names only). The filter never selects or edits anything.
            Widgets.Label(new Rect(Pad, searchY, 88f, SearchHeight), "Search pawns");
            searchText = Widgets.TextField(new Rect(Pad + 92f, searchY, 240f, SearchHeight - 2f), searchText ?? "");

            bool filtering = !string.IsNullOrWhiteSpace(searchText);
            List<Pawn> colonists = filtering ? allColonists.Where(IsSearchMatch).ToList() : allColonists;

            // Column header.
            Widgets.Label(new Rect(bodyRect.x, headerY, leftWidth - 6f, HeaderHeight), "Pawn");
            Widgets.Label(new Rect(laneViewport.x, headerY, laneViewport.width, HeaderHeight),
                "Work order (left = highest). Drag to reorder, × remove, + add.");

            if (colonists.Count == 0)
            {
                DrawCentered(bodyRect, "No pawns match");
                return;
            }

            List<WorkTypeDef> available = WorkOrderService.GetAvailableWorkTypes();

            int maxLane = 0;
            foreach (Pawn pawn in colonists)
            {
                maxLane = Mathf.Max(maxLane, BuildLane(pawn).types.Count);
            }

            float laneContentWidth = Mathf.Max(laneViewport.width, (maxLane + 1) * (CardWidth + CardGap) + CardGap);
            float rowsHeight = colonists.Count * RowHeight;
            float contentHeight = Mathf.Max(laneViewport.height, rowsHeight);
            Rect laneContent = new Rect(0f, 0f, laneContentWidth, contentHeight);

            Widgets.BeginScrollView(laneViewport, ref gridScroll, laneContent);
            try
            {
                HandleDrop();

                for (int i = 0; i < colonists.Count; i++)
                {
                    Pawn pawn = colonists[i];
                    float rowY = i * RowHeight;
                    Rect rowRect = new Rect(0f, rowY, laneContentWidth, RowHeight);

                    Color rowColor = (i % 2 == 0)
                        ? new Color(0.14f, 0.15f, 0.17f, 0.55f)
                        : new Color(0.16f, 0.17f, 0.19f, 0.55f);
                    Widgets.DrawBoxSolid(rowRect, rowColor);
                    DrawRowState(rowRect, pawn);

                    DrawRowLane(pawn, rowY, available);
                }
            }
            finally
            {
                Widgets.EndScrollView();
                Text.Font = GameFont.Small;
            }

            DrawPawnColumn(colonists, bodyRect, leftWidth);
        }

        private void DrawTopControls(Rect recommendRect, Rect detailsRect, Rect modeRect, Rect openRect)
        {
            bool hasSelection = selectedPawn != null;

            // Top-bar recommendation: only the selected Work Control row, never a guess.
            if (Widgets.ButtonText(recommendRect, "R", true, true, hasSelection))
            {
                WorkOrderService.GenerateAndApplyRecommendation(selectedPawn);
            }
            if (Mouse.IsOver(recommendRect))
            {
                TooltipHandler.TipRegion(recommendRect, "Generate recommendation for selected pawn");
            }

            if (Widgets.ButtonText(detailsRect, "Details", true, true, hasSelection))
            {
                Find.WindowStack.Add(new Dialog_WorkControlDetails(selectedPawn));
            }
            if (Mouse.IsOver(detailsRect))
            {
                TooltipHandler.TipRegion(detailsRect, "More details for selected pawn");
            }

            OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
            if (component != null)
            {
                if (Widgets.ButtonText(modeRect, "Mode: " + component.viewMode))
                {
                    component.viewMode = NextViewMode(component.viewMode);
                }
                if (Mouse.IsOver(modeRect))
                {
                    TooltipHandler.TipRegion(modeRect, "Click to cycle Draw / Manual / Both");
                }
            }

            if (Widgets.ButtonText(openRect, "Open Work tab"))
            {
                MainButtonDef workTab = DefDatabase<MainButtonDef>.GetNamedSilentFail("Work");
                if (workTab != null)
                {
                    Find.MainTabsRoot.ToggleTab(workTab, true);
                }
            }
        }

        private void DrawRowState(Rect rowRect, Pawn pawn)
        {
            if (selectedPawn == pawn)
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

        private void DrawPawnColumn(List<Pawn> colonists, Rect bodyRect, float leftWidth)
        {
            GUI.BeginGroup(bodyRect);
            try
            {
                for (int i = 0; i < colonists.Count; i++)
                {
                    float y = i * RowHeight - gridScroll.y;
                    if (y + RowHeight < 0f || y > bodyRect.height)
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

                    TextAnchor previous = Text.Anchor;
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(new Rect(4f, y, leftWidth - 8f, RowHeight), PawnLabel(pawn));
                    Text.Anchor = previous;

                    // Single click selects visually. Double-click selects in game (unless
                    // already selected) and pans the camera; neither changes priorities.
                    Rect nameRect = new Rect(0f, y, leftWidth, RowHeight);
                    if (Event.current.type == EventType.MouseDown
                        && Event.current.button == 0
                        && nameRect.Contains(Event.current.mousePosition))
                    {
                        selectedPawn = pawn;
                        if (Event.current.clickCount == 2 && !Find.Selector.IsSelected(pawn))
                        {
                            Find.Selector.ClearSelection();
                            Find.Selector.Select(pawn, true, false);
                            if (pawn.Spawned && pawn.Map != null)
                            {
                                CameraJumper.TryJump(pawn.Position, pawn.Map);
                            }
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

            for (int i = 0; i < lane.types.Count; i++)
            {
                Rect cardRect = new Rect(CardGap + i * (CardWidth + CardGap), cardY, CardWidth, CardHeight);
                DrawCard(pawn, cardRect, lane.types[i], lane.ids, i, ref removeIndex);
            }

            Rect addRect = new Rect(CardGap + lane.types.Count * (CardWidth + CardGap), cardY, CardWidth, CardHeight);
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
            Widgets.DrawBoxSolid(cardRect, new Color(0.17f, 0.18f, 0.20f, 0.55f));

            if (dragging && pawn == dragPawn && index == dragTargetIndex)
            {
                Widgets.DrawHighlight(cardRect);
            }
            else if (Mouse.IsOver(cardRect))
            {
                Widgets.DrawHighlight(cardRect);
            }

            Rect removeRect = new Rect(cardRect.xMax - 20f, cardRect.y + (cardRect.height - 18f) / 2f, 18f, 18f);
            Rect bodyRect = new Rect(cardRect.x, cardRect.y, cardRect.width - 20f, cardRect.height);

            Passion passion;
            int level = WorkOrderService.BestSkillLevel(pawn, workType, out passion);

            // Fixed order on one line: passion icon -> level -> name -> ×.
            // With no relevant skill only the name and × are drawn (no placeholders).
            float x = cardRect.x + 4f;
            if (level >= 0)
            {
                if (passion == Passion.Major || passion == Passion.Minor)
                {
                    DrawPassionIcon(new Rect(x, cardRect.y + (cardRect.height - PassionIconSize) / 2f, PassionIconSize, PassionIconSize), passion);
                }
                x += PassionIconSize + 2f;

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
                workType.labelShort);
            Text.WordWrap = previousWrap;

            if (Mouse.IsOver(cardRect))
            {
                TooltipHandler.TipRegion(cardRect, CardTooltip(pawn, workType));
            }

            if (Widgets.ButtonText(removeRect, "×"))
            {
                removeIndex = index;
            }

            // The remove button must never start a drag.
            bool overRemove = removeRect.Contains(Event.current.mousePosition);
            if (!overRemove
                && Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && bodyRect.Contains(Event.current.mousePosition))
            {
                selectedPawn = pawn;
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

        private static void DrawPassionIcon(Rect rect, Passion passion)
        {
            Texture2D icon = null;
            if (passion == Passion.Major)
            {
                icon = WidgetsWork.PassionWorkboxMajorIcon;
            }
            else if (passion == Passion.Minor)
            {
                icon = WidgetsWork.PassionWorkboxMinorIcon;
            }

            if (icon != null)
            {
                GUI.DrawTexture(rect, icon);
            }
        }

        private void HandleDrop()
        {
            if (!dragging)
            {
                return;
            }

            if (Event.current.type != EventType.MouseUp)
            {
                dragTargetIndex = IndexAtX(Event.current.mousePosition.x, dragCount);
                return;
            }

            int from = dragSourceIndex;
            int to = IndexAtX(Event.current.mousePosition.x, dragCount);
            Pawn pawn = dragPawn;

            dragging = false;
            dragPawn = null;
            dragSourceIndex = -1;
            dragTargetIndex = -1;

            if (pawn != null && from >= 0 && to >= 0 && from != to)
            {
                List<string> ids = BuildLane(pawn).ids;
                if (from < ids.Count && to < ids.Count)
                {
                    string moved = ids[from];
                    ids.RemoveAt(from);
                    ids.Insert(Mathf.Clamp(to, 0, ids.Count), moved);
                    WorkOrderService.SaveAndApply(pawn, ids);
                }
            }
            Event.current.Use();
        }

        private void OpenAddMenu(Pawn pawn, List<string> currentOrder, List<WorkTypeDef> available)
        {
            HashSet<string> inOrder = new HashSet<string>(currentOrder);
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            foreach (WorkTypeDef workType in available)
            {
                if (inOrder.Contains(workType.defName))
                {
                    continue;
                }

                int level = WorkOrderService.BestSkillLevel(pawn, workType, out _);
                string levelText = level >= 0 ? level.ToString() : "-";
                string label = workType.labelShort + " [" + levelText + "]";

                if (WorkOrderService.IsIncapable(pawn, workType))
                {
                    // Shown but disabled: incapable types can never be added to the lane.
                    options.Add(new FloatMenuOption(label + " - Incapaz", null));
                    continue;
                }

                WorkTypeDef local = workType;
                options.Add(new FloatMenuOption(label, delegate
                {
                    List<string> updated = WorkOrderService.GetOrder(pawn);
                    if (!updated.Contains(local.defName))
                    {
                        updated.Add(local.defName);
                        WorkOrderService.SaveAndApply(pawn, updated);
                    }
                }));
            }

            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption("Nothing available to add", null));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static string CardTooltip(Pawn pawn, WorkTypeDef workType)
        {
            string text = workType.labelShort;
            if (workType.relevantSkills != null && workType.relevantSkills.Count > 0)
            {
                foreach (SkillDef skill in workType.relevantSkills)
                {
                    if (skill == null)
                    {
                        continue;
                    }
                    SkillRecord record = pawn.skills != null ? pawn.skills.GetSkill(skill) : null;
                    string level = record != null ? record.Level.ToString() : "—";
                    text += "\n" + skill.label + ": " + level;
                }
            }
            else
            {
                text += "\nNo relevant skill";
            }
            return text;
        }

        /// <summary>
        /// Flexible pawn-name match across label, full/short name, colored name, title and
        /// the first/nick/last components. Used only to filter drawn rows.
        /// </summary>
        private bool IsSearchMatch(Pawn pawn)
        {
            string term = searchText?.Trim();
            if (string.IsNullOrEmpty(term))
            {
                return true;
            }
            return SearchBlob(pawn).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string SearchBlob(Pawn pawn)
        {
            string blob = pawn.LabelShort + "\n" + pawn.Label;
            if (pawn.Name != null)
            {
                blob += "\n" + pawn.Name.ToStringFull + "\n" + pawn.Name.ToStringShort;
            }
            blob += "\n" + (string)pawn.NameFullColored;
            if (pawn.story != null)
            {
                blob += "\n" + pawn.story.Title;
            }

            if (pawn.Name is NameTriple triple)
            {
                blob += "\n" + triple.First + "\n" + triple.Nick + "\n" + triple.Last;
            }
            else if (pawn.Name is NameSingle singleName)
            {
                blob += "\n" + singleName.Name;
            }
            return blob;
        }

        private static WorkViewMode NextViewMode(WorkViewMode mode)
        {
            switch (mode)
            {
                case WorkViewMode.Draw:
                    return WorkViewMode.Manual;
                case WorkViewMode.Manual:
                    return WorkViewMode.Both;
                default:
                    return WorkViewMode.Draw;
            }
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

        private static int IndexAtX(float x, int count)
        {
            if (count <= 0)
            {
                return 0;
            }
            int index = Mathf.FloorToInt((x - CardGap) / (CardWidth + CardGap));
            return Mathf.Clamp(index, 0, count - 1);
        }

        private sealed class LaneData
        {
            public readonly List<WorkTypeDef> types = new List<WorkTypeDef>();
            public readonly List<string> ids = new List<string>();
        }
    }
}
