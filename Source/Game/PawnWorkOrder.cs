#nullable disable
using System.Collections.Generic;
using Verse;

namespace OrderedWork.Data
{
    /// <summary>
    /// One saved per-pawn policy record. <see cref="order"/> is the legacy full
    /// snapshot; the link fields and sparse override tables are the forward model.
    /// </summary>
    public class PawnWorkOrder : IExposable
    {
        public const int CurrentPolicySchemaVersion = 3;

        public Pawn pawn;
        public string pawnId;
        public int policySchemaVersion = CurrentPolicySchemaVersion;
        // Runtime-only revision used by UI view caches; intentionally not scribed.
        public int orderVersion;

        // Legacy full snapshot. Still authoritative when no workPresetId is present.
        public List<string> order = new List<string>();
        public string workPresetId;
        public List<PawnWorkOverride> workOverrides = new List<PawnWorkOverride>();
        public List<PawnWorkGiverOrder> workGiverOrders = new List<PawnWorkGiverOrder>();

        public string schedulePresetId;
        public List<PawnScheduleOverride> scheduleOverrides = new List<PawnScheduleOverride>();
        public List<PawnScheduleWorkOverride> scheduleWorkOverrides = new List<PawnScheduleWorkOverride>();
        public List<PawnHourFocusOverride> hourFocusOverrides = new List<PawnHourFocusOverride>();

        public List<PawnZoneBinding> zones = new List<PawnZoneBinding>();

        // Reserved for the later clothing policy; no UI or resolver yet.
        public string clothingPresetId;
        public List<PawnClothingOverride> clothingOverrides = new List<PawnClothingOverride>();

        /// <summary>
        /// Runtime-only: while the current tick is below this value, vanilla SetPriority
        /// capture is ignored for this pawn (post-spawn / post-resurrection settling).
        /// Not scribed.
        /// </summary>
        public int captureSuppressedUntilTick;

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref pawnId, "pawnId");
            Scribe_Values.Look(ref policySchemaVersion, "policySchemaVersion", 1);
            Scribe_Collections.Look(ref order, "order", LookMode.Value);
            Scribe_Values.Look(ref workPresetId, "workPresetId");
            Scribe_Collections.Look(ref workOverrides, "workOverrides", LookMode.Deep);
            Scribe_Collections.Look(ref workGiverOrders, "workGiverOrders", LookMode.Deep);
            Scribe_Values.Look(ref schedulePresetId, "schedulePresetId");
            Scribe_Collections.Look(ref scheduleOverrides, "scheduleOverrides", LookMode.Deep);
            Scribe_Collections.Look(ref scheduleWorkOverrides, "scheduleWorkOverrides", LookMode.Deep);
            Scribe_Collections.Look(ref hourFocusOverrides, "hourFocusOverrides", LookMode.Deep);
            Scribe_Collections.Look(ref zones, "zones", LookMode.Deep);
            Scribe_Values.Look(ref clothingPresetId, "clothingPresetId");
            Scribe_Collections.Look(ref clothingOverrides, "clothingOverrides", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (order == null) order = new List<string>();
                if (workOverrides == null) workOverrides = new List<PawnWorkOverride>();
                if (workGiverOrders == null) workGiverOrders = new List<PawnWorkGiverOrder>();
                if (scheduleOverrides == null) scheduleOverrides = new List<PawnScheduleOverride>();
                if (scheduleWorkOverrides == null) scheduleWorkOverrides = new List<PawnScheduleWorkOverride>();
                if (hourFocusOverrides == null) hourFocusOverrides = new List<PawnHourFocusOverride>();
                if (clothingOverrides == null) clothingOverrides = new List<PawnClothingOverride>();
            }
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                NormalizePolicy();
            }
        }

        public void NormalizePolicy()
        {
            if (pawn != null && string.IsNullOrEmpty(pawnId))
            {
                pawnId = pawn.GetUniqueLoadID();
            }
            if (policySchemaVersion <= 0)
            {
                policySchemaVersion = CurrentPolicySchemaVersion;
            }
            if (order == null) order = new List<string>();
            if (workOverrides == null) workOverrides = new List<PawnWorkOverride>();
            if (workGiverOrders == null) workGiverOrders = new List<PawnWorkGiverOrder>();
            if (scheduleOverrides == null) scheduleOverrides = new List<PawnScheduleOverride>();
            if (scheduleWorkOverrides == null) scheduleWorkOverrides = new List<PawnScheduleWorkOverride>();
            if (hourFocusOverrides == null) hourFocusOverrides = new List<PawnHourFocusOverride>();
            if (zones == null) zones = new List<PawnZoneBinding>();
            if (clothingOverrides == null) clothingOverrides = new List<PawnClothingOverride>();

            PolicyValidation.Normalize(workOverrides);
            PolicyValidation.NormalizeWorkGivers(workGiverOrders);
            if (scheduleWorkOverrides != null && scheduleWorkOverrides.Count > 0)
            {
                if (hourFocusOverrides.Count == 0)
                {
                    foreach (PawnScheduleWorkOverride legacy in scheduleWorkOverrides)
                    {
                        if (legacy == null || string.IsNullOrEmpty(legacy.workTypeDefName))
                        {
                            continue;
                        }
                        hourFocusOverrides.Add(new PawnHourFocusOverride
                        {
                            hour = legacy.hour,
                            workTypeDefNames = new List<string> { legacy.workTypeDefName },
                            source = legacy.source
                        });
                    }
                }
                scheduleWorkOverrides.Clear();
            }
            PolicyValidation.Normalize(scheduleOverrides);
            PolicyValidation.NormalizeHourFocus(hourFocusOverrides);
            PolicyValidation.Normalize(zones);
            PolicyValidation.Normalize(clothingOverrides);
        }
    }
}