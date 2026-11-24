using System;
using System.Collections.Generic;
using System.IO;

namespace OrderedWork.Core
{
    /// <summary>
    /// JSON persistence for the new equipment-set schema.
    /// Legacy ClothingPreset files are intentionally not read or migrated.
    /// </summary>
    public sealed class EquipmentSetJsonStore
    {
        private readonly AtomicJsonRecordStore<EquipmentSet> store;

        public EquipmentSetJsonStore(
            string directory,
            Action<string>? warning = null,
            Action<string>? error = null)
        {
            store = new AtomicJsonRecordStore<EquipmentSet>(
                directory,
                set => set.SchemaVersion,
                1,
                EquipmentSet.CurrentSchemaVersion,
                set => set.Normalize(),
                BeforeSave,
                null,
                set => set.Id,
                NormalizeId,
                set => set.Name,
                false,
                warning ?? (_ => { }),
                error ?? (_ => { }));
        }

        public string DirectoryPath
        {
            get { return store.DirectoryPath; }
        }

        public string PathFor(string id)
        {
            return store.PathFor(id);
        }

        public List<EquipmentSet> LoadAll()
        {
            return store.LoadAll();
        }

        public EquipmentSet? TryLoadById(string id)
        {
            return store.TryLoadById(id);
        }

        public EquipmentSet? TryLoad(string path)
        {
            return store.TryLoad(path);
        }

        public bool Save(EquipmentSet? set)
        {
            if (set == null)
            {
                return false;
            }
            set.Normalize();
            return store.Save(set);
        }

        public bool Delete(string id)
        {
            return store.Delete(id);
        }

        private static void BeforeSave(EquipmentSet set)
        {
            set.Normalize();
            set.SchemaVersion = EquipmentSet.CurrentSchemaVersion;
            set.ModifiedUtc = DateTime.UtcNow.ToString("o");
        }

        private static string NormalizeId(string id)
        {
            return AtomicJsonRecordStore<EquipmentSet>.SanitizeFileName(
                id, Guid.NewGuid().ToString("N"), false);
        }
    }
}
