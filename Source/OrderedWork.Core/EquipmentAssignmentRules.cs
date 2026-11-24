using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderedWork.Core
{
    /// <summary>
    /// Pure normalization rules shared by pawn equipment assignments.
    /// Keeps the Verse-facing policy model thin and testable.
    /// </summary>
    public static class EquipmentAssignmentRules
    {
        public static List<string> NormalizeSetIds(IEnumerable<string>? values)
        {
            return (values ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        public static bool IsValidOverride(
            string? kind,
            string? bodyDefName,
            string? bodyPartGroupDefName,
            string? apparelLayerDefName,
            string? defName)
        {
            if (string.IsNullOrWhiteSpace(kind) || string.IsNullOrWhiteSpace(defName))
            {
                return false;
            }
            if (!string.Equals(kind.Trim(), "forced_apparel", StringComparison.Ordinal))
            {
                return true;
            }
            return !string.IsNullOrWhiteSpace(bodyDefName)
                && !string.IsNullOrWhiteSpace(bodyPartGroupDefName)
                && !string.IsNullOrWhiteSpace(apparelLayerDefName);
        }

        public static string OverrideKey(
            string kind,
            string bodyDefName,
            string bodyPartGroupDefName,
            string apparelLayerDefName,
            string defName)
        {
            return kind + "|" + bodyDefName + "|" + bodyPartGroupDefName + "|"
                + apparelLayerDefName + "|" + defName;
        }
    }
}
