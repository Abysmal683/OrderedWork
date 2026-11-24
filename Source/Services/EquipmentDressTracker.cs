using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using Verse.AI;

namespace OrderedWork.Services
{
    /// <summary>
    /// Tracks only the jobs created by the equipment execution layer. Weak pawn keys
    /// avoid retaining pawns across saves or map destruction.
    /// </summary>
    internal static class EquipmentDressTracker
    {
        private sealed class State
        {
            public readonly HashSet<Job> Jobs = new HashSet<Job>();
        }

        private static readonly ConditionalWeakTable<Pawn, State> states =
            new ConditionalWeakTable<Pawn, State>();

        public static void Register(Pawn? pawn, IEnumerable<Job>? jobs)
        {
            if (pawn == null || jobs == null)
            {
                return;
            }

            State state = states.GetOrCreateValue(pawn);
            foreach (Job job in jobs)
            {
                if (job != null)
                {
                    state.Jobs.Add(job);
                }
            }
        }

        public static bool IsTracked(Pawn? pawn, Job? job)
        {
            return pawn != null && job != null
                && states.TryGetValue(pawn, out State? state)
                && state.Jobs.Contains(job);
        }

        public static bool HasQueued(Pawn? pawn)
        {
            if (pawn?.jobs?.jobQueue == null || !states.TryGetValue(pawn, out State? state))
            {
                return false;
            }

            foreach (QueuedJob queued in pawn.jobs.jobQueue)
            {
                if (queued?.job != null && state.Jobs.Contains(queued.job))
                {
                    return true;
                }
            }

            return false;
        }

        public static void Unregister(Pawn? pawn, Job? job)
        {
            if (pawn == null || job == null || !states.TryGetValue(pawn, out State? state))
            {
                return;
            }

            state.Jobs.Remove(job);
            if (state.Jobs.Count == 0)
            {
                states.Remove(pawn);
            }
        }

        public static int CancelQueued(Pawn? pawn)
        {
            if (pawn?.jobs?.jobQueue == null || !states.TryGetValue(pawn, out State? state))
            {
                return 0;
            }

            int before = pawn.jobs.jobQueue.Count;
            pawn.jobs.jobQueue.RemoveAll(pawn, job => job != null && state.Jobs.Contains(job));
            int removed = before - pawn.jobs.jobQueue.Count;
            states.Remove(pawn);
            return removed;
        }

        public static void Clear(Pawn? pawn)
        {
            if (pawn != null)
            {
                states.Remove(pawn);
            }
        }
    }
}
