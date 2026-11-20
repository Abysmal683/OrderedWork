using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using OrderedWork.Config;
using Verse;

namespace OrderedWork.Patches
{
    /// <summary>
    /// The vanilla Work tab asks WorkTypeDefsUtility for its priority order. Natural
    /// priority is applied after defs load, so use the profile order directly here.
    /// </summary>
    [HarmonyPatch(typeof(WorkTypeDefsUtility), nameof(WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder), MethodType.Getter)]
    internal static class Patch_WorkTypeDefsInPriorityOrder
    {
        private static void Postfix(ref IEnumerable<WorkTypeDef> __result)
        {
            try
            {
                WorkConfigProfile profile = WorkConfigStore.LoadCachedActive()?.Profile;
                if (profile == null || profile.WorkTypes == null || __result == null)
                {
                    return;
                }

                Dictionary<string, int> order = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (KeyValuePair<string, WorkTypeConfig> pair in profile.WorkTypes
                    .Where(pair => pair.Value != null)
                    .OrderBy(pair => pair.Value.Order))
                {
                    if (!order.ContainsKey(pair.Key))
                    {
                        order[pair.Key] = order.Count;
                    }
                }

                __result = __result
                    .OrderBy(workType =>
                    {
                        int index;
                        return order.TryGetValue(workType.defName, out index) ? index : int.MaxValue;
                    })
                    .ThenByDescending(workType => workType.naturalPriority)
                    .ToList();
            }
            catch (Exception exception)
            {
                Log.WarningOnce("[Ordered Work] Work-tab order fallback: " + exception.Message, 817231);
            }
        }
    }
}