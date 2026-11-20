#nullable disable
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
    /// Optional VSE/Alpha Skills discovery. This class owns reflection and the
    /// registered passion list; it does not own UI labels, icons or weights.
    /// </summary>
    public static class PassionRegistry
    {
        private static bool initialized;
        private static List<PassionSource.PassionInfo> registered;
        private static bool vseApiResolved;
        private static MethodInfo passionToDefMethod;

        public static List<PassionSource.PassionInfo> Registered
        {
            get
            {
                EnsureInitialized();
                return registered;
            }
        }

        internal static object GetVseDef(Passion passion)
        {
            ResolveVseApi();
            if (passionToDefMethod == null)
            {
                return null;
            }
            try
            {
                return passionToDefMethod.Invoke(null, new object[] { passion });
            }
            catch
            {
                return null;
            }
        }

        internal static T GetMember<T>(object instance, string name)
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

        internal static string GetStringMember(object instance, string name)
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

        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;
            registered = new List<PassionSource.PassionInfo>();
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
                registered.Add(new PassionSource.PassionInfo { Passion = Passion.None, DefName = "None", Label = "None" });
                registered.Add(new PassionSource.PassionInfo { Passion = Passion.Minor, DefName = "Minor", Label = "Minor" });
                registered.Add(new PassionSource.PassionInfo { Passion = Passion.Major, DefName = "Major", Label = "Major" });
            }
            OrderedWorkMod.DebugLog("[Passion] VSE manager=" + (managerType != null)
                + " registered=" + registered.Count
                + " names=" + string.Join(",", registered.Select(info => info.DefName)), OrderedWorkLogCategory.Passion);
        }

        private static void AddPassion(Passion passion)
        {
            string defName = PassionMetadata.DefNameOf(passion);
            if (string.IsNullOrEmpty(defName)
                || registered.Any(p => p.DefName == defName))
            {
                return;
            }
            string label = null;
            try
            {
                label = PassionMetadata.LabelOf(passion);
            }
            catch (Exception exception)
            {
                Log.Warning("[Ordered Work] Could not read passion label for " + defName + ": " + exception.Message);
            }
            if (string.IsNullOrEmpty(label))
            {
                label = defName;
            }
            registered.Add(new PassionSource.PassionInfo
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

        private static void ResolveVseApi()
        {
            if (vseApiResolved)
            {
                return;
            }
            vseApiResolved = true;
            Type managerType = FindType("VSE.Passions.PassionManager");
            if (managerType == null)
            {
                return;
            }
            MethodInfo[] methods = managerType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == "PassionToDef" && methods[i].GetParameters().Length == 1)
                {
                    passionToDefMethod = methods[i];
                    return;
                }
            }
        }

        private static Type FindType(string fullName)
        {
            Type type = Type.GetType(fullName + ", VSE");
            if (type != null)
            {
                return type;
            }
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }
    }
}
