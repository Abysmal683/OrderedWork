using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderedWork.Core
{
    /// <summary>
    /// Stable identity of one apparel cell: body, body-part group and layer.
    /// UI order, tab names and row grouping must never change this value.
    /// </summary>
    public readonly record struct ApparelSlotKey(
        string BodyDefName,
        string BodyPartGroupDefName,
        string ApparelLayerDefName)
    {
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(BodyDefName) &&
            !string.IsNullOrWhiteSpace(BodyPartGroupDefName) &&
            !string.IsNullOrWhiteSpace(ApparelLayerDefName);
    }

    /// <summary>
    /// All cells a single apparel def can occupy.
    /// </summary>
    public sealed class ApparelFootprint
    {
        public string DefName { get; set; } = "";
        public List<ApparelSlotKey> Cells { get; set; } = new List<ApparelSlotKey>();

        public void Normalize()
        {
            DefName = (DefName ?? "").Trim();
            Cells = (Cells ?? new List<ApparelSlotKey>())
                .Where(cell => cell.IsValid)
                .Distinct()
                .ToList();
        }
    }

    /// <summary>
    /// Dynamic slot catalog built from apparel footprints.
    /// </summary>
    public sealed class ApparelSlotCatalog
    {
        private readonly Dictionary<ApparelSlotKey, SortedSet<string>> defsByCell =
            new Dictionary<ApparelSlotKey, SortedSet<string>>();

        public IReadOnlyCollection<ApparelSlotKey> Cells => defsByCell.Keys;

        public void Add(ApparelFootprint? footprint)
        {
            if (footprint == null)
            {
                return;
            }

            footprint.Normalize();
            if (string.IsNullOrWhiteSpace(footprint.DefName))
            {
                return;
            }

            foreach (ApparelSlotKey cell in footprint.Cells)
            {
                if (!defsByCell.TryGetValue(cell, out SortedSet<string>? defs))
                {
                    defs = new SortedSet<string>(StringComparer.Ordinal);
                    defsByCell[cell] = defs;
                }
                defs.Add(footprint.DefName);
            }
        }

        public IReadOnlyCollection<string> DefsFor(ApparelSlotKey cell)
        {
            return defsByCell.TryGetValue(cell, out SortedSet<string>? defs)
                ? defs.ToList()
                : Array.Empty<string>();
        }
    }

    /// <summary>
    /// One candidate inside a cell's local priority list.
    /// </summary>
    public sealed class CellAlternative
    {
        public string DefName { get; set; } = "";
        public int Priority { get; set; }
    }

    /// <summary>
    /// Alternatives for one exact cell. Local priorities are independent per cell.
    /// </summary>
    public sealed class CellRule
    {
        public ApparelSlotKey Cell { get; set; }
        public List<CellAlternative> Alternatives { get; set; } = new List<CellAlternative>();

        public void Normalize()
        {
            Alternatives = (Alternatives ?? new List<CellAlternative>())
                .Where(alternative => alternative != null && !string.IsNullOrWhiteSpace(alternative.DefName))
                .GroupBy(alternative => alternative.DefName.Trim(), StringComparer.Ordinal)
                .Select(group => group.OrderBy(alternative => alternative.Priority).First())
                .OrderBy(alternative => alternative.Priority)
                .ToList();

            foreach (CellAlternative alternative in Alternatives)
            {
                alternative.DefName = alternative.DefName.Trim();
            }
        }
    }

    public enum EquipmentSetRole
    {
        Mandatory = 0,
        Emergency = 1,
        WorkType = 2,
        Principal = 3
    }

    public sealed class InventoryTarget
    {
        public string DefName { get; set; } = "";
        public int Expected { get; set; }
    }

    /// <summary>
    /// One saved set. Mandatory/Emergency/Principal may be combined;
    /// WorkType sets are keyed by WorkTypeDefName.
    /// </summary>
    public sealed class EquipmentSet
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "new set";
        public EquipmentSetRole Role { get; set; }
        public int Order { get; set; }
        public string? WorkTypeDefName { get; set; }
        public string? PrimaryEquipmentDefName { get; set; }
        public List<CellRule> Cells { get; set; } = new List<CellRule>();
        public List<InventoryTarget> Inventory { get; set; } = new List<InventoryTarget>();

        public void Normalize()
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                Id = Guid.NewGuid().ToString("N");
            }
            Name = string.IsNullOrWhiteSpace(Name) ? "new set" : Name.Trim();
            WorkTypeDefName = string.IsNullOrWhiteSpace(WorkTypeDefName) ? null : WorkTypeDefName.Trim();
            PrimaryEquipmentDefName = string.IsNullOrWhiteSpace(PrimaryEquipmentDefName)
                ? null
                : PrimaryEquipmentDefName.Trim();

            Cells = (Cells ?? new List<CellRule>())
                .Where(rule => rule != null && rule.Cell.IsValid)
                .GroupBy(rule => rule.Cell)
                .Select(group =>
                {
                    CellRule merged = new CellRule { Cell = group.Key };
                    merged.Alternatives = group
                        .SelectMany(rule => rule.Alternatives ?? new List<CellAlternative>())
                        .Where(alternative => alternative != null)
                        .ToList();
                    merged.Normalize();
                    return merged;
                })
                .ToList();
            foreach (CellRule rule in Cells)
            {
                rule.Normalize();
            }

            Inventory = (Inventory ?? new List<InventoryTarget>())
                .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.DefName))
                .GroupBy(entry => entry.DefName.Trim(), StringComparer.Ordinal)
                .Select(group => group.Last())
                .OrderBy(entry => entry.DefName, StringComparer.Ordinal)
                .ToList();
            foreach (InventoryTarget entry in Inventory)
            {
                entry.DefName = entry.DefName.Trim();
            }
        }
    }

    /// <summary>
    /// Absolute per-pawn overrides. They never mutate the saved set.
    /// </summary>
    public sealed class EquipmentOverrides
    {
        public Dictionary<ApparelSlotKey, string> ForcedApparel { get; set; } =
            new Dictionary<ApparelSlotKey, string>();
        public string? PrimaryEquipmentDefName { get; set; }
        public Dictionary<string, int> Inventory { get; set; } =
            new Dictionary<string, int>(StringComparer.Ordinal);

        public bool IsEmpty =>
            ForcedApparel.Count == 0 &&
            string.IsNullOrWhiteSpace(PrimaryEquipmentDefName) &&
            Inventory.Count == 0;
    }
}
