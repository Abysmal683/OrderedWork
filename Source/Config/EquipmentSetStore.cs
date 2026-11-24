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

        private static List<EquipmentSet>? cached;
        private static int cachedTick = -1;

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

        public static List<EquipmentSet> LoadCached()
        {
            int tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (cached != null && cachedTick >= 0 && tick - cachedTick < 250)
            {
                return cached;
            }

            cached = Store.LoadAll();
            cachedTick = tick;
            return cached;
        }

        public static void InvalidateCache()
        {
            cached = null;
            cachedTick = -1;
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
            bool saved = Store.Save(set);
            if (saved)
            {
                InvalidateCache();
            }
            return saved;
        }

        public static bool Delete(string id)
        {
            bool deleted = Store.Delete(id);
            if (deleted)
            {
                InvalidateCache();
            }
            return deleted;
        }
    }
}
