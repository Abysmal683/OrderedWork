using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Owns Work Control drag state and the palette/card drop actions.
    /// </summary>
    internal sealed class WorkControlInteraction
    {
        internal bool Dragging;
        internal Pawn DragPawn;
        internal int DragSourceIndex = -1;
        internal int DragTargetIndex = -1;
        internal int DragCount;

        internal bool DraggingPalette;
        internal WorkTypeDef PaletteWorkType;
        internal Pawn PaletteHoverPawn;
        internal int PaletteDropIndex;

        internal bool IsAnyDragging
        {
            get { return Dragging || DraggingPalette; }
        }

        internal void BeginPaletteDrag(WorkTypeDef type)
        {
            DraggingPalette = true;
            PaletteWorkType = type;
            PaletteHoverPawn = null;
            PaletteDropIndex = 0;
        }

        internal void CancelPaletteDrag()
        {
            DraggingPalette = false;
            PaletteWorkType = null;
            PaletteHoverPawn = null;
        }

        internal void UpdatePaletteDrag(Rect bodyRect, float leftWidth, List<Pawn> colonists, Vector2 gridScroll)
        {
            if (!DraggingPalette || PaletteWorkType == null)
            {
                return;
            }

            Vector2 mouse = Event.current.mousePosition;
            PaletteHoverPawn = null;
            PaletteDropIndex = 0;

            if (bodyRect.Contains(mouse))
            {
                float contentY = mouse.y - bodyRect.y + gridScroll.y;
                int row = Mathf.FloorToInt(contentY / WorkControlMetrics.RowHeight);
                if (row >= 0 && row < colonists.Count)
                {
                    PaletteHoverPawn = colonists[row];
                    int laneCount = WorkControlRows.BuildLane(PaletteHoverPawn).types.Count;
                    float contentX = mouse.x - (bodyRect.x + leftWidth) + gridScroll.x;
                    PaletteDropIndex = WorkControlMetrics.SlotAtX(contentX, laneCount);
                }
            }

            if (Event.current.type == EventType.MouseUp)
            {
                if (PaletteHoverPawn != null)
                {
                    List<Pawn> selected = GetSelectedColonists();
                    List<Pawn> targets;
                    if (selected.Count >= 2 && selected.Contains(PaletteHoverPawn))
                    {
                        targets = selected;
                    }
                    else
                    {
                        targets = new List<Pawn> { PaletteHoverPawn };
                    }
                    WorkTypePaletteView.ApplyDrop(PaletteWorkType, PaletteHoverPawn, PaletteDropIndex, targets);
                }
                CancelPaletteDrag();
                Event.current.Use();
            }
        }

        internal void HandleDrop(Vector2 gridScroll)
        {
            if (!Dragging)
            {
                return;
            }

            int slot = WorkControlMetrics.SlotAtX(Event.current.mousePosition.x, DragCount);
            if (Event.current.type != EventType.MouseUp)
            {
                DragTargetIndex = slot;
                return;
            }

            int from = DragSourceIndex;
            Pawn pawn = DragPawn;

            Dragging = false;
            DragPawn = null;
            DragSourceIndex = -1;
            DragTargetIndex = -1;

            if (pawn != null && from >= 0 && slot >= 0 && !(slot == from || slot == from + 1))
            {
                List<string> ids = WorkControlRows.BuildLane(pawn).ids;
                if (from < ids.Count)
                {
                    string moved = ids[from];
                    ids.RemoveAt(from);
                    int insert = slot;
                    if (insert > from)
                    {
                        insert--;
                    }
                    ids.Insert(Mathf.Clamp(insert, 0, ids.Count), moved);
                    WorkOrderService.SaveAndApply(pawn, ids);
                }
            }
            Event.current.Use();
        }

        internal void BeginCardDrag(Pawn pawn, int index, int count)
        {
            Dragging = true;
            DragPawn = pawn;
            DragSourceIndex = index;
            DragTargetIndex = index;
            DragCount = count;
        }

        internal static List<Pawn> GetSelectedColonists()
        {
            if (Find.Selector == null)
            {
                return new List<Pawn>();
            }
            return Find.Selector.SelectedPawns
                .Where(p => p != null && !p.Destroyed && p.IsFreeColonist)
                .ToList();
        }

        internal static void OpenAddMenu(Pawn pawn, List<string> currentOrder, List<WorkTypeDef> available)
        {
            HashSet<string> inOrder = new HashSet<string>(currentOrder);
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            foreach (WorkTypeDef workType in available)
            {
                if (inOrder.Contains(workType.defName))
                {
                    continue;
                }

                SkillRecord bestSkill = WorkTypePaletteView.SafeBestSkill(pawn, workType, out _);
                string levelText = bestSkill != null ? bestSkill.Level.ToString() : "-";
                string label = WorkTypePaletteView.DisplayName(workType) + " [" + levelText + "]";

                if (WorkOrderService.IsIncapable(pawn, workType))
                {
                    options.Add(new FloatMenuOption(label + OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.IncapableSuffix", " - Incapable"), null));
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
                options.Add(new FloatMenuOption(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.NothingAdd", "Nothing available to add"), null));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}
