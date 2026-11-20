using System;
using System.Collections.Generic;

namespace OrderedWork.Config
{
    /// <summary>
    /// Canonical global work configuration. Givers are keyed by defName; giverClass and
    /// priority are deliberately not identities (Core repeats classes/priorities).
    /// </summary>
    public sealed class WorkConfigProfile
    {
        public int SchemaVersion { get; set; } = WorkConfigStore.CurrentSchemaVersion;
        public string Profile { get; set; } = "default";
        public string CreatedUtc { get; set; } = DateTime.UtcNow.ToString("o");
        public List<WorkConfigMod> Mods { get; set; } = new List<WorkConfigMod>();
        public Dictionary<string, WorkTypeConfig> WorkTypes { get; set; }
            = new Dictionary<string, WorkTypeConfig>(StringComparer.Ordinal);
    }

    public sealed class WorkTypeConfig
    {
        public bool Vanilla { get; set; }
        public string? Base { get; set; }
        public string Label { get; set; } = "";
        public string ActionWord { get; set; } = "";
        public string Description { get; set; } = "";
        public bool Visible { get; set; } = true;
        public bool Hidden { get; set; }
        public string ClothingPresetId { get; set; } = "";
        public int Order { get; set; }
        public string Mod { get; set; } = "";
        public List<GiverConfig> Givers { get; set; } = new List<GiverConfig>();
    }

    public sealed class GiverConfig
    {
        public string DefName { get; set; } = "";
        public int Order { get; set; }
        public string Mod { get; set; } = "";
    }

    public sealed class WorkConfigMod
    {
        public string PackageId { get; set; } = "";
        public string Name { get; set; } = "";
    }

    public sealed class WorkConfigLoadResult
    {
        public WorkConfigProfile? Profile { get; set; }
        public string? RequestedProfile { get; set; }
        public string? ResolvedProfile { get; set; }
        public bool FellBackToDefault { get; set; }
        public bool FellBackToVanilla { get; set; }
        public string? Error { get; set; }
    }
}
