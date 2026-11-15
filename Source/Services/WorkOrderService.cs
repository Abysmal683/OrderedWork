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
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private static List<WorkTypeDef> cachedAvailable;
        private static FieldInfo cachedPrioritiesField;
        private static bool prioritiesFieldResolved;
        private static FieldInfo cachedWorkSettingsPawnField;
        private static bool workSettingsPawnFieldResolved;

        /// <summary>True while our own Apply is writing, so the vanilla hook ignores it.</summary>
        public static bool IsApplying { get; private set; }

        /// <summary>True while a pawn is spawning, so the vanilla hook ignores setup writes.</summary>
        public static bool IsSpawning { get; set; }

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

        /// <summary>
        /// Default order for a pawn: visible, capable WorkTypes by naturalPriority descending.
        /// Incapable types are always excluded so they never appear in the lane.
        /// </summary>
        public static List<string> GetDefaultOrder(Pawn pawn)
        {
            return GetAvailableWorkTypes()
                .Where(w => !IsIncapable(pawn, w))
                .Select(w => w.defName)
                .ToList();
        }

        /// <summary>
        /// Skill recommendation: capable visible WorkTypes by best relevant skill descending,
        /// then naturalPriority descending. Used for new colonies and new colonists.
        /// </summary>
        public static List<string> GetRecommendation(Pawn pawn)
        {
            return GetAvailableWorkTypes()
                .Where(w => !IsIncapable(pawn, w))
                .OrderByDescending(w => BestSkillLevel(pawn, w, out _))
                .ThenByDescending(w => w.naturalPriority)
                .Select(w => w.defName)
                .ToList();
        }

        /// <summary>
        /// Stored order for the pawn, filtered to visible and capable WorkTypes. Before the
        /// colony is initialized this falls back to the default order; afterwards it never
        /// refills from the default, using the skill recommendation for an unknown pawn.
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
                return Normalize(pawn, entry.order);
            }

            if (component != null && component.initialized)
            {
                return GetRecommendation(pawn);
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

            List<string> normalized = Normalize(pawn, order);
            component.SetOrder(pawn, normalized);
            Apply(pawn, normalized);
        }

        /// <summary>Recomputes and applies the skill recommendation for this pawn only.</summary>
        public static void GenerateAndApplyRecommendation(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }
            SaveAndApply(pawn, GetRecommendation(pawn));
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

                    Log.Message("[Ordered Work] Applied order for " + pawn.LabelShort + ": " +
                        string.Join(", ", capableOrder.Select((id, index) => id + "=" + (index + 1))));
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

        /// <summary>
        /// One-time colony setup. Existing colonies copy the current vanilla priority order
        /// without writing; new/default colonies get the skill recommendation applied once.
        /// </summary>
        public static void EnsureColonyInitialized()
        {
            try
            {
                if (Current.Game == null || Find.CurrentMap == null)
                {
                    return;
                }

                OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
                if (component == null || component.initialized)
                {
                    return;
                }

                List<Pawn> colonists = Find.CurrentMap.mapPawns.FreeColonists.ToList();
                if (colonists.Count == 0)
                {
                    return;
                }

                bool existingColony = colonists.Any(HasNonDefaultPriorities);

                foreach (Pawn pawn in colonists)
                {
                    if (existingColony)
                    {
                        // Capture the established vanilla order but do not write priorities.
                        component.SetOrder(pawn, Normalize(pawn, ReadVanillaOrder(pawn)));
                    }
                    else
                    {
                        List<string> recommendation = GetRecommendation(pawn);
                        component.SetOrder(pawn, recommendation);
                        Apply(pawn, recommendation);
                    }
                }

                component.initialized = true;
                Log.Message("[Ordered Work] Colony initialized (" +
                    (existingColony ? "existing" : "new") + "): " + colonists.Count + " colonist(s).");
            }
            catch (Exception ex)
            {
                Log.Error("[Ordered Work] Colony initialization failed: " + ex);
            }
        }

        /// <summary>
        /// Vanilla Work-tab capture. Called from the SetPriority Harmony postfix; rebuilds and
        /// stores the order from the current priorities. Never calls Apply/Notify.
        /// </summary>
        public static void OnVanillaPriorityChanged(Pawn_WorkSettings settings)
        {
            if (IsApplying || IsSpawning)
            {
                return;
            }
            if (Scribe.mode != LoadSaveMode.Inactive || settings == null)
            {
                return;
            }

            try
            {
                Pawn pawn = GetOwningPawn(settings);
                if (!IsManagedFreeColonist(pawn))
                {
                    return;
                }

                OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
                if (component == null || !component.initialized)
                {
                    return;
                }

                component.SetOrder(pawn, Normalize(pawn, ReadVanillaOrder(pawn)));
            }
            catch (Exception ex)
            {
                Log.Error("[Ordered Work] Vanilla-priority sync failed: " + ex);
            }
        }

        /// <summary>
        /// Spawn/resurrection handling. If a stored order exists it is reapplied so the
        /// vanilla Work tab matches; otherwise the new pawn gets the recommendation applied.
        /// </summary>
        public static void OnPawnSpawned(Pawn pawn, bool respawningAfterLoad)
        {
            if (respawningAfterLoad)
            {
                return;
            }
            if (Scribe.mode != LoadSaveMode.Inactive || !IsManagedFreeColonist(pawn))
            {
                return;
            }

            try
            {
                OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
                if (component == null || !component.initialized)
                {
                    return;
                }

                PawnWorkOrder entry = component.GetEntry(pawn);
                if (entry != null)
                {
                    Apply(pawn, Normalize(pawn, entry.order));
                }
                else
                {
                    List<string> recommendation = GetRecommendation(pawn);
                    component.SetOrder(pawn, recommendation);
                    Apply(pawn, recommendation);
                }
            }
            catch (Exception ex)
            {
                Log.Error("[Ordered Work] Spawn/reapply failed: " + ex);
            }
        }

        /// <summary>
        /// Faction change handling. When a pawn becomes a free colonist (for example a
        /// recruited prisoner) and has no stored order yet, recommend and apply one.
        /// </summary>
        public static void OnPawnFactionChanged(Pawn pawn)
        {
            if (IsApplying || IsSpawning)
            {
                return;
            }
            if (Scribe.mode != LoadSaveMode.Inactive || !IsManagedFreeColonist(pawn))
            {
                return;
            }

            try
            {
                OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
                if (component == null || !component.initialized || component.GetEntry(pawn) != null)
                {
                    return;
                }

                List<string> recommendation = GetRecommendation(pawn);
                component.SetOrder(pawn, recommendation);
                Apply(pawn, recommendation);
            }
            catch (Exception ex)
            {
                Log.Error("[Ordered Work] Faction-change recommendation failed: " + ex);
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
        /// Highest level among the WorkType's relevant skills; -1 when there is none.
        /// Also returns the passion of that best skill for the UI marker.
        /// </summary>
        public static int BestSkillLevel(Pawn pawn, WorkTypeDef workType, out Passion passion)
        {
            passion = Passion.None;
            if (pawn == null || pawn.skills == null || workType == null || workType.relevantSkills == null)
            {
                return -1;
            }

            int best = -1;
            foreach (SkillDef skill in workType.relevantSkills)
            {
                if (skill == null)
                {
                    continue;
                }
                SkillRecord record = pawn.skills.GetSkill(skill);
                if (record == null)
                {
                    continue;
                }
                if (record.Level > best)
                {
                    best = record.Level;
                    passion = record.passion;
                }
            }
            return best;
        }

        /// <summary>
        /// Keeps the requested subset in order, drops ids that are not visible or capable,
        /// and removes duplicates. Missing types are intentionally NOT re-appended: an
        /// absent type means "off" (priority 0).
        /// </summary>
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

        /// <summary>Active, capable vanilla types (priority &gt; 0), sorted by priority then naturalPriority.</summary>
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

        /// <summary>Anything other than the vanilla default 0 (off) or 3 (active).</summary>
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

        private static bool IsManagedFreeColonist(Pawn pawn)
        {
            return pawn != null && !pawn.Dead && !pawn.Destroyed && pawn.IsFreeColonist;
        }

        private static Pawn GetOwningPawn(Pawn_WorkSettings settings)
        {
            if (!workSettingsPawnFieldResolved)
            {
                cachedWorkSettingsPawnField = typeof(Pawn_WorkSettings).GetField("pawn", PrivateInstance);
                workSettingsPawnFieldResolved = true;
            }

            return cachedWorkSettingsPawnField != null
                ? cachedWorkSettingsPawnField.GetValue(settings) as Pawn
                : null;
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
    }
}
