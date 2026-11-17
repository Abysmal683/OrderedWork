using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using OrderedWork.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    internal static partial class WorkTypePaletteView
    {        internal static string PassionText(Passion passion)
        {
            string vseLabel = TryGetVseLabel(passion);
            if (!string.IsNullOrEmpty(vseLabel))
            {
                return vseLabel;
            }
            if (passion == Passion.Major)
            {
                return "major";
            }
            if (passion == Passion.Minor)
            {
                return "minor";
            }
            if (passion == Passion.None)
            {
                return "none";
            }
            return passion.ToString();
        }

        /// <summary>Distinct warm tint for the vanilla monochrome passion icons.</summary>
        internal static Color PassionColor(Passion passion)
        {
            if (passion == Passion.Major)
            {
                return MajorPassionTint;
            }
            if (passion == Passion.Minor)
            {
                return MinorPassionTint;
            }
            return Color.white;
        }

        /// <summary>
        /// Resolves the icon texture/tint for a passion. Uses VSE/Alpha Skills when loaded:
        /// colored <c>PassionDef.Icon</c> first, then the gray <c>WorkBoxIcon</c> tinted by
        /// <c>PassionDef.color</c>. Without VSE it uses the vanilla colored textures, falling
        /// back to the vanilla gray work-box icons with the existing warm tints. Never draws
        /// an icon for <see cref="Passion.None"/>; never throws or spams the log.
        /// </summary>
        internal static PassionVisual ResolvePassionVisual(Passion passion)
        {
            if (passion == Passion.None)
            {
                return default(PassionVisual);
            }

            int key = ToInt(passion);
            PassionVisual cached;
            if (passionVisualCache.TryGetValue(key, out cached))
            {
                return cached;
            }

            PassionVisual visual = ComputePassionVisual(passion);
            passionVisualCache[key] = visual;
            return visual;
        }

        internal static void DrawPassionIcon(Rect rect, Passion passion)
        {
            DrawPassionIcon(rect, ResolvePassionVisual(passion));
        }

        internal static void DrawPassionIcon(Rect rect, PassionVisual visual)
        {
            if (visual.Texture == null)
            {
                return;
            }

            Color previous = GUI.color;
            GUI.color = visual.Tint;
            GUI.DrawTexture(rect, visual.Texture);
            GUI.color = previous;
        }

        private static PassionVisual ComputePassionVisual(Passion passion)
        {
            object passionDef = GetVsePassionDef(passion);
            if (passionDef != null)
            {
                Texture2D colored = GetDefTexture(iconProperty, passionDef);
                if (colored != null)
                {
                    PassionVisual result = new PassionVisual();
                    result.Texture = colored;
                    result.Tint = Color.white;
                    return result;
                }

                Texture2D workBox = GetDefTexture(workBoxIconProperty, passionDef);
                if (workBox != null)
                {
                    PassionVisual result = new PassionVisual();
                    result.Texture = workBox;
                    result.Tint = TintForPassionColor(passionDef);
                    return result;
                }
            }

            // Vanilla colored textures first, then the vanilla gray work-box icons.
            Texture2D vanillaColored = VanillaColoredTexture(passion);
            if (vanillaColored != null)
            {
                PassionVisual result = new PassionVisual();
                result.Texture = vanillaColored;
                result.Tint = Color.white;
                return result;
            }

            Texture2D vanillaGray = VanillaGrayTexture(passion);
            if (vanillaGray != null)
            {
                PassionVisual result = new PassionVisual();
                result.Texture = vanillaGray;
                result.Tint = PassionColor(passion);
                return result;
            }

            return default(PassionVisual);
        }

        private static Texture2D VanillaColoredTexture(Passion passion)
        {
            ResolveVanillaTextures();
            if (passion == Passion.Major)
            {
                return vanillaMajorColored;
            }
            if (passion == Passion.Minor)
            {
                return vanillaMinorColored;
            }
            return null;
        }

        private static Texture2D VanillaGrayTexture(Passion passion)
        {
            if (passion == Passion.Major)
            {
                return WidgetsWork.PassionWorkboxMajorIcon;
            }
            if (passion == Passion.Minor)
            {
                return WidgetsWork.PassionWorkboxMinorIcon;
            }
            return null;
        }

        private static void ResolveVanillaTextures()
        {
            if (vanillaTexturesResolved)
            {
                return;
            }
            vanillaTexturesResolved = true;
            try
            {
                vanillaMinorColored = ContentFinder<Texture2D>.Get("UI/Icons/PassionMinor", false);
                vanillaMajorColored = ContentFinder<Texture2D>.Get("UI/Icons/PassionMajor", false);
            }
            catch (Exception)
            {
                vanillaMinorColored = null;
                vanillaMajorColored = null;
            }
        }

        private static Texture2D GetDefTexture(PropertyInfo property, object passionDef)
        {
            if (property == null || passionDef == null)
            {
                return null;
            }
            try
            {
                return property.GetValue(passionDef, null) as Texture2D;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Color TintForPassionColor(object passionDef)
        {
            string colorName = PassionColorName(passionDef);
            switch (colorName)
            {
                case "Minor":
                    return MinorPassionTint;
                case "Major":
                    return MajorPassionTint;
                case "Disabled":
                    return DisabledPassionTint;
                case "Main":
                default:
                    return Color.white;
            }
        }

        private static string PassionColorName(object passionDef)
        {
            if (passionDef == null || colorField == null)
            {
                return "";
            }
            try
            {
                object value = colorField.GetValue(passionDef);
                return value != null ? value.ToString() : "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static int ColorRankOf(Passion passion)
        {
            int key = ToInt(passion);
            int cached;
            if (colorRankCache.TryGetValue(key, out cached))
            {
                return cached;
            }

            int rank = 0;
            object passionDef = GetVsePassionDef(passion);
            if (passionDef != null)
            {
                switch (PassionColorName(passionDef))
                {
                    case "Major":
                        rank = 4;
                        break;
                    case "Minor":
                        rank = 3;
                        break;
                    case "Main":
                        rank = 2;
                        break;
                    case "Disabled":
                        rank = 1;
                        break;
                }
            }

            colorRankCache[key] = rank;
            return rank;
        }

        private static string TryGetVseLabel(Passion passion)
        {
            object passionDef = GetVsePassionDef(passion);
            if (passionDef == null)
            {
                return null;
            }

            // PassionDef extends Verse.Def; the inherited LabelCap is the reliable source.
            Def def = passionDef as Def;
            if (def != null && !string.IsNullOrEmpty(def.LabelCap))
            {
                return def.LabelCap;
            }

            try
            {
                PropertyInfo property = labelCapProperty ?? passionDef.GetType().GetProperty("LabelCap");
                if (property != null)
                {
                    string labelCap = property.GetValue(passionDef, null) as string;
                    if (!string.IsNullOrEmpty(labelCap))
                    {
                        return labelCap;
                    }
                }

                FieldInfo labelField = passionDef.GetType().GetField("label", BindingFlags.Public | BindingFlags.Instance);
                string label = labelField != null ? labelField.GetValue(passionDef) as string : null;
                return string.IsNullOrEmpty(label) ? null : label.CapitalizeFirst();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static object GetVsePassionDef(Passion passion)
        {
            if (passion == Passion.None)
            {
                return null;
            }
            ResolveVseApi();
            if (!vseAvailable || passionToDefMethod == null)
            {
                return null;
            }
            try
            {
                return passionToDefMethod.Invoke(null, new object[] { passion });
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void ResolveVseApi()
        {
            if (vseResolved)
            {
                return;
            }
            vseResolved = true;

            try
            {
                Type managerType = null;
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    try
                    {
                        managerType = assemblies[i].GetType("VSE.Passions.PassionManager", false);
                    }
                    catch (Exception)
                    {
                        managerType = null;
                    }
                    if (managerType != null)
                    {
                        break;
                    }
                }

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
                        break;
                    }
                }

                Type passionDefType = managerType.Assembly.GetType("VSE.Passions.PassionDef", false);
                if (passionDefType != null)
                {
                    iconProperty = passionDefType.GetProperty("Icon", BindingFlags.Public | BindingFlags.Instance);
                    workBoxIconProperty = passionDefType.GetProperty("WorkBoxIcon", BindingFlags.Public | BindingFlags.Instance);
                    colorField = passionDefType.GetField("color", BindingFlags.Public | BindingFlags.Instance);
                    labelCapProperty = passionDefType.GetProperty("LabelCap", BindingFlags.Public | BindingFlags.Instance);
                }

                vseAvailable = passionToDefMethod != null;
            }
            catch (Exception)
            {
                vseAvailable = false;
            }
        }

        private static int ToInt(Passion passion)
        {
            try
            {
                return Convert.ToInt32(passion);
            }
            catch (Exception)
            {
                return (int)passion;
            }
        }

        private static bool vseResolved;
        private static bool vseAvailable;
        private static MethodInfo passionToDefMethod;
        private static PropertyInfo iconProperty;
        private static PropertyInfo workBoxIconProperty;
        private static PropertyInfo labelCapProperty;
        private static FieldInfo colorField;

        private static bool vanillaTexturesResolved;
        private static Texture2D vanillaMinorColored;
        private static Texture2D vanillaMajorColored;

        private static readonly Dictionary<int, PassionVisual> passionVisualCache = new Dictionary<int, PassionVisual>();
        private static readonly Dictionary<int, int> colorRankCache = new Dictionary<int, int>();
    }
}