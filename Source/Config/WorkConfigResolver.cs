#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Verse;

namespace OrderedWork.Config
{
    /// <summary>
    /// Profile selection and fallback. It chooses which profile is active/editing;
    /// serializer owns the bytes and paths own the layout.
    /// </summary>
    public static class WorkConfigResolver
    {
        public static WorkConfigLoadResult LoadActiveWithFallback(string requested)
        {
            Directory.CreateDirectory(WorkConfigPaths.ProfilesDirectory);
            WorkConfigSerializer.EnsureReference();

            string profile = WorkConfigPaths.SanitizeProfile(requested);
            WorkConfigProfile loaded = WorkConfigSerializer.TryLoad(WorkConfigPaths.ProfilePath(profile));
            if (loaded != null)
            {
                WorkConfigProfileService.ReconcileWithLive(loaded);
                return new WorkConfigLoadResult
                {
                    Profile = loaded,
                    RequestedProfile = profile,
                    ResolvedProfile = profile
                };
            }

            loaded = WorkConfigSerializer.TryLoad(WorkConfigPaths.ProfilePath("default"));
            if (loaded != null)
            {
                WorkConfigProfileService.ReconcileWithLive(loaded);
                return new WorkConfigLoadResult
                {
                    Profile = loaded,
                    RequestedProfile = profile,
                    ResolvedProfile = "default",
                    FellBackToDefault = profile != "default"
                };
            }

            WorkConfigProfile vanilla = WorkConfigProfileService.BuildReferenceProfile("vanilla");
            WorkConfigSerializer.SaveProfile(vanilla, false);
            return new WorkConfigLoadResult
            {
                Profile = vanilla,
                RequestedProfile = profile,
                ResolvedProfile = "vanilla",
                FellBackToDefault = profile != "default",
                FellBackToVanilla = true
            };
        }

        public static WorkConfigProfile LoadEditingProfile(string selected)
        {
            WorkConfigProfile pending = LoadPending(selected);
            if (pending != null)
            {
                WorkConfigProfileService.ReconcileWithLive(pending);
                return pending;
            }
            WorkConfigLoadResult active = LoadActiveWithFallback(selected);
            return active != null ? active.Profile : null;
        }

        public static WorkConfigProfile LoadPending(string profile)
        {
            return WorkConfigSerializer.TryLoad(WorkConfigPaths.PendingPath(profile));
        }

        public static bool PromotePending(string profile)
        {
            string pending = WorkConfigPaths.PendingPath(profile);
            if (!File.Exists(pending))
            {
                return false;
            }
            WorkConfigProfile loaded = WorkConfigSerializer.TryLoad(pending);
            if (loaded == null)
            {
                Log.Warning("[Ordered Work] Pending profile failed validation; keeping active profile.");
                return false;
            }
            loaded.Profile = WorkConfigPaths.SanitizeProfile(profile);
            return WorkConfigSerializer.SaveProfile(loaded, true);
        }

        public static string LoadSelectedProfileName()
        {
            try
            {
                string path = WorkConfigPaths.ActiveProfilePath;
                return File.Exists(path)
                    ? WorkConfigPaths.SanitizeProfile(File.ReadAllText(path).Trim())
                    : "default";
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
                Directory.CreateDirectory(WorkConfigPaths.RootDirectory);
                File.WriteAllText(WorkConfigPaths.ActiveProfilePath, WorkConfigPaths.SanitizeProfile(profile));
            }
            catch (Exception exception)
            {
                Log.Warning("[Ordered Work] Could not save active profile: " + exception.Message);
            }
        }
    }
}
