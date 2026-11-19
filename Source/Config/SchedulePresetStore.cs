using System;
using System.Collections.Generic;
using System.IO;
using OrderedWork.Core;
using Verse;

namespace OrderedWork.Config
{
    /// <summary>
    /// One JSON file per named schedule preset. The shared atomic store owns the
    /// file mechanics; schedule owns only schema, normalization and id policy.
    /// </summary>
    public static class SchedulePresetStore
    {
        private static readonly AtomicJsonRecordStore<SchedulePreset> Store =
            new AtomicJsonRecordStore<SchedulePreset>(
                Path.Combine(WorkConfigStore.RootDirectory, "schedules"),
                preset => preset.SchemaVersion,
                1,
                SchedulePreset.CurrentSchemaVersion,
                preset => preset.Normalize(),
                preset =>
                {
                    preset.SchemaVersion = SchedulePreset.CurrentSchemaVersion;
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

        public static List<SchedulePreset> LoadAll()
        {
            return Store.LoadAll();
        }

        public static SchedulePreset TryLoadById(string id)
        {
            return Store.TryLoadById(id);
        }

        public static SchedulePreset TryLoad(string path)
        {
            return Store.TryLoad(path);
        }

        public static bool Save(SchedulePreset preset)
        {
            return Store.Save(preset);
        }

        public static bool Delete(string id)
        {
            return Store.Delete(id);
        }

        private static string NormalizeId(string id)
        {
            return AtomicJsonRecordStore<SchedulePreset>.SanitizeFileName(
                id, Guid.NewGuid().ToString("N"), false);
        }
    }
}
