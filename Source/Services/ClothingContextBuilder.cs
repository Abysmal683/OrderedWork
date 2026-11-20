using System.Collections.Generic;
using System.Linq;
using OrderedWork.Core;
using RimWorld;
using Verse;

namespace OrderedWork.Services
{
    /// <summary>
    /// Builds the pure resolver context from live RimWorld defs. This is a read-only
    /// adapter: it does not equip, pathfind or mutate the pawn.
    /// </summary>
    internal static class ClothingContextBuilder
    {
        public static ClothingContext Build(Pawn? pawn, ClothingPreset? preset)
        {
            ApparelPolicy? policy = pawn?.outfits?.CurrentApparelPolicy;
            HashSet<string> apparel = new HashSet<string>(System.StringComparer.Ordinal);
            HashSet<string> tools = new HashSet<string>(System.StringComparer.Ordinal);
            HashSet<string> slotKeys = new HashSet<string>(System.StringComparer.Ordinal);
            List<ClothingSlot> slots = new List<ClothingSlot>();

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def == null)
                {
                    continue;
                }

                if (def.IsApparel)
                {
                    if (policy == null || policy.filter == null || policy.filter.Allows(def))
                    {
                        apparel.Add(def.defName);
                    }
                    if (def.apparel != null)
                    {
                        foreach (BodyPartGroupDef group in def.apparel.bodyPartGroups ?? new List<BodyPartGroupDef>())
                        {
                            foreach (ApparelLayerDef layer in def.apparel.layers ?? new List<ApparelLayerDef>())
                            {
                                AddSlot(slots, slotKeys, group.defName, layer.defName, ClothingTarget.Apparel);
                            }
                        }
                    }
                }
                else if (def.HasComp(typeof(CompEquippable)))
                {
                    tools.Add(def.defName);
                }
            }

            if (preset != null && preset.Rules != null)
            {
                foreach (ClothingRule rule in preset.Rules)
                {
                    if (rule != null && rule.Target == ClothingTarget.Tool && !string.IsNullOrWhiteSpace(rule.Slot))
                    {
                        AddSlot(slots, slotKeys, rule.Slot, "", ClothingTarget.Tool);
                    }
                }
            }

            return new ClothingContext(apparel, tools, slots);
        }

        private static void AddSlot(
            List<ClothingSlot> slots,
            HashSet<string> keys,
            string slot,
            string bodyLayer,
            ClothingTarget target)
        {
            string key = target == ClothingTarget.Tool
                ? "Tool|" + slot
                : "Apparel|" + slot + "|" + bodyLayer;
            if (keys.Add(key))
            {
                slots.Add(new ClothingSlot(slot, bodyLayer, target));
            }
        }
    }
}
