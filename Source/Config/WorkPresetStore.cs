using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Verse;

namespace OrderedWork.Config
{
    /// <summary>
    /// Named, reusable Work layouts. Clothing is intentionally not part of this
    /// preset yet; it will have its own denser contract later.
    /// </summary>
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

    public static class WorkPresetStore
    {
        private const int CurrentSchemaVersion = 2;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static string PresetsDirectory
        {
            get { return Path.Combine(WorkConfigStore.RootDirectory, "WorkPresets"); }
        }

        public static List<WorkPreset> List()
        {
            List<WorkPreset> result = new List<WorkPreset>();
            try
            {
                Directory.CreateDirectory(PresetsDirectory);
                foreach (string path in Directory.GetFiles(PresetsDirectory, "*.json"))
                {
                    WorkPreset preset = TryLoad(path);
                    if (preset != null)
                    {
                        result.Add(preset);
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Warning("[Ordered Work] Could not list Work presets: " + exception.Message);
            }
            return result.OrderByDescending(p => p.CreatedUtc).ToList();
        }

        public static WorkPreset TryLoad(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }
                WorkPreset preset = JsonSerializer.Deserialize<WorkPreset>(File.ReadAllText(path), JsonOptions);
                if (preset == null || preset.SchemaVersion < 1 || preset.SchemaVersion > CurrentSchemaVersion)
                {
                    return null;
                }
                Normalize(preset);
                return preset;
            }
            catch (Exception exception)
            {
                Log.Warning("[Ordered Work] Could not read Work preset " + path + ": " + exception.Message);
                return null;
            }
        }

        public static bool Save(WorkPreset preset)
        {
            if (preset == null || string.IsNullOrWhiteSpace(preset.Name))
            {
                return false;
            }
            try
            {
                Directory.CreateDirectory(PresetsDirectory);
                preset.Name = preset.Name.Trim();
                preset.SchemaVersion = CurrentSchemaVersion;
                Normalize(preset);
                string path = PathFor(preset.Name);
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonSerializer.Serialize(preset, JsonOptions));
                if (File.Exists(path))
                {
                    File.Replace(temp, path, null);
                }
                else
                {
                    File.Move(temp, path);
                }
                return true;
            }
            catch (Exception exception)
            {
                Log.Error("[Ordered Work] Could not save Work preset: " + exception);
                return false;
            }
        }

        public static bool Delete(string name)
        {
            try
            {
                string path = PathFor(name);
                if (!File.Exists(path))
                {
                    return false;
                }
                File.Delete(path);
                return true;
            }
            catch (Exception exception)
            {
                Log.Warning("[Ordered Work] Could not delete Work preset: " + exception.Message);
                return false;
            }
        }

        public static string PathFor(string name)
        {
            string safe = string.IsNullOrWhiteSpace(name) ? "preset" : name.Trim();
            char[] invalid = Path.GetInvalidFileNameChars();
            safe = new string(safe.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
            return Path.Combine(PresetsDirectory, safe + ".json");
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
