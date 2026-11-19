using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Core;
using OrderedWork.Data;
using RimWorld;
using Verse;

namespace OrderedWork.Services
{
    /// <summary>
    /// Applies the temporary per-hour focus overlay at hour boundaries only. The
    /// pawn's stored order is never changed; applying a focus is just a priority write
    /// that the next boundary replaces with the base order or the next focus.
    /// </summary>
    public static class HourFocusService
    {
        private sealed class FocusState
        {
            public int Hour;
            public string Signature;
            public bool HasFocus;
        }

        private static readonly Dictionary<Pawn, FocusState> states = new Dictionary<Pawn, FocusState>();

        public static void Reset()
        {
            states.Clear();
        }

        public static void OnHourBoundary()
        {
            if (Find.Maps == null)
            {
                return;
            }

            OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
            if (component == null)
            {
                return;
            }

            foreach (Map map in Find.Maps)
            {
                if (map == null || map.mapPawns == null)
                {
                    continue;
                }
                foreach (Pawn pawn in map.mapPawns.FreeColonists)
                {
                    if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.workSettings == null)
                    {
                        continue;
                    }
                    ApplyPawn(component, pawn, map);
                }
            }
        }

        private static void ApplyPawn(OrderedWorkGameComponent component, Pawn pawn, Map map)
        {
            int hour = GenLocalDate.HourOfDay(map);
            FocusState state;
            if (states.TryGetValue(pawn, out state) && state.Hour == hour)
            {
                return;
            }

            PawnWorkOrder entry = component.GetEntry(pawn);
            List<string> baseOrder = WorkOrderService.GetOrder(pawn) ?? new List<string>();
            List<string> focus = FocusForHour(entry, hour);
            List<string> effective = HourFocusStack.BuildEffectiveOrder(
                baseOrder,
                focus,
                delegate(string id)
                {
                    WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(id);
                    return workType != null && !WorkOrderService.IsIncapable(pawn, workType);
                });

            bool hasFocus = focus.Count > 0 && effective.Count > 0
                && effective.Take(focus.Count).Any(id => focus.Contains(id));
            string signature = string.Join("|", effective);

            bool wasFocus = state != null && state.HasFocus;
            if (state != null && state.Signature == signature && state.HasFocus == hasFocus)
            {
                state.Hour = hour;
                return;
            }

            if (!hasFocus && !wasFocus)
            {
                states[pawn] = new FocusState { Hour = hour, Signature = signature, HasFocus = false };
                return;
            }

            if (effective.Count == 0)
            {
                states[pawn] = new FocusState { Hour = hour, Signature = signature, HasFocus = false };
                return;
            }

            WorkOrderService.Apply(pawn, effective, false);
            states[pawn] = new FocusState { Hour = hour, Signature = signature, HasFocus = hasFocus };
            if (OrderedWorkMod.Settings != null && OrderedWorkMod.Settings.verboseLogging)
            {
                OrderedWorkMod.DebugLog("Hour focus " + hour.ToString("00") + " for " + pawn.LabelShort
                    + ": " + (hasFocus ? string.Join(", ", focus) : "base order"));
            }
        }

        private static List<string> FocusForHour(PawnWorkOrder entry, int hour)
        {
            if (entry == null || entry.hourFocusOverrides == null)
            {
                return new List<string>();
            }
            PawnHourFocusOverride value = entry.hourFocusOverrides.FirstOrDefault(v => v != null && v.hour == hour);
            return value != null ? HourFocusStack.NormalizeFocus(value.workTypeDefNames) : new List<string>();
        }
    }
}
