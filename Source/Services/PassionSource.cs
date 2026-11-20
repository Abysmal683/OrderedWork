using System.Collections.Generic;
using RimWorld;
using UnityEngine;

namespace OrderedWork.Services
{
    /// <summary>
    /// Public compatibility facade for passion discovery, metadata and icons.
    /// </summary>
    public static class PassionSource
    {
        public sealed class PassionInfo
        {
            public Passion Passion;
            public string DefName = "";
            public string Label = "";
        }

        public static List<PassionInfo> Registered
        {
            get { return PassionRegistry.Registered; }
        }

        public static string DefNameOf(Passion passion)
        {
            return PassionMetadata.DefNameOf(passion);
        }

        public static string LabelOf(Passion passion)
        {
            return PassionMetadata.LabelOf(passion);
        }

        public static string? VseLabelOf(Passion passion)
        {
            return PassionMetadata.VseLabelOf(passion);
        }

        public static Texture2D? IconOf(Passion passion)
        {
            return PassionIconResolver.IconOf(passion);
        }

        public static Texture2D? WorkBoxIconOf(Passion passion)
        {
            return PassionIconResolver.WorkBoxIconOf(passion);
        }

        public static string? ColorNameOf(Passion passion)
        {
            return PassionMetadata.ColorNameOf(passion);
        }

        public static string? DescriptionOf(Passion passion)
        {
            return PassionMetadata.DescriptionOf(passion);
        }

        public static bool IsBadOf(Passion passion)
        {
            return PassionMetadata.IsBadOf(passion);
        }

        public static float LearnRateOf(Passion passion)
        {
            return PassionMetadata.LearnRateOf(passion);
        }
    }
}
