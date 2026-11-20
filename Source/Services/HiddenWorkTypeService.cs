using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using OrderedWork.Config;
using RimWorld;
using Verse;

namespace OrderedWork.Services
{
    /// <summary>
    /// Hidden WorkTypes are a fixed priority layer above the normal work order.
    /// They are stored in the profile, but never shown in Work tab or Work Control.
    /// </summary>
    internal static class HiddenWorkTypeService
    {
        private static Dictionary<string, int>? ranks;
        private static HashSet<string>? names;

        public static void Invalidate()
        {
            ranks = null;
            names = null;
        }

        public static bool IsHidden(WorkTypeDef? workType)
        {
            Ensure();
            return workType != null && names != null && names.Contains(workType.defName);
        }

        public static bool TryGetRank(WorkTypeDef? workType, out int rank)
        {
            Ensure();
            if (workType != null && ranks != null && ranks.TryGetValue(workType.defName, out rank))
            {
                return true;
            }
            rank = 0;
            return false;
        }

        public static void RefreshPawn(Pawn pawn)
        {
            if (pawn == null || pawn.workSettings == null)
            {
                return;
            }
            try
            {
                Traverse.Create(pawn.workSettings).Method("CacheWorkGiversInOrder").GetValue();
            }
            catch (Exception exception)
            {
                Log.WarningOnce("[Ordered Work] Hidden WorkType cache refresh failed: " + exception.Message, 817232);
            }
        }

        private static void Ensure()
        {
            if (ranks != null)
            {
                return;
            }
            WorkConfigProfile? profile = WorkConfigStore.LoadCachedActive()?.Profile;
            ranks = new Dictionary<string, int>(StringComparer.Ordinal);
            names = new HashSet<string>(StringComparer.Ordinal);
            if (profile == null)
            {
                return;
            }

            int rank = 1;
            foreach (KeyValuePair<string, WorkTypeConfig> pair in profile.WorkTypes
                .Where(pair => pair.Value != null && pair.Value.Hidden)
                .OrderBy(pair => pair.Value.Order)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal))
            {
                if (DefDatabase<WorkTypeDef>.GetNamedSilentFail(pair.Key) == null)
                {
                    continue;
                }
                ranks[pair.Key] = rank++;
                names.Add(pair.Key);
            }
        }
    }
}