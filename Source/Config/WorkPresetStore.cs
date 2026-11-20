using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OrderedWork.Core;
using Verse;

namespace OrderedWork.Config
{
    public sealed class WorkPreset
    {
        public int SchemaVersion { get; set; } = 2;
        public string Name { get; set; } = "";
        public string CreatedUtc { get; set; } = DateTime.UtcNow.ToString("o");
        public List<string> WorkTypes { get; set; } = new List<string>();
        public Dictionary<string, List<string>> Givers { get; set; } =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);
        public List<string> Mods { get; set; } = new List<string>();
        public Dictionary<string, int> WorkTypeMods { get; set; } =
            new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, string> WorkTypeLabels { get; set; } =
            new Dictionary<string, string>(StringComparer.Ordinal);
        public Dictionary<string, int> GiverMods { get; set; } =
            new Dictionary<string, int>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Named, reusable Work layouts. The shared atomic store owns file mechanics;
    /// this store owns the Work preset schema and name-based file policy.
    /// </summary>
    public static class WorkPresetStore
    {
        private const int CurrentSchemaVersion = 2;
        private static readonly AtomicJsonRecordStore<WorkPreset> Store =
            new AtomicJsonRecordStore<WorkPreset>(
                Path.Combine(WorkConfigStore.RootDirectory, "WorkPresets"),
                preset => preset.SchemaVersion,
                1,
                CurrentSchemaVersion,
                Normalize,
                preset =>
                {
                    preset.Name = preset.Name.Trim();
                    preset.SchemaVersion = CurrentSchemaVersion;
                },
                null,
                preset => preset.Name,
                name => AtomicJsonRecordStore<WorkPreset>.SanitizeFileName(name, "preset", true),
                preset => preset.CreatedUtc,
                true,
                message => Log.Warning(message),
                message => Log.Error(message));

        public static string PresetsDirectory
        {
            get { return Store.DirectoryPath; }
        }

        public static List<WorkPreset> List()
        {
            return Store.LoadAll();
        }

        public static WorkPreset? TryLoad(string path)
        {
            return Store.TryLoad(path);
        }

        public static bool Save(WorkPreset preset)
        {
            if (preset == null || string.IsNullOrWhiteSpace(preset.Name))
            {
                return false;
            }
            return Store.Save(preset);
        }

        public static bool Delete(string name)
        {
            return Store.Delete(name);
        }

        public static string PathFor(string name)
        {
            return Store.PathFor(name);
        }

        private static void Normalize(WorkPreset preset)
        {
            preset.WorkTypes = preset.WorkTypes ?? new List<string>();
            preset.WorkTypes = preset.WorkTypes
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            preset.Givers = preset.Givers ?? new Dictionary<string, List<string>>(StringComparer.Ordinal);
            preset.Mods = preset.Mods ?? new List<string>();
            preset.WorkTypeMods = preset.WorkTypeMods ?? new Dictionary<string, int>(StringComparer.Ordinal);
            preset.WorkTypeLabels = preset.WorkTypeLabels ?? new Dictionary<string, string>(StringComparer.Ordinal);
            preset.GiverMods = preset.GiverMods ?? new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string key in preset.Givers.Keys.ToList())
            {
                preset.Givers[key] = (preset.Givers[key] ?? new List<string>())
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
            }
        }
    }
}
