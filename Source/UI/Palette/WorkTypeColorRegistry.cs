using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Deterministic, distinct colour per WorkType from its defName hash. User overrides are
    /// stored in Mod Settings so a future colour picker can reuse them.
    /// </summary>
    internal static class WorkTypeColorRegistry
    {
        private static readonly Dictionary<string, Color> overrides = new Dictionary<string, Color>();
        private static bool loaded;

        internal static Color GetColor(WorkTypeDef workType)
        {
            if (workType == null)
            {
                return new Color(0.50f, 0.50f, 0.50f, 1f);
            }
            EnsureLoaded();
            Color color;
            if (overrides.TryGetValue(workType.defName, out color))
            {
                return color;
            }
            return HashColor(workType.defName);
        }

        internal static bool HasOverride(string defName)
        {
            EnsureLoaded();
            return !string.IsNullOrEmpty(defName) && overrides.ContainsKey(defName);
        }

        internal static void SetOverride(string defName, Color color)
        {
            if (string.IsNullOrEmpty(defName))
            {
                return;
            }
            EnsureLoaded();
            overrides[defName] = color;
            SaveToSettings();
        }

        internal static Color HashColor(string defName)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int i = 0; i < defName.Length; i++)
                {
                    hash ^= defName[i];
                    hash *= 16777619u;
                }
                float hue = (hash % 360u) / 360f;
                return Color.HSVToRGB(hue, 0.55f, 0.70f);
            }
        }

        private static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }
            loaded = true;
            OrderedWorkSettings settings = OrderedWorkMod.Settings;
            if (settings == null || settings.workTypeColors == null)
            {
                return;
            }
            foreach (WorkTypeColorEntry entry in settings.workTypeColors)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.defName))
                {
                    overrides[entry.defName] = new Color(entry.r, entry.g, entry.b, 1f);
                }
            }
        }

        private static void SaveToSettings()
        {
            OrderedWorkSettings settings = OrderedWorkMod.Settings;
            if (settings == null)
            {
                return;
            }
            if (settings.workTypeColors == null)
            {
                settings.workTypeColors = new List<WorkTypeColorEntry>();
            }
            settings.workTypeColors.Clear();
            foreach (KeyValuePair<string, Color> pair in overrides)
            {
                settings.workTypeColors.Add(new WorkTypeColorEntry
                {
                    defName = pair.Key,
                    r = pair.Value.r,
                    g = pair.Value.g,
                    b = pair.Value.b
                });
            }
        }
    }
}
