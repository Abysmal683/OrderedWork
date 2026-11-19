using System;
using System.Collections.Generic;

namespace OrderedWork.Config
{
    /// <summary>
    /// Global, named vanilla timetable preset. Slots contain TimeAssignmentDef
    /// defNames only; the first schedule slice does not store WorkType windows.
    /// </summary>
    public sealed class SchedulePreset
    {
        public const int CurrentSchemaVersion = 2;
        public const int SlotCount = 24;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "new schedule";
        public string ModifiedUtc { get; set; } = "";
        public List<string> Slots { get; set; } = NewSlots();
        public List<string> Mods { get; set; } = new List<string>();
        public List<int> SlotMods { get; set; } = new List<int>();

        public static List<string> NewSlots()
        {
            List<string> slots = new List<string>(SlotCount);
            for (int i = 0; i < SlotCount; i++)
            {
                slots.Add("");
            }
            return slots;
        }

        public void Normalize()
        {
            if (SchemaVersion <= 0)
            {
                SchemaVersion = CurrentSchemaVersion;
            }
            if (string.IsNullOrEmpty(Id))
            {
                Id = Guid.NewGuid().ToString("N");
            }
            if (string.IsNullOrEmpty(Name))
            {
                Name = "new schedule";
            }
            if (Slots == null)
            {
                Slots = NewSlots();
            }
            while (Slots.Count < SlotCount)
            {
                Slots.Add("");
            }
            if (Slots.Count > SlotCount)
            {
                Slots.RemoveRange(SlotCount, Slots.Count - SlotCount);
            }
            if (Mods == null)
            {
                Mods = new List<string>();
            }
            if (SlotMods == null)
            {
                SlotMods = new List<int>();
            }
            while (SlotMods.Count < SlotCount)
            {
                SlotMods.Add(-1);
            }
            if (SlotMods.Count > SlotCount)
            {
                SlotMods.RemoveRange(SlotCount, SlotMods.Count - SlotCount);
            }
        }
    }
}
