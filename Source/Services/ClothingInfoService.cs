using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.Services
{
    internal sealed class ClothingInfo
    {
        public string DefName { get; set; } = "";
        public string Label { get; set; } = "";
        public string Description { get; set; } = "";
        public string Layers { get; set; } = "";
        public string BodyParts { get; set; } = "";
        public Texture2D? Icon { get; set; }
    }

    /// <summary>
    /// Read-only clothing metadata and tooltip surface. No UI state or mutation.
    /// </summary>
    internal static class ClothingInfoService
    {
        public static ClothingInfo? Find(string? defName)
        {
            if (string.IsNullOrWhiteSpace(defName))
            {
                return null;
            }
            ThingDef? def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                return null;
            }

            ClothingInfo info = new ClothingInfo
            {
                DefName = def.defName,
                Label = def.LabelCap.ToString(),
                Description = def.description ?? "",
                Icon = def.uiIcon
            };
            if (def.apparel != null)
            {
                info.Layers = string.Join(", ", (def.apparel.layers ?? new List<ApparelLayerDef>())
                    .Where(value => value != null)
                    .Select(value => value.defName));
                info.BodyParts = string.Join(", ", (def.apparel.bodyPartGroups ?? new List<BodyPartGroupDef>())
                    .Where(value => value != null)
                    .Select(value => value.label));
            }
            return info;
        }

        public static string TooltipOf(string? defName)
        {
            ClothingInfo? info = Find(defName);
            if (info == null)
            {
                return defName ?? "";
            }
            string result = info.Label + "\n" + info.DefName;
            if (!string.IsNullOrEmpty(info.Layers))
            {
                result += "\nLayers: " + info.Layers;
            }
            if (!string.IsNullOrEmpty(info.BodyParts))
            {
                result += "\nBody: " + info.BodyParts;
            }
            if (!string.IsNullOrEmpty(info.Description))
            {
                result += "\n\n" + info.Description;
            }
            return result;
        }
    }
}