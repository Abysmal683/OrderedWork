using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using HarmonyLib;
using OrderedWork.Config;
using OrderedWork.Startup;
using RimWorld;
using Verse;

namespace OrderedWork
{
    [StaticConstructorOnStartup]
    public static class OrderedWorkStartup
    {
        static OrderedWorkStartup()
        {
            try
            {
                OrderedWorkMod mod = LoadedModManager.GetMod<OrderedWorkMod>();
                OrderedWorkSettings settings = mod?.GetSettings<OrderedWorkSettings>();
                if (settings == null)
                {
                    return;
                }

                string selectedProfile = WorkConfigStore.LoadSelectedProfileName();
                WorkConfigStore.PromotePending(selectedProfile);
                WorkConfigLoadResult loaded = WorkConfigStore.LoadActiveWithFallback(selectedProfile);
                if (loaded != null && loaded.Profile != null)
                {
                    ApplyProfile(loaded.Profile, settings, mod);
                    return;
                }

                RebuildCaches();
            }
            catch (Exception exception)
            {
                Log.Error("[Ordered Work] Failed to apply work changes: " + exception);
            }
        }

        /// <summary>
        /// Applies saved work changes immediately for settings feedback. Safe to call after
        /// adding a split or merge; it is the same pipeline used at startup and remains
        /// restart-backed because DefDatabase mutations cannot be cleanly rolled back.
        /// </summary>
        public static void ApplyProfileNow(WorkConfigProfile profile, OrderedWorkSettings settings, OrderedWorkMod mod)
        {
            try
            {
                ApplyProfile(profile, settings, mod);
            }
            catch (Exception exception)
            {
                Log.Error("[Ordered Work] Failed to apply profile immediately: " + exception);
            }
        }

        private static void ApplyProfile(WorkConfigProfile profile, OrderedWorkSettings settings, OrderedWorkMod mod)
        {
            if (profile == null || profile.WorkTypes == null || profile.WorkTypes.Count == 0)
            {
                return;
            }

            List<KeyValuePair<string, WorkTypeConfig>> ordered = profile.WorkTypes
                .Where(e => e.Value != null)
                .OrderBy(e => e.Value.Order)
                .ToList();

            DynamicWorkTypeFactory.EnsureSplitWorkTypes(ordered, mod);

            int workOrder = 0;
            foreach (KeyValuePair<string, WorkTypeConfig> entry in ordered)
            {
                WorkTypeDef def = DefDatabase<WorkTypeDef>.GetNamedSilentFail(entry.Key);
                if (def == null)
                {
                    continue;
                }
                WorkTypeConfig wt = entry.Value;
                string label = string.IsNullOrWhiteSpace(wt.Label) ? def.defName : wt.Label;
                def.labelShort = label;
                def.pawnLabel = label;
                def.gerundLabel = string.IsNullOrWhiteSpace(wt.ActionWord)
                    ? (string.IsNullOrWhiteSpace(def.gerundLabel) ? label : def.gerundLabel)
                    : wt.ActionWord;
                if (!string.IsNullOrWhiteSpace(wt.Description))
                {
                    def.description = wt.Description;
                }
                def.visible = wt.Visible && !wt.Hidden;
                if (wt.Hidden)
                {
                    def.alwaysStartActive = true;
                }
                def.naturalPriority = 10000 - workOrder * 10;

                int giverOrder = 0;
                if (wt.Givers != null)
                {
                    foreach (GiverConfig giverConfig in wt.Givers.Where(g => g != null).OrderBy(g => g.Order))
                    {
                        WorkGiverDef giver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(giverConfig.DefName);
                        if (giver == null)
                        {
                            continue;
                        }
                        giver.workType = def;
                        giver.priorityInType = 10000 - giverOrder * 10;
                        giverOrder++;
                    }
                }
                workOrder++;
            }

            HashSet<string> profileNames = new HashSet<string>(profile.WorkTypes.Keys, StringComparer.Ordinal);
            foreach (WorkTypeDef stale in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (stale != null
                    && stale.defName.StartsWith("OrderedWork_", StringComparison.Ordinal)
                    && !profileNames.Contains(stale.defName))
                {
                    stale.visible = false;
                }
            }
            RebuildCaches();
            OrderedWork.Services.WorkOrderService.InvalidateAvailableWorkTypes();
        }





        internal static void RefreshWorkTabColumnOrder()
        {
            WorkConfigProfile profile = WorkConfigStore.LoadCachedActive()?.Profile;
            if (profile != null)
            {
                ReorderWorkTabColumns(profile);
            }
        }

        internal static void ReorderWorkTableColumns(List<PawnColumnDef> columns)
        {
            WorkConfigProfile profile = WorkConfigStore.LoadCachedActive()?.Profile;
            ReorderColumns(columns, profile);
        }

        private static void ReorderWorkTabColumns(WorkConfigProfile profile)
        {
            if (PawnTableDefOf.Work == null)
            {
                return;
            }
            ReorderColumns(PawnTableDefOf.Work.columns, profile);
        }

        private static void ReorderColumns(List<PawnColumnDef> columns, WorkConfigProfile profile)
        {
            if (profile == null || columns == null)
            {
                return;
            }

            Dictionary<string, int> order = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, WorkTypeConfig> entry in profile.WorkTypes
                .Where(entry => entry.Value != null)
                .OrderBy(entry => entry.Value.Order))
            {
                if (!order.ContainsKey(entry.Key))
                {
                    order[entry.Key] = order.Count;
                }
            }

            List<int> slots = new List<int>();
            List<PawnColumnDef> workColumns = new List<PawnColumnDef>();
            for (int i = 0; i < columns.Count; i++)
            {
                PawnColumnDef column = columns[i];
                if (column != null && column.workType != null)
                {
                    slots.Add(i);
                    workColumns.Add(column);
                }
            }
            workColumns.Sort((left, right) =>
            {
                int leftOrder;
                int rightOrder;
                bool hasLeft = order.TryGetValue(left.workType.defName, out leftOrder);
                bool hasRight = order.TryGetValue(right.workType.defName, out rightOrder);
                if (hasLeft != hasRight)
                {
                    return hasLeft ? -1 : 1;
                }
                if (hasLeft && leftOrder != rightOrder)
                {
                    return leftOrder.CompareTo(rightOrder);
                }
                return right.workType.naturalPriority.CompareTo(left.workType.naturalPriority);
            });
            for (int i = 0; i < slots.Count; i++)
            {
                columns[slots[i]] = workColumns[i];
            }

            Type controller = AccessTools.TypeByName("WorkTab.Controller");
            if (controller != null)
            {
                FieldInfo allColumns = controller.GetField("allColumns",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (allColumns != null)
                {
                    allColumns.SetValue(null, new List<PawnColumnDef>(columns));
                }
            }
        }

        private static void RebuildCaches()
        {
            FieldInfo field = typeof(WorkTypeDef).GetField("workGiversByPriority", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                return;
            }
            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                List<WorkGiverDef> list = DefDatabase<WorkGiverDef>.AllDefsListForReading
                    .Where(g => g.workType == workType)
                    .OrderByDescending(g => g.priorityInType)
                    .ToList();
                field.SetValue(workType, list);
            }
        }
    }
}