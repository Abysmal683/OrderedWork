using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Verse;

namespace OrderedWork.Config
{
    /// <summary>
    /// One JSON file per named schedule preset. One file per record keeps a corrupt
    /// preset from taking down the whole schedule library.
    /// </summary>
    public static class SchedulePresetStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static string DirectoryPath
        {
            get { return Path.Combine(WorkConfigStore.RootDirectory, "schedules"); }
        }

        public static string PathFor(string id)
        {
            return Path.Combine(DirectoryPath, SanitizeId(id) + ".json");
        }

        public static List<SchedulePreset> LoadAll()
        {
            List<SchedulePreset> result = new List<SchedulePreset>();
            try
            {
                Directory.CreateDirectory(DirectoryPath);
                foreach (string path in Directory.GetFiles(DirectoryPath, "*.json"))
                {
                    SchedulePreset preset = TryLoad(path);
                    if (preset != null)
                    {
                        result.Add(preset);
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Warning("[Ordered Work] Could not list schedules: " + exception.Message);
            }
            return result.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static SchedulePreset TryLoad(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }
                SchedulePreset preset = JsonSerializer.Deserialize<SchedulePreset>(
                    File.ReadAllText(path), JsonOptions);
                if (preset == null || preset.SchemaVersion != SchedulePreset.CurrentSchemaVersion)
                {
                    Log.Warning("[Ordered Work] Ignoring schedule with unsupported schema: " + path);
                    return null;
                }
                preset.Normalize();
                return preset;
            }
            catch (Exception exception)
            {
                Log.Warning("[Ordered Work] Could not read schedule " + path + ": " + exception.Message);
                return null;
            }
        }

        public static bool Save(SchedulePreset preset)
        {
            if (preset == null)
            {
                return false;
            }
            try
            {
                preset.SchemaVersion = SchedulePreset.CurrentSchemaVersion;
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
                Log.Error("[Ordered Work] Could not save schedule: " + exception);
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
                Log.Warning("[Ordered Work] Could not delete schedule: " + exception.Message);
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
