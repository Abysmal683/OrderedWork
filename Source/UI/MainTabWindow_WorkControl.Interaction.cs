using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OrderedWork.Config;
using OrderedWork.Services;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    public partial class MainTabWindow_WorkControl : MainTabWindow
    {
        private void BeginPaletteDrag(WorkTypeDef type)
        {
            draggingPalette = true;
            paletteWorkType = type;
            paletteHoverPawn = null;
            paletteDropIndex = 0;
        }

        private void CancelPaletteDrag()
        {
            draggingPalette = false;
            paletteWorkType = null;
            paletteHoverPawn = null;
        }

        private void UpdatePaletteDrag(Rect bodyRect, float leftWidth, List<Pawn> colonists)
        {
            if (!draggingPalette || paletteWorkType == null)
            {
                return;
            }

            Vector2 mouse = Event.current.mousePosition;
            paletteHoverPawn = null;
            paletteDropIndex = 0;

            if (bodyRect.Contains(mouse))
            {
                float contentY = mouse.y - bodyRect.y + gridScroll.y;
                int row = Mathf.FloorToInt(contentY / RowHeight);
                if (row >= 0 && row < colonists.Count)
                {
                    paletteHoverPawn = colonists[row];
                    int laneCount = BuildLane(paletteHoverPawn).types.Count;
                    float contentX = mouse.x - (bodyRect.x + leftWidth) + gridScroll.x;
                    paletteDropIndex = SlotAtX(contentX, laneCount);
                }
            }

            if (Event.current.type == EventType.MouseUp)
            {
                if (paletteHoverPawn != null)
                {
                    List<Pawn> selected = GetSelectedColonists();
                    List<Pawn> targets;
                    if (selected.Count >= 2 && selected.Contains(paletteHoverPawn))
                    {
                        // Drop inside the selected group: apply to every selected pawn.
                        targets = selected;
                    }
                    else
                    {
                        // No group, or drop on an unselected row: the hovered pawn only.
                        targets = new List<Pawn> { paletteHoverPawn };
                    }
                    WorkTypePaletteView.ApplyDrop(paletteWorkType, paletteHoverPawn, paletteDropIndex, targets);
                }
                CancelPaletteDrag();
                Event.current.Use();
            }
        }

        private void DrawTopControls(Rect recommendRect, Rect detailsRect, Rect viewRect)
        {
            List<Pawn> selected = GetSelectedColonists();
            bool hasSelection = selected.Count > 0;

            if (UiButtonBar.Button(recommendRect, "Recommend", hasSelection))
            {
                for (int i = 0; i < selected.Count; i++)
                {
                    WorkOrderService.GenerateAndApplyRecommendation(selected[i]);
                }
            }
            if (Mouse.IsOver(recommendRect))
            {
                TooltipHandler.TipRegion(recommendRect, "Generate recommendation for selected pawn(s)");
            }

            if (UiButtonBar.Button(detailsRect, "Details", hasSelection))
            {
                Find.WindowStack.Add(new Dialog_WorkControlDetails(selected[0]));
            }
            if (Mouse.IsOver(detailsRect))
            {
                TooltipHandler.TipRegion(detailsRect, "More details for the first selected pawn");
            }

            if (Widgets.ButtonText(viewRect, "View: Numeric"))
            {
                MainButtonDef workTab = DefDatabase<MainButtonDef>.GetNamedSilentFail("Work");
                if (workTab != null)
                {
                    Find.MainTabsRoot.ToggleTab(workTab, true);
                }
            }
            if (Mouse.IsOver(viewRect))
            {
                TooltipHandler.TipRegion(viewRect, "Open the vanilla Work tab (Numeric view)");
            }
        }

        private static List<Pawn> GetSelectedColonists()
        {
            if (Find.Selector == null)
            {
                return new List<Pawn>();
            }
            return Find.Selector.SelectedPawns
                .Where(p => p != null && !p.Destroyed && p.IsFreeColonist)
                .ToList();
        }

        /// <summary>
        /// Palette order follows the user's Work-type order (the same list shown in Mod
        /// Settings and used by the Work tab), not naturalPriority. Unknown/hidden ids are
        /// skipped, and any visible type missing from the list is appended at the end.
        /// </summary>
        private static List<WorkTypeDef> GetPaletteOrder(List<WorkTypeDef> available)
        {
            if (available == null || available.Count == 0)
            {
                return new List<WorkTypeDef>();
            }
            WorkConfigProfile profile = WorkConfigStore.LoadEditingProfile(
                WorkConfigStore.LoadSelectedProfileName());
            List<WorkTypeDef> ordered = WorkConfigStore.GetOrderedVisibleWorkTypes(profile);
            HashSet<string> availableNames = new HashSet<string>(
                available.Where(t => t != null).Select(t => t.defName), StringComparer.Ordinal);
            List<WorkTypeDef> result = ordered
                .Where(t => t != null && availableNames.Contains(t.defName))
                .ToList();
            foreach (WorkTypeDef type in available)
            {
                if (type != null && !result.Contains(type))
                {
                    result.Add(type);
                }
            }
            return result;
        }

        private static List<Pawn> GetColonistsInDisplayOrder()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return new List<Pawn>();
            }

            List<Pawn> bar = null;
            try
            {
                bar = Find.ColonistBar != null ? Find.ColonistBar.GetColonistsInOrder() : null;
            }
            catch (Exception)
            {
                bar = null;
            }

            List<Pawn> result = new List<Pawn>();
            if (bar != null)
            {
                foreach (Pawn pawn in bar)
                {
                    if (pawn != null && !pawn.Dead && pawn.IsFreeColonist && pawn.Spawned && pawn.Map == map)
                    {
                        result.Add(pawn);
                    }
                }
            }

            if (result.Count == 0)
            {
                result = map.mapPawns.FreeColonists.Where(p => p != null && !p.Dead).ToList();
            }
            return result;
        }

        private void HandleDrop()
        {
            if (!dragging)
            {
                return;
            }

            int slot = SlotAtX(Event.current.mousePosition.x, dragCount);
            if (Event.current.type != EventType.MouseUp)
            {
                dragTargetIndex = slot;
                return;
            }

            int from = dragSourceIndex;
            Pawn pawn = dragPawn;

            dragging = false;
            dragPawn = null;
            dragSourceIndex = -1;
            dragTargetIndex = -1;

            if (pawn != null && from >= 0 && slot >= 0 && !(slot == from || slot == from + 1))
            {
                List<string> ids = BuildLane(pawn).ids;
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

                SkillRecord bestSkill = WorkTypePaletteView.SafeBestSkill(pawn, workType, out _);
                string levelText = bestSkill != null ? bestSkill.Level.ToString() : "-";
                string label = WorkTypePaletteView.DisplayName(workType) + " [" + levelText + "]";

                if (WorkOrderService.IsIncapable(pawn, workType))
                {
                    options.Add(new FloatMenuOption(label + " - Incapable", null));
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

    }
}