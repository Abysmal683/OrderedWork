using System;
using System.Collections.Generic;

namespace OrderedWork.Core
{
    /// <summary>
    /// Pure gate for the pre-work equipment hook. It uses def names only so the
    /// decision stays testable without RimWorld types.
    /// </summary>
    public static class EquipmentPreWorkRules
    {
        private static readonly HashSet<string> allowedWorkGivers = new HashSet<string>(
            new[]
            {
                "DoBill",
                "Research",
                "Mine",
                "ConstructFinishFrames",
                "Deconstruct",
                "Repair",
                "SmoothFloor",
                "SmoothWall",
                "Hunt",
                "Fish",
                "FishAnimal",
                "Train",
                "Tame"
            },
            StringComparer.Ordinal);

        private static readonly HashSet<string> excludedWorkGivers = new HashSet<string>(
            new[]
            {
                "CleanFilth",
                "Haul",
                "Flick",
                "Refuel",
                "Ingest",
                "Rescue",
                "FeedPatient",
                "TendPatient",
                "TendEntity",
                "PrisonerBloodfeed"
            },
            StringComparer.Ordinal);

        private static readonly HashSet<string> excludedJobDefs = new HashSet<string>(
            new[]
            {
                "Wear",
                "Equip",
                "TakeInventory",
                "Ingest",
                "LayDown",
                "Goto",
                "Wait",
                "Idle",
                "Flee",
                "AttackMelee",
                "UseVerbOnThing"
            },
            StringComparer.Ordinal);

        public static bool ShouldPrepare(string? workGiverDefName, string? jobDefName, bool playerForced, bool fromQueue)
        {
            if (playerForced || fromQueue)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(workGiverDefName)
                || string.IsNullOrWhiteSpace(jobDefName)
                || !allowedWorkGivers.Contains(workGiverDefName)
                || excludedWorkGivers.Contains(workGiverDefName)
                || excludedJobDefs.Contains(jobDefName))
            {
                return false;
            }

            return true;
        }
    }
}
