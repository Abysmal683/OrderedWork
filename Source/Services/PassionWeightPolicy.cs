using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.Services
{
    /// <summary>
    /// Owns passion-weight defaults, one-time migration and persistence helpers.
    /// The actual passion registry and metadata live in <see cref="PassionSource"/>.
    /// </summary>
    public static class PassionWeightPolicy
    {
        private const int DefaultsVersion = 3;

        public static void EnsureDefaults(OrderedWorkSettings settings)
        {
            if (settings == null)
            {
                return;
            }
            if (settings.passionWeights == null)
            {
                settings.passionWeights = new List<PassionWeightEntry>();
            }
            bool migrate = settings.passionDefaultsVersion < DefaultsVersion;
            foreach (PassionSource.PassionInfo info in PassionSource.Registered)
            {
                PassionWeightEntry entry = settings.passionWeights
                    .FirstOrDefault(e => e != null && e.defName == info.DefName);
                if (entry == null)
                {
                    settings.passionWeights.Add(DefaultEntry(info));
                    continue;
                }
                if (migrate
                    && !IsVanillaName(info.DefName)
                    && IsLegacyNeutral(entry))
                {
                    ApplyWeights(entry, DefaultEntry(info));
                }
            }
            settings.passionDefaultsVersion = DefaultsVersion;
        }

        public static void ResetToDefaults(OrderedWorkSettings settings)
        {
            if (settings == null)
            {
                return;
            }
            settings.passionWeights = PassionSource.Registered.Select(DefaultEntry).ToList();
            settings.passionDefaultsVersion = DefaultsVersion;
        }

        public static PassionWeightEntry Find(OrderedWorkSettings settings, Passion passion)
        {
            if (settings == null)
            {
                return null;
            }
            EnsureDefaults(settings);
            string defName = PassionSource.DefNameOf(passion);
            PassionWeightEntry entry = settings.passionWeights
                .FirstOrDefault(e => e != null && e.defName == defName);
            if (entry == null)
            {
                PassionSource.PassionInfo info = PassionSource.Registered
                    .FirstOrDefault(p => p.DefName == defName);
                if (info != null)
                {
                    entry = DefaultEntry(info);
                    settings.passionWeights.Add(entry);
                }
            }
            return entry;
        }

        private static PassionWeightEntry DefaultEntry(PassionSource.PassionInfo info)
        {
            string name = info.DefName ?? "";
            if (name == "Major")
            {
                return new PassionWeightEntry { defName = name, level0Score = 3, perLevelBonus = 4, minLevel = 0 };
            }
            if (name == "Minor")
            {
                return new PassionWeightEntry { defName = name, level0Score = 1, perLevelBonus = 2, minLevel = 0 };
            }
            if (name == "None"
                || name.IndexOf("Disabled", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Desinterested", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Disinterested", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new PassionWeightEntry { defName = name, level0Score = -3, perLevelBonus = 0, minLevel = 0 };
            }

            float learnRate = PassionSource.LearnRateOf(info.Passion);
            if (learnRate > 0f)
            {
                int level0 = Mathf.Clamp(Mathf.RoundToInt(learnRate * 4f - 3f), -20, 20);
                int perLevel = Mathf.Clamp(Mathf.RoundToInt(learnRate * 4f - 2f), -20, 20);
                return new PassionWeightEntry
                {
                    defName = name,
                    level0Score = level0,
                    perLevelBonus = perLevel,
                    minLevel = 0
                };
            }
            return new PassionWeightEntry { defName = name, level0Score = 0, perLevelBonus = 0, minLevel = 0 };
        }

        private static bool IsVanillaName(string defName)
        {
            return defName == "None" || defName == "Minor" || defName == "Major";
        }

        private static bool IsLegacyNeutral(PassionWeightEntry entry)
        {
            return entry != null
                && entry.level0Score == 0
                && entry.perLevelBonus == 0
                && entry.minLevel == 0;
        }

        private static bool LooksLikePreviousDefault(PassionSource.PassionInfo info, PassionWeightEntry entry)
        {
            if (entry == null || entry.minLevel != 0)
            {
                return false;
            }
            float learnRate = PassionSource.LearnRateOf(info.Passion);
            if (learnRate <= 0f)
            {
                return false;
            }
            int oldLevel0 = Mathf.Clamp(Mathf.RoundToInt(learnRate * 4f - 3f), -20, 20);
            int oldPerLevel = Mathf.Clamp(Mathf.RoundToInt(learnRate * 4f - 2f), -20, 20);
            return entry.level0Score == oldLevel0 && entry.perLevelBonus == oldPerLevel;
        }

        private static void ApplyWeights(PassionWeightEntry target, PassionWeightEntry source)
        {
            target.level0Score = source.level0Score;
            target.perLevelBonus = source.perLevelBonus;
            target.minLevel = source.minLevel;
        }
    }
}
