using System;
using System.Collections.Generic;
using System.IO;
using OrderedWork.Core;
using Verse;

namespace OrderedWork.Config
{
    /// <summary>
    /// One JSON file per named clothing preset. File mechanics live in the atomic
    /// store; this class owns only the clothing schema and id policy.
    /// </summary>
    public static class ClothingPresetStore
    {
        private static readonly AtomicJsonRecordStore<ClothingPreset> Store =
            new AtomicJsonRecordStore<ClothingPreset>(
                Path.Combine(WorkConfigStore.RootDirectory, "ClothingPresets"),
                preset => preset.SchemaVersion,
                1,
                ClothingPreset.CurrentSchemaVersion,
                preset => preset.Normalize(),
                preset =>
                {
                    preset.Normalize();
                    preset.SchemaVersion = ClothingPreset.CurrentSchemaVersion;
                    preset.ModifiedUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
                },
                null,
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

        public static List<ClothingPreset> LoadAll()
        {
            return Store.LoadAll();
        }

        public static ClothingPreset? TryLoadById(string id)
        {
            return Store.TryLoadById(id);
        }

        public static ClothingPreset? TryLoad(string path)
        {
            return Store.TryLoad(path);
        }

        public static bool Save(ClothingPreset? preset)
        {
            if (preset == null)
            {
                return false;
            }
            preset.Normalize();
            return Store.Save(preset);
        }

        public static bool Delete(string id)
        {
            return Store.Delete(id);
        }

        private static string NormalizeId(string id)
        {
            return AtomicJsonRecordStore<ClothingPreset>.SanitizeFileName(
                id, Guid.NewGuid().ToString("N"), false);
        }
    }
}
