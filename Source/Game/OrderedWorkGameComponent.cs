using System;
using System.Collections.Generic;
using OrderedWork.Services;
using Verse;

namespace OrderedWork.Data
{
    /// <summary>How Work Control and the vanilla Work tab share the job of setting up orders.</summary>
    public enum WorkViewMode
    {
        Draw,
        Manual,
        Both
    }

    /// <summary>
    /// Save-scoped store of per-pawn work orders. Kept small: one entry per pawn
    /// that the player has actually edited.
    /// </summary>
    public class OrderedWorkGameComponent : GameComponent
    {
        public List<PawnWorkOrder> entries = new List<PawnWorkOrder>();
        public bool initialized;
        public WorkViewMode viewMode = WorkViewMode.Both;

        // RimWorld instantiates GameComponents with Activator.CreateInstance(type, game),
        // so the subclass must expose a (Game) constructor. Verse.GameComponent itself only
        // has a protected parameterless constructor, so it is called implicitly here.
        public OrderedWorkGameComponent(Game game) : base()
        {
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref entries, "entries", LookMode.Deep);
            Scribe_Values.Look(ref initialized, "initialized", false);
            Scribe_Values.Look(ref viewMode, "viewMode", WorkViewMode.Both);

            if (Scribe.mode == LoadSaveMode.LoadingVars && entries == null)
            {
                entries = new List<PawnWorkOrder>();
            }
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Cleanup();
            }
        }

        /// <summary>
        /// Initializes the colony as soon as a map exists, even if the vanilla Work tab is
        /// opened first. Idempotent: EnsureColonyInitialized returns once initialized is set.
        /// </summary>
        public override void GameComponentTick()
        {
            if (initialized || Find.CurrentMap == null)
            {
                return;
            }
            WorkOrderService.EnsureColonyInitialized();
        }

        public PawnWorkOrder GetEntry(Pawn pawn)
        {
            if (pawn == null || entries == null)
            {
                return null;
            }
            for (int i = 0; i < entries.Count; i++)
            {
                PawnWorkOrder entry = entries[i];
                if (entry != null && entry.pawn == pawn)
                {
                    return entry;
                }
            }
            return null;
        }

        public List<string> GetOrder(Pawn pawn)
        {
            PawnWorkOrder entry = GetEntry(pawn);
            return entry != null ? entry.order : null;
        }

        public void SetOrder(Pawn pawn, List<string> order)
        {
            if (pawn == null)
            {
                return;
            }
            if (entries == null)
            {
                entries = new List<PawnWorkOrder>();
            }

            PawnWorkOrder entry = GetEntry(pawn);
            if (entry == null)
            {
                entry = new PawnWorkOrder { pawn = pawn };
                entries.Add(entry);
            }
            entry.order = order != null ? new List<string>(order) : new List<string>();
        }

        /// <summary>
        /// Drops null entries and entries whose pawn was destroyed. Dead (but not destroyed)
        /// pawns keep their order so a resurrection can reapply it.
        /// </summary>
        public void Cleanup()
        {
            if (entries == null)
            {
                return;
            }
            entries.RemoveAll(e =>
                e == null ||
                e.pawn == null ||
                e.pawn.Destroyed);
        }

        /// <summary>
        /// Returns the live component, creating one if the game did not auto-register it.
        /// Returns null (and logs) only when no game is loaded or creation fails.
        /// </summary>
        public static OrderedWorkGameComponent GetOrCreate()
        {
            if (Current.Game == null)
            {
                return null;
            }

            OrderedWorkGameComponent component = Current.Game.GetComponent<OrderedWorkGameComponent>();
            if (component != null)
            {
                return component;
            }

            try
            {
                component = new OrderedWorkGameComponent(Current.Game);
                Current.Game.components.Add(component);
                return component;
            }
            catch (Exception ex)
            {
                Log.Error("[Ordered Work] Could not create OrderedWorkGameComponent: " + ex);
                return null;
            }
        }
    }
}
