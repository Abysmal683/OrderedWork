#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Core;
using Verse;

namespace OrderedWork.Data
{
    public enum PolicySource
    {
        Preset,
        Manual,
        Schedule,
        Emergency,
        Legacy
    }

    public class PawnWorkGiverOrder : IExposable
    {
        public string workTypeDefName;
        public List<string> giverDefNames = new List<string>();
        public PolicySource source = PolicySource.Manual;

        public void ExposeData()
        {
            Scribe_Values.Look(ref workTypeDefName, "workTypeDefName");
            Scribe_Collections.Look(ref giverDefNames, "giverDefNames", LookMode.Value);
            Scribe_Values.Look(ref source, "source", PolicySource.Manual);
            if (Scribe.mode == LoadSaveMode.LoadingVars && giverDefNames == null)
            {
                giverDefNames = new List<string>();
            }
        }
    }

    public class PawnWorkOverride : IExposable
    {
        public string workTypeDefName;
        public int rank;
        public bool enabled = true;
        public bool locked;
        public PolicySource source = PolicySource.Manual;

        public void ExposeData()
        {
            Scribe_Values.Look(ref workTypeDefName, "workTypeDefName");
            Scribe_Values.Look(ref rank, "rank", 0);
            Scribe_Values.Look(ref enabled, "enabled", true);
            Scribe_Values.Look(ref locked, "locked", false);
            Scribe_Values.Look(ref source, "source", PolicySource.Manual);
        }
    }

    public class PawnScheduleOverride : IExposable
    {
        public int hour;
        public string assignmentDefName;
        public PolicySource source = PolicySource.Manual;

        public void ExposeData()
        {
            Scribe_Values.Look(ref hour, "hour", 0);
            Scribe_Values.Look(ref assignmentDefName, "assignmentDefName");
            Scribe_Values.Look(ref source, "source", PolicySource.Manual);
        }
    }

    public class PawnZoneBinding : IExposable
    {
        public int mapUniqueId;
        public int areaId;
        public string areaUniqueId;
        public string areaLabel;
        public bool unrestricted;
        public PolicySource source = PolicySource.Manual;

        public void ExposeData()
        {
            Scribe_Values.Look(ref mapUniqueId, "mapUniqueId", 0);
            Scribe_Values.Look(ref areaId, "areaId", 0);
            Scribe_Values.Look(ref areaUniqueId, "areaUniqueId");
            Scribe_Values.Look(ref areaLabel, "areaLabel");
            Scribe_Values.Look(ref unrestricted, "unrestricted", false);
            Scribe_Values.Look(ref source, "source", PolicySource.Manual);
        }
    }

    public class PawnScheduleWorkOverride : IExposable
    {
        public int hour;
        public string workTypeDefName;
        public PolicySource source = PolicySource.Manual;

        public void ExposeData()
        {
            Scribe_Values.Look(ref hour, "hour", 0);
            Scribe_Values.Look(ref workTypeDefName, "workTypeDefName");
            Scribe_Values.Look(ref source, "source", PolicySource.Manual);
        }
    }

    public class PawnHourFocusOverride : IExposable
    {
        public int hour;
        public List<string> workTypeDefNames = new List<string>();
        public PolicySource source = PolicySource.Manual;

        public void ExposeData()
        {
            Scribe_Values.Look(ref hour, "hour", 0);
            Scribe_Collections.Look(ref workTypeDefNames, "workTypeDefNames", LookMode.Value);
            Scribe_Values.Look(ref source, "source", PolicySource.Manual);
            if (Scribe.mode == LoadSaveMode.LoadingVars && workTypeDefNames == null)
            {
                workTypeDefNames = new List<string>();
            }
        }
    }

    public class PawnClothingOverride : IExposable
    {
        public string workTypeDefName;
        public string apparelPolicyId;
        public PolicySource source = PolicySource.Manual;

        public void ExposeData()
        {
            Scribe_Values.Look(ref workTypeDefName, "workTypeDefName");
            Scribe_Values.Look(ref apparelPolicyId, "apparelPolicyId");
            Scribe_Values.Look(ref source, "source", PolicySource.Manual);
        }
    }

    internal static class PolicyValidation
    {
        public static void Normalize(List<PawnWorkOverride> values)
        {
            if (values == null)
            {
                return;
            }
            Dictionary<string, PawnWorkOverride> unique = new Dictionary<string, PawnWorkOverride>();
            foreach (PawnWorkOverride value in values)
            {
                if (value == null || string.IsNullOrEmpty(value.workTypeDefName))
                {
                    continue;
                }
                unique[value.workTypeDefName] = value;
            }
            values.Clear();
            values.AddRange(unique.Values);
        }

        public static void NormalizeWorkGivers(List<PawnWorkGiverOrder> values)
        {
            if (values == null)
            {
                return;
            }
            Dictionary<string, PawnWorkGiverOrder> unique = new Dictionary<string, PawnWorkGiverOrder>(StringComparer.Ordinal);
            foreach (PawnWorkGiverOrder value in values)
            {
                if (value == null || string.IsNullOrEmpty(value.workTypeDefName))
                {
                    continue;
                }
                value.giverDefNames = value.giverDefNames ?? new List<string>();
                value.giverDefNames = value.giverDefNames
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                if (value.giverDefNames.Count == 0)
                {
                    continue;
                }
                unique[value.workTypeDefName] = value;
            }
            values.Clear();
            values.AddRange(unique.Values);
        }

        public static void Normalize(List<PawnScheduleOverride> values)
        {
            if (values == null)
            {
                return;
            }
            Dictionary<int, PawnScheduleOverride> unique = new Dictionary<int, PawnScheduleOverride>();
            foreach (PawnScheduleOverride value in values)
            {
                if (value == null || value.hour < 0 || value.hour > 23
                    || string.IsNullOrEmpty(value.assignmentDefName))
                {
                    continue;
                }
                unique[value.hour] = value;
            }
            values.Clear();
            values.AddRange(unique.Values);
        }

        public static void Normalize(List<PawnZoneBinding> values)
        {
            if (values == null)
            {
                return;
            }
            Dictionary<int, PawnZoneBinding> unique = new Dictionary<int, PawnZoneBinding>();
            foreach (PawnZoneBinding value in values)
            {
                if (value == null || value.mapUniqueId == 0)
                {
                    continue;
                }
                unique[value.mapUniqueId] = value;
            }
            values.Clear();
            values.AddRange(unique.Values);
        }

        public static void NormalizeScheduleWork(List<PawnScheduleWorkOverride> values)
        {
            if (values == null)
            {
                return;
            }
            Dictionary<string, PawnScheduleWorkOverride> unique = new Dictionary<string, PawnScheduleWorkOverride>();
            foreach (PawnScheduleWorkOverride value in values)
            {
                if (value == null || value.hour < 0 || value.hour > 23
                    || string.IsNullOrEmpty(value.workTypeDefName))
                {
                    continue;
                }
                unique[value.hour + "|" + value.workTypeDefName] = value;
            }
            values.Clear();
            values.AddRange(unique.Values);
        }

        public static void NormalizeHourFocus(List<PawnHourFocusOverride> values)
        {
            if (values == null)
            {
                return;
            }
            Dictionary<int, PawnHourFocusOverride> unique = new Dictionary<int, PawnHourFocusOverride>();
            foreach (PawnHourFocusOverride value in values)
            {
                if (value == null || value.hour < 0 || value.hour > 23)
                {
                    continue;
                }
                value.workTypeDefNames = HourFocusStack.NormalizeFocus(value.workTypeDefNames);
                if (value.workTypeDefNames.Count == 0)
                {
                    continue;
                }
                unique[value.hour] = value;
            }
            values.Clear();
            values.AddRange(unique.Values);
        }
        public static void Normalize(List<PawnClothingOverride> values)
        {
            if (values == null)
            {
                return;
            }
            Dictionary<string, PawnClothingOverride> unique = new Dictionary<string, PawnClothingOverride>();
            foreach (PawnClothingOverride value in values)
            {
                if (value == null || string.IsNullOrEmpty(value.workTypeDefName))
                {
                    continue;
                }
                unique[value.workTypeDefName] = value;
            }
            values.Clear();
            values.AddRange(unique.Values);
        }
    }
}
