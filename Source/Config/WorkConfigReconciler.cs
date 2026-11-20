#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderedWork.Config
{
    /// <summary>
    /// Pure profile transformations used by Split/Merge/Order. Kept free of UI and RimWorld
    /// types so the ownership rules can be tested without the game runtime.
    /// </summary>
    public static class WorkConfigReconciler
    {
        public static bool CreateSplit(
            WorkConfigProfile profile,
            string originName,
            string newDefName,
            IEnumerable<string> givers,
            string label,
            string actionWord,
            string description,
            string mod,
            out WorkTypeConfig split)
        {
            split = null;
            if (profile == null || string.IsNullOrEmpty(originName) || string.IsNullOrEmpty(newDefName)
                || !profile.WorkTypes.TryGetValue(originName, out WorkTypeConfig origin)
                || origin == null)
            {
                return false;
            }

            List<string> incoming = (givers ?? Enumerable.Empty<string>())
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (incoming.Count == 0)
            {
                return false;
            }

            foreach (string giverName in incoming)
            {
                foreach (WorkTypeConfig workType in profile.WorkTypes.Values)
                {
                    if (workType == null || workType.Givers == null)
                    {
                        continue;
                    }
                    workType.Givers.RemoveAll(g => g != null && g.DefName == giverName);
                    for (int i = 0; i < workType.Givers.Count; i++)
                    {
                        workType.Givers[i].Order = i;
                    }
                }
            }

            split = new WorkTypeConfig
            {
                Vanilla = false,
                Base = originName,
                Label = label ?? "",
                ActionWord = actionWord ?? "",
                Description = description ?? "",
                Visible = true,
                Order = profile.WorkTypes.Count == 0
                    ? 0
                    : profile.WorkTypes.Values.Where(w => w != null).Select(w => w.Order).DefaultIfEmpty(0).Max() + 1,
                Mod = mod,
                Givers = incoming.Select((name, index) => new GiverConfig
                {
                    DefName = name,
                    Order = index,
                    Mod = mod
                }).ToList()
            };
            profile.WorkTypes[newDefName] = split;
            if (!origin.Vanilla && (origin.Givers == null || origin.Givers.Count == 0))
            {
                profile.WorkTypes.Remove(originName);
            }
            else if (origin.Vanilla && (origin.Givers == null || origin.Givers.Count == 0))
            {
                origin.Visible = false;
            }
            return true;
        }
        public static bool RevertMerge(WorkConfigProfile profile, WorkConfigProfile reference, string defName)
        {
            if (profile == null || reference == null || string.IsNullOrEmpty(defName)
                || !profile.WorkTypes.TryGetValue(defName, out WorkTypeConfig current)
                || current == null || current.Givers == null
                || !reference.WorkTypes.TryGetValue(defName, out WorkTypeConfig baseline)
                || baseline == null || baseline.Givers == null)
            {
                return false;
            }

            HashSet<string> referenceNames = new HashSet<string>(
                baseline.Givers.Where(g => g != null).Select(g => g.DefName), StringComparer.Ordinal);
            HashSet<string> currentNames = new HashSet<string>(
                current.Givers.Where(g => g != null).Select(g => g.DefName), StringComparer.Ordinal);
            List<GiverConfig> extras = current.Givers
                .Where(g => g != null && !referenceNames.Contains(g.DefName))
                .ToList();

            foreach (GiverConfig missing in baseline.Givers.Where(g => g != null && !currentNames.Contains(g.DefName)))
            {
                KeyValuePair<string, WorkTypeConfig> ownerEntry = profile.WorkTypes
                    .FirstOrDefault(pair => pair.Key != defName && pair.Value != null && pair.Value.Givers != null
                        && pair.Value.Givers.Any(g => g != null && g.DefName == missing.DefName));
                if (string.IsNullOrEmpty(ownerEntry.Key) || ownerEntry.Value == null)
                {
                    continue;
                }
                ownerEntry.Value.Givers.RemoveAll(g => g != null && g.DefName == missing.DefName);
                WorkTypeConfig ownerReference;
                if (reference.WorkTypes.TryGetValue(ownerEntry.Key, out ownerReference))
                {
                    RestoreGiverOrder(ownerEntry.Value, ownerReference);
                }
            }

            foreach (GiverConfig extra in extras)
            {
                KeyValuePair<string, WorkTypeConfig> ownerEntry = reference.WorkTypes
                    .FirstOrDefault(pair => pair.Value != null && pair.Value.Givers != null
                        && pair.Value.Givers.Any(g => g != null && g.DefName == extra.DefName));
                if (string.IsNullOrEmpty(ownerEntry.Key) || ownerEntry.Key == defName
                    || !profile.WorkTypes.TryGetValue(ownerEntry.Key, out WorkTypeConfig owner)
                    || owner == null)
                {
                    continue;
                }
                owner.Givers = owner.Givers ?? new List<GiverConfig>();
                if (!owner.Givers.Any(g => g != null && g.DefName == extra.DefName))
                {
                    owner.Givers.Add(new GiverConfig
                    {
                        DefName = extra.DefName,
                        Order = owner.Givers.Count,
                        Mod = extra.Mod
                    });
                }
                RestoreGiverOrder(owner, ownerEntry.Value);
                owner.Visible = true;
            }

            current.Visible = true;
            current.Givers = baseline.Givers
                .Where(g => g != null)
                .Select((g, index) => new GiverConfig
                {
                    DefName = g.DefName,
                    Order = index,
                    Mod = g.Mod
                })
                .ToList();
            return true;
        }

        public static bool RevertOrder(WorkConfigProfile profile, WorkConfigProfile reference, string defName)
        {
            if (profile == null || reference == null || string.IsNullOrEmpty(defName)
                || !profile.WorkTypes.TryGetValue(defName, out WorkTypeConfig current)
                || current == null
                || !reference.WorkTypes.TryGetValue(defName, out WorkTypeConfig baseline)
                || baseline == null || baseline.Givers == null)
            {
                return false;
            }

            Dictionary<string, GiverConfig> byName = (current.Givers ?? new List<GiverConfig>())
                .Where(g => g != null)
                .GroupBy(g => g.DefName, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            current.Givers = baseline.Givers
                .Where(g => g != null)
                .Select((g, index) =>
                {
                    GiverConfig existing;
                    if (byName.TryGetValue(g.DefName, out existing))
                    {
                        existing.Order = index;
                        return existing;
                    }
                    return new GiverConfig { DefName = g.DefName, Order = index, Mod = g.Mod };
                })
                .ToList();
            current.Visible = true;
            return true;
        }

        private static void RestoreGiverOrder(WorkTypeConfig target, WorkTypeConfig reference)
        {
            if (target == null || reference == null || reference.Givers == null)
            {
                return;
            }
            Dictionary<string, int> order = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < reference.Givers.Count; i++)
            {
                GiverConfig giver = reference.Givers[i];
                if (giver != null && !order.ContainsKey(giver.DefName))
                {
                    order[giver.DefName] = i;
                }
            }
            target.Givers = (target.Givers ?? new List<GiverConfig>())
                .Where(g => g != null)
                .OrderBy(g =>
                {
                    int index;
                    return order.TryGetValue(g.DefName, out index) ? index : int.MaxValue;
                })
                .ThenBy(g => g.Order)
                .ToList();
            for (int i = 0; i < target.Givers.Count; i++)
            {
                target.Givers[i].Order = i;
            }
        }
    }
}