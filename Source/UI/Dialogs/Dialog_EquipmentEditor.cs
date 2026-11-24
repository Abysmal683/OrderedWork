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
    /// local alternatives. Metadata and the pawn Equipment tab follow in later batches.
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

        public override Vector2 InitialSize => new Vector2(1080f, 780f);

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

            DrawHeader(inRect);
            DrawTabs(inRect);
            DrawGrid(inRect);
            DrawCandidates(inRect);
        }

        private void DrawHeader(Rect inRect)
        {
            if (set == null)
            {
                return;
            }
            Rect row = new Rect(0f, 34f, inRect.width, 26f);
            Rect[] cells = UiFlex.Columns(row, 6f, 2.4f, 1f, 1f, 1f);
            if (Widgets.ButtonText(cells[0], (set?.Name ?? "")))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (EquipmentSet item in EquipmentSetStore.LoadAll())
                {
                    EquipmentSet local = item;
                    options.Add(new FloatMenuOption(item.Name, delegate { LoadSet(local.Id); }));
                }
                if (options.Count > 0)
                {
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }
            if (Widgets.ButtonText(cells[1], T("OrderedWork.Equipment.Editor.New", "New")))
            {
                CreateSet();
            }
            if (set != null)
            {
                set.Name = Widgets.TextField(cells[2], set.Name ?? "");
            }
            if (Widgets.ButtonText(cells[3], T("OrderedWork.Equipment.Editor.Save", "Save")))
            {
                SaveSet();
            }
        }

        private void DrawTabs(Rect inRect)
        {
            if (layout == null || layout.Tabs.Count == 0)
            {
                return;
            }

            float x = 0f;
            for (int i = 0; i < layout.Tabs.Count; i++)
            {
                EquipmentViewTab tab = layout.Tabs[i];
                string label = TabLabel(tab.Id);
                float width = Mathf.Max(84f, Text.CalcSize(label).x + 18f);
                Rect button = new Rect(x, 68f, width, 26f);
                if (Widgets.ButtonText(button, label, true, true, selectedTab == i))
                {
                    selectedTab = i;
                    selectedCell = null;
                }
                x += width + 4f;
            }
        }

        private void DrawGrid(Rect inRect)
        {
            if (layout == null || layout.Tabs.Count == 0)
            {
                return;
            }

            EquipmentViewTab tab = layout.Tabs[Mathf.Clamp(selectedTab, 0, layout.Tabs.Count - 1)];
            Rect area = new Rect(0f, 102f, inRect.width, 270f);
            Widgets.DrawBoxSolid(area, new Color(0.10f, 0.11f, 0.13f, 0.55f));
            float contentHeight = Mathf.Max(area.height, tab.Rows.Count * 58f + 6f);
            UiScrollPane.Draw(area, ref gridScroll, contentHeight, this, delegate(Rect view)
            {
                for (int rowIndex = 0; rowIndex < tab.Rows.Count; rowIndex++)
                {
                    EquipmentViewRow row = tab.Rows[rowIndex];
                    float y = rowIndex * 58f + 4f;
                    Widgets.Label(new Rect(6f, y + 10f, 96f, 24f), RowLabel(row.Id));
                    for (int cellIndex = 0; cellIndex < row.Cells.Count; cellIndex++)
                    {
                        DrawCell(new Rect(106f + cellIndex * 104f, y, 98f, 48f), row.Cells[cellIndex]);
                    }
                }
            });
        }

        private void DrawCell(Rect rect, ApparelSlotKey cell)
        {
            bool selected = selectedCell.HasValue && selectedCell.Value == cell;
            CellRule? rule = set?.Cells.FirstOrDefault(value => value.Cell == cell);
            int count = rule?.Alternatives.Count ?? 0;
            Widgets.DrawBoxSolid(rect, selected
                ? new Color(0.18f, 0.34f, 0.48f, 0.95f)
                : new Color(0.14f, 0.15f, 0.18f, 0.95f));
            Widgets.DrawBox(rect, selected ? 2 : 1);
            Widgets.Label(new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, 20f), LayerLabel(cell.ApparelLayerDefName));
            Widgets.Label(new Rect(rect.x + 4f, rect.y + 24f, rect.width - 8f, 18f),
                count == 0 ? "—" : count.ToString());
            if (Widgets.ButtonInvisible(rect))
            {
                selectedCell = cell;
                search = "";
            }
        }

        private void DrawCandidates(Rect inRect)
        {
            Rect area = new Rect(0f, 380f, inRect.width, inRect.height - 420f);
            Rect[] columns = UiFlex.Columns(area, 8f, 1f, 1f);
            DrawAdded(columns[0]);
            DrawAvailable(columns[1]);
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
            UiScrollPane.Draw(list, ref addedScroll, Mathf.Max(list.height, alternatives.Count * 30f + 4f), this, delegate(Rect view)
            {
                for (int i = 0; i < alternatives.Count; i++)
                {
                    CellAlternative alternative = alternatives[i];
                    Rect row = new Rect(0f, i * 30f, view.width, 27f);
                    Widgets.DrawBoxSolid(row, new Color(0.14f, 0.20f, 0.17f, 0.95f));
                    Widgets.Label(new Rect(row.x + 6f, row.y + 4f, row.width - 12f, 20f),
                        alternative.Priority + " · " + DefLabel(alternative.DefName));
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && row.Contains(Event.current.mousePosition))
                    {
                        rule.Alternatives.Remove(alternative);
                        NormalizePriorities(rule);
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
            search = Widgets.TextField(searchRect, search ?? "");
            Rect list = new Rect(area.x, area.y + 50f, area.width, area.height - 50f);

            if (!selectedCell.HasValue)
            {
                Widgets.Label(list, T("OrderedWork.Equipment.Editor.SelectCell", "Select a cell."));
                return;
            }

            CellRule? rule = set?.Cells.FirstOrDefault(value => value.Cell == selectedCell.Value);
            HashSet<string> added = new HashSet<string>(
                (rule?.Alternatives ?? new List<CellAlternative>()).Select(value => value.DefName),
                StringComparer.Ordinal);
            string term = (search ?? "").Trim();
            List<ThingDef> candidates = catalog.Footprints.Values
                .Where(footprint => footprint.Cells.Contains(selectedCell.Value))
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

            UiScrollPane.Draw(list, ref availableScroll, Mathf.Max(list.height, candidates.Count * 30f + 4f), this, delegate(Rect view)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    ThingDef def = candidates[i];
                    Rect row = new Rect(0f, i * 30f, view.width, 27f);
                    Widgets.DrawBoxSolid(row, new Color(0.13f, 0.14f, 0.17f, 0.95f));
                    Widgets.Label(new Rect(row.x + 6f, row.y + 4f, row.width - 12f, 20f), def.LabelCap);
                    if (Widgets.ButtonInvisible(row))
                    {
                        AddAlternative(def.defName);
                        break;
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
            rule.Alternatives.Add(new CellAlternative
            {
                DefName = defName,
                Priority = rule.Alternatives.Count
            });
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
        }

        private void CreateSet()
        {
            EquipmentSet created = new EquipmentSet { Name = "New equipment", Role = EquipmentSetRole.Principal };
            EquipmentSetStore.Save(created);
            set = created;
            selectedCell = null;
            selectedTab = 0;
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
                Messages.Message(T("OrderedWork.Equipment.Editor.Saved", "Equipment set saved."), MessageTypeDefOf.TaskCompletion, false);
            }
        }

        private static string TabFor(ApparelSlotKey cell)
        {
            switch (cell.ApparelLayerDefName)
            {
                case "OnSkin":
                case "OnSkin_A":
                case "Underwear":
                case "UnderwearTop":
                    return "Skin";
                case "Middle":
                case "Middle_A":
                    return "Middle";
                case "Shell":
                case "Shell_A":
                case "DankPyon_LegShell":
                case "DankPyon_ShellAttachment":
                    return "Outer";
                case "Belt":
                case "Belt_A":
                case "AM_Pocket":
                    return "Utility";
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
                default:
                    return "Other";
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
