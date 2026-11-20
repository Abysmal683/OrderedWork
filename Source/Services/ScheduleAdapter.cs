#nullable disable
using System;
using System.Collections.Generic;
using OrderedWork.Config;
using OrderedWork.Data;
using RimWorld;
using Verse;

namespace OrderedWork.Services
{
    /// <summary>
    /// Small write-through adapter for the vanilla timetable. It never creates a
    /// second timetable and never overwrites a slot with an unresolved def.
    /// </summary>
    public static class ScheduleAdapter
    {
        public static bool Apply(Pawn pawn, PawnWorkOrder entry)
        {
            if (pawn == null || pawn.timetable == null || entry == null)
            {
                return false;
            }

            List<string> slots = SchedulePolicyResolver.Resolve(pawn, entry);
            int changed = 0;
            for (int hour = 0; hour < SchedulePolicyResolver.Hours; hour++)
            {
                TimeAssignmentDef def = ResolveForApply(slots[hour]);
                if (def == null)
                {
                    continue;
                }
                if (pawn.timetable.GetAssignment(hour) != def)
                {
                    pawn.timetable.SetAssignment(hour, def);
                    changed++;
                }
            }
            OrderedWorkMod.DebugLog("Schedule applied to " + pawn.LabelShort + ": " + changed + " slot(s) changed.", OrderedWorkLogCategory.Schedule);
            return true;
        }

        public static bool ApplyPreset(Pawn pawn, SchedulePreset preset)
        {
            if (pawn == null || preset == null)
            {
                return false;
            }
            OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
            if (component == null)
            {
                return false;
            }
            PawnWorkOrder entry = component.GetOrCreateEntry(pawn);
            if (entry == null)
            {
                return false;
            }
            entry.schedulePresetId = preset.Id;
            return Apply(pawn, entry);
        }

        public static bool ApplyDraft(Pawn pawn, SchedulePreset preset)
        {
            if (pawn == null || preset == null || pawn.timetable == null)
            {
                return false;
            }
            OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
            PawnWorkOrder entry = component?.GetOrCreateEntry(pawn);
            if (entry == null)
            {
                return false;
            }
            entry.schedulePresetId = null;
            int changed = 0;
            for (int hour = 0; hour < SchedulePreset.SlotCount; hour++)
            {
                string slot = preset.Slots != null && hour < preset.Slots.Count ? preset.Slots[hour] : null;
                TimeAssignmentDef def = ResolveForApply(slot);
                if (def == null)
                {
                    continue;
                }
                if (pawn.timetable.GetAssignment(hour) != def)
                {
                    pawn.timetable.SetAssignment(hour, def);
                    changed++;
                }
            }
            OrderedWorkMod.DebugLog("Schedule draft applied to " + pawn.LabelShort + ": " + changed + " slot(s) changed.", OrderedWorkLogCategory.Schedule);
            return true;
        }

        private static TimeAssignmentDef ResolveForApply(string slotName)
        {
            if (string.IsNullOrEmpty(slotName))
            {
                return null;
            }
            TimeAssignmentDef def = SchedulePolicyResolver.ResolveDef(slotName);
            return def ?? TimeAssignmentDefOf.Anything;
        }

        private static int ModIndex(SchedulePreset preset, TimeAssignmentDef def)
        {
            if (def == null || def.modContentPack == null)
            {
                return -1;
            }
            string packageId = def.modContentPack.PackageId;
            if (string.IsNullOrEmpty(packageId))
            {
                return -1;
            }
            int index = preset.Mods.IndexOf(packageId);
            if (index < 0)
            {
                index = preset.Mods.Count;
                preset.Mods.Add(packageId);
            }
            return index;
        }

        public static SchedulePreset CaptureCurrent(Pawn pawn, string name)
        {
            if (pawn == null || pawn.timetable == null)
            {
                return null;
            }
            SchedulePreset preset = new SchedulePreset
            {
                Name = string.IsNullOrWhiteSpace(name) ? "captured schedule" : name
            };
            for (int hour = 0; hour < SchedulePreset.SlotCount; hour++)
            {
                TimeAssignmentDef def = pawn.timetable.GetAssignment(hour);
                preset.Slots[hour] = def != null ? def.defName : "";
                preset.SlotMods[hour] = ModIndex(preset, def);
            }
            return preset;
        }
    }
}
