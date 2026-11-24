using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.Core;
using OrderedWork.Data;
using OrderedWork.Localization;
using OrderedWork.Services;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    public partial class Dialog_WorkControlDetails
    {
        private string? selectedEquipmentSetId;
        private Vector2 equipmentLeftScroll;
        private Vector2 equipmentRightScroll;
        private Vector2 equipmentOverrideScroll;
        private EquipmentMetadataState equipmentMetadata = new EquipmentMetadataState();
        private bool equipmentOverridesView;

        private void DrawEquipmentTab(Rect content)
        {
            try
            {
                PawnWorkOrder? entry = OrderedWorkGameComponent.GetOrCreate()?.GetEntry(pawn);
                EquipmentCatalogSnapshot catalog = EquipmentCatalogBuilder.Build(pawn);
                List<EquipmentSet> sets = EquipmentSetStore.LoadAll();
                EquipmentSet? selectedSet = selectedEquipmentSetId == null
                    ? null
                    : sets.FirstOrDefault(value => value.Id == selectedEquipmentSetId);

                UiRows root = new UiRows(content, 20f, 8f);
                Rect top = root.Next(Mathf.Max(280f, content.height - 300f));
                Rect[] cols = UiFlex.Columns(top, 8f, 1.25f, 1f);
                DrawCurrentGear(cols[0], entry, catalog);
                DrawEquipmentSets(cols[1], entry, sets);
                DrawEquipmentMetadata(root.Remaining, selectedSet, catalog);
            }
            catch (Exception exception)
            {
                Log.ErrorOnce("[Ordered Work] Equipment tab failed: " + exception, 817235);
                Widgets.Label(content, OrderedWorkText.T("OrderedWork.Equipment.TabFailed", "Equipment tab unavailable."));
            }
        }

        private void DrawCurrentGear(Rect area, PawnWorkOrder? entry, EquipmentCatalogSnapshot catalog)
        {
            float capacity = pawn.GetStatValue(StatDefOf.CarryingCapacity);
            float gearMass = MassUtility.GearAndInventoryMass(pawn);
            string weight = gearMass.ToString("0.#") + " / " + capacity.ToString("0.#") + " kg";
            string carrying = pawn.carryTracker?.CarriedThing?.LabelCap.ToString() ?? "—";

            Widgets.Label(new Rect(area.x, area.y, area.width, 22f), OrderedWorkText.T("OrderedWork.Equipment.CurrentGear", "Current gear"));
            Widgets.Label(new Rect(area.x, area.y + 22f, area.width, 20f),
                OrderedWorkText.T("OrderedWork.Equipment.Weight", "Weight") + ": " + weight);

            Rect list = new Rect(area.x, area.y + 46f, area.width, area.height - 46f);
            UiScrollPane.Draw(list, ref equipmentLeftScroll, Mathf.Max(list.height, 260f), this, delegate(Rect view)
            {
                UiRows rows = new UiRows(view, 28f, 4f);
                Widgets.Label(rows.Next(22f), OrderedWorkText.T("OrderedWork.Equipment.Apparel", "Apparel"));
                foreach (Apparel apparel in pawn.apparel?.WornApparel ?? new List<Apparel>())
                {
                    if (apparel == null) continue;
                    Rect row = rows.Next();
                    UiCard.Draw(row, apparel.LabelCap, null, false, null, apparel.def.defName);
                }

                Widgets.Label(rows.Next(22f), OrderedWorkText.T("OrderedWork.Equipment.Primary", "Primary"));
                if (pawn.equipment?.Primary == null)
                {
                    Widgets.Label(rows.Next(), "—");
                }
                else
                {
                    Rect row = rows.Next();
                    UiCard.Draw(row, pawn.equipment.Primary.LabelCap, null, false, null, pawn.equipment.Primary.def.defName);
                }

                Widgets.Label(rows.Next(22f), OrderedWorkText.T("OrderedWork.Equipment.Inventory", "Inventory"));
                if (pawn.inventory?.innerContainer == null || pawn.inventory.innerContainer.Count == 0)
                {
                    Widgets.Label(rows.Next(), "—");
                }
                else
                {
                    foreach (Thing thing in pawn.inventory.innerContainer)
                    {
                        if (thing == null) continue;
                        Rect row = rows.Next();
                        UiCard.Draw(row, thing.LabelCap, "x" + thing.stackCount, false, null, thing.def.defName);
                    }
                }

                Widgets.Label(rows.Next(22f), OrderedWorkText.T("OrderedWork.Equipment.Carrying", "Carrying"));
                Widgets.Label(rows.Next(), carrying);
            });
        }

        private void DrawEquipmentSets(Rect area, PawnWorkOrder? entry, List<EquipmentSet> sets)
        {
            Widgets.Label(new Rect(area.x, area.y, area.width, 22f), OrderedWorkText.T("OrderedWork.Equipment.Sets", "Sets"));
            Rect[] actions = UiFlex.Columns(new Rect(area.x + area.width - 236f, area.y - 2f, 236f, 24f), 4f, 1f, 1f);
            if (UiButtonBar.Button(actions[0], OrderedWorkText.T("OrderedWork.Equipment.Overrides", "Overrides")))
            {
                equipmentOverridesView = !equipmentOverridesView;
            }
            if (UiButtonBar.Button(actions[1], OrderedWorkText.T("OrderedWork.Equipment.OpenEditor", "Open editor"), selectedEquipmentSetId != null))
            {
                Find.WindowStack.Add(new Dialog_EquipmentEditor(
                    LoadedModManager.GetMod<OrderedWorkMod>(), selectedEquipmentSetId));
            }

            Rect list = new Rect(area.x, area.y + 28f, area.width, area.height - 28f);
            if (equipmentOverridesView)
            {
                DrawEquipmentOverrides(list, entry);
                return;
            }
            UiScrollPane.Draw(list, ref equipmentRightScroll, Mathf.Max(list.height, 300f), this, delegate(Rect view)
            {
                UiRows rows = new UiRows(view, 28f, 4f);
                foreach (EquipmentSetRole role in new[]
                {
                    EquipmentSetRole.Mandatory,
                    EquipmentSetRole.Emergency,
                    EquipmentSetRole.WorkType,
                    EquipmentSetRole.Principal
                })
                {
                    Widgets.Label(rows.Next(22f), RoleLabel(role));
                    List<EquipmentSet> roleSets = sets.Where(value => value.Role == role).ToList();
                    if (roleSets.Count == 0)
                    {
                        Widgets.Label(rows.Next(), "—");
                        continue;
                    }
                    foreach (EquipmentSet item in roleSets)
                    {
                        Rect row = rows.Next();
                        bool assigned = IsAssigned(entry, role, item.Id);
                        bool selected = string.Equals(selectedEquipmentSetId, item.Id, StringComparison.Ordinal);
                        string sub = assigned
                            ? OrderedWorkText.T("OrderedWork.Equipment.Assigned", "assigned")
                            : OrderedWorkText.T("OrderedWork.Equipment.Available", "available");
                        if (UiCard.Draw(row, item.Name, sub, selected, null,
                            role == EquipmentSetRole.WorkType
                                ? OrderedWorkText.T("OrderedWork.Equipment.WorkTypeHint", "Global WorkType set.")
                                : OrderedWorkText.T("OrderedWork.Equipment.AssignHint", "Right-click to assign or remove.")))
                        {
                            selectedEquipmentSetId = item.Id;
                        }
                        if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && row.Contains(Event.current.mousePosition))
                        {
                            ToggleAssignment(entry, role, item.Id);
                            Event.current.Use();
                        }
                    }
                }
            });
        }

        private void DrawEquipmentMetadata(Rect area, EquipmentSet? selectedSet, EquipmentCatalogSnapshot catalog)
        {
            if (selectedSet == null)
            {
                Widgets.Label(area, OrderedWorkText.T("OrderedWork.Equipment.SelectSet", "Select a set."));
                return;
            }
            EquipmentMetadataPanel.Draw(area, selectedSet, catalog, equipmentMetadata);
        }

        private void DrawEquipmentOverrides(Rect area, PawnWorkOrder? entry)
        {
            if (entry == null || entry.equipmentOverrides.Count == 0)
            {
                Widgets.Label(new Rect(area.x, area.y, area.width, 24f),
                    OrderedWorkText.T("OrderedWork.Equipment.NoOverrides", "No equipment overrides."));
                return;
            }

            if (UiButtonBar.Button(new Rect(area.x, area.y, 150f, 24f),
                OrderedWorkText.T("OrderedWork.Equipment.ClearOverrides", "Clear all overrides")))
            {
                entry.equipmentOverrides.Clear();
                entry.NormalizePolicy();
            }

            Rect list = new Rect(area.x, area.y + 30f, area.width, area.height - 30f);
            UiScrollPane.Draw(list, ref equipmentOverrideScroll, Mathf.Max(list.height, entry.equipmentOverrides.Count * 32f + 4f), this, delegate(Rect view)
            {
                UiRows rows = new UiRows(view, 28f, 4f);
                foreach (PawnEquipmentOverride value in entry.equipmentOverrides.ToList())
                {
                    string label = string.Format(
                        OrderedWorkText.T("OrderedWork.Equipment.OverrideLine", "{0} · {1}"),
                        value.kind, value.defName);
                    string sub = value.forbidden
                        ? OrderedWorkText.T("OrderedWork.Equipment.Forbidden", "forbidden")
                        : value.count.ToString();
                    Rect row = rows.Next();
                    string tip = string.Format(
                        OrderedWorkText.T("OrderedWork.Equipment.OverrideCell", "{0} · {1}"),
                        value.bodyPartGroupDefName, value.apparelLayerDefName);
                    UiCard.Draw(row, label, sub, false, null, tip);
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && row.Contains(Event.current.mousePosition))
                    {
                        entry.equipmentOverrides.Remove(value);
                        entry.NormalizePolicy();
                        Event.current.Use();
                        break;
                    }
                }
            });
        }
        private static void ToggleAssignment(PawnWorkOrder? entry, EquipmentSetRole role, string setId)
        {
            if (entry == null || string.IsNullOrWhiteSpace(setId) || role == EquipmentSetRole.WorkType)
            {
                return;
            }

            List<string>? list = role switch
            {
                EquipmentSetRole.Mandatory => entry.mandatoryEquipmentSetIds,
                EquipmentSetRole.Emergency => entry.emergencyEquipmentSetIds,
                EquipmentSetRole.Principal => entry.principalEquipmentSetIds,
                _ => null
            };
            if (list == null)
            {
                return;
            }
            if (list.Contains(setId))
            {
                list.RemoveAll(value => string.Equals(value, setId, StringComparison.Ordinal));
            }
            else
            {
                list.Add(setId);
            }
            entry.NormalizePolicy();
        }
        private static bool IsAssigned(PawnWorkOrder? entry, EquipmentSetRole role, string setId)
        {
            if (entry == null) return false;
            List<string>? list = role switch
            {
                EquipmentSetRole.Mandatory => entry.mandatoryEquipmentSetIds,
                EquipmentSetRole.Emergency => entry.emergencyEquipmentSetIds,
                EquipmentSetRole.Principal => entry.principalEquipmentSetIds,
                _ => null
            };
            return list != null && list.Contains(setId);
        }

        private static string RoleLabel(EquipmentSetRole role)
        {
            switch (role)
            {
                case EquipmentSetRole.Mandatory:
                    return OrderedWorkText.T("OrderedWork.Equipment.Role.Mandatory", "Mandatory");
                case EquipmentSetRole.Emergency:
                    return OrderedWorkText.T("OrderedWork.Equipment.Role.Emergency", "Emergency");
                case EquipmentSetRole.WorkType:
                    return OrderedWorkText.T("OrderedWork.Equipment.Role.WorkType", "WorkType");
                default:
                    return OrderedWorkText.T("OrderedWork.Equipment.Role.Principal", "Principal");
            }
        }
    }
}
