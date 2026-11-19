using System;
using HarmonyLib;
using OrderedWork.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.Patches
{
    /// <summary>
    /// Vanilla Work-tab manual priority override. The vanilla cell draw/click path assumes
    /// priorities 0..4; we replace the capable-cell draw with a dynamic 0..max cycle and
    /// keep our existing capture path in sync.
    /// </summary>
    internal static class WorkTabPriorityOverride
    {
        private static bool loggedFailure;

        internal static bool DrawWorkBox(float x, float y, Pawn pawn, WorkTypeDef workType, bool incapableBecauseOfCapacities)
        {
            try
            {
                if (pawn == null || workType == null || pawn.workSettings == null || !pawn.workSettings.EverWork)
                {
                    return true; // let vanilla handle
                }
                if (!Find.PlaySettings.useWorkPriorities)
                {
                    return true; // simple mode: vanilla checkboxes, no numeric cells
                }
                if (!pawn.IsFreeColonist)
                {
                    return true; // managed pawns only, as elsewhere in the mod
                }
                if (incapableBecauseOfCapacities || WorkOrderService.IsIncapable(pawn, workType))
                {
                    return true; // vanilla draws the disabled cell
                }

                Rect rect = new Rect(x, y, WidgetsWork.WorkBoxSize, WidgetsWork.WorkBoxSize);
                int max = WorkOrderService.GetManualMaxPriority(pawn);
                int priority = pawn.workSettings.GetPriority(workType);

                if (Event.current.type == EventType.MouseDown && Mouse.IsOver(rect))
                {
                    if (Event.current.button == 0)
                    {
                        priority = priority >= max ? 0 : priority + 1;
                        pawn.workSettings.SetPriority(workType, priority);
                        Event.current.Use();
                    }
                    else if (Event.current.button == 1)
                    {
                        priority = priority <= 0 ? max : priority - 1;
                        pawn.workSettings.SetPriority(workType, priority);
                        Event.current.Use();
                    }
                    priority = pawn.workSettings.GetPriority(workType);
                }

                bool aboveVanilla = priority > 4;

                Color boxColor;
                if (aboveVanilla)
                {
                    boxColor = new Color(0.10f, 0.26f, 0.36f, 0.95f);
                }
                else if (priority <= 0)
                {
                    boxColor = new Color(0.14f, 0.14f, 0.14f, 0.92f);
                }
                else
                {
                    Color vanilla = WidgetsWork.ColorOfPriority(priority);
                    boxColor = new Color(vanilla.r, vanilla.g, vanilla.b, 0.35f);
                }
                Widgets.DrawBoxSolid(rect, boxColor);

                Color previousColor = GUI.color;
                if (aboveVanilla)
                {
                    GUI.color = new Color(0.45f, 0.9f, 1f);
                    Widgets.DrawBox(rect, 1);
                }

                if (priority > 0)
                {
                    TextAnchor previousAnchor = Text.Anchor;
                    GameFont previousFont = Text.Font;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Text.Font = GameFont.Tiny;
                    GUI.color = aboveVanilla
                        ? new Color(0.55f, 0.95f, 1f)
                        : WidgetsWork.ColorOfPriority(priority);
                    Widgets.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f), priority.ToString());
                    Text.Font = previousFont;
                    Text.Anchor = previousAnchor;
                }
                GUI.color = previousColor;

                if (aboveVanilla)
                {
                    TooltipHandler.TipRegion(
                        rect,
                        new TipSignal(
                            string.Format(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Config.WorkTab.Priority", "Priority {0} of {1} (above vanilla 4)"), priority, max),
                            pawn.thingIDNumber ^ workType.shortHash,
                            TooltipPriority.Pawn));
                }

                return false; // we drew the cell
            }
            catch (Exception ex)
            {
                if (!loggedFailure)
                {
                    loggedFailure = true;
                    Log.Error("[Ordered Work] Work-tab priority override failed; falling back to vanilla: " + ex);
                }
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.SetPriority))]
    internal static class Patch_WorkPriority_SetPriority
    {
        // SetPriority logs for values above the vanilla cap, so for >4 we write the private
        // map directly and skip the original. The postfix below still notifies and captures.
        private static bool Prefix(Pawn_WorkSettings __instance, WorkTypeDef w, ref int priority)
        {
            int max = WorkOrderService.GetManualMaxPriority();
            if (priority < 0)
            {
                priority = 0;
            }
            else if (priority > max)
            {
                priority = max;
            }

            if (priority <= 4)
            {
                return true; // vanilla handles 0..4
            }

            if (WorkOrderService.TrySetPriorityDirect(__instance, w, priority))
            {
                // Harmless if the postfix also runs; capture is idempotent.
                __instance.Notify_UseWorkPrioritiesChanged();
                WorkOrderService.OnVanillaPriorityChanged(__instance);
                return false;
            }

            return true; // private map unavailable: let vanilla handle (may log)
        }

        private static void Postfix(Pawn_WorkSettings __instance)
        {
            if (__instance == null)
            {
                return;
            }

            // Our own Apply writes the map directly and notifies itself.
            if (!WorkOrderService.IsApplying)
            {
                __instance.Notify_UseWorkPrioritiesChanged();
            }
            WorkOrderService.OnVanillaPriorityChanged(__instance);
        }
    }

    [HarmonyPatch(typeof(WidgetsWork), nameof(WidgetsWork.DrawWorkBoxFor))]
    internal static class Patch_WorkPriority_DrawWorkBoxFor
    {
        private static bool Prefix(float x, float y, Pawn p, WorkTypeDef wType, bool incapableBecauseOfCapacities)
        {
            return WorkTabPriorityOverride.DrawWorkBox(x, y, p, wType, incapableBecauseOfCapacities);
        }
    }
}
