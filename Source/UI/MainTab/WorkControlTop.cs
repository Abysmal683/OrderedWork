using System;
using System.Collections.Generic;
using System.Linq;
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
        private void DrawTopControls(Rect clothesRect, Rect recommendRect, Rect detailsRect, Rect viewRect)
        {
            List<Pawn> selected = WorkControlInteraction.GetSelectedColonists();
            bool hasSelection = selected.Count > 0;

            if (UiButtonBar.Button(clothesRect, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Equipment.Open", "Equipment")))
            {
                Find.WindowStack.Add(new Dialog_EquipmentEditor(LoadedModManager.GetMod<OrderedWorkMod>()));
            }
            if (Mouse.IsOver(clothesRect))
            {
                TooltipHandler.TipRegion(clothesRect, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Equipment.OpenTip", "Equipment sets and editor"));
            }

            if (UiButtonBar.Button(recommendRect, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.Recommend", "Recommend"), hasSelection))
            {
                for (int i = 0; i < selected.Count; i++)
                {
                    WorkOrderService.GenerateAndApplyRecommendation(selected[i]);
                }
            }
            if (Mouse.IsOver(recommendRect))
            {
                TooltipHandler.TipRegion(recommendRect, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.RecommendTip", "Generate recommendation for selected pawn(s)"));
            }

            if (UiButtonBar.Button(detailsRect, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.Details", "Details"), hasSelection))
            {
                Find.WindowStack.Add(new Dialog_WorkControlDetails(selected[0]));
            }
            if (Mouse.IsOver(detailsRect))
            {
                TooltipHandler.TipRegion(detailsRect, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.DetailsTip", "More details for the first selected pawn"));
            }

            if (Widgets.ButtonText(viewRect, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.ViewNumeric", "View: Numeric")))
            {
                MainButtonDef workTab = DefDatabase<MainButtonDef>.GetNamedSilentFail("Work");
                if (workTab != null)
                {
                    Find.MainTabsRoot.ToggleTab(workTab, true);
                }
            }
            if (Mouse.IsOver(viewRect))
            {
                TooltipHandler.TipRegion(viewRect, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkControl.ViewTip", "Open the vanilla Work tab (Numeric view)"));
            }
        }

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

            List<Pawn>? bar = null;
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

        private List<Pawn> GetFilteredColonists(List<Pawn> allColonists, bool filtering)
        {
            int tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            string signature = (searchText ?? "") + "\u001f" + (workFilterText ?? "") + "\u001f"
                + invertWorkFilter + "\u001f" + workFilterScope + "\u001f" + allColonists.Count;
            if (cachedFilteredColonists != null && cachedFilterSignature == signature
                && tick - cachedFilterTick < 60)
            {
                return cachedFilteredColonists;
            }

            cachedFilteredColonists = filtering
                ? allColonists.Where(p => IsSearchMatch(p) && MatchesLineWorkFilter(p)).ToList()
                : new List<Pawn>(allColonists);
            cachedFilterSignature = signature;
            cachedFilterTick = tick;
            return cachedFilteredColonists;
        }
        private static void DrawCentered(Rect inRect, string message)
        {
            TextAnchor previous = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(inRect, message);
            Text.Anchor = previous;
        }
    }
}