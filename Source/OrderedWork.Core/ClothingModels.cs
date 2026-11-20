using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderedWork.Core
{
    public enum ClothingRulePhase
    {
        Locked = 0,
        Emergency = 1,
        Work = 2
    }

    public enum ClothingTarget
    {
        Apparel,
        Tool
    }

    public readonly record struct ClothingSlot(string Slot, string BodyLayer, ClothingTarget Target);

    public sealed class ClothingPreset
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int SchemaVersion { get; set; } = 1;
        public List<ClothingRule>? Rules { get; set; } = new List<ClothingRule>();
    }

    public sealed class ClothingRule
    {
        public ClothingRulePhase Phase { get; set; }
        public int Priority { get; set; }
        public string? Group { get; set; }
        public ClothingTarget Target { get; set; }
        public string Slot { get; set; } = "";
        public string? BodyLayer { get; set; }
        public List<string>? Candidates { get; set; } = new List<string>();
        public bool Mandatory { get; set; }
    }

    public sealed class ClothingSelection
    {
        public ClothingRulePhase Phase { get; set; }
        public ClothingTarget Target { get; set; }
        public string Slot { get; set; } = "";
        public string? BodyLayer { get; set; }
        public string DefName { get; set; } = "";
    }

    public enum ClothingConflictReason
    {
        InvalidRule,
        NoCandidates,
        PolicyDenied,
        PolicyContradiction,
        SlotOccupied
    }

    public sealed class ClothingConflict
    {
        public ClothingRule? Rule { get; set; }
        public ClothingConflictReason Reason { get; set; }
        public string? Candidate { get; set; }
    }

    public sealed class ClothingResolution
    {
        public List<ClothingSelection> Selections { get; } = new List<ClothingSelection>();
        public List<ClothingConflict> Conflicts { get; } = new List<ClothingConflict>();
        public List<ClothingSlot> FreeSlots { get; } = new List<ClothingSlot>();
    }

    public sealed class ClothingContext
    {
        public HashSet<string> AllowedApparel { get; }
        public HashSet<string> AllowedTools { get; }
        public List<ClothingSlot> KnownSlots { get; }

        public ClothingContext(
            IEnumerable<string>? allowedApparel = null,
            IEnumerable<string>? allowedTools = null,
            IEnumerable<ClothingSlot>? knownSlots = null)
        {
            AllowedApparel = new HashSet<string>(
                (allowedApparel ?? Enumerable.Empty<string>())
                    .Where(value => !string.IsNullOrWhiteSpace(value)),
                StringComparer.Ordinal);
            AllowedTools = new HashSet<string>(
                (allowedTools ?? Enumerable.Empty<string>())
                    .Where(value => !string.IsNullOrWhiteSpace(value)),
                StringComparer.Ordinal);
            KnownSlots = (knownSlots ?? Enumerable.Empty<ClothingSlot>()).ToList();
        }
    }
}
