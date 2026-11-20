using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderedWork.Core
{
    /// <summary>
    /// Pure clothing-stack selection. It does not equip, pathfind or mutate a pawn.
    /// ApparelPolicy is represented by the allowed sets in <see cref="ClothingContext"/>.
    /// </summary>
    public static class ClothingResolver
    {
        public static ClothingResolution Resolve(ClothingPreset? preset, ClothingContext? context)
        {
            ClothingResolution result = new ClothingResolution();
            if (preset == null || preset.Rules == null || context == null)
            {
                return result;
            }

            Dictionary<string, ClothingSelection> occupied = new Dictionary<string, ClothingSelection>(StringComparer.Ordinal);
            List<ClothingRule> ordered = preset.Rules
                .Where(rule => rule != null)
                .OrderBy(rule => rule.Phase)
                .ThenBy(rule => rule.Priority)
                .ToList();

            foreach (ClothingRule rule in ordered)
            {
                if (string.IsNullOrWhiteSpace(rule.Slot))
                {
                    result.Conflicts.Add(new ClothingConflict { Rule = rule, Reason = ClothingConflictReason.InvalidRule });
                    continue;
                }
                if (rule.Target == ClothingTarget.Apparel && string.IsNullOrWhiteSpace(rule.BodyLayer))
                {
                    result.Conflicts.Add(new ClothingConflict { Rule = rule, Reason = ClothingConflictReason.InvalidRule });
                    continue;
                }

                HashSet<string> allowed = rule.Target == ClothingTarget.Apparel
                    ? context.AllowedApparel
                    : context.AllowedTools;
                List<string> candidates = (rule.Candidates ?? new List<string>())
                    .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
                    .ToList();
                if (candidates.Count == 0)
                {
                    result.Conflicts.Add(new ClothingConflict { Rule = rule, Reason = ClothingConflictReason.NoCandidates });
                    continue;
                }

                string? selected = candidates.FirstOrDefault(allowed.Contains);
                if (selected == null)
                {
                    result.Conflicts.Add(new ClothingConflict
                    {
                        Rule = rule,
                        Reason = rule.Mandatory
                            ? ClothingConflictReason.PolicyContradiction
                            : ClothingConflictReason.PolicyDenied,
                        Candidate = candidates[0]
                    });
                    continue;
                }

                string key = SlotKey(rule.Target, rule.Slot, rule.BodyLayer);
                if (occupied.ContainsKey(key))
                {
                    result.Conflicts.Add(new ClothingConflict
                    {
                        Rule = rule,
                        Reason = ClothingConflictReason.SlotOccupied,
                        Candidate = selected
                    });
                    continue;
                }

                ClothingSelection selection = new ClothingSelection
                {
                    Phase = rule.Phase,
                    Target = rule.Target,
                    Slot = rule.Slot,
                    BodyLayer = rule.BodyLayer,
                    DefName = selected
                };
                occupied[key] = selection;
                result.Selections.Add(selection);
            }

            foreach (ClothingSlot slot in context.KnownSlots)
            {
                if (slot.Slot == null)
                {
                    continue;
                }
                string key = SlotKey(slot.Target, slot.Slot, slot.BodyLayer);
                if (!occupied.ContainsKey(key))
                {
                    result.FreeSlots.Add(slot);
                }
            }
            return result;
        }

        private static string SlotKey(ClothingTarget target, string slot, string? bodyLayer)
        {
            return target == ClothingTarget.Tool
                ? "Tool|" + slot
                : "Apparel|" + slot + "|" + (bodyLayer ?? "");
        }
    }
}
