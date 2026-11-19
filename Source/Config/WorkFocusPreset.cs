using System;
using System.Collections.Generic;
using OrderedWork.Core;

namespace OrderedWork.Config
{
    /// <summary>
    /// Portable, named hourly focus plan. Each of the 24 hours stores at most
    /// three ordered WorkType defNames. It never contains pawn, map or timetable data.
    /// </summary>
    public sealed class WorkFocusPreset
    {
        public const int CurrentSchemaVersion = 1;
        public const int SlotCount = 24;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "new focus";
        public string ModifiedUtc { get; set; } = "";
        public List<List<string>> Slots { get; set; } = NewSlots();

        public static List<List<string>> NewSlots()
        {
            List<List<string>> slots = new List<List<string>>(SlotCount);
            for (int i = 0; i < SlotCount; i++)
            {
                slots.Add(new List<string>());
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
                Name = "new focus";
            }
            if (Slots == null)
            {
                Slots = NewSlots();
            }
            while (Slots.Count < SlotCount)
            {
                Slots.Add(new List<string>());
            }
            if (Slots.Count > SlotCount)
            {
                Slots.RemoveRange(SlotCount, Slots.Count - SlotCount);
            }
            for (int hour = 0; hour < SlotCount; hour++)
            {
                Slots[hour] = HourFocusStack.NormalizeFocus(Slots[hour]);
            }
        }

        public List<string> FocusAt(int hour)
        {
            if (Slots == null || hour < 0 || hour >= Slots.Count)
            {
                return new List<string>();
            }
            return Slots[hour] ?? new List<string>();
        }
    }
}
