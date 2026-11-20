using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.Data;
using RimWorld;
using Verse;

namespace OrderedWork.Services
{
    /// <summary>
    /// Per-pawn WorkGiver order overrides. The global order is the active JSON profile;
    /// vanilla order is only a fallback when the profile has no entry.
    /// </summary>
    public static class WorkGiverOrderService
    {
        public static List<string> GetGlobalOrder(WorkTypeDef workType)
        {
            List<string> result = new List<string>();
            if (workType == null)
            {
                return result;
            }

            WorkConfigProfile profile = null;
            try
            {
                WorkConfigLoadResult loaded = WorkConfigStore.LoadCachedActive();
                profile = loaded != null ? loaded.Profile : null;
            }
            catch (Exception ex)
            {
                Log.Warning("[Ordered Work] Could not load global giver order: " + ex.Message);
            }

            WorkTypeConfig config;
            if (profile != null && profile.WorkTypes != null
                && profile.WorkTypes.TryGetValue(workType.defName, out config)
                && config != null && config.Givers != null)
            {
                foreach (GiverConfig giver in config.Givers.OrderBy(g => g.Order))
                {
                    if (giver != null && !string.IsNullOrEmpty(giver.DefName)
                        && !result.Contains(giver.DefName))
                    {
                        result.Add(giver.DefName);
                    }
                }
            }

            foreach (WorkGiverDef giver in DefDatabase<WorkGiverDef>.AllDefsListForReading
                .Where(g => g != null && g.workType == workType)
                .OrderByDescending(g => g.priorityInType))
            {
                if (!result.Contains(giver.defName))
                {
                    result.Add(giver.defName);
                }
            }
            return result;
        }

        public static List<string> GetGlobalOrder(Pawn pawn, WorkTypeDef workType)
        {
            List<string> order = GetGlobalOrder(workType);
            AppendRuntimeGivers(pawn, workType, order);
            return order;
        }

        public static List<string> GetStoredOrder(Pawn pawn, WorkTypeDef workType)
        {
            if (pawn == null || workType == null)
            {
                return null;
            }
            OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
            PawnWorkOrder entry = component != null ? component.GetEntry(pawn) : null;
            if (entry == null || entry.workGiverOrders == null)
            {
                return null;
            }
            PawnWorkGiverOrder value = entry.workGiverOrders.FirstOrDefault(
                o => o != null && string.Equals(o.workTypeDefName, workType.defName, StringComparison.Ordinal));
            return value != null && value.giverDefNames != null
                ? new List<string>(value.giverDefNames)
                : null;
        }

        public static List<string> GetEffectiveOrder(Pawn pawn, WorkTypeDef workType)
        {
            List<string> stored = GetStoredOrder(pawn, workType);
            List<string> result = stored ?? GetGlobalOrder(workType);
            AppendRuntimeGivers(pawn, workType, result);
            return result;
        }

        public static bool HasAnyOverride(Pawn pawn)
        {
            OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
            PawnWorkOrder entry = component != null ? component.GetEntry(pawn) : null;
            return entry != null && entry.workGiverOrders != null && entry.workGiverOrders.Count > 0;
        }

        public static bool HasOverride(Pawn pawn, WorkTypeDef workType)
        {
            return GetStoredOrder(pawn, workType) != null;
        }

        public static void SaveOrder(Pawn pawn, WorkTypeDef workType, List<string> order)
        {
            if (pawn == null || workType == null)
            {
                return;
            }
            OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
            PawnWorkOrder entry = component != null ? component.GetOrCreateEntry(pawn) : null;
            if (entry == null)
            {
                Log.Warning("[Ordered Work] Cannot save WorkGiver order without a game component.");
                return;
            }

            List<string> normalized = (order ?? new List<string>())
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            entry.workGiverOrders.RemoveAll(
                o => o == null || string.Equals(o.workTypeDefName, workType.defName, StringComparison.Ordinal));
            if (normalized.Count > 0)
            {
                entry.workGiverOrders.Add(new PawnWorkGiverOrder
                {
                    workTypeDefName = workType.defName,
                    giverDefNames = normalized,
                    source = PolicySource.Manual
                });
            }
        }

        public static void ResetOrder(Pawn pawn, WorkTypeDef workType)
        {
            if (pawn == null || workType == null)
            {
                return;
            }
            OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
            PawnWorkOrder entry = component != null ? component.GetEntry(pawn) : null;
            if (entry == null || entry.workGiverOrders == null)
            {
                return;
            }
            entry.workGiverOrders.RemoveAll(
                o => o == null || string.Equals(o.workTypeDefName, workType.defName, StringComparison.Ordinal));
        }

        public static void ResetAll(Pawn pawn)
        {
            OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
            PawnWorkOrder entry = component != null ? component.GetEntry(pawn) : null;
            if (entry != null)
            {
                entry.workGiverOrders.Clear();
            }
        }

        public static bool TryReorder(Pawn pawn, List<WorkGiver> source, out List<WorkGiver> result)
        {
            result = source;
            if (pawn == null || source == null || source.Count < 2)
            {
                return false;
            }

            OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
            PawnWorkOrder entry = component != null ? component.GetEntry(pawn) : null;
            if (entry == null || entry.workGiverOrders == null || entry.workGiverOrders.Count == 0)
            {
                return false;
            }

            Dictionary<string, int> ranks = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (PawnWorkGiverOrder value in entry.workGiverOrders)
            {
                if (value == null || string.IsNullOrEmpty(value.workTypeDefName))
                {
                    continue;
                }
                if (value.giverDefNames == null)
                {
                    continue;
                }
                for (int i = 0; i < value.giverDefNames.Count; i++)
                {
                    ranks[value.workTypeDefName + "|" + value.giverDefNames[i]] = i;
                }
            }
            if (ranks.Count == 0)
            {
                return false;
            }

            List<WorkGiver> reordered = new List<WorkGiver>(source);
            bool changed = false;
            // Reorder only within each WorkType, preserving the cross-WorkType grouping.
            List<string> workTypes = new List<string>();
            foreach (WorkGiver giver in source)
            {
                if (giver != null && giver.def != null && giver.def.workType != null
                    && !workTypes.Contains(giver.def.workType.defName))
                {
                    workTypes.Add(giver.def.workType.defName);
                }
            }
            foreach (string workTypeName in workTypes)
            {
                List<int> indexes = new List<int>();
                List<WorkGiver> group = new List<WorkGiver>();
                for (int i = 0; i < source.Count; i++)
                {
                    WorkGiver giver = source[i];
                    if (giver != null && giver.def != null && giver.def.workType != null
                        && string.Equals(giver.def.workType.defName, workTypeName, StringComparison.Ordinal))
                    {
                        indexes.Add(i);
                        group.Add(giver);
                    }
                }
                if (group.Count < 2)
                {
                    continue;
                }
                string prefix = workTypeName + "|";
                if (!group.Any(g => ranks.ContainsKey(prefix + g.def.defName)))
                {
                    continue;
                }
                group = group
                    .Select((giver, index) => new { giver, index })
                    .OrderBy(item =>
                    {
                        int rank;
                        return ranks.TryGetValue(prefix + item.giver.def.defName, out rank)
                            ? rank
                            : int.MaxValue;
                    })
                    .ThenBy(item => item.index)
                    .Select(item => item.giver)
                    .ToList();
                for (int i = 0; i < indexes.Count; i++)
                {
                    if (!ReferenceEquals(reordered[indexes[i]], group[i]))
                    {
                        changed = true;
                    }
                    reordered[indexes[i]] = group[i];
                }
            }
            if (!changed)
            {
                return false;
            }
            result = reordered;
            return true;
        }

        private static void AppendRuntimeGivers(Pawn pawn, WorkTypeDef workType, List<string> order)
        {
            if (pawn == null || workType == null || pawn.workSettings == null)
            {
                return;
            }
            List<WorkGiver> runtime;
            try
            {
                runtime = pawn.workSettings.WorkGiversInOrderNormal;
            }
            catch
            {
                runtime = null;
            }
            if (runtime == null)
            {
                return;
            }
            foreach (WorkGiver giver in runtime)
            {
                if (giver != null && giver.def != null && giver.def.workType == workType
                    && !order.Contains(giver.def.defName))
                {
                    order.Add(giver.def.defName);
                }
            }
        }
    }
}
