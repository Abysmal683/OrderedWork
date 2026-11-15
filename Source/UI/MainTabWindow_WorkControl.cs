using System.Collections.Generic;
using System.Linq;
using OrderedWork.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Full-screen "Work Control" tab. Shows a pawn selector, a horizontal WorkType lane
    /// and a read-only result table, all inside one outer vertical scroll view.
    /// Edits write straight into the pawn's work priorities.
    /// </summary>
    public class MainTabWindow_WorkControl : MainTabWindow
    {
        private const float Pad = 12f;
        private const float TitleHeight = 34f;
        private const float PawnRowHeight = 30f;
        private const float PawnRowGap = 4f;
        private const float LaneHeight = 140f;
        private const float CardWidth = 220f;
        private const float CardHeight = 100f;
        private const float CardGap = 8f;
        private const float TableRowHeight = 24f;

        private Pawn selectedPawn;
        private Vector2 outerScroll;
        private Vector2 laneScroll;

        private bool dragging;
        private int dragSourceIndex = -1;
        private int dragTargetIndex = -1;

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.DrawBoxSolid(inRect, new Color(0.11f, 0.12f, 0.13f, 1f));

            if (Find.CurrentMap == null)
            {
                DrawCentered(inRect, "Load a colony");
                return;
            }

            List<Pawn> colonists = Find.CurrentMap.mapPawns.FreeColonists.ToList();
            if (colonists.Count == 0)
            {
                DrawCentered(inRect, "No free colonists");
                return;
            }

            // Initial selection must not apply or write anything.
            if (selectedPawn == null || !colonists.Contains(selectedPawn))
            {
                selectedPawn = colonists[0];
            }

            List<WorkTypeDef> available = WorkOrderService.GetAvailableWorkTypes();
            int orderCount = WorkOrderService.GetOrder(selectedPawn).Count;

            float pawnListTop = Pad + TitleHeight;
            float pawnListHeight = colonists.Count * (PawnRowHeight + PawnRowGap);
            float laneTop = pawnListTop + pawnListHeight + Pad + TableRowHeight;
            float tableTop = laneTop + LaneHeight + Pad + TableRowHeight;
            float tableHeight = TableRowHeight + orderCount * TableRowHeight + Pad;
            float contentHeight = Mathf.Max(inRect.height, tableTop + tableHeight);
            float contentWidth = inRect.width - 24f;

            Rect contentRect = new Rect(0f, 0f, contentWidth, contentHeight);
            Widgets.BeginScrollView(inRect, ref outerScroll, contentRect);
            try
            {
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(Pad, Pad, contentWidth - Pad * 2f, TitleHeight),
                    "Work Control — " + selectedPawn.LabelShort);
                Text.Font = GameFont.Small;

                DrawPawnList(colonists, pawnListTop, contentWidth - Pad * 2f);

                Rect laneLabelRect = new Rect(Pad, laneTop - TableRowHeight, contentWidth - Pad * 2f, 20f);
                Widgets.Label(laneLabelRect, "Order (left = highest). Drag to reorder, × to remove.");
                DrawLane(new Rect(Pad, laneTop, contentWidth - Pad * 2f, LaneHeight), available);

                Rect tableLabelRect = new Rect(Pad, tableTop - TableRowHeight, contentWidth - Pad * 2f, 20f);
                Widgets.Label(tableLabelRect, "Result (read-only)");
                DrawResultTable(new Rect(Pad, tableTop, contentWidth - Pad * 2f, tableHeight));
            }
            finally
            {
                Widgets.EndScrollView();
                Text.Font = GameFont.Small;
            }
        }

        private void DrawCentered(Rect inRect, string message)
        {
            TextAnchor previous = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(inRect, message);
            Text.Anchor = previous;
        }

        private void DrawPawnList(List<Pawn> colonists, float top, float width)
        {
            float y = top;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                Rect rowRect = new Rect(Pad, y, Mathf.Min(420f, width), PawnRowHeight);

                if (pawn == selectedPawn)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }

                string label = pawn.Name != null ? pawn.NameFullColored : pawn.LabelShort;
                if (Widgets.ButtonText(rowRect, label) && pawn != selectedPawn)
                {
                    // Keep laneScroll where it is when switching pawns.
                    selectedPawn = pawn;
                }
                y += PawnRowHeight + PawnRowGap;
            }
        }

        private void DrawLane(Rect laneRect, List<WorkTypeDef> available)
        {
            List<WorkTypeDef> laneTypes = new List<WorkTypeDef>();
            List<string> laneIds = new List<string>();
            foreach (string id in WorkOrderService.GetOrder(selectedPawn))
            {
                WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(id);
                if (workType != null)
                {
                    laneTypes.Add(workType);
                    laneIds.Add(id);
                }
            }

            float contentWidth = Mathf.Max(laneRect.width, (laneTypes.Count + 1) * (CardWidth + CardGap) + CardGap);
            Rect viewRect = new Rect(0f, 0f, contentWidth, CardHeight + CardGap * 2f);

            Widgets.BeginScrollView(laneRect, ref laneScroll, viewRect);
            try
            {
                HandleDrag(laneIds, laneTypes.Count);

                int removeIndex = -1;
                for (int i = 0; i < laneTypes.Count; i++)
                {
                    Rect cardRect = new Rect(CardGap + i * (CardWidth + CardGap), CardGap, CardWidth, CardHeight);
                    DrawCard(cardRect, laneTypes[i], i, ref removeIndex);
                }

                Rect addRect = new Rect(CardGap + laneTypes.Count * (CardWidth + CardGap), CardGap, CardWidth, CardHeight);
                if (Widgets.ButtonText(addRect, "+"))
                {
                    OpenAddMenu(available, laneIds);
                }

                if (removeIndex >= 0 && removeIndex < laneIds.Count)
                {
                    List<string> reduced = new List<string>(laneIds);
                    reduced.RemoveAt(removeIndex);
                    WorkOrderService.SaveAndApply(selectedPawn, reduced);
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        private void HandleDrag(List<string> laneIds, int count)
        {
            if (!dragging)
            {
                return;
            }

            dragTargetIndex = IndexAtX(Event.current.mousePosition.x, count);

            if (Event.current.type == EventType.MouseUp)
            {
                int from = dragSourceIndex;
                int to = dragTargetIndex;
                dragging = false;
                dragSourceIndex = -1;
                dragTargetIndex = -1;

                if (from >= 0 && to >= 0 && from != to && from < laneIds.Count && to < laneIds.Count)
                {
                    List<string> reordered = new List<string>(laneIds);
                    string moved = reordered[from];
                    reordered.RemoveAt(from);
                    reordered.Insert(Mathf.Clamp(to, 0, reordered.Count), moved);
                    WorkOrderService.SaveAndApply(selectedPawn, reordered);
                }
                Event.current.Use();
            }
        }

        private void DrawCard(Rect cardRect, WorkTypeDef workType, int index, ref int removeIndex)
        {
            bool incapable = WorkOrderService.IsIncapable(selectedPawn, workType);

            Widgets.DrawBoxSolid(cardRect, new Color(0.18f, 0.2f, 0.23f, 0.95f));

            if (dragging && index == dragTargetIndex)
            {
                Widgets.DrawHighlight(cardRect);
                Widgets.DrawBox(cardRect, 2);
            }

            Rect removeRect = new Rect(cardRect.xMax - 24f, cardRect.y + 4f, 20f, 20f);
            Rect bodyRect = new Rect(cardRect.x, cardRect.y + 26f, cardRect.width, cardRect.height - 30f);

            Color previous = GUI.color;
            if (incapable)
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            }

            Rect labelRect = new Rect(cardRect.x + 6f, cardRect.y + 6f, cardRect.width - 32f, cardRect.height - 12f);
            bool previousWrap = Text.WordWrap;
            Text.WordWrap = true;
            Widgets.Label(labelRect, workType.labelShort);
            Text.WordWrap = previousWrap;
            GUI.color = previous;

            if (Widgets.ButtonText(removeRect, "×"))
            {
                removeIndex = index;
            }

            // Card body is a proper IMGUI control; drag start is read from the raw event
            // so the remove button never begins a drag.
            bool overRemove = removeRect.Contains(Event.current.mousePosition);
            if (!incapable
                && !overRemove
                && Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && bodyRect.Contains(Event.current.mousePosition))
            {
                dragging = true;
                dragSourceIndex = index;
                dragTargetIndex = index;
                Event.current.Use();
            }

            if (Widgets.ButtonInvisible(bodyRect))
            {
                // Click only: reordering is handled by the drag events above.
            }
        }

        private void OpenAddMenu(List<WorkTypeDef> available, List<string> currentOrder)
        {
            HashSet<string> inOrder = new HashSet<string>(currentOrder);
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            foreach (WorkTypeDef workType in available)
            {
                if (inOrder.Contains(workType.defName))
                {
                    continue;
                }
                if (WorkOrderService.IsIncapable(selectedPawn, workType))
                {
                    continue;
                }

                WorkTypeDef local = workType;
                options.Add(new FloatMenuOption(local.labelShort, delegate
                {
                    if (selectedPawn == null)
                    {
                        return;
                    }
                    List<string> updated = WorkOrderService.GetOrder(selectedPawn);
                    if (!updated.Contains(local.defName))
                    {
                        updated.Add(local.defName);
                        WorkOrderService.SaveAndApply(selectedPawn, updated);
                    }
                }));
            }

            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption("Nothing available to add", null));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void DrawResultTable(Rect rect)
        {
            float rankWidth = 70f;
            float statusWidth = 140f;
            float labelWidth = Mathf.Max(100f, rect.width - rankWidth - statusWidth);

            Widgets.Label(new Rect(rect.x, rect.y, rankWidth, TableRowHeight), "Rank");
            Widgets.Label(new Rect(rect.x + rankWidth, rect.y, labelWidth, TableRowHeight), "Work type");
            Widgets.Label(new Rect(rect.x + rankWidth + labelWidth, rect.y, statusWidth, TableRowHeight), "Status");

            float y = rect.y + TableRowHeight;
            int rank = 0;
            foreach (string id in WorkOrderService.GetOrder(selectedPawn))
            {
                WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(id);
                if (workType == null)
                {
                    continue;
                }

                bool incapable = WorkOrderService.IsIncapable(selectedPawn, workType);
                string rankText = "—";
                string status = "Incapable";
                if (!incapable)
                {
                    rank++;
                    rankText = rank.ToString();
                    status = "Active";
                }

                Widgets.Label(new Rect(rect.x, y, rankWidth, TableRowHeight), rankText);
                Widgets.Label(new Rect(rect.x + rankWidth, y, labelWidth, TableRowHeight), workType.labelShort);
                Widgets.Label(new Rect(rect.x + rankWidth + labelWidth, y, statusWidth, TableRowHeight), status);
                y += TableRowHeight;
            }

            Widgets.Label(new Rect(rect.x, y, rect.width, TableRowHeight),
                "Removed types are off (priority 0).");
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
    }
}
