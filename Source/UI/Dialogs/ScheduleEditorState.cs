#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.Services;
using RimWorld;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Draft state for the Schedule editor. No drawing, file IO or brush policy.
    /// </summary>
    internal sealed class ScheduleEditorState
    {
        internal SchedulePreset Draft;
        internal string Name = "";

        internal void Reset()
        {
            Draft = null;
            Name = "";
        }

        internal void Ensure(Pawn pawn)
        {
            if (Draft == null)
            {
                Draft = ScheduleAdapter.CaptureCurrent(pawn, Name) ?? new SchedulePreset { Name = "" };
            }
        }

        internal bool HasChanges(Pawn pawn)
        {
            if (Draft == null || Draft.Slots == null || pawn == null || pawn.timetable == null)
            {
                return false;
            }
            for (int hour = 0; hour < SchedulePreset.SlotCount; hour++)
            {
                string draft = Draft.Slots[hour];
                TimeAssignmentDef current = pawn.timetable.GetAssignment(hour);
                string currentName = current != null ? current.defName : "";
                if (!string.IsNullOrEmpty(draft) && draft != currentName)
                {
                    return true;
                }
            }
            return false;
        }

        internal TimeAssignmentDef ResolveDraft(int hour)
        {
            if (Draft == null || Draft.Slots == null || hour < 0 || hour >= Draft.Slots.Count)
            {
                return null;
            }
            return SchedulePolicyResolver.ResolveDef(Draft.Slots[hour]);
        }

        internal void SetDraft(SchedulePreset preset)
        {
            Draft = preset;
            Name = preset != null ? preset.Name ?? "" : "";
        }
    }
}