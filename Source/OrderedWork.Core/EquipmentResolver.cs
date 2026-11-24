using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderedWork.Core
{
    public enum EquipmentSelectionSource
    {
        Forced = 0,
        Mandatory = 1,
        Emergency = 2,
        WorkType = 3,
        Principal = 4
    }

    public enum EquipmentConflictReason
    {
        MissingFootprint,
        PolicyDenied,
        CellOccupied,
        MandatoryUncovered,
        Unavailable
    }

    public sealed class EquipmentSelection
    {
        public string DefName { get; set; } = "";
        public EquipmentSelectionSource Source { get; set; }
        public string? SetId { get; set; }
        public List<ApparelSlotKey> Cells { get; set; } = new List<ApparelSlotKey>();
    }

    public sealed class EquipmentConflict
    {
        public EquipmentConflictReason Reason { get; set; }
        public EquipmentSelectionSource Source { get; set; }
        public string? SetId { get; set; }
        public ApparelSlotKey? Cell { get; set; }
        public string? DefName { get; set; }
    }

    public sealed class EquipmentPlan
    {
        public List<EquipmentSelection> Selections { get; } = new List<EquipmentSelection>();
        public Dictionary<ApparelSlotKey, EquipmentSelection> ByCell { get; } =
            new Dictionary<ApparelSlotKey, EquipmentSelection>();
        public List<EquipmentConflict> Conflicts { get; } = new List<EquipmentConflict>();
        public List<ApparelSlotKey> MandatoryUncovered { get; } = new List<ApparelSlotKey>();
        public Dictionary<string, int> Inventory { get; } =
            new Dictionary<string, int>(StringComparer.Ordinal);
        public string? PrimaryEquipmentDefName { get; set; }
    }

    public sealed class EquipmentResolutionContext
    {
        public IReadOnlyDictionary<string, ApparelFootprint> Footprints { get; }
        public HashSet<string> AllowedApparel { get; }
        public IReadOnlyCollection<EquipmentSet> Sets { get; }
        public string? ActiveWorkTypeDefName { get; }
        public bool EmergencyActive { get; }
        public EquipmentOverrides Overrides { get; }
        public Func<string, bool>? IsApparelAvailable { get; }

        public EquipmentResolutionContext(
            IReadOnlyDictionary<string, ApparelFootprint>? footprints = null,
            IEnumerable<string>? allowedApparel = null,
            IEnumerable<EquipmentSet>? sets = null,
            string? activeWorkTypeDefName = null,
            bool emergencyActive = false,
            EquipmentOverrides? overrides = null,
            Func<string, bool>? isApparelAvailable = null)
        {
            Footprints = footprints ?? new Dictionary<string, ApparelFootprint>(StringComparer.Ordinal);
            AllowedApparel = new HashSet<string>(
                (allowedApparel ?? Enumerable.Empty<string>())
                    .Where(value => !string.IsNullOrWhiteSpace(value)),
                StringComparer.Ordinal);
            Sets = (sets ?? Enumerable.Empty<EquipmentSet>())
                .Where(set => set != null)
                .ToList();
            ActiveWorkTypeDefName = activeWorkTypeDefName;
            EmergencyActive = emergencyActive;
            Overrides = overrides ?? new EquipmentOverrides();
            IsApparelAvailable = isApparelAvailable;
        }
    }

    /// <summary>
    /// Pure set resolver. It selects apparel targets and inventory targets; it never equips,
    /// pathfinds, reserves or mutates a pawn.
    /// </summary>
    public static class EquipmentResolver
    {
        public static EquipmentPlan Resolve(EquipmentResolutionContext? context)
        {
            EquipmentPlan plan = new EquipmentPlan();
            if (context == null)
            {
                return plan;
            }

            HashSet<ApparelSlotKey> occupied = new HashSet<ApparelSlotKey>();
            ApplyForcedOverrides(context, plan, occupied);

            ApplySets(context, plan, occupied, EquipmentSetRole.Mandatory);
            CheckMandatoryCoverage(context, plan);
            ApplySets(context, plan, occupied, EquipmentSetRole.Emergency);
            ApplySets(context, plan, occupied, EquipmentSetRole.WorkType);
            ApplySets(context, plan, occupied, EquipmentSetRole.Principal);
            AddInventory(context, plan);
            ResolvePrimaryEquipment(context, plan);
            ApplyInventoryOverrides(context, plan);
            return plan;
        }

        private static void ApplySets(
            EquipmentResolutionContext context,
            EquipmentPlan plan,
            HashSet<ApparelSlotKey> occupied,
            EquipmentSetRole role)
        {
            foreach (EquipmentSet set in OrderedSets(context).Where(set => set.Role == role))
            {
                EquipmentSelectionSource source = ToSource(set.Role);
                foreach (CellRule rule in set.Cells)
                {
                    if (occupied.Contains(rule.Cell))
                    {
                        continue;
                    }

                    foreach (CellAlternative alternative in rule.Alternatives.OrderBy(value => value.Priority))
                    {
                        if (!context.AllowedApparel.Contains(alternative.DefName))
                        {
                            plan.Conflicts.Add(new EquipmentConflict
                            {
                                Reason = EquipmentConflictReason.PolicyDenied,
                                Source = source,
                                SetId = set.Id,
                                Cell = rule.Cell,
                                DefName = alternative.DefName
                            });
                            continue;
                        }

                        if (!context.Footprints.TryGetValue(alternative.DefName, out ApparelFootprint? footprint))
                        {
                            plan.Conflicts.Add(new EquipmentConflict
                            {
                                Reason = EquipmentConflictReason.MissingFootprint,
                                Source = source,
                                SetId = set.Id,
                                Cell = rule.Cell,
                                DefName = alternative.DefName
                            });
                            continue;
                        }

                        if (context.IsApparelAvailable != null && !context.IsApparelAvailable(alternative.DefName))
                        {
                            plan.Conflicts.Add(new EquipmentConflict
                            {
                                Reason = EquipmentConflictReason.Unavailable,
                                Source = source,
                                SetId = set.Id,
                                Cell = rule.Cell,
                                DefName = alternative.DefName
                            });
                            continue;
                        }

                        footprint.Normalize();
                        if (footprint.Cells.Any(occupied.Contains))
                        {
                            continue;
                        }

                        EquipmentSelection selection = new EquipmentSelection
                        {
                            DefName = alternative.DefName,
                            Source = source,
                            SetId = set.Id,
                            Cells = footprint.Cells.ToList()
                        };
                        plan.Selections.Add(selection);
                        foreach (ApparelSlotKey cell in selection.Cells)
                        {
                            occupied.Add(cell);
                            plan.ByCell[cell] = selection;
                        }
                        break;
                    }
                }
            }
        }
        private static void ApplyForcedOverrides(
            EquipmentResolutionContext context,
            EquipmentPlan plan,
            HashSet<ApparelSlotKey> occupied)
        {
            foreach (KeyValuePair<ApparelSlotKey, string> forced in context.Overrides.ForcedApparel)
            {
                if (!forced.Key.IsValid || string.IsNullOrWhiteSpace(forced.Value))
                {
                    continue;
                }

                if (!context.Footprints.TryGetValue(forced.Value, out ApparelFootprint? footprint))
                {
                    plan.Conflicts.Add(new EquipmentConflict
                    {
                        Reason = EquipmentConflictReason.MissingFootprint,
                        Source = EquipmentSelectionSource.Forced,
                        Cell = forced.Key,
                        DefName = forced.Value
                    });
                    continue;
                }

                footprint.Normalize();
                EquipmentSelection selection = new EquipmentSelection
                {
                    DefName = forced.Value,
                    Source = EquipmentSelectionSource.Forced,
                    Cells = footprint.Cells.ToList()
                };
                plan.Selections.Add(selection);
                foreach (ApparelSlotKey cell in selection.Cells)
                {
                    occupied.Add(cell);
                    plan.ByCell[cell] = selection;
                }
            }
        }

        private static IEnumerable<EquipmentSet> OrderedSets(EquipmentResolutionContext context)
        {
            return context.Sets
                .Where(set => IsActive(set, context))
                .OrderBy(set => set.Role)
                .ThenBy(set => set.Order)
                .ThenBy(set => set.Id, StringComparer.Ordinal);
        }

        private static bool IsActive(EquipmentSet set, EquipmentResolutionContext context)
        {
            switch (set.Role)
            {
                case EquipmentSetRole.Emergency:
                    return context.EmergencyActive;
                case EquipmentSetRole.WorkType:
                    return !string.IsNullOrWhiteSpace(context.ActiveWorkTypeDefName) &&
                        string.Equals(set.WorkTypeDefName, context.ActiveWorkTypeDefName, StringComparison.Ordinal);
                default:
                    return true;
            }
        }

        private static EquipmentSelectionSource ToSource(EquipmentSetRole role)
        {
            switch (role)
            {
                case EquipmentSetRole.Mandatory:
                    return EquipmentSelectionSource.Mandatory;
                case EquipmentSetRole.Emergency:
                    return EquipmentSelectionSource.Emergency;
                case EquipmentSetRole.WorkType:
                    return EquipmentSelectionSource.WorkType;
                default:
                    return EquipmentSelectionSource.Principal;
            }
        }

        private static void CheckMandatoryCoverage(EquipmentResolutionContext context, EquipmentPlan plan)
        {
            foreach (EquipmentSet set in context.Sets.Where(set => set.Role == EquipmentSetRole.Mandatory))
            {
                foreach (CellRule rule in set.Cells)
                {
                    if (plan.ByCell.ContainsKey(rule.Cell))
                    {
                        continue;
                    }

                    plan.MandatoryUncovered.Add(rule.Cell);
                    plan.Conflicts.Add(new EquipmentConflict
                    {
                        Reason = EquipmentConflictReason.MandatoryUncovered,
                        Source = EquipmentSelectionSource.Mandatory,
                        SetId = set.Id,
                        Cell = rule.Cell
                    });
                }
            }
        }

        private static void AddInventory(EquipmentResolutionContext context, EquipmentPlan plan)
        {
            foreach (EquipmentSet set in OrderedSets(context))
            {
                foreach (InventoryTarget entry in set.Inventory)
                {
                    if (!plan.Inventory.ContainsKey(entry.DefName))
                    {
                        plan.Inventory[entry.DefName] = 0;
                    }
                    plan.Inventory[entry.DefName] += entry.Expected;
                }
            }
        }

        private static void ResolvePrimaryEquipment(EquipmentResolutionContext context, EquipmentPlan plan)
        {
            if (!string.IsNullOrWhiteSpace(context.Overrides.PrimaryEquipmentDefName))
            {
                plan.PrimaryEquipmentDefName = context.Overrides.PrimaryEquipmentDefName;
                return;
            }

            foreach (EquipmentSet set in OrderedSets(context))
            {
                if (string.IsNullOrWhiteSpace(set.PrimaryEquipmentDefName))
                {
                    continue;
                }

                if (plan.PrimaryEquipmentDefName == null)
                {
                    plan.PrimaryEquipmentDefName = set.PrimaryEquipmentDefName;
                }
                else
                {
                    plan.Inventory.TryGetValue(set.PrimaryEquipmentDefName, out int count);
                    plan.Inventory[set.PrimaryEquipmentDefName] = count + 1;
                }
            }
        }

        private static void ApplyInventoryOverrides(EquipmentResolutionContext context, EquipmentPlan plan)
        {
            foreach (KeyValuePair<string, int> entry in context.Overrides.Inventory)
            {
                plan.Inventory[entry.Key] = entry.Value;
            }
        }
    }
}
