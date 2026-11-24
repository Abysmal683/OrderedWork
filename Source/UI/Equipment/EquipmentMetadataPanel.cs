using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Core;
using OrderedWork.Localization;
using OrderedWork.Services;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    internal sealed class EquipmentMetadataState
    {
        public string? SelectedDefName;
        public string? CompareDefName;
        public bool Compare;
    }

    /// <summary>
    /// Reusable 40/60 metadata panel. Summary, detail and two-item comparison.
    /// </summary>
    internal static class EquipmentMetadataPanel
    {
        private static readonly Dictionary<string, Thing> thingsByDef =
            new Dictionary<string, Thing>(StringComparer.Ordinal);
        private static Vector2 summaryScroll;

        public static void Draw(
            Rect rect,
            EquipmentSet? set,
            EquipmentCatalogSnapshot catalog,
            EquipmentMetadataState state)
        {
            Widgets.DrawBoxSolid(rect, UiColors.ColumnBackground);
            Rect[] cols = UiFlex.Columns(UiInset.All(rect, 6f), 8f, 1f, 1f);
            DrawSummary(cols[0], set, catalog);
            DrawDetail(cols[1], state, catalog);
        }

        private static void DrawSummary(Rect area, EquipmentSet? set, EquipmentCatalogSnapshot catalog)
        {
            UiRows rows = new UiRows(area, 24f, 6f);
            Widgets.Label(rows.Next(), OrderedWorkText.T("OrderedWork.Equipment.Metadata.Summary", "Summary"));
            Rect list = rows.Remaining;
            if (set == null)
            {
                Widgets.Label(list, OrderedWorkText.T("OrderedWork.Equipment.Metadata.NoSet", "No set."));
                return;
            }

            List<SummaryRow> summary = BuildSummary(set, catalog);
            UiScrollPane.Draw(list, ref summaryScroll, Mathf.Max(list.height, summary.Count * 32f + 4f), typeof(EquipmentMetadataPanel), delegate(Rect view)
            {
                UiRows listRows = new UiRows(view, 28f, 4f);
                foreach (SummaryRow item in summary)
                {
                    Rect row = listRows.Next();
                    string sub = item.DefName + " · " + ArmorLine(item.DefName);
                    UiCard.Draw(row, item.Label, sub, false, null, item.Tooltip);
                }
            });
        }

        private static void DrawDetail(Rect area, EquipmentMetadataState state, EquipmentCatalogSnapshot catalog)
        {
            UiRows rows = new UiRows(area, 24f, 6f);
            Rect header = rows.Next(28f);
            Rect[] headerCols = UiFlex.Columns(header, 6f, 2f, 1f, 1f);
            Widgets.Label(headerCols[0], OrderedWorkText.T("OrderedWork.Equipment.Metadata.Detail", "Detail"));
            if (UiButtonBar.Button(headerCols[1], OrderedWorkText.T("OrderedWork.Equipment.Metadata.Compare", "Compare"), true, null, state.Compare ? UiColors.WarningText : null))
            {
                state.Compare = !state.Compare;
                if (state.Compare && string.IsNullOrWhiteSpace(state.CompareDefName))
                {
                    state.CompareDefName = state.SelectedDefName;
                }
            }
            if (state.Compare && UiButtonBar.Button(headerCols[2], OrderedWorkText.T("OrderedWork.Equipment.Metadata.Pick", "Pick")))
            {
                List<FloatMenuOption> options = catalog.Footprints.Keys
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .Take(200)
                    .Select(defName => new FloatMenuOption(defName, delegate { state.CompareDefName = defName; }))
                    .ToList();
                if (options.Count > 0)
                {
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }

            Rect body = rows.Remaining;
            if (state.Compare)
            {
                Rect[] panes = UiFlex.Columns(body, 8f, 1f, 1f);
                DrawItemPane(panes[0], state.SelectedDefName);
                DrawItemPane(panes[1], state.CompareDefName);
            }
            else
            {
                DrawItemPane(body, state.SelectedDefName);
            }
        }

        private static void DrawItemPane(Rect area, string? selectedDefName)
        {
            if (string.IsNullOrWhiteSpace(selectedDefName))
            {
                Widgets.Label(area, OrderedWorkText.T("OrderedWork.Equipment.Metadata.Select", "Select an item."));
                return;
            }

            ThingDef? def = DefDatabase<ThingDef>.GetNamedSilentFail(selectedDefName);
            if (def == null)
            {
                Widgets.Label(area, selectedDefName);
                return;
            }

            Thing? thing = GetThing(def);
            UiRows rows = new UiRows(area, 24f, 6f);
            Rect header = rows.Next(28f);
            Rect[] headerCols = UiFlex.Columns(header, 6f, 3f, 1f);
            Widgets.Label(headerCols[0], def.LabelCap.ToString());
            if (UiButtonBar.Button(headerCols[1], OrderedWorkText.T("OrderedWork.Equipment.Metadata.InfoCard", "Info card")))
            {
                if (thing != null)
                {
                    Find.WindowStack.Add(new Dialog_InfoCard(thing));
                }
            }

            if (thing == null)
            {
                Widgets.Label(rows.Remaining, def.LabelCap.ToString());
                return;
            }

            string material = def.MadeFromStuff
                ? (thing.Stuff?.LabelCap.ToString() ?? OrderedWorkText.T("OrderedWork.Equipment.Metadata.NoMaterial", "none"))
                : OrderedWorkText.T("OrderedWork.Equipment.Metadata.NoMaterial", "none");
            Widgets.Label(rows.Next(20f), OrderedWorkText.T("OrderedWork.Equipment.Metadata.Material", "Material") + ": " + material);
            if (def.HasComp(typeof(CompQuality)))
            {
                Widgets.Label(rows.Next(20f), OrderedWorkText.T("OrderedWork.Equipment.Metadata.Quality", "Quality") + ": "
                    + OrderedWorkText.T("OrderedWork.Equipment.Metadata.DefaultQuality", "Normal"));
            }

            Rect statsRect = rows.Remaining;
            StatsReportUtility.DrawStatsReport(statsRect, thing);
        }
        private static List<SummaryRow> BuildSummary(EquipmentSet set, EquipmentCatalogSnapshot catalog)
        {
            List<SummaryRow> result = new List<SummaryRow>();
            foreach (CellRule rule in set.Cells)
            {
                if (rule.Alternatives.Count == 0)
                {
                    continue;
                }
                string defName = rule.Alternatives.OrderBy(value => value.Priority).First().DefName;
                if (!catalog.Footprints.TryGetValue(defName, out ApparelFootprint? footprint))
                {
                    continue;
                }
                foreach (ApparelSlotKey cell in footprint.Cells)
                {
                    BodyPartGroupDef? group = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail(cell.BodyPartGroupDefName);
                    ApparelLayerDef? layer = DefDatabase<ApparelLayerDef>.GetNamedSilentFail(cell.ApparelLayerDefName);
                    result.Add(new SummaryRow
                    {
                        Label = (group == null ? cell.BodyPartGroupDefName : group.LabelCap.ToString())
                            + " · " + (layer == null ? cell.ApparelLayerDefName : layer.LabelCap.ToString()),
                        DefName = defName,
                        Tooltip = ArmorLine(defName)
                    });
                }
            }

            HashSet<string> activeDefs = new HashSet<string>(
                result.Select(row => row.DefName), StringComparer.Ordinal);
            foreach (string defName in activeDefs)
            {
                ThingDef? def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                Thing? thing = def == null ? null : GetThing(def);
                if (thing == null)
                {
                    continue;
                }
                result.Add(new SummaryRow
                {
                    Label = OrderedWorkText.T("OrderedWork.Equipment.Metadata.Insulation", "Insulation") + " · " + (def?.LabelCap.ToString() ?? defName),
                    DefName = defName,
                    Tooltip = OrderedWorkText.T("OrderedWork.Equipment.Metadata.Cold", "Cold") + " "
                        + thing.GetStatValue(StatDefOf.Insulation_Cold).ToStringTemperature()
                        + " · " + OrderedWorkText.T("OrderedWork.Equipment.Metadata.Heat", "Heat") + " "
                        + thing.GetStatValue(StatDefOf.Insulation_Heat).ToStringTemperature()
                });
            }
            return result;
        }

        private static Thing? GetThing(ThingDef def)
        {
            if (thingsByDef.TryGetValue(def.defName, out Thing? cached))
            {
                return cached;
            }
            ThingDef? stuff = def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null;
            Thing thing = ThingMaker.MakeThing(def, stuff);
            thingsByDef[def.defName] = thing;
            return thing;
        }

        private static string ArmorLine(string defName)
        {
            ThingDef? def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            Thing? thing = def == null ? null : GetThing(def);
            if (thing == null)
            {
                return "-";
            }
            return "S " + thing.GetStatValue(StatDefOf.ArmorRating_Sharp).ToStringPercent()
                + " · B " + thing.GetStatValue(StatDefOf.ArmorRating_Blunt).ToStringPercent()
                + " · H " + thing.GetStatValue(StatDefOf.ArmorRating_Heat).ToStringPercent();
        }

        private sealed class SummaryRow
        {
            public string Label = "";
            public string DefName = "";
            public string Tooltip = "";
        }
    }
}
