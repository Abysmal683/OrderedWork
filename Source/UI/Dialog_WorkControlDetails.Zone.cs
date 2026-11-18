using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Data;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    public partial class Dialog_WorkControlDetails
    {
        private Map detailZoneMap;
        private Area detailZoneArea;
        private bool detailZoneInitialized;
        private bool detailZoneMissing;

        private void DrawZoneTab(Rect content)
        {
            EnsureZoneState();
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, content.width, 24f), "Zona");
            Text.Font = GameFont.Small;
            UiRows rows = new UiRows(new Rect(0f, 30f, content.width, Mathf.Max(0f, content.height - 30f)), 26f, 6f);

            Rect zoneInfo = rows.Next(20f);
            Widgets.Label(zoneInfo, "Asigna un área existente. Usa Manage zones para crear o editar.");
            TooltipHandler.TipRegion(zoneInfo,
                "No se crean zonas desde esta sección. El gestor vanilla permite crear, borrar, expandir y reducir áreas.");

            Rect mapRow = rows.Next(26f);
            UiTable mapColumns = new UiTable(mapRow, 4f);
            mapColumns.Add(48f).Add(230f).Flex();
            Widgets.Label(mapColumns.Column(0), "Mapa:");
            if (Widgets.ButtonText(mapColumns.Column(1), MapLabel(detailZoneMap)))
            {
                ShowMapMenu();
            }

            Rect areaRow = rows.Next(26f);
            Rect swatch = new Rect(areaRow.x, areaRow.y + 5f, 16f, 16f);
            if (detailZoneArea == null)
            {
                Widgets.DrawBoxSolid(swatch, new Color(0.65f, 0.65f, 0.65f, 0.18f));
                Widgets.DrawBox(swatch);
            }
            else
            {
                Widgets.DrawBoxSolid(swatch, detailZoneArea.Color);
            }
            string areaLabel = detailZoneArea == null ? "Sin restricciones" : detailZoneArea.Label;
            if (Widgets.ButtonText(new Rect(areaRow.x + 20f, areaRow.y, Mathf.Min(300f, Mathf.Max(40f, areaRow.width - 20f)), areaRow.height), areaLabel))
            {
                ShowAreaMenu();
            }
            bool zoneChanged = ZoneHasChanges();

            PawnWorkOrder lastEntry = OrderedWorkGameComponent.GetOrCreate()?.GetEntry(pawn);
            PawnZoneBinding lastBinding = lastEntry?.zones?.FirstOrDefault(
                z => z.mapUniqueId == (detailZoneMap != null ? detailZoneMap.uniqueID : 0));
            string lastZone;
            if (lastBinding != null)
            {
                lastZone = lastBinding.unrestricted ? "Sin restricciones" : lastBinding.areaLabel;
            }
            else if (detailZoneMap == pawn.Map && pawn.playerSettings != null)
            {
                Area currentArea = pawn.playerSettings.AreaRestrictionInPawnCurrentMap;
                lastZone = currentArea != null ? currentArea.Label : "Sin restricciones";
            }
            else
            {
                lastZone = "none";
            }
            Widgets.Label(rows.Next(20f), "Last applied on this map: " + lastZone);

            Rect buttonRow = rows.Next(26f);
            Rect applyRect = new Rect(buttonRow.x, buttonRow.y, 130f, buttonRow.height);
            Rect manageRect = new Rect(buttonRow.x + 134f, buttonRow.y, 130f, buttonRow.height);
            if (UiButtonBar.Button(applyRect, "Apply zone", zoneChanged && !detailZoneMissing))
            {
                ApplyZone();
            }
            if (Mouse.IsOver(applyRect) && (!zoneChanged || detailZoneMissing))
            {
                TooltipHandler.TipRegion(applyRect,
                    detailZoneMissing
                        ? "The saved area no longer exists. Choose an existing area first."
                        : "Apply is enabled only when the selected zone differs from the applied zone.");
            }
            if (UiButtonBar.Button(manageRect, "Manage zones", true))
            {
                if (detailZoneMap != null)
                {
                    Find.WindowStack.Add(new Dialog_ManageAreas(detailZoneMap));
                }
            }
            if (Mouse.IsOver(manageRect))
            {
                TooltipHandler.TipRegion(manageRect,
                    "Open the vanilla area manager. Create, delete, expand or reduce areas there; Ordered Work only assigns existing areas.");
            }

            PawnWorkOrder entry = OrderedWorkGameComponent.GetOrCreate()?.GetOrCreateEntry(pawn);
            PawnZoneBinding binding = entry?.zones?.FirstOrDefault(z => z.mapUniqueId == (detailZoneMap != null ? detailZoneMap.uniqueID : 0));
            if (detailZoneMissing || (binding != null && detailZoneArea == null && !binding.unrestricted))
            {
                GUI.color = new Color(1f, 0.75f, 0.35f, 1f);
                Widgets.Label(rows.Next(20f), "Aviso: la zona guardada ya no existe en este mapa. Se conserva la referencia.");
                GUI.color = Color.white;
            }
        }
        private void EnsureZoneState()
        {
            if (detailZoneMap == null)
            {
                detailZoneMap = pawn != null ? pawn.Map : Find.CurrentMap;
            }
            if (!detailZoneInitialized)
            {
                detailZoneInitialized = true;
                PawnWorkOrder entry = OrderedWorkGameComponent.GetOrCreate()?.GetEntry(pawn);
                PawnZoneBinding binding = entry?.zones?.FirstOrDefault(z => z.mapUniqueId == (detailZoneMap != null ? detailZoneMap.uniqueID : 0));
                if (binding != null)
                {
                    detailZoneArea = ResolveArea(detailZoneMap, binding);
                    detailZoneMissing = !binding.unrestricted && detailZoneArea == null;
                    if (detailZoneMissing && detailZoneMap == pawn.Map && pawn.playerSettings != null)
                    {
                        detailZoneArea = pawn.playerSettings.AreaRestrictionInPawnCurrentMap;
                    }
                }
                else if (detailZoneMap == pawn?.Map && pawn?.playerSettings != null)
                {
                    detailZoneArea = pawn.playerSettings.AreaRestrictionInPawnCurrentMap;
                }
            }
        }

        private static string MapLabel(Map map)
        {
            return map == null ? "—" : map.ToString();
        }

        private static Area ResolveArea(Map map, PawnZoneBinding binding)
        {
            if (map == null || binding == null || binding.unrestricted)
            {
                return null;
            }
            return map.areaManager.AllAreas
                .FirstOrDefault(a => a != null && (a.ID == binding.areaId || a.GetUniqueLoadID() == binding.areaUniqueId));
        }

        private void ShowMapMenu()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (Map map in Find.Maps)
            {
                Map captured = map;
                options.Add(new FloatMenuOption(MapLabel(captured), delegate
                {
                    detailZoneMap = captured;
                    PawnWorkOrder entry = OrderedWorkGameComponent.GetOrCreate()?.GetEntry(pawn);
                    PawnZoneBinding binding = entry?.zones?.FirstOrDefault(z => z.mapUniqueId == captured.uniqueID);
                    detailZoneArea = binding != null
                        ? ResolveArea(captured, binding)
                        : (captured == pawn.Map ? pawn.playerSettings?.AreaRestrictionInPawnCurrentMap : null);
                }));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void ShowAreaMenu()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            options.Add(new FloatMenuOption("Sin restricciones", delegate
            {
                detailZoneArea = null;
            }));
            if (detailZoneMap != null)
            {
                foreach (Area area in detailZoneMap.areaManager.AllAreas
                    .Where(a => a is Area_Allowed || a is Area_Home)
                    .OrderBy(a => a.Label, StringComparer.OrdinalIgnoreCase))
                {
                    Area captured = area;
                    options.Add(new FloatMenuOption(captured.Label, delegate
                    {
                        detailZoneArea = captured;
                    }));
                }
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private bool ZoneHasChanges()
        {
            if (detailZoneMap == null)
            {
                return false;
            }
            PawnWorkOrder entry = OrderedWorkGameComponent.GetOrCreate()?.GetEntry(pawn);
            PawnZoneBinding binding = entry?.zones?.FirstOrDefault(z => z.mapUniqueId == detailZoneMap.uniqueID);
            if (binding != null)
            {
                if (binding.unrestricted != (detailZoneArea == null))
                {
                    return true;
                }
                if (detailZoneArea == null)
                {
                    return false;
                }
                return binding.areaId != detailZoneArea.ID
                    && binding.areaUniqueId != detailZoneArea.GetUniqueLoadID();
            }
            if (detailZoneMap == pawn.Map && pawn.playerSettings != null)
            {
                Area current = pawn.playerSettings.AreaRestrictionInPawnCurrentMap;
                if (current == null && detailZoneArea == null)
                {
                    return false;
                }
                if (current == null || detailZoneArea == null)
                {
                    return true;
                }
                return current.ID != detailZoneArea.ID
                    || current.GetUniqueLoadID() != detailZoneArea.GetUniqueLoadID();
            }
            return true;
        }
        private void ApplyZone()
        {
            if (detailZoneMap == null)
            {
                return;
            }
            OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
            PawnWorkOrder entry = component?.GetOrCreateEntry(pawn);
            if (entry == null)
            {
                return;
            }
            entry.zones.RemoveAll(z => z.mapUniqueId == detailZoneMap.uniqueID);
            entry.zones.Add(new PawnZoneBinding
            {
                mapUniqueId = detailZoneMap.uniqueID,
                areaId = detailZoneArea != null ? detailZoneArea.ID : 0,
                areaUniqueId = detailZoneArea != null ? detailZoneArea.GetUniqueLoadID() : null,
                areaLabel = detailZoneArea != null ? detailZoneArea.Label : null,
                unrestricted = detailZoneArea == null,
                source = PolicySource.Manual
            });

            if (detailZoneMap == pawn.Map && pawn.playerSettings != null)
            {
                pawn.playerSettings.AreaRestrictionInPawnCurrentMap = detailZoneArea;
            }
        }
    }
}