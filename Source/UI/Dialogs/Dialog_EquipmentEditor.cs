using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.Core;
using OrderedWork.Localization;
using OrderedWork.Services;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// First functional Equipment editor slice: dynamic cells, tabs, rows and
    /// local alternatives. Metadata and the pawn Equipment tab follow later.
    /// </summary>
    public sealed class Dialog_EquipmentEditor : Window
    {
        private EquipmentSet? set;
        private EquipmentCatalogSnapshot catalog = new EquipmentCatalogSnapshot();
        private EquipmentViewLayout? layout;
        private int selectedTab;
        private ApparelSlotKey? selectedCell;
        private string search = "";
        private Vector2 gridScroll;
        private Vector2 addedScroll;
        private Vector2 availableScroll;
        private bool dirty;
        private Action? pendingAction;
        private readonly EquipmentMetadataState metadataState = new EquipmentMetadataState();
        private string? previewDefName;
        private HashSet<ApparelSlotKey>? previewCells;

        public Dialog_EquipmentEditor(OrderedWorkMod host, string? initialSetId = null)
        {
            doCloseX = true;
            doCloseButton = false;
            resizeable = true;
            draggable = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            LoadCatalog();
            LoadSet(initialSetId);
        }

        public override Vector2 InitialSize => new Vector2(1080f, 980f);

        public override bool OnCloseRequest()
        {
            if (!dirty)
            {
                return true;
            }
            if (pendingAction == null)
            {
                pendingAction = delegate { dirty = false; Close(); };
            }
            return false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 28f), T("OrderedWork.Equipment.Editor.Title", "Equipment editor"));
            Text.Font = GameFont.Small;

            if (set == null)
            {
                Widgets.Label(new Rect(0f, 38f, inRect.width, 24f), T("OrderedWork.Equipment.Editor.NoSet", "No equipment set."));
                return;
            }

            if (pendingAction != null)
            {
                DrawDirtyPrompt(inRect);
                return;
            }

            previewDefName = null;
            previewCells = null;
            DrawHeader(inRect);
            DrawTabs(inRect);
            DrawCandidates(inRect);
            DrawGrid(inRect);
            DrawMetadata(inRect);
        }

        private void DrawHeader(Rect inRect)
        {
            if (set == null)
            {
                return;
            }

            Rect row = new Rect(0f, 34f, inRect.width, 26f);
            Rect[] cells = UiFlex.Columns(row, 6f, 2.4f, 1f, 1f, 1f);
            if (UiButtonBar.Button(cells[0], set.Name ?? ""))
            {
                OpenLoad();
            }
            if (UiButtonBar.Button(cells[1], T("OrderedWork.Equipment.Editor.Load", "Load...")))
            {
                OpenLoad();
            }
            if (UiButtonBar.Button(cells[2], T("OrderedWork.Equipment.Editor.Save", "Save")))
            {
                SaveSet();
            }
            if (UiButtonBar.Button(cells[3], T("OrderedWork.Equipment.Editor.SaveAs", "Save As...")))
            {
                OpenSaveAs();
            }
        }

        private void DrawTabs(Rect inRect)
        {
            if (layout == null || layout.Tabs.Count == 0)
            {
                return;
            }

            string[] labels = layout.Tabs.Select(tab => TabLabel(tab.Id)).ToArray();
            UiTabBar.Draw(new Rect(0f, 68f, inRect.width, 30f), labels, selectedTab, delegate(int index)
            {
                selectedTab = index;
                selectedCell = null;
                metadataState.SelectedDefName = null;
            });
        }

        private void DrawGrid(Rect inRect)
        {
            if (layout == null || layout.Tabs.Count == 0)
            {
                return;
            }

            EquipmentViewTab tab = layout.Tabs[Mathf.Clamp(selectedTab, 0, layout.Tabs.Count - 1)];
            Rect area = new Rect(0f, 108f, inRect.width, 250f);
            Widgets.DrawBoxSolid(area, UiColors.ColumnBackground);
            const float cellSize = 54f;
            float contentHeight = Mathf.Max(area.height, tab.Rows.Count * (cellSize + 12f) + 6f);
            UiScrollPane.Draw(area, ref gridScroll, contentHeight, this, delegate(Rect view)
            {
                UiRows rows = new UiRows(new Rect(0f, 4f, view.width, view.height - 4f), cellSize, 12f);
                foreach (EquipmentViewRow row in tab.Rows)
                {
                    Rect rowRect = rows.Next();
                    Rect labelRect = new Rect(rowRect.x + 6f, rowRect.y + 14f, 96f, 24f);
                    Widgets.Label(labelRect, RowLabel(row.Id));
                    Rect cellsArea = new Rect(rowRect.x + 106f, rowRect.y, Mathf.Max(0f, rowRect.width - 110f), cellSize);
                    for (int i = 0; i < row.Cells.Count; i++)
                    {
                        ApparelSlotKey cell = row.Cells[i];
                        Rect cellRect = UiGrid.Cell(cellsArea, i, cellSize, 6f);
                        DrawCell(cellRect, cell);
                    }
                }
            });
        }

        private void DrawCell(Rect rect, ApparelSlotKey cell)
        {
            bool selected = selectedCell.HasValue && selectedCell.Value == cell;
            CellRule? rule = set?.Cells.FirstOrDefault(value => value.Cell == cell);
            int count = rule?.Alternatives.Count ?? 0;
            string label = LayerLabel(cell.ApparelLayerDefName);
            string subLabel = count == 0 ? "—" : count.ToString();
            bool preview = previewCells != null && previewCells.Contains(cell);
            if (UiCard.Draw(rect, label, subLabel, selected, null,
                T("OrderedWork.Equipment.Editor.CellTooltip", "Select this cell.")))
            {
                selectedCell = cell;
                metadataState.SelectedDefName = null;
                search = "";
            }

            if (!preview)
            {
                return;
            }

            Widgets.DrawBox(rect, 2);
            if (count > 0 || string.IsNullOrWhiteSpace(previewDefName))
            {
                return;
            }

            string previewDef = previewDefName ?? "";
            if (string.IsNullOrWhiteSpace(previewDef))
            {
                return;
            }
            Texture2D? icon = ClothingIconResolver.IconOf(previewDef);
            if (icon == null)
            {
                return;
            }

            Rect iconRect = UiInset.All(rect, 14f);
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            GUI.color = previous;
        }

        private void DrawCandidates(Rect inRect)
        {
            Rect area = new Rect(0f, 366f, inRect.width, 230f);
            Rect[] columns = UiFlex.Columns(area, 8f, 1f, 1f);
            DrawAdded(columns[0]);
            DrawAvailable(columns[1]);
        }

        private void DrawMetadata(Rect inRect)
        {
            Rect area = new Rect(0f, 604f, inRect.width, Mathf.Max(120f, inRect.height - 614f));
            EquipmentMetadataPanel.Draw(area, set, catalog, metadataState);
        }
        private void DrawAdded(Rect area)
        {
            Widgets.Label(new Rect(area.x, area.y, area.width, 20f), T("OrderedWork.Equipment.Editor.Added", "Added"));
            Rect list = new Rect(area.x, area.y + 24f, area.width, area.height - 24f);
            if (!selectedCell.HasValue)
            {
                Widgets.Label(list, T("OrderedWork.Equipment.Editor.SelectCell", "Select a cell."));
                return;
            }

            CellRule? rule = set?.Cells.FirstOrDefault(value => value.Cell == selectedCell.Value);
            if (rule == null || rule.Alternatives.Count == 0)
            {
                Widgets.Label(list, T("OrderedWork.Equipment.Editor.NoAdded", "No alternatives for this cell."));
                return;
            }

            List<CellAlternative> alternatives = rule.Alternatives.OrderBy(value => value.Priority).ToList();
            UiScrollPane.Draw(list, ref addedScroll, Mathf.Max(list.height, alternatives.Count * 32f + 4f), this, delegate(Rect view)
            {
                UiRows rows = new UiRows(view, 28f, 4f);
                for (int i = 0; i < alternatives.Count; i++)
                {
                    CellAlternative alternative = alternatives[i];
                    Rect row = rows.Next();
                    string sub = T("OrderedWork.Equipment.Editor.Equip", "equip") + " · " + alternative.Priority + " · " + DefLabel(alternative.DefName);
                    if (Mouse.IsOver(row))
                    {
                        previewDefName = alternative.DefName;
                        if (catalog.Footprints.TryGetValue(alternative.DefName, out ApparelFootprint? footprint))
                        {
                            previewCells = new HashSet<ApparelSlotKey>(footprint.Cells);
                        }
                    }
                    if (UiCard.Draw(row, sub, null, false, null,
                        T("OrderedWork.Equipment.Editor.RemoveHint", "Right-click to remove.")))
                    {
                        metadataState.SelectedDefName = alternative.DefName;
                    }
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && row.Contains(Event.current.mousePosition))
                    {
                        rule.Alternatives.Remove(alternative);
                        NormalizePriorities(rule);
                        dirty = true;
                        Event.current.Use();
                        break;
                    }
                }
            });
        }

        private void DrawAvailable(Rect area)
        {
            Widgets.Label(new Rect(area.x, area.y, area.width, 20f), T("OrderedWork.Equipment.Editor.Available", "Available"));
            Rect searchRect = new Rect(area.x, area.y + 22f, area.width, 24f);
            search = UiSearchField.Draw(searchRect, search, T("OrderedWork.Equipment.Editor.SearchTip", "Search apparel."));
            Rect list = new Rect(area.x, area.y + 50f, area.width, area.height - 50f);

            bool hasCell = selectedCell.HasValue;
            CellRule? rule = hasCell ? set?.Cells.FirstOrDefault(value => value.Cell == selectedCell!.Value) : null;
            HashSet<string> added = new HashSet<string>(
                (rule?.Alternatives ?? new List<CellAlternative>()).Select(value => value.DefName),
                StringComparer.Ordinal);
            string term = (search ?? "").Trim();
            List<ThingDef> candidates = catalog.Footprints.Values
                .Where(footprint => !hasCell || footprint.Cells.Contains(selectedCell!.Value))
                .Where(footprint => catalog.AllowedApparel.Contains(footprint.DefName))
                .Where(footprint => !added.Contains(footprint.DefName))
                .Select(footprint => DefDatabase<ThingDef>.GetNamedSilentFail(footprint.DefName))
                .Where(def => def != null)
                .Where(def => string.IsNullOrEmpty(term)
                    || def.defName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0
                    || def.label.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(def => def.label)
                .ThenBy(def => def.defName, StringComparer.Ordinal)
                .Take(200)
                .ToList();

            UiScrollPane.Draw(list, ref availableScroll, Mathf.Max(list.height, candidates.Count * 32f + 4f), this, delegate(Rect view)
            {
                UiRows rows = new UiRows(view, 28f, 4f);
                foreach (ThingDef def in candidates)
                {
                    Rect row = rows.Next();
                    if (Mouse.IsOver(row))
                    {
                        previewDefName = def.defName;
                        if (catalog.Footprints.TryGetValue(def.defName, out ApparelFootprint? footprint))
                        {
                            previewCells = new HashSet<ApparelSlotKey>(footprint.Cells);
                        }
                    }
                    if (UiCard.Draw(row, def.LabelCap, null, false, ClothingIconResolver.IconOf(def.defName),
                        hasCell
                            ? T("OrderedWork.Equipment.Editor.AddHint", "Click to add.")
                            : T("OrderedWork.Equipment.Editor.SelectCell", "Select a cell.")))
                    {
                        if (hasCell)
                        {
                            AddAlternative(def.defName);
                            break;
                        }
                    }
                }
            });
        }

        private void AddAlternative(string defName)
        {
            if (set == null || !selectedCell.HasValue)
            {
                return;
            }
            CellRule? rule = set.Cells.FirstOrDefault(value => value.Cell == selectedCell.Value);
            if (rule == null)
            {
                rule = new CellRule { Cell = selectedCell.Value };
                set.Cells.Add(rule);
            }
            if (rule.Alternatives.Any(value => string.Equals(value.DefName, defName, StringComparison.Ordinal)))
            {
                return;
            }
            rule.Alternatives.Add(new CellAlternative { DefName = defName, Priority = rule.Alternatives.Count });
            dirty = true;
        }

        private static void NormalizePriorities(CellRule rule)
        {
            List<CellAlternative> ordered = rule.Alternatives.OrderBy(value => value.Priority).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].Priority = i;
            }
            rule.Alternatives = ordered;
        }

        private void LoadCatalog()
        {
            catalog = EquipmentCatalogBuilder.Build(null);
            layout = EquipmentViewLayout.Build(catalog.Cells, TabFor, cell => cell.BodyPartGroupDefName);
        }

        private void LoadSet(string? id)
        {
            List<EquipmentSet> all = EquipmentSetStore.LoadAll();
            set = !string.IsNullOrWhiteSpace(id)
                ? all.FirstOrDefault(value => value.Id == id)
                : all.FirstOrDefault();
            if (set == null)
            {
                set = new EquipmentSet { Name = "New equipment", Role = EquipmentSetRole.Principal };
                EquipmentSetStore.Save(set);
            }
            selectedCell = null;
            selectedTab = 0;
            dirty = false;
        }

        private void OpenLoad()
        {
            if (dirty)
            {
                pendingAction = delegate { dirty = false; OpenLoad(); };
                return;
            }
            Find.WindowStack.Add(new Dialog_EquipmentSetLoad(delegate(EquipmentSet loaded)
            {
                LoadSet(loaded.Id);
            }));
        }

        private void OpenSaveAs()
        {
            if (set == null)
            {
                return;
            }
            Find.WindowStack.Add(new Dialog_EquipmentSetName(
                T("OrderedWork.Equipment.Editor.SaveAsTitle", "Save equipment set as"),
                set.Name + " copy",
                delegate(string name)
                {
                    EquipmentSet copy = CopySet(set, name);
                    if (EquipmentSetStore.Save(copy))
                    {
                        set = copy;
                        dirty = false;
                    }
                },
                name => !EquipmentSetStore.LoadAll().Any(item =>
                    string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))));
        }

        private void SaveSet()
        {
            if (set == null)
            {
                return;
            }
            set.Normalize();
            if (EquipmentSetStore.Save(set))
            {
                dirty = false;
                Messages.Message(T("OrderedWork.Equipment.Editor.Saved", "Equipment set saved."), MessageTypeDefOf.TaskCompletion, false);
            }
        }

        private void DrawDirtyPrompt(Rect inRect)
        {
            Widgets.DrawBoxSolid(inRect, UiColors.PanelBody);
            Rect panel = UiInset.All(inRect, 24f);
            UiRows rows = new UiRows(panel, 30f, 8f);
            Widgets.Label(rows.Next(), T("OrderedWork.Equipment.Editor.Unsaved", "Unsaved changes."));
            Rect buttons = rows.Next(30f);
            Rect[] cells = UiFlex.Columns(buttons, 8f, 1f, 1f, 1f);
            if (UiButtonBar.Button(cells[0], T("OrderedWork.Common.Save", "Save")))
            {
                SaveSet();
                if (!dirty)
                {
                    Action? action = pendingAction;
                    pendingAction = null;
                    action?.Invoke();
                }
            }
            if (UiButtonBar.Button(cells[1], T("OrderedWork.Equipment.Editor.Discard", "Discard")))
            {
                dirty = false;
                Action? action = pendingAction;
                pendingAction = null;
                action?.Invoke();
            }
            if (UiButtonBar.Button(cells[2], T("OrderedWork.Common.Cancel", "Cancel")))
            {
                pendingAction = null;
            }
        }

        private static EquipmentSet CopySet(EquipmentSet source, string name)
        {
            EquipmentSet copy = new EquipmentSet
            {
                Name = name,
                Role = source.Role,
                Order = source.Order,
                WorkTypeDefName = source.WorkTypeDefName,
                PrimaryEquipmentDefName = source.PrimaryEquipmentDefName,
                Cells = source.Cells.Select(rule => new CellRule
                {
                    Cell = rule.Cell,
                    Alternatives = rule.Alternatives.Select(alternative => new CellAlternative
                    {
                        DefName = alternative.DefName,
                        Priority = alternative.Priority
                    }).ToList()
                }).ToList(),
                Inventory = source.Inventory.Select(entry => new InventoryTarget
                {
                    DefName = entry.DefName,
                    Expected = entry.Expected
                }).ToList()
            };
            copy.Normalize();
            return copy;
        }

        private static string TabFor(ApparelSlotKey cell)
        {
            switch (cell.ApparelLayerDefName)
            {
                case "OnSkin":
                case "OnSkin_A":
                case "Underwear":
                case "UnderwearTop":
                case "Middle":
                case "Middle_A":
                case "Shell":
                case "Shell_A":
                case "Belt":
                case "Belt_A":
                case "AM_Pocket":
                case "DankPyon_LegShell":
                case "DankPyon_ShellAttachment":
                    return "Body";
                case "Overhead":
                case "Overhead_A":
                case "DankPyon_Hood":
                case "DankPyon_InnerHelmet":
                    return "Head";
                case "EyeCover":
                case "Accessories":
                case "DankPyon_NecklaceLayer":
                case "DankPyon_Ring1Layer":
                    return "Accessory";
                case "Cape":
                case "Armor_Cape":
                case "VFEC_OuterShell":
                    return "Cape";
                default:
                    return cell.ApparelLayerDefName.IndexOf("Exosuit", StringComparison.Ordinal) >= 0 ||
                        cell.ApparelLayerDefName.IndexOf("WalkerGear", StringComparison.Ordinal) >= 0
                        ? "Exosuit"
                        : "Other";
            }
        }

        private static string TabLabel(string id)
        {
            return T("OrderedWork.Equipment.Tabs." + id, id);
        }

        private static string RowLabel(string defName)
        {
            BodyPartGroupDef? def = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail(defName);
            return def == null ? defName : def.LabelCap.ToString();
        }

        private static string LayerLabel(string defName)
        {
            ApparelLayerDef? def = DefDatabase<ApparelLayerDef>.GetNamedSilentFail(defName);
            return def == null ? defName : def.LabelCap.ToString();
        }

        private static string DefLabel(string defName)
        {
            ThingDef? def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            return def == null ? defName : def.LabelCap.ToString();
        }

        private static string T(string key, string fallback)
        {
            return OrderedWorkText.T(key, fallback);
        }
    }
}
