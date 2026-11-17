using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace OrderedWork.Services
{
    /// <summary>
    /// Read-only adapter over the passion registry exposed by VSE/Alpha Skills.
    /// The source is discovered at runtime; no third-party code or text is copied.
    /// </summary>
    public static class PassionSource
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

        public static string DescriptionOf(Passion passion)
        {
            object def = GetVseDef(passion);
            if (def == null)
            {
                return null;
            }
            string description = GetStringMember(def, "description");
            if (string.IsNullOrEmpty(description))
            {
                description = GetStringMember(def, "Description");
            }
            return string.IsNullOrEmpty(description) ? null : description;
        }

        public static bool IsBadOf(Passion passion)
        {
            object def = GetVseDef(passion);
            if (def == null)
            {
                return false;
            }
            object value = GetMember<object>(def, "isBad");
            if (value == null)
            {
                return false;
            }
            try
            {
                return Convert.ToBoolean(value);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static float LearnRateOf(Passion passion)
        {
            object def = GetVseDef(passion);
            if (def == null)
            {
                return 0f;
            }
            object value = GetMember<object>(def, "learnRateFactor");
            if (value == null)
            {
                return 0f;
            }
            try
            {
                return Convert.ToSingle(value);
            }
            catch (Exception)
            {
                return 0f;
            }
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
                if (field != null)
                {
                    index = Convert.ToInt32(field.GetValue(def));
                    return true;
                }
                PropertyInfo property = defType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (property != null)
                {
                    index = Convert.ToInt32(property.GetValue(def, null));
                    return true;
                }
            }
            return false;
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
    }
}
