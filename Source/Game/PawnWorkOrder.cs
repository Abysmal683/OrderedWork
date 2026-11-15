using System.Collections.Generic;
using Verse;

namespace OrderedWork.Data
{
    /// <summary>
    /// One saved per-pawn order. The list holds WorkTypeDef defNames, left = highest.
    /// Absent types are treated as priority 0 (off).
    /// </summary>
    public class PawnWorkOrder : IExposable
    {
        public Pawn pawn;
        public List<string> order = new List<string>();

        /// <summary>
        /// Runtime-only: while the current tick is below this value, vanilla SetPriority
        /// capture is ignored for this pawn (post-spawn / post-resurrection settling).
        /// Not scribed.
        /// </summary>
        public int captureSuppressedUntilTick;

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Collections.Look(ref order, "order", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.LoadingVars && order == null)
            {
                order = new List<string>();
            }
        }
    }
}
