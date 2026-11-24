using System;

namespace OrderedWork
{
    [Flags]
    public enum OrderedWorkLogCategory
    {
        None = 0,
        WorkControl = 1,
        Schedule = 2,
        Stores = 4,
        Passion = 8,
        Config = 16,
        Harmony = 32,
        Equipment = 64,
        All = WorkControl | Schedule | Stores | Passion | Config | Harmony | Equipment
    }
}