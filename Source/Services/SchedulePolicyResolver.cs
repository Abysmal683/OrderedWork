using System;
using System.Collections.Generic;
using OrderedWork.Config;
using OrderedWork.Data;
using RimWorld;
using Verse;

namespace OrderedWork.Services
{
    /// <summary>
    /// Resolves a pawn's effective 24-slot timetable from a linked schedule preset
    /// plus sparse hourly overrides. Missing presets or defs fall back to the pawn's
    /// current vanilla timetable instead of wiping it.
    /// </summary>
    public static class SchedulePolicyResolver
    {
        public const int Hours = 24;

        public static List<string> Resolve(Pawn pawn, PawnWorkOrder entry)
        {
            string[] slots = new string[Hours];
            SchedulePreset preset = entry != null && !string.IsNullOrEmpty(entry.schedulePresetId)
                ? SchedulePresetStore.TryLoadById(entry.schedulePresetId)
                : null;
            if (entry != null && !string.IsNullOrEmpty(entry.schedulePresetId) && preset == null)
            {
                OrderedWorkMod.DebugLog("Schedule preset not found: " + entry.schedulePresetId, OrderedWorkLogCategory.Schedule);
            }

            for (int hour = 0; hour < Hours; hour++)
            {
                if (preset != null && preset.Slots != null && hour < preset.Slots.Count)
                {
                    slots[hour] = preset.Slots[hour];
                }
                if (string.IsNullOrEmpty(slots[hour]) && pawn != null && pawn.timetable != null)
                {
                    TimeAssignmentDef current = pawn.timetable.GetAssignment(hour);
                    slots[hour] = current != null ? current.defName : null;
                }
            }

            if (entry != null && entry.scheduleOverrides != null)
            {
                foreach (PawnScheduleOverride entryOverride in entry.scheduleOverrides)
                {
                    if (entryOverride != null
                        && entryOverride.hour >= 0
                        && entryOverride.hour < Hours
                        && !string.IsNullOrEmpty(entryOverride.assignmentDefName))
                    {
                        slots[entryOverride.hour] = entryOverride.assignmentDefName;
                    }
                }
            }

            return new List<string>(slots);
        }

        public static TimeAssignmentDef ResolveDef(string defName)
        {
            return string.IsNullOrEmpty(defName)
                ? null
                : DefDatabase<TimeAssignmentDef>.GetNamedSilentFail(defName);
        }
    }
}
