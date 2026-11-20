#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderedWork.Config
{
    /// <summary>
    /// Computes modification state against the captured reference. A persisted "modified"
    /// boolean is avoided: it is denormalized and goes stale after merges, mod changes or
    /// profile copies. The reference is the source of truth; the comparison is tiny
    /// (~100 givers) and is cached by the caller if needed.
    /// </summary>
    public static class WorkConfigDiff
    {
        public static bool IsModified(WorkTypeConfig current, WorkTypeConfig reference)
        {
            if (current == null)
            {
                return false;
            }
            if (reference == null)
            {
                return true;
            }

            if (current.Vanilla != reference.Vanilla
                || current.Visible != reference.Visible
                || current.Order != reference.Order
                || !string.Equals(current.Base, reference.Base, StringComparison.Ordinal)
                || !string.Equals(current.Label, reference.Label, StringComparison.Ordinal)
                || !string.Equals(current.Description, reference.Description, StringComparison.Ordinal))
            {
                return true;
            }

            List<GiverConfig> currentGivers = current.Givers ?? new List<GiverConfig>();
            List<GiverConfig> referenceGivers = reference.Givers ?? new List<GiverConfig>();
            if (currentGivers.Count != referenceGivers.Count)
            {
                return true;
            }
            for (int i = 0; i < currentGivers.Count; i++)
            {
                GiverConfig a = currentGivers[i];
                GiverConfig b = referenceGivers[i];
                if (a == null || b == null)
                {
                    if (a != b)
                    {
                        return true;
                    }
                    continue;
                }
                // defName is identity; repeated giver classes and priorities are intentional.
                if (!string.Equals(a.DefName, b.DefName, StringComparison.Ordinal)
                    || a.Order != b.Order)
                {
                    return true;
                }
            }
            return false;
        }

        public static List<string> GetModifiedDefNames(WorkConfigProfile current, WorkConfigProfile reference)
        {
            List<string> result = new List<string>();
            if (current == null || current.WorkTypes == null)
            {
                return result;
            }
            foreach (KeyValuePair<string, WorkTypeConfig> entry in current.WorkTypes)
            {
                WorkTypeConfig referenceType = null;
                if (reference != null && reference.WorkTypes != null)
                {
                    reference.WorkTypes.TryGetValue(entry.Key, out referenceType);
                }
                if (IsModified(entry.Value, referenceType))
                {
                    result.Add(entry.Key);
                }
            }
            return result;
        }
    }
}
