using System.Collections.Generic;
using System.IO;
using OrderedWork.Core;
using Verse;

namespace OrderedWork.Config
{
    /// <summary>
    /// New equipment-set storage. Legacy ClothingPreset files are ignored.
    /// </summary>
    public static class EquipmentSetStore
    {
        private static readonly EquipmentSetJsonStore Store = new EquipmentSetJsonStore(
            Path.Combine(WorkConfigStore.RootDirectory, "EquipmentSets"),
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

        public static List<EquipmentSet> LoadAll()
        {
            return Store.LoadAll();
        }

        public static EquipmentSet? TryLoadById(string id)
        {
            return Store.TryLoadById(id);
        }

        public static EquipmentSet? TryLoad(string path)
        {
            return Store.TryLoad(path);
        }

        public static bool Save(EquipmentSet? set)
        {
            return Store.Save(set);
        }

        public static bool Delete(string id)
        {
            return Store.Delete(id);
        }
    }
}
