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
    public static partial class WorkOrderService
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const int PostSpawnCaptureSuppressionTicks = 120;

        private static readonly string[] DefaultRecommendationPins = { "Patient", "PatientBedRest", "Firefighter" };

        private static List<WorkTypeDef> cachedAvailable;
        private static readonly Dictionary<Pawn, int> manualMaxCache = new Dictionary<Pawn, int>();
        private static int manualMaxCacheTick = int.MinValue;
        private static int manualMaxForAll = -1;
        private static FieldInfo cachedPrioritiesField;
        private static bool prioritiesFieldResolved;
        private static FieldInfo cachedWorkSettingsPawnField;
        private static bool workSettingsPawnFieldResolved;

        /// <summary>True while our own Apply is writing, so the vanilla hook ignores it.</summary>
        public static bool IsApplying { get; private set; }

        /// <summary>True while a pawn is spawning, so the vanilla hook ignores setup writes.</summary>
        public static bool IsSpawning { get; set; }

        /// <summary>True while Pawn_WorkSettings initializes, so the vanilla hook ignores it.</summary>
        public static bool IsInitializing { get; set; }

        /// <summary>Visible WorkTypeDefs, cached; ordered by naturalPriority descending.</summary>
        public static void InvalidateAvailableWorkTypes()
        {
            cachedAvailable = null;
        }

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
        /// Highest priority value the vanilla Work tab may cycle to. Dynamic when safe:
        /// max(4, visible capable WorkTypes), capped at 99. Falls back to 99 on failure.
        /// Cached per pawn per game tick, because the Work tab asks once per cell per frame.
        /// Pass a pawn to scope the count to its capable types; pass null for all visible.
        /// </summary>
        public static int GetManualMaxPriority(Pawn pawn = null)
        {
            int tick = CurrentTick;
            if (tick != manualMaxCacheTick)
            {
                manualMaxCache.Clear();
                manualMaxCacheTick = tick;
                manualMaxForAll = -1;
            }

            if (pawn == null)
            {
                if (manualMaxForAll < 0)
                {
                    manualMaxForAll = ComputeManualMaxPriority(null);
                }
                return manualMaxForAll;
            }

            int cached;
            if (manualMaxCache.TryGetValue(pawn, out cached))
            {
                return cached;
            }

            int max = ComputeManualMaxPriority(pawn);
            manualMaxCache[pawn] = max;
            return max;
        }

        private static int ComputeManualMaxPriority(Pawn pawn)
        {
            try
            {
                int count = 0;
                foreach (WorkTypeDef workType in GetAvailableWorkTypes())
                {
                    if (workType == null)
                    {
                        continue;
                    }
                    if (pawn != null && IsIncapable(pawn, workType))
                    {
                        continue;
                    }
                    count++;
                }

                int max = Math.Max(4, count);
                return max > 99 ? 99 : max;
            }
            catch (Exception ex)
            {
                Log.Error("[Ordered Work] GetManualMaxPriority failed; using 99: " + ex);
                return 99;
            }
        }

        /// <summary>
        /// Writes a priority straight into the private map, bypassing
        /// Pawn_WorkSettings.SetPriority (which logs for values above the vanilla cap).
        /// Returns false when the private map is unavailable.
        /// </summary>

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
        /// Skill recommendation. Patient, PatientBedRest and Firefighter are pinned first
        /// (when capable, in that order), then the remaining capable WorkTypes are scored:
        /// no relevant skill = 0; level 0 with no/minor/major passion = -3 / 1 / 3;
        /// level &gt;= 1 = level * 2 + (minor 2 / major 4). Descending score, then
        /// naturalPriority descending. Used for new colonies and new colonists only.
        /// </summary>
        public static List<string> GetRecommendation(Pawn pawn)
        {
            PassionWeightPolicy.EnsureDefaults(OrderedWorkMod.Settings);
            List<string> pins = new List<string>();
            List<string> pinNames = OrderedWorkMod.Settings != null
                && OrderedWorkMod.Settings.recommendationPins != null
                && OrderedWorkMod.Settings.recommendationPins.Count > 0
                ? OrderedWorkMod.Settings.recommendationPins
                : new List<string>(DefaultRecommendationPins);
            foreach (string defName in pinNames)
            {
                WorkTypeDef pinned = DefDatabase<WorkTypeDef>.GetNamedSilentFail(defName);
                if (pinned != null && pinned.visible && !IsIncapable(pawn, pinned) && !pins.Contains(pinned.defName))
                {
                    pins.Add(pinned.defName);
                }
            }

            List<WorkTypeDef> scored = new List<WorkTypeDef>();
            foreach (WorkTypeDef workType in GetAvailableWorkTypes())
            {
                if (workType == null || IsIncapable(pawn, workType) || pins.Contains(workType.defName))
                {
                    continue;
                }
                scored.Add(workType);
            }

            List<string> result = new List<string>(pins);
            result.AddRange(scored
                .OrderByDescending(w => RecommendationScorer.Score(pawn, w, OrderedWorkMod.Settings))
                .ThenByDescending(w => w.naturalPriority)
                .Select(w => w.defName));
            return result;
        }

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
                // With auto-recommend off, never show an un-applied recommendation:
                // fall back to the current vanilla order instead.
                return AutoRecommendNewColony
                    ? GetRecommendation(pawn)
                    : Normalize(pawn, ReadVanillaOrder(pawn));
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

        /// <summary>
        /// One-time colony setup. Existing colonies copy the current vanilla priority order
        /// without writing; new/default colonies get the skill recommendation applied once.
        /// </summary>

        /// <summary>
        /// Vanilla Work-tab capture. Called from the SetPriority Harmony postfix; rebuilds and
        /// stores the order from the current priorities. Never calls Apply/Notify.
        /// </summary>

        /// <summary>
        /// Spawn/resurrection handling. If a stored order exists it is reapplied so the
        /// vanilla Work tab matches; otherwise the new pawn gets the recommendation applied.
        /// </summary>

        /// <summary>
        /// Faction change handling. An existing stored order is always reapplied first; only
        /// a pawn with no stored order gets the auto-recommend/copy behavior.
        /// </summary>

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
            return RecommendationScorer.BestSkillLevel(pawn, workType, out passion);
        }

        /// <summary>
        /// Keeps the requested subset in order, drops ids that are not visible or capable,
        /// and removes duplicates. Missing types are intentionally NOT re-appended: an
        /// absent type means "off" (priority 0).
        /// </summary>

        /// <summary>Active, capable vanilla types (priority &gt; 0), sorted by priority then naturalPriority.</summary>

        /// <summary>Anything other than the vanilla default 0 (off) or 3 (active).</summary>


        private static bool AutoRecommendNewColony
        {
            get
            {
                OrderedWorkSettings settings = OrderedWorkMod.Settings;
                return settings == null || settings.autoRecommendNewColony;
            }
        }

        private static int CurrentTick
        {
            get { return Find.TickManager != null ? Find.TickManager.TicksGame : 0; }
        }


    }
}