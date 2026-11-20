#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using OrderedWork.Core;
using Verse;

namespace OrderedWork.Config
{
    /// <summary>
    /// One JSON file per named hourly focus preset. The shared atomic store owns the
    /// file mechanics; focus owns only schema, normalization and id policy.
    /// </summary>
    public static class WorkFocusPresetStore
    {
        private static readonly AtomicJsonRecordStore<WorkFocusPreset> Store =
            new AtomicJsonRecordStore<WorkFocusPreset>(
                Path.Combine(WorkConfigStore.RootDirectory, "focus"),
                preset => preset.SchemaVersion,
                1,
                WorkFocusPreset.CurrentSchemaVersion,
                preset => preset.Normalize(),
                preset =>
                {
                    preset.SchemaVersion = WorkFocusPreset.CurrentSchemaVersion;
                    preset.ModifiedUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
                },
                (preset, path) =>
                {
                    if (string.IsNullOrEmpty(preset.ModifiedUtc))
                    {
                        preset.ModifiedUtc = File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm");
                    }
                },
                preset => preset.Id,
                NormalizeId,
                preset => preset.Name,
                false,
                message => Log.Warning(message),
                message => Log.Error(message));

        public static string DirectoryPath
        {
            get { return Store.DirectoryPath; }
        }

        public static string PathFor(string id)
        {
            return Store.PathFor(id);
        }

        public static List<WorkFocusPreset> LoadAll()
        {
            return Store.LoadAll();
        }

        public static WorkFocusPreset TryLoadById(string id)
        {
            return Store.TryLoadById(id);
        }

        public static WorkFocusPreset TryLoad(string path)
        {
            return Store.TryLoad(path);
        }

        public static bool Save(WorkFocusPreset preset)
        {
            return Store.Save(preset);
        }

        public static bool Delete(string id)
        {
            return Store.Delete(id);
        }

        private static string NormalizeId(string id)
        {
            return AtomicJsonRecordStore<WorkFocusPreset>.SanitizeFileName(
                id, Guid.NewGuid().ToString("N"), false);
        }
    }
}
