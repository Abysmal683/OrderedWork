using System;
using System.Collections.Generic;
using OrderedWork.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    internal static partial class WorkTypePaletteView
    {        internal static string? PassionText(Passion passion)
        {
            string? vseLabel = PassionSource.VseLabelOf(passion);
            if (!string.IsNullOrEmpty(vseLabel))
            {
                return vseLabel;
            }
            if (passion == Passion.Major)
            {
                return OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Passion.Major", "major");
            }
            if (passion == Passion.Minor)
            {
                return OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Passion.Minor", "minor");
            }
            if (passion == Passion.None)
            {
                return OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Passion.None", "none");
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
            Texture2D? colored = PassionSource.IconOf(passion);
            if (colored != null)
            {
                PassionVisual result = new PassionVisual();
                result.Texture = colored;
                result.Tint = Color.white;
                return result;
            }

            Texture2D? workBox = PassionSource.WorkBoxIconOf(passion);
            if (workBox != null)
            {
                PassionVisual result = new PassionVisual();
                result.Texture = workBox;
                result.Tint = TintForPassionColor(passion);
                return result;
            }

            // Vanilla colored textures first, then the vanilla gray work-box icons.
            Texture2D? vanillaColored = VanillaColoredTexture(passion);
            if (vanillaColored != null)
            {
                PassionVisual result = new PassionVisual();
                result.Texture = vanillaColored;
                result.Tint = Color.white;
                return result;
            }

            Texture2D? vanillaGray = VanillaGrayTexture(passion);
            if (vanillaGray != null)
            {
                PassionVisual result = new PassionVisual();
                result.Texture = vanillaGray;
                result.Tint = PassionColor(passion);
                return result;
            }

            return default(PassionVisual);
        }

        private static Texture2D? VanillaColoredTexture(Passion passion)
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

        private static Texture2D? VanillaGrayTexture(Passion passion)
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

        private static Color TintForPassionColor(Passion passion)
        {
            string? colorName = PassionSource.ColorNameOf(passion);
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

        private static int ColorRankOf(Passion passion)
        {
            int key = ToInt(passion);
            int cached;
            if (colorRankCache.TryGetValue(key, out cached))
            {
                return cached;
            }

            int rank = 0;
            switch (PassionSource.ColorNameOf(passion))
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

            colorRankCache[key] = rank;
            return rank;
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

        private static bool vanillaTexturesResolved;
        private static Texture2D? vanillaMinorColored;
        private static Texture2D? vanillaMajorColored;

        private static readonly Dictionary<int, PassionVisual> passionVisualCache = new Dictionary<int, PassionVisual>();
        private static readonly Dictionary<int, int> colorRankCache = new Dictionary<int, int>();
    }
}