using System.Collections.Generic;
using RimWorld;
using Verse;

namespace OrderedWork.Config
{
    /// <summary>
    /// Compatibility facade for the global configuration. Paths, serialization and
    /// profile resolution live in their respective services.
    /// </summary>
    public static class WorkConfigStore
    {
        public const int CurrentSchemaVersion = 3;

        public static string RootDirectory
        {
            get { return WorkConfigPaths.RootDirectory; }
        }

        public static string ProfilesDirectory
        {
            get { return WorkConfigPaths.ProfilesDirectory; }
        }

        public static string ReferencePath
        {
            get { return WorkConfigPaths.ReferencePath; }
        }

        public static string ProfilePath(string profile)
        {
            return WorkConfigPaths.ProfilePath(profile);
        }

        public static string PendingPath(string profile)
        {
            return WorkConfigPaths.PendingPath(profile);
        }

        public static WorkConfigLoadResult LoadActiveWithFallback(string requested)
        {
            return WorkConfigResolver.LoadActiveWithFallback(requested);
        }

        public static WorkConfigProfile LoadEditingProfile(string selected)
        {
            return WorkConfigResolver.LoadEditingProfile(selected);
        }

        public static WorkConfigProfile LoadPending(string profile)
        {
            return WorkConfigResolver.LoadPending(profile);
        }

        public static bool PromotePending(string profile)
        {
            return WorkConfigResolver.PromotePending(profile);
        }

        public static List<WorkTypeDef> GetOrderedVisibleWorkTypes(WorkConfigProfile profile)
        {
            return WorkConfigProfileService.GetOrderedVisibleWorkTypes(profile);
        }

        public static WorkConfigProfile TryLoad(string path)
        {
            return WorkConfigSerializer.TryLoad(path);
        }

        public static bool SavePending(WorkConfigProfile profile)
        {
            return WorkConfigSerializer.SavePending(profile);
        }

        public static bool SaveProfile(WorkConfigProfile profile, bool backupExisting)
        {
            return WorkConfigSerializer.SaveProfile(profile, backupExisting);
        }

        public static WorkConfigProfile LoadReference()
        {
            return WorkConfigSerializer.LoadReference();
        }

        public static bool EnsureReference()
        {
            return WorkConfigSerializer.EnsureReference();
        }

        public static WorkConfigProfile BuildReferenceProfile(string name)
        {
            return WorkConfigProfileService.BuildReferenceProfile(name);
        }

        public static void Normalize(WorkConfigProfile profile)
        {
            WorkConfigProfileService.Normalize(profile);
        }

        public static string LoadSelectedProfileName()
        {
            return WorkConfigResolver.LoadSelectedProfileName();
        }

        public static void SaveSelectedProfileName(string profile)
        {
            WorkConfigResolver.SaveSelectedProfileName(profile);
        }
    }
}
