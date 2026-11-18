using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace OrderedWork.Services
{
    /// <summary>
    /// Temporary, opt-in spike for per-pawn WorkGiver order. It does not change
    /// WorkGiverDef.priorityInType or any global def; it only reorders the
    /// WorkGivers returned for the selected pawn.
    /// </summary>
    public static class WorkGiverOrderSpike
    {
        private const string LogPrefix = "[OrderedWork][Spike]";

        private sealed class State
        {
            public WorkTypeDef WorkType;
            public List<string> GiverDefNames = new List<string>();
            public int NormalLogs;
            public int EmergencyLogs;
        }

        private static readonly Dictionary<Pawn, State> states = new Dictionary<Pawn, State>();

        public static bool IsEnabled
        {
            get { return OrderedWorkMod.Settings != null && OrderedWorkMod.Settings.workGiverOrderSpike; }
        }

        public static bool ApplyToSelectedPawn()
        {
            Pawn pawn = Find.Selector != null ? Find.Selector.SingleSelectedThing as Pawn : null;
            if (pawn == null)
            {
                Log.Warning(LogPrefix + " Select exactly one pawn before applying the spike.");
                return false;
            }
            return Apply(pawn, "Hauling");
        }

        public static bool Apply(Pawn pawn, string workTypeDefName)
        {
            if (pawn == null || pawn.workSettings == null)
            {
                Log.Warning(LogPrefix + " No pawn/work settings available.");
                return false;
            }

            WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(workTypeDefName);
            if (workType == null)
            {
                Log.Warning(LogPrefix + " WorkType not found: " + workTypeDefName);
                return false;
            }

            states.Remove(pawn);
            List<WorkGiver> source = pawn.workSettings.WorkGiversInOrderNormal;
            List<string> original = GiversOfType(source, workType);
            if (original.Count < 2)
            {
                Log.Warning(LogPrefix + " WorkType " + workTypeDefName
                    + " has fewer than two active WorkGivers for " + pawn.LabelShort + ".");
                return false;
            }

            List<string> desired = new List<string>(original);
            desired.Reverse();
            State state = new State
            {
                WorkType = workType,
                GiverDefNames = desired
            };
            states[pawn] = state;
            if (OrderedWorkMod.Settings != null)
            {
                OrderedWorkMod.Settings.workGiverOrderSpike = true;
            }

            Log.Message(LogPrefix + " Apply pawn=" + pawn.LabelShort
                + " workType=" + workTypeDefName
                + " original=[" + string.Join(", ", original) + "]"
                + " desired=[" + string.Join(", ", desired) + "]");
            return true;
        }

        public static void ClearSelectedPawn()
        {
            Pawn pawn = Find.Selector != null ? Find.Selector.SingleSelectedThing as Pawn : null;
            if (pawn == null)
            {
                Log.Warning(LogPrefix + " Select exactly one pawn before clearing the spike.");
                return;
            }
            if (states.Remove(pawn))
            {
                Log.Message(LogPrefix + " Cleared pawn=" + pawn.LabelShort);
            }
            else
            {
                Log.Message(LogPrefix + " No spike state for pawn=" + pawn.LabelShort);
            }
        }

        public static void ClearAll()
        {
            states.Clear();
        }

        public static void LogCurrentOrder(Pawn pawn)
        {
            if (pawn == null || pawn.workSettings == null)
            {
                Log.Warning(LogPrefix + " No pawn/work settings available.");
                return;
            }

            Log.Message(LogPrefix + " Current normal order pawn=" + pawn.LabelShort
                + " [" + Describe(pawn.workSettings.WorkGiversInOrderNormal) + "]");
            Log.Message(LogPrefix + " Current emergency order pawn=" + pawn.LabelShort
                + " [" + Describe(pawn.workSettings.WorkGiversInOrderEmergency) + "]");
        }

        public static bool TryReorder(
            Pawn pawn,
            List<WorkGiver> source,
            bool emergency,
            out List<WorkGiver> result)
        {
            result = source;
            if (!IsEnabled || pawn == null || source == null || source.Count == 0)
            {
                return false;
            }

            State state;
            if (!states.TryGetValue(pawn, out state) || state == null || state.WorkType == null)
            {
                return false;
            }

            List<WorkGiver> target = new List<WorkGiver>();
            List<int> targetIndexes = new List<int>();
            for (int i = 0; i < source.Count; i++)
            {
                WorkGiver giver = source[i];
                if (giver != null && giver.def != null && giver.def.workType == state.WorkType)
                {
                    target.Add(giver);
                    targetIndexes.Add(i);
                }
            }

            if (target.Count < 2)
            {
                return false;
            }

            Dictionary<string, int> desiredIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < state.GiverDefNames.Count; i++)
            {
                desiredIndexes[state.GiverDefNames[i]] = i;
            }

            string before = Describe(target);
            Dictionary<WorkGiver, int> sourceIndexes = new Dictionary<WorkGiver, int>();
            for (int i = 0; i < target.Count; i++)
            {
                sourceIndexes[target[i]] = i;
            }

            target.Sort(delegate(WorkGiver left, WorkGiver right)
            {
                int leftRank;
                int rightRank;
                if (!desiredIndexes.TryGetValue(left.def.defName, out leftRank))
                {
                    leftRank = int.MaxValue;
                }
                if (!desiredIndexes.TryGetValue(right.def.defName, out rightRank))
                {
                    rightRank = int.MaxValue;
                }
                if (leftRank != rightRank)
                {
                    return leftRank.CompareTo(rightRank);
                }
                return sourceIndexes[left].CompareTo(sourceIndexes[right]);
            });

            List<WorkGiver> reordered = new List<WorkGiver>(source.Count);
            int targetCursor = 0;
            for (int i = 0; i < source.Count; i++)
            {
                if (i == targetIndexes[targetCursor])
                {
                    reordered.Add(target[targetCursor]);
                    targetCursor++;
                    if (targetCursor >= targetIndexes.Count)
                    {
                        break;
                    }
                }
                else
                {
                    reordered.Add(source[i]);
                }
            }
            for (int i = reordered.Count; i < source.Count; i++)
            {
                reordered.Add(source[i]);
            }

            result = reordered;
            int logCount = emergency ? state.EmergencyLogs : state.NormalLogs;
            if (logCount < 3 || logCount % 100 == 0)
            {
                Log.Message(LogPrefix + " " + (emergency ? "Emergency" : "Normal")
                    + " getter pawn=" + pawn.LabelShort
                    + " target=" + state.WorkType.defName
                    + " before=[" + before + "]"
                    + " after=[" + Describe(target) + "]"
                    + " logCount=" + logCount);
            }
            if (emergency)
            {
                state.EmergencyLogs++;
            }
            else
            {
                state.NormalLogs++;
            }
            return true;
        }

        private static List<string> GiversOfType(List<WorkGiver> source, WorkTypeDef workType)
        {
            List<string> result = new List<string>();
            if (source == null)
            {
                return result;
            }
            foreach (WorkGiver giver in source)
            {
                if (giver != null && giver.def != null && giver.def.workType == workType)
                {
                    result.Add(giver.def.defName);
                }
            }
            return result;
        }

        private static string Describe(List<WorkGiver> givers)
        {
            if (givers == null || givers.Count == 0)
            {
                return "";
            }
            return string.Join(", ", givers.Select(g => g != null && g.def != null ? g.def.defName : "<null>"));
        }
    }
}
