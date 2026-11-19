using System;
using System.IO;
using System.Linq;
using RimWorld;
using Verse;

namespace OrderedWork.Config
{
    /// <summary>
    /// Filesystem layout for the global configuration. No loading or writing belongs here.
    /// </summary>
    public static class WorkConfigPaths
    {
        public const string ActiveProfileFile = "active-profile.txt";
        public const string ReferenceFile = "reference.json";

        public static string RootDirectory
        {
            get { return Path.Combine(GenFilePaths.ConfigFolderPath, "OrderedWork"); }
        }

        public static string ProfilesDirectory
        {
            get { return Path.Combine(RootDirectory, "profiles"); }
        }

        public static string BackupsDirectory
        {
            get { return Path.Combine(RootDirectory, "backups"); }
        }

        public static string ActiveProfilePath
        {
            get { return Path.Combine(RootDirectory, ActiveProfileFile); }
        }

        public static string ReferencePath
        {
            get { return Path.Combine(RootDirectory, ReferenceFile); }
        }

        public static string ProfilePath(string profile)
        {
            return Path.Combine(ProfilesDirectory, SanitizeProfile(profile) + ".json");
        }

        public static string PendingPath(string profile)
        {
            return Path.Combine(ProfilesDirectory, SanitizeProfile(profile) + ".pending.json");
        }

        public static string BackupPath(string profile)
        {
            return Path.Combine(BackupsDirectory,
                SanitizeProfile(profile) + "." + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".json");
        }

        public static string SanitizeProfile(string name)
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
