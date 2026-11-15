using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OrderedWork.Core;
using OrderedWork.Data;
using RimWorld;
using Verse;

namespace OrderedWork.Services
{
    /// <summary>
    /// Reads and writes per-pawn work orders. Applying writes the dense rank straight
    /// into the private <see cref="Pawn_WorkSettings.priorities"/> map so ranks above 4
    /// survive (falling back to SetPriority when the field is unavailable).
    /// </summary>
    public static class WorkOrderService
    {
        private const BindingFlags PrioritiesFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        private static List<WorkTypeDef> cachedAvailable;
        private static FieldInfo cachedPrioritiesField;
        private static bool prioritiesFieldResolved;

        /// <summary>Visible WorkTypeDefs, cached; ordered by naturalPriority descending.</summary>
        public static List<WorkTypeDef> GetAvailableWorkTypes()
        {
            if (cachedAvailable == null)
            {
                cachedAvailable = DefDatabase<WorkTypeDef>.AllDefsListForReading
                    .Where(w => w != null && w.visible)
                    .OrderByDescending(w => w.naturalPriority)
                    .ToList();
            }
            return cachedAvailable;
        }

        /// <summary>Default order: every visible WorkType by naturalPriority descending.</summary>
        public static List<string> GetDefaultOrder(Pawn pawn)
        {
            return GetAvailableWorkTypes().Select(w => w.defName).ToList();
        }

        /// <summary>
        /// Stored order for the pawn, or the default order when the pawn was never edited.
        /// Reading never touches pawn priorities.
        /// </summary>
        public static List<string> GetOrder(Pawn pawn)
        {
            if (pawn == null)
            {
                return new List<string>();
            }

            OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
            PawnWorkOrder entry = component?.GetEntry(pawn);
            if (entry != null)
            {
                return entry.order != null ? new List<string>(entry.order) : new List<string>();
            }
            return GetDefaultOrder(pawn);
        }

        /// <summary>Normalizes, persists, and applies the order to the pawn.</summary>
        public static void SaveAndApply(Pawn pawn, List<string> order)
        {
            if (pawn == null)
            {
                return;
            }

            OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
            if (component == null)
            {
                Log.Error("[Ordered Work] No game component; cannot save the order for " + pawn.LabelShort + ".");
                return;
            }

            List<string> normalized = Normalize(order);
            component.SetOrder(pawn, normalized);
            Apply(pawn, normalized);
        }

        /// <summary>
        /// Writes the dense rank into the pawn's priority map. Missing and incapable
        /// WorkTypes are written as 0 so the vanilla tab reflects "off". Never throws.
        /// </summary>
        public static void Apply(Pawn pawn, List<string> order)
        {
            if (pawn == null)
            {
                return;
            }

            try
            {
                Pawn_WorkSettings settings = pawn.workSettings;
                if (settings == null)
                {
                    Log.Warning("[Ordered Work] " + pawn.LabelShort + " has no work settings; skipping apply.");
                    return;
                }

                List<string> normalized = Normalize(order);

                // Dense rank is built only over capable types, so the written values
                // stay unique and gap-free (1..M). Incapable types are forced to 0.
                List<string> capableOrder = new List<string>();
                foreach (string defName in normalized)
                {
                    WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(defName);
                    if (workType != null && !IsIncapable(pawn, workType))
                    {
                        capableOrder.Add(defName);
                    }
                }

                Dictionary<string, int> ranks = DenseRank.BuildRanks(capableOrder, capableOrder);
                DefMap<WorkTypeDef, int> priorities = GetPrioritiesMap(settings);

                foreach (WorkTypeDef workType in GetAvailableWorkTypes())
                {
                    int value;
                    if (!ranks.TryGetValue(workType.defName, out value))
                    {
                        value = 0;
                    }

                    if (priorities != null)
                    {
                        priorities[workType] = value;
                    }
                    else
                    {
                        settings.SetPriority(workType, value);
                    }
                }

                settings.Notify_UseWorkPrioritiesChanged();

                Log.Message("[Ordered Work] Applied order for " + pawn.LabelShort + ": " +
                    string.Join(", ", capableOrder.Select((id, index) => id + "=" + (index + 1))));
            }
            catch (Exception ex)
            {
                Log.Error("[Ordered Work] Failed to apply the order for " +
                    (pawn?.LabelShort ?? "null") + ": " + ex);
            }
        }

        /// <summary>
        /// True when the pawn cannot perform the WorkType at all (trait/backstory/health).
        /// Uses WorkTypeIsDisabled when callable, otherwise the disabled type list.
        /// </summary>
        public static bool IsIncapable(Pawn pawn, WorkTypeDef workType)
        {
            if (pawn == null || workType == null)
            {
                return false;
            }

            try
            {
                return pawn.WorkTypeIsDisabled(workType);
            }
            catch (Exception)
            {
                try
                {
                    List<WorkTypeDef> disabled = pawn.GetDisabledWorkTypes();
                    return disabled != null && disabled.Contains(workType);
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Keeps the requested subset in order, drops ids that are not visible WorkTypes
        /// and removes duplicates. Missing types are intentionally NOT re-appended: an
        /// absent type means "off" (priority 0).
        /// </summary>
        private static List<string> Normalize(List<string> order)
        {
            HashSet<string> valid = new HashSet<string>(
                GetAvailableWorkTypes().Select(w => w.defName),
                StringComparer.Ordinal);

            List<string> filtered = (order ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id) && valid.Contains(id))
                .ToList();

            return DenseRank.NormalizeOrder(filtered, filtered);
        }

        private static DefMap<WorkTypeDef, int> GetPrioritiesMap(Pawn_WorkSettings settings)
        {
            if (!prioritiesFieldResolved)
            {
                cachedPrioritiesField = typeof(Pawn_WorkSettings).GetField("priorities", PrioritiesFlags);
                prioritiesFieldResolved = true;
            }

            return cachedPrioritiesField != null
                ? cachedPrioritiesField.GetValue(settings) as DefMap<WorkTypeDef, int>
                : null;
        }
    }
}
