using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using Verse.AI;

namespace OrderedWork.Services
{
    /// <summary>
    /// Small, tick-throttled lookup for the WorkType of a pawn's current job.
    /// Sleep, food, joy and jobs without a WorkGiver return null.
    /// </summary>
    public static class ActiveWorkService
    {
        private sealed class CacheEntry
        {
            public Job? Job;
            public WorkTypeDef? WorkType;
            public int Tick;
        }

        private static ConditionalWeakTable<Pawn, CacheEntry> cache =
            new ConditionalWeakTable<Pawn, CacheEntry>();

        public static void Reset()
        {
            cache = new ConditionalWeakTable<Pawn, CacheEntry>();
        }

        public static WorkTypeDef? GetActiveWorkType(Pawn? pawn)
        {
            Job? job = pawn != null ? pawn.CurJob : null;
            if (job == null || pawn == null)
            {
                return null;
            }

            int tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (cache.TryGetValue(pawn, out CacheEntry? entry)
                && ReferenceEquals(entry.Job, job)
                && tick - entry.Tick < 5)
            {
                return entry.WorkType;
            }

            WorkTypeDef? workType = job.workGiverDef != null ? job.workGiverDef.workType : null;
            cache.Remove(pawn);
            cache.Add(pawn, new CacheEntry { Job = job, WorkType = workType, Tick = tick });
            return workType;
        }

        public static bool IsActive(Pawn? pawn, WorkTypeDef? workType)
        {
            return workType != null && GetActiveWorkType(pawn) == workType;
        }
    }
}