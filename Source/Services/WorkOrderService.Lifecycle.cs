#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OrderedWork.Core;
using OrderedWork.Data;
using RimWorld;
using Verse;

namespace OrderedWork.Services
{
    public static partial class WorkOrderService
    {        public static void EnsureColonyInitialized()
        {
            try
            {
                if (Current.Game == null || Find.CurrentMap == null)
                {
                    return;
                }

                EnsureManualPriorities();
                OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
                if (component == null || component.initialized)
                {
                    return;
                }

                List<Pawn> colonists = Find.CurrentMap.mapPawns.FreeColonists.ToList();
                if (colonists.Count == 0)
                {
                    return;
                }

                // Auto recommend off behaves like an existing colony (copy the vanilla
                // order, no write). On keeps the current new/default recommendation.
                bool autoRecommend = AutoRecommendNewColony;
                bool existingColony = !autoRecommend || colonists.Any(HasNonDefaultPriorities);

                foreach (Pawn pawn in colonists)
                {
                    if (existingColony)
                    {
                        // Capture the established vanilla order but do not write priorities.
                        component.SetOrder(pawn, Normalize(pawn, ReadVanillaOrder(pawn)));
                    }
                    else
                    {
                        List<string> recommendation = GetRecommendation(pawn);
                        component.SetOrder(pawn, recommendation);
                        Apply(pawn, recommendation);
                    }
                }

                component.initialized = true;
                Log.Message("[Ordered Work] Colony initialized (" +
                    (!autoRecommend ? "copy" : existingColony ? "existing" : "new") + "): " + colonists.Count + " colonist(s).");
            }
            catch (Exception ex)
            {
                Log.Error("[Ordered Work] Colony initialization failed: " + ex);
            }
        }

        internal static void EnsureManualPriorities()
        {
            if (Current.Game == null || Current.Game.playSettings == null
                || Current.Game.playSettings.useWorkPriorities)
            {
                return;
            }
            Current.Game.playSettings.useWorkPriorities = true;
            Log.Message("[Ordered Work] Manual work priorities enabled for this game.");
        }

        public static void OnVanillaPriorityChanged(Pawn_WorkSettings settings)
        {
            if (IsApplying || IsSpawning || IsInitializing)
            {
                return;
            }
            if (Scribe.mode != LoadSaveMode.Inactive || settings == null)
            {
                return;
            }

            try
            {
                Pawn pawn = GetOwningPawn(settings);
                if (!IsManagedFreeColonist(pawn))
                {
                    return;
                }

                EnsureManualPriorities();
                OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
                if (component == null || !component.initialized)
                {
                    return;
                }

                // Ignore the settling window right after spawn/resurrection, where the
                // vanilla Work tab may still be partially initialized.
                PawnWorkOrder entry = component.GetEntry(pawn);
                if (entry != null && CurrentTick < entry.captureSuppressedUntilTick)
                {
                    return;
                }

                component.SetOrder(pawn, Normalize(pawn, ReadVanillaOrder(pawn)));
            }
            catch (Exception ex)
            {
                Log.Error("[Ordered Work] Vanilla-priority sync failed: " + ex);
            }
        }

        public static void OnPawnSpawned(Pawn pawn, bool respawningAfterLoad)
        {
            if (respawningAfterLoad)
            {
                return;
            }
            if (Scribe.mode != LoadSaveMode.Inactive || !IsManagedFreeColonist(pawn))
            {
                return;
            }

            try
            {
                EnsureManualPriorities();
                OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
                if (component == null || !component.initialized)
                {
                    return;
                }

                PawnWorkOrder entry = component.GetEntry(pawn);
                int suppressUntil = CurrentTick + PostSpawnCaptureSuppressionTicks;
                if (entry != null)
                {
                    // Reapply the stored order exactly once and suppress vanilla capture
                    // while the Work tab settles, so a partial state cannot overwrite it.
                    entry.captureSuppressedUntilTick = suppressUntil;
                    Apply(pawn, Normalize(pawn, entry.order));
                    Log.Message("[Ordered Work] Reapplied stored order after spawn for " + pawn.LabelShort +
                        " (" + (entry.order != null ? entry.order.Count : 0) + " types).");
                }
                else if (!AutoRecommendNewColony)
                {
                    // Auto recommend off: capture the vanilla order but do not write.
                    component.SetOrder(pawn, Normalize(pawn, ReadVanillaOrder(pawn)));
                    PawnWorkOrder created = component.GetEntry(pawn);
                    if (created != null)
                    {
                        created.captureSuppressedUntilTick = suppressUntil;
                    }
                    Log.Message("[Ordered Work] Copied vanilla order after spawn for " + pawn.LabelShort + " (auto-recommend off).");
                }
                else
                {
                    List<string> recommendation = GetRecommendation(pawn);
                    component.SetOrder(pawn, recommendation);
                    PawnWorkOrder created = component.GetEntry(pawn);
                    if (created != null)
                    {
                        created.captureSuppressedUntilTick = suppressUntil;
                    }
                    Apply(pawn, recommendation);
                    Log.Message("[Ordered Work] Recommended new pawn after spawn for " + pawn.LabelShort + ".");
                }
            }
            catch (Exception ex)
            {
                Log.Error("[Ordered Work] Spawn/reapply failed: " + ex);
            }
        }

        public static void OnPawnFactionChanged(Pawn pawn)
        {
            if (IsApplying || IsSpawning)
            {
                return;
            }
            if (Scribe.mode != LoadSaveMode.Inactive || !IsManagedFreeColonist(pawn))
            {
                return;
            }

            try
            {
                EnsureManualPriorities();
                OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
                if (component == null || !component.initialized)
                {
                    return;
                }

                int suppressUntil = CurrentTick + PostSpawnCaptureSuppressionTicks;
                PawnWorkOrder entry = component.GetEntry(pawn);
                if (entry != null)
                {
                    entry.captureSuppressedUntilTick = suppressUntil;
                    Apply(pawn, Normalize(pawn, entry.order));
                    Log.Message("[Ordered Work] Reapplied stored order after faction change for " + pawn.LabelShort +
                        " (" + (entry.order != null ? entry.order.Count : 0) + " types).");
                    return;
                }

                if (!AutoRecommendNewColony)
                {
                    component.SetOrder(pawn, Normalize(pawn, ReadVanillaOrder(pawn)));
                    PawnWorkOrder copied = component.GetEntry(pawn);
                    if (copied != null)
                    {
                        copied.captureSuppressedUntilTick = suppressUntil;
                    }
                    Log.Message("[Ordered Work] Copied vanilla order after faction change for " + pawn.LabelShort + " (auto-recommend off).");
                    return;
                }

                List<string> recommendation = GetRecommendation(pawn);
                component.SetOrder(pawn, recommendation);
                PawnWorkOrder created = component.GetEntry(pawn);
                if (created != null)
                {
                    created.captureSuppressedUntilTick = suppressUntil;
                }
                Apply(pawn, recommendation);
                Log.Message("[Ordered Work] Recommended new pawn after faction change for " + pawn.LabelShort + ".");
            }
            catch (Exception ex)
            {
                Log.Error("[Ordered Work] Faction-change recommendation failed: " + ex);
            }
        }

        private static bool IsManagedFreeColonist(Pawn pawn)
        {
            return pawn != null && !pawn.Dead && !pawn.Destroyed && pawn.IsFreeColonist;
        }

        internal static Pawn GetOwningPawn(Pawn_WorkSettings settings)
        {
            if (!workSettingsPawnFieldResolved)
            {
                cachedWorkSettingsPawnField = typeof(Pawn_WorkSettings).GetField("pawn", PrivateInstance);
                workSettingsPawnFieldResolved = true;
            }

            return cachedWorkSettingsPawnField != null
                ? cachedWorkSettingsPawnField.GetValue(settings) as Pawn
                : null;
        }
    }
}
