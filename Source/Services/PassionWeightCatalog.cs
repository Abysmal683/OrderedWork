using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.Services
{
    /// <summary>
    /// Enumerates vanilla, VSE and Alpha Skills PassionDefs through public reflection and
    /// provides stable defaults keyed by defName. No third-party code or assets are copied.
    /// </summary>
    public static class PassionWeightCatalog
    {
        public sealed class PassionInfo
        {
            public Passion Passion;
            public string DefName;
            public string Label;
        }

        private static bool initialized;
        private static List<PassionInfo> registered;

        public static List<PassionInfo> Registered
        {
            get
            {
                EnsureInitialized();
                return registered;
            }
        }

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
            foreach (PassionInfo info in Registered)
            {
                if (settings.passionWeights.Any(e => e != null && e.defName == info.DefName))
                {
                    continue;
                }
                PassionWeightEntry entry = DefaultEntry(info);
                settings.passionWeights.Add(entry);
            }
        }

        public static void ResetToDefaults(OrderedWorkSettings settings)
        {
            if (settings == null)
            {
                return;
            }
            settings.passionWeights = Registered.Select(DefaultEntry).ToList();
        }

        public static PassionWeightEntry Find(OrderedWorkSettings settings, Passion passion)
        {
            if (settings == null)
            {
                return null;
            }
            EnsureDefaults(settings);
            string defName = DefNameOf(passion);
            PassionWeightEntry entry = settings.passionWeights
                .FirstOrDefault(e => e != null && e.defName == defName);
            if (entry == null)
            {
                PassionInfo info = Registered.FirstOrDefault(p => p.DefName == defName);
                if (info != null)
                {
                    entry = DefaultEntry(info);
                    settings.passionWeights.Add(entry);
                }
            }
            return entry;
        }

        public static string DefNameOf(Passion passion)
        {
            object def = GetVseDef(passion);
            if (def != null)
            {
                string name = GetMember<string>(def, "defName");
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
            if (passion == Passion.Major) return "Major";
            if (passion == Passion.Minor) return "Minor";
            return "None";
        }

        public static string LabelOf(Passion passion)
        {
            object def = GetVseDef(passion);
            if (def != null)
            {
                string label = GetStringMember(def, "LabelCap");
                if (string.IsNullOrEmpty(label))
                {
                    label = GetStringMember(def, "label");
                }
                if (!string.IsNullOrEmpty(label))
                {
                    return label;
                }
            }
            return passion == Passion.Major ? "Major" : passion == Passion.Minor ? "Minor" : "None";
        }

        private static PassionWeightEntry DefaultEntry(PassionInfo info)
        {
            string name = info.DefName ?? "";
            if (name.IndexOf("Major", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new PassionWeightEntry { defName = name, level0Score = 3, perLevelBonus = 4, minLevel = 0 };
            }
            if (name.IndexOf("Minor", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new PassionWeightEntry { defName = name, level0Score = 1, perLevelBonus = 2, minLevel = 0 };
            }
            if (name.IndexOf("Disabled", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Desinterested", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Disinterested", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new PassionWeightEntry { defName = name, level0Score = -3, perLevelBonus = 0, minLevel = 0 };
            }
            if (name.IndexOf("None", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new PassionWeightEntry { defName = name, level0Score = -3, perLevelBonus = 0, minLevel = 0 };
            }
            return new PassionWeightEntry { defName = name, level0Score = 0, perLevelBonus = 0, minLevel = 0 };
        }

        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;
            registered = new List<PassionInfo>();
            Type managerType = FindType("VSE.Passions.PassionManager");
            if (managerType != null)
            {
                try
                {
                    PropertyInfo all = managerType.GetProperty("AllPassions", BindingFlags.Public | BindingFlags.Static);
                    if (all != null && all.GetValue(null, null) is IEnumerable values)
                    {
                        foreach (object value in values)
                        {
                            if (value is Passion passion)
                            {
                                AddPassion(passion);
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    Log.Warning("[Ordered Work] Could not enumerate VSE passions: " + exception.Message);
                }
            }
            AddPassionsFromDefDatabase();
            if (registered.Count == 0)
            {
                registered.Add(new PassionInfo { Passion = Passion.None, DefName = "None", Label = "None" });
                registered.Add(new PassionInfo { Passion = Passion.Minor, DefName = "Minor", Label = "Minor" });
                registered.Add(new PassionInfo { Passion = Passion.Major, DefName = "Major", Label = "Major" });
            }
        }

        private static void AddPassion(Passion passion)
        {
            string defName = DefNameOf(passion);
            if (string.IsNullOrEmpty(defName)
                || registered.Any(p => p.DefName == defName))
            {
                return;
            }
            string label = null;
            try
            {
                label = LabelOf(passion);
            }
            catch (Exception exception)
            {
                Log.Warning("[Ordered Work] Could not read passion label for " + defName + ": " + exception.Message);
            }
            if (string.IsNullOrEmpty(label))
            {
                label = defName;
            }
            registered.Add(new PassionInfo
            {
                Passion = passion,
                DefName = defName,
                Label = label
            });
        }

        private static void AddPassionsFromDefDatabase()
        {
            Type defType = FindType("VSE.Passions.PassionDef");
            if (defType == null)
            {
                return;
            }
            try
            {
                Type databaseType = typeof(DefDatabase<>).MakeGenericType(defType);
                PropertyInfo allDefs = databaseType.GetProperty(
                    "AllDefsListForReading",
                    BindingFlags.Public | BindingFlags.Static);
                if (allDefs == null || !(allDefs.GetValue(null, null) is IEnumerable defs))
                {
                    return;
                }
                foreach (object def in defs)
                {
                    if (!TryReadPassionIndex(defType, def, out int index))
                    {
                        continue;
                    }
                    AddPassion((Passion)index);
                }
            }
            catch (Exception exception)
            {
                Log.Warning("[Ordered Work] Could not enumerate PassionDef database: " + exception.Message);
            }
        }

        private static bool TryReadPassionIndex(Type defType, object def, out int index)
        {
            index = -1;
            if (def == null)
            {
                return false;
            }
            string[] names = { "index", "Index", "passionIndex", "PassionIndex" };
            foreach (string name in names)
            {
                FieldInfo field = defType.GetField(name, BindingFlags.Public | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(int))
                {
                    index = (int)field.GetValue(def);
                    return true;
                }
                PropertyInfo property = defType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (property != null && property.PropertyType == typeof(int))
                {
                    index = (int)property.GetValue(def, null);
                    return true;
                }
            }
            return false;
        }

        private static string GetStringMember(object instance, string name)
        {
            if (instance == null)
            {
                return null;
            }
            object value = GetMember<object>(instance, name);
            if (value is string text)
            {
                return text;
            }
            if (value is TaggedString tagged)
            {
                return tagged.ToString();
            }
            return value?.ToString();
        }
        private static object GetVseDef(Passion passion)
        {
            Type managerType = FindType("VSE.Passions.PassionManager");
            if (managerType == null)
            {
                return null;
            }
            try
            {
                MethodInfo method = managerType.GetMethod("PassionToDef", BindingFlags.Public | BindingFlags.Static);
                return method != null ? method.Invoke(null, new object[] { passion }) : null;
            }
            catch
            {
                return null;
            }
        }

        private static Type FindType(string fullName)
        {
            Type type = Type.GetType(fullName + ", VSE");
            if (type != null) return type;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName);
                if (type != null) return type;
            }
            return null;
        }

        private static T GetMember<T>(object instance, string name)
        {
            if (instance == null)
            {
                return default;
            }
            Type type = instance.GetType();
            PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property != null)
            {
                return (T)property.GetValue(instance, null);
            }
            FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            return field != null ? (T)field.GetValue(instance) : default;
        }
    }
}