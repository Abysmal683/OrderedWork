using System;
using System.IO;
using System.Text.Json;
using Verse;

namespace OrderedWork.Config
{
    /// <summary>
    /// JSON loading and atomic writing for canonical profiles. Resolution and path
    /// selection stay outside this class.
    /// </summary>
    public static class WorkConfigSerializer
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static WorkConfigProfile TryLoad(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }
                WorkConfigProfile profile = JsonSerializer.Deserialize<WorkConfigProfile>(
                    File.ReadAllText(path), JsonOptions);
                if (profile == null || profile.SchemaVersion != WorkConfigStore.CurrentSchemaVersion)
                {
                    return null;
                }
                WorkConfigProfileService.Normalize(profile);
                return profile;
            }
            catch (Exception exception)
            {
                Log.Warning("[Ordered Work] Could not read " + path + ": " + exception.Message);
                return null;
            }
        }

        public static bool SavePending(WorkConfigProfile profile)
        {
            if (profile == null)
            {
                return false;
            }
            profile.SchemaVersion = WorkConfigStore.CurrentSchemaVersion;
            WorkConfigProfileService.Normalize(profile);
            return WriteAtomic(WorkConfigPaths.PendingPath(profile.Profile), profile);
        }

        public static bool SaveProfile(WorkConfigProfile profile, bool backupExisting)
        {
            if (profile == null)
            {
                return false;
            }
            profile.SchemaVersion = WorkConfigStore.CurrentSchemaVersion;
            WorkConfigProfileService.Normalize(profile);
            string path = WorkConfigPaths.ProfilePath(profile.Profile);
            if (backupExisting && File.Exists(path))
            {
                Directory.CreateDirectory(WorkConfigPaths.BackupsDirectory);
                File.Copy(path, WorkConfigPaths.BackupPath(profile.Profile), true);
            }
            return WriteAtomic(path, profile);
        }

        public static WorkConfigProfile LoadReference()
        {
            return TryLoad(WorkConfigPaths.ReferencePath);
        }

        public static bool EnsureReference()
        {
            if (File.Exists(WorkConfigPaths.ReferencePath))
            {
                return true;
            }
            WorkConfigProfile reference = WorkConfigProfileService.BuildReferenceProfile("reference");
            return WriteAtomic(WorkConfigPaths.ReferencePath, reference);
        }

        public static bool WriteAtomic(string path, WorkConfigProfile profile)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonSerializer.Serialize(profile, JsonOptions));
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
                Log.Error("[Ordered Work] Could not write " + path + ": " + exception);
                return false;
            }
        }
    }
}
