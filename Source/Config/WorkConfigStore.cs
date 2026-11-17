using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RimWorld;
using Verse;

namespace OrderedWork.Config
{
    /// <summary>
    /// File layout for the global configuration. This is intentionally independent from the
    /// colony save: pending edits never mutate live defs until the next RimWorld startup.
    /// </summary>
    public static class WorkConfigStore
    {
        public const int CurrentSchemaVersion = 3;
        private const string ActiveProfileFile = "active-profile.txt";
        private const string ReferenceFile = "reference.json";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static string RootDirectory
        {
            get { return Path.Combine(GenFilePaths.ConfigFolderPath, "OrderedWork"); }
        }

        public static string ProfilesDirectory
        {
            get { return Path.Combine(RootDirectory, "profiles"); }
        }

        public static string ProfilePath(string profile)
        {
            return Path.Combine(ProfilesDirectory, SanitizeProfile(profile) + ".json");
        }

        public static string PendingPath(string profile)
        {
            return Path.Combine(ProfilesDirectory, SanitizeProfile(profile) + ".pending.json");
        }

        public static string ReferencePath
        {
            get { return Path.Combine(RootDirectory, ReferenceFile); }
        }

        public static WorkConfigLoadResult LoadActiveWithFallback(string requested)
        {
            Directory.CreateDirectory(ProfilesDirectory);
            EnsureReference();

            string profile = SanitizeProfile(requested);
            WorkConfigProfile loaded = TryLoad(ProfilePath(profile));
            if (loaded != null)
            {
                return new WorkConfigLoadResult
                {
                    Profile = loaded,
                    RequestedProfile = profile,
                    ResolvedProfile = profile
                };
            }

            loaded = TryLoad(ProfilePath("default"));
            if (loaded != null)
            {
                return new WorkConfigLoadResult
                {
                    Profile = loaded,
                    RequestedProfile = profile,
                    ResolvedProfile = "default",
                    FellBackToDefault = profile != "default"
                };
            }

            WorkConfigProfile vanilla = BuildReferenceProfile("vanilla");
            SaveProfile(vanilla, false);
            return new WorkConfigLoadResult
            {
                Profile = vanilla,
                RequestedProfile = profile,
                ResolvedProfile = "vanilla",
                FellBackToDefault = profile != "default",
                FellBackToVanilla = true
            };
        }

        public static WorkConfigProfile TryLoad(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }
                string json = File.ReadAllText(path);
                WorkConfigProfile profile = JsonSerializer.Deserialize<WorkConfigProfile>(json, JsonOptions);
                if (profile == null || profile.SchemaVersion != CurrentSchemaVersion)
                {
                    return null;
                }
                Normalize(profile);
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
            profile.SchemaVersion = CurrentSchemaVersion;
            Normalize(profile);
            return WriteAtomic(PendingPath(profile.Profile), profile);
        }

        public static bool PromotePending(string profile)
        {
            string pending = PendingPath(profile);
            if (!File.Exists(pending))
            {
                return false;
            }
            WorkConfigProfile loaded = TryLoad(pending);
            if (loaded == null)
            {
                Log.Warning("[Ordered Work] Pending profile failed validation; keeping active profile.");
                return false;
            }
            loaded.Profile = SanitizeProfile(profile);
            return SaveProfile(loaded, true);
        }

        public static bool SaveProfile(WorkConfigProfile profile, bool backupExisting)
        {
            if (profile == null)
            {
                return false;
            }
            profile.SchemaVersion = CurrentSchemaVersion;
            Normalize(profile);
            string path = ProfilePath(profile.Profile);
            if (backupExisting && File.Exists(path))
            {
                string backupDir = Path.Combine(RootDirectory, "backups");
                Directory.CreateDirectory(backupDir);
                File.Copy(path, Path.Combine(backupDir,
                    profile.Profile + "." + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".json"), true);
            }
            return WriteAtomic(path, profile);
        }

        public static WorkConfigProfile LoadReference()
        {
            return TryLoad(ReferencePath);
        }

        public static bool EnsureReference()
        {
            if (File.Exists(ReferencePath))
            {
                return true;
            }
            WorkConfigProfile reference = BuildReferenceProfile("reference");
            return WriteAtomic(ReferencePath, reference);
        }

        public static WorkConfigProfile BuildReferenceProfile(string name)
        {
            WorkConfigProfile profile = new WorkConfigProfile { Profile = name };
            HashSet<string> mods = new HashSet<string>(StringComparer.Ordinal);
            int order = 0;
            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading
                .Where(w => w != null && w.visible)
                .OrderByDescending(w => w.naturalPriority))
            {
                WorkTypeConfig wt = new WorkTypeConfig
                {
                    Vanilla = true,
                    Base = null,
                    Label = workType.labelShort,
                    Description = workType.description,
                    Visible = workType.visible,
                    Order = order++,
                    Mod = workType.modContentPack != null ? workType.modContentPack.PackageId : "unknown"
                };
                int giverOrder = 0;
                foreach (WorkGiverDef giver in DefDatabase<WorkGiverDef>.AllDefsListForReading
                    .Where(g => g != null && g.workType == workType)
                    .OrderByDescending(g => g.priorityInType))
                {
                    string packageId = giver.modContentPack != null ? giver.modContentPack.PackageId : "unknown";
                    wt.Givers.Add(new GiverConfig
                    {
                        DefName = giver.defName,
                        Order = giverOrder++,
                        Mod = packageId
                    });
                    mods.Add(packageId);
                }
                profile.WorkTypes[workType.defName] = wt;
                mods.Add(wt.Mod);
            }
            foreach (string packageId in mods)
            {
                profile.Mods.Add(new WorkConfigMod { PackageId = packageId, Name = packageId });
            }
            return profile;
        }

        public static void Normalize(WorkConfigProfile profile)
        {
            if (profile.WorkTypes == null)
            {
                profile.WorkTypes = new Dictionary<string, WorkTypeConfig>(StringComparer.Ordinal);
            }
            if (profile.Mods == null)
            {
                profile.Mods = new List<WorkConfigMod>();
            }
            foreach (WorkTypeConfig workType in profile.WorkTypes.Values)
            {
                if (workType.Givers == null)
                {
                    workType.Givers = new List<GiverConfig>();
                }
                // defName is the only identity. Repeated giverClass/priority is intentional.
                workType.Givers = workType.Givers
                    .Where(g => g != null && !string.IsNullOrEmpty(g.DefName))
                    .GroupBy(g => g.DefName, StringComparer.Ordinal)
                    .Select(g => g.First())
                    .ToList();
            }
        }

        public static string LoadSelectedProfileName()
        {
            try
            {
                string path = Path.Combine(RootDirectory, ActiveProfileFile);
                return File.Exists(path) ? SanitizeProfile(File.ReadAllText(path).Trim()) : "default";
            }
            catch
            {
                return "default";
            }
        }

        public static void SaveSelectedProfileName(string profile)
        {
            try
            {
                Directory.CreateDirectory(RootDirectory);
                File.WriteAllText(Path.Combine(RootDirectory, ActiveProfileFile), SanitizeProfile(profile));
            }
            catch (Exception exception)
            {
                Log.Warning("[Ordered Work] Could not save active profile: " + exception.Message);
            }
        }

        private static bool WriteAtomic(string path, WorkConfigProfile profile)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temp = path + ".tmp";
                string json = JsonSerializer.Serialize(profile, JsonOptions);
                File.WriteAllText(temp, json);
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

        private static string SanitizeProfile(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "default";
            }
            char[] invalid = Path.GetInvalidFileNameChars();
            return new string(name.Trim().Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        }
    }
}
