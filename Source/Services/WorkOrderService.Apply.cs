#nullable disable
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
    public static partial class WorkOrderService
    {        public static bool TrySetPriorityDirect(Pawn_WorkSettings settings, WorkTypeDef workType, int priority)
        {
            if (settings == null || workType == null)
            {
                return false;
            }
            Pawn pawn = GetOwningPawn(settings);
            if (pawn != null && IsIncapable(pawn, workType))
            {
                return false;
            }
            DefMap<WorkTypeDef, int> map = GetPrioritiesMap(settings);
            if (map == null)
            {
                return false;
            }
            map[workType] = priority;
            return true;
        }

        private static List<string> Normalize(Pawn pawn, List<string> order)
        {
            HashSet<string> valid = new HashSet<string>(
                GetAvailableWorkTypes().Select(w => w.defName),
                StringComparer.Ordinal);

            List<string> filtered = (order ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id) && valid.Contains(id))
                .Where(id =>
                {
                    WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(id);
                    return workType != null && !IsIncapable(pawn, workType);
                })
                .ToList();

            return DenseRank.NormalizeOrder(filtered, filtered);
        }

        private static List<string> ReadVanillaOrder(Pawn pawn)
        {
            Pawn_WorkSettings settings = pawn?.workSettings;
            if (settings == null)
            {
                return new List<string>();
            }

            return GetAvailableWorkTypes()
                .Where(w => !IsIncapable(pawn, w))
                .Select(w => new { workType = w, priority = settings.GetPriority(w) })
                .Where(entry => entry.priority > 0)
                .OrderBy(entry => entry.priority)
                .ThenByDescending(entry => entry.workType.naturalPriority)
                .Select(entry => entry.workType.defName)
                .ToList();
        }

        private static bool HasNonDefaultPriorities(Pawn pawn)
        {
            Pawn_WorkSettings settings = pawn?.workSettings;
            if (settings == null)
            {
                return false;
            }

            foreach (WorkTypeDef workType in GetAvailableWorkTypes())
            {
                int priority = settings.GetPriority(workType);
                if (priority != 0 && priority != 3)
                {
                    return true;
                }
            }
            return false;
        }

        private static DefMap<WorkTypeDef, int> GetPrioritiesMap(Pawn_WorkSettings settings)
        {
            if (!prioritiesFieldResolved)
            {
                cachedPrioritiesField = typeof(Pawn_WorkSettings).GetField("priorities", PrivateInstance);
                prioritiesFieldResolved = true;
            }

            return cachedPrioritiesField != null
                ? cachedPrioritiesField.GetValue(settings) as DefMap<WorkTypeDef, int>
                : null;
        }

        public static void Apply(Pawn pawn, List<string> order, bool log = true)
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
                    if (log)
                    {
                        Log.Warning("[Ordered Work] " + pawn.LabelShort + " has no work settings; skipping apply.");
                    }
                    return;
                }

                bool previousApplying = IsApplying;
                IsApplying = true;
                try
                {
                    List<string> normalized = Normalize(pawn, order);

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

                    if (log)
                    {
                        Log.Message("[Ordered Work] Applied order for " + pawn.LabelShort + ": " +
                            string.Join(", ", capableOrder.Select((id, index) => id + "=" + (index + 1))));
                    }
                }
                finally
                {
                    IsApplying = previousApplying;
                }
            }
            catch (Exception ex)
            {
                Log.Error("[Ordered Work] Failed to apply the order for " +
                    (pawn?.LabelShort ?? "null") + ": " + ex);
            }
        }
    }
}
