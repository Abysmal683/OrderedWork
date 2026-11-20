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
        private static WorkConfigLoadResult cachedActive;
        private static WorkConfigProfile cachedEditing;
        private static string cachedEditingName;
        private static bool cacheValid;
        private static string cachedSelectedProfile;
        private static bool selectedProfileCached;

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

        /// <summary>Cached active profile for read-only UI/tooltip consumers.</summary>
        public static WorkConfigLoadResult LoadCachedActive()
        {
            if (!cacheValid || cachedActive == null)
            {
                cachedActive = WorkConfigResolver.LoadActiveWithFallback(LoadSelectedProfileName());
                cacheValid = true;
            }
            return cachedActive;
        }

        public static WorkConfigProfile LoadEditingProfile(string selected)
        {
            if (cacheValid && cachedEditing != null && cachedEditingName == selected)
            {
                return cachedEditing;
            }
            cachedEditing = WorkConfigResolver.LoadEditingProfile(selected);
            cachedEditingName = selected;
            cacheValid = true;
            return cachedEditing;
        }

        public static void InvalidateCache()
        {
            cacheValid = false;
            cachedActive = null;
            cachedEditing = null;
            cachedEditingName = null;
            cachedSelectedProfile = null;
            selectedProfileCached = false;
        }

        public static WorkConfigProfile LoadPending(string profile)
        {
            return WorkConfigResolver.LoadPending(profile);
        }

        public static bool PromotePending(string profile)
        {
            bool result = WorkConfigResolver.PromotePending(profile);
            InvalidateCache();
            return result;
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
            bool result = WorkConfigSerializer.SavePending(profile);
            InvalidateCache();
            return result;
        }

        public static bool SaveProfile(WorkConfigProfile profile, bool backupExisting)
        {
            bool result = WorkConfigSerializer.SaveProfile(profile, backupExisting);
            InvalidateCache();
            return result;
        }

        public static WorkConfigProfile LoadReference()
        {
            return WorkConfigSerializer.LoadReference();
        }

        public static bool EnsureReference()
        {
            bool result = WorkConfigSerializer.EnsureReference();
            InvalidateCache();
            return result;
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
            if (!selectedProfileCached)
            {
                cachedSelectedProfile = WorkConfigResolver.LoadSelectedProfileName();
                selectedProfileCached = true;
            }
            return cachedSelectedProfile;
        }

        public static void SaveSelectedProfileName(string profile)
        {
            WorkConfigResolver.SaveSelectedProfileName(profile);
            InvalidateCache();
            cachedSelectedProfile = profile;
            selectedProfileCached = true;
        }
    }
}
