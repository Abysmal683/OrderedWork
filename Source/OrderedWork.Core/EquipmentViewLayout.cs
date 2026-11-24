using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderedWork.Core
{
    public sealed class EquipmentViewTab
    {
        public string Id { get; set; } = "";
        public string Label { get; set; } = "";
        public int Order { get; set; }
        public List<EquipmentViewRow> Rows { get; set; } = new List<EquipmentViewRow>();
    }

    public sealed class EquipmentViewRow
    {
        public string Id { get; set; } = "";
        public string Label { get; set; } = "";
        public int Order { get; set; }
        public List<ApparelSlotKey> Cells { get; set; } = new List<ApparelSlotKey>();
    }

    public sealed class EquipmentViewCell
    {
        public ApparelSlotKey Key { get; set; }
        public string TabId { get; set; } = "";
        public string RowId { get; set; } = "";
        public int Order { get; set; }
    }

    /// <summary>
    /// View-only tab/row placement. Slot identity remains ApparelSlotKey.
    /// Renaming or reordering this model never changes occupancy or candidates.
    /// </summary>
    public sealed class EquipmentViewLayout
    {
        private readonly List<EquipmentViewTab> tabs = new List<EquipmentViewTab>();
        private readonly Dictionary<ApparelSlotKey, EquipmentViewCell> cells =
            new Dictionary<ApparelSlotKey, EquipmentViewCell>();

        public IReadOnlyList<EquipmentViewTab> Tabs => tabs;
        public IReadOnlyDictionary<ApparelSlotKey, EquipmentViewCell> Cells => cells;

        public static EquipmentViewLayout Build(
            IEnumerable<ApparelSlotKey> sourceCells,
            Func<ApparelSlotKey, string> tabFor,
            Func<ApparelSlotKey, string> rowFor)
        {
            ArgumentNullException.ThrowIfNull(sourceCells);
            ArgumentNullException.ThrowIfNull(tabFor);
            ArgumentNullException.ThrowIfNull(rowFor);

            EquipmentViewLayout layout = new EquipmentViewLayout();
            int tabOrder = 0;
            foreach (ApparelSlotKey cell in sourceCells.Where(value => value.IsValid).Distinct())
            {
                string tabKey = NormalizeKey(tabFor(cell), "Other");
                string rowKey = NormalizeKey(rowFor(cell), cell.BodyPartGroupDefName);

                EquipmentViewTab? tab = layout.tabs.FirstOrDefault(value =>
                    string.Equals(value.Id, tabKey, StringComparison.Ordinal));
                if (tab == null)
                {
                    tab = new EquipmentViewTab
                    {
                        Id = tabKey,
                        Label = tabKey,
                        Order = tabOrder++
                    };
                    layout.tabs.Add(tab);
                }

                EquipmentViewRow? row = tab.Rows.FirstOrDefault(value =>
                    string.Equals(value.Id, rowKey, StringComparison.Ordinal));
                if (row == null)
                {
                    row = new EquipmentViewRow
                    {
                        Id = rowKey,
                        Label = rowKey,
                        Order = tab.Rows.Count
                    };
                    tab.Rows.Add(row);
                }

                row.Cells.Add(cell);
                layout.cells[cell] = new EquipmentViewCell
                {
                    Key = cell,
                    TabId = tab.Id,
                    RowId = row.Id,
                    Order = row.Cells.Count - 1
                };
            }

            return layout;
        }

        public bool TryGetCell(ApparelSlotKey key, out EquipmentViewCell? cell)
        {
            return cells.TryGetValue(key, out cell);
        }

        public bool RenameTab(string tabId, string label)
        {
            EquipmentViewTab? tab = FindTab(tabId);
            if (tab == null || string.IsNullOrWhiteSpace(label))
            {
                return false;
            }
            tab.Label = label.Trim();
            return true;
        }

        public bool RenameRow(string tabId, string rowId, string label)
        {
            EquipmentViewRow? row = FindRow(tabId, rowId);
            if (row == null || string.IsNullOrWhiteSpace(label))
            {
                return false;
            }
            row.Label = label.Trim();
            return true;
        }

        public bool MoveTab(string tabId, int order)
        {
            EquipmentViewTab? tab = FindTab(tabId);
            if (tab == null)
            {
                return false;
            }

            tabs.Remove(tab);
            tabs.Insert(Math.Clamp(order, 0, tabs.Count), tab);
            for (int i = 0; i < tabs.Count; i++)
            {
                tabs[i].Order = i;
            }
            return true;
        }

        public bool MoveRow(string tabId, string rowId, int order)
        {
            EquipmentViewTab? tab = FindTab(tabId);
            EquipmentViewRow? row = tab == null ? null : FindRow(tabId, rowId);
            if (tab == null || row == null)
            {
                return false;
            }

            tab.Rows.Remove(row);
            tab.Rows.Insert(Math.Clamp(order, 0, tab.Rows.Count), row);
            for (int i = 0; i < tab.Rows.Count; i++)
            {
                tab.Rows[i].Order = i;
            }
            return true;
        }

        private EquipmentViewTab? FindTab(string tabId)
        {
            return tabs.FirstOrDefault(value =>
                string.Equals(value.Id, tabId, StringComparison.Ordinal));
        }

        private EquipmentViewRow? FindRow(string tabId, string rowId)
        {
            EquipmentViewTab? tab = FindTab(tabId);
            return tab == null
                ? null
                : tab.Rows.FirstOrDefault(value =>
                    string.Equals(value.Id, rowId, StringComparison.Ordinal));
        }


        private static string NormalizeKey(string? value, string fallback)
        {
            string raw = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            char[] chars = raw
                .Select(character => char.IsLetterOrDigit(character) ? character : '_')
                .ToArray();
            string key = new string(chars).Trim('_');
            return string.IsNullOrWhiteSpace(key) ? "Other" : key;
        }
    }
}
