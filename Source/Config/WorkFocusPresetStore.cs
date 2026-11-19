using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Verse;

namespace OrderedWork.Config
{
    /// <summary>
    /// One JSON file per named hourly focus preset. Kept independent from schedule
    /// presets: a broken or missing schedule never affects focus, and vice versa.
    /// </summary>
    public static class WorkFocusPresetStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static string DirectoryPath
        {
            get { return Path.Combine(WorkConfigStore.RootDirectory, "focus"); }
        }

        public static string PathFor(string id)
        {
            return Path.Combine(DirectoryPath, SanitizeId(id) + ".json");
        }

        public static List<WorkFocusPreset> LoadAll()
        {
            List<WorkFocusPreset> result = new List<WorkFocusPreset>();
            try
            {
                Directory.CreateDirectory(DirectoryPath);
                foreach (string path in Directory.GetFiles(DirectoryPath, "*.json"))
                {
                    WorkFocusPreset preset = TryLoad(path);
                    if (preset != null)
                    {
                        result.Add(preset);
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Warning("[Ordered Work] Could not list focus presets: " + exception.Message);
            }
            return result.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static WorkFocusPreset TryLoadById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }
            return TryLoad(PathFor(id));
        }

        public static WorkFocusPreset TryLoad(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }
                WorkFocusPreset preset = JsonSerializer.Deserialize<WorkFocusPreset>(
                    File.ReadAllText(path), JsonOptions);
                if (preset == null || preset.SchemaVersion < 1
                    || preset.SchemaVersion > WorkFocusPreset.CurrentSchemaVersion)
                {
                    Log.Warning("[Ordered Work] Ignoring focus preset with unsupported schema: " + path);
                    return null;
                }
                preset.Normalize();
                if (string.IsNullOrEmpty(preset.ModifiedUtc))
                {
                    preset.ModifiedUtc = File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm");
                }
                return preset;
            }
            catch (Exception exception)
            {
                Log.Warning("[Ordered Work] Could not read focus preset " + path + ": " + exception.Message);
                return null;
            }
        }

        public static bool Save(WorkFocusPreset preset)
        {
            if (preset == null)
            {
                return false;
            }
            try
            {
                preset.SchemaVersion = WorkFocusPreset.CurrentSchemaVersion;
                preset.ModifiedUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
                preset.Normalize();
                Directory.CreateDirectory(DirectoryPath);
                string path = PathFor(preset.Id);
                if (File.Exists(path))
                {
                    File.Copy(path, path + ".bak", true);
                }
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
                Log.Error("[Ordered Work] Could not save focus preset: " + exception);
                return false;
            }
        }

        public static bool Delete(string id)
        {
            try
            {
                string path = PathFor(id);
                if (!File.Exists(path))
                {
                    return false;
                }
                if (File.Exists(path + ".bak"))
                {
                    File.Delete(path + ".bak");
                }
                File.Delete(path);
                return true;
            }
            catch (Exception exception)
            {
                Log.Warning("[Ordered Work] Could not delete focus preset: " + exception.Message);
                return false;
            }
        }

        private static string SanitizeId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Guid.NewGuid().ToString("N");
            }
            char[] invalid = Path.GetInvalidFileNameChars();
            string clean = new string(id.Where(c => !invalid.Contains(c) && c != '.' && c != '\\' && c != '/').ToArray());
            return string.IsNullOrEmpty(clean) ? Guid.NewGuid().ToString("N") : clean;
        }
    }
}
