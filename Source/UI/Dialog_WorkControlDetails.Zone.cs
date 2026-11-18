using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Data;
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

        private void DrawZoneTab(Rect content)
        {
            EnsureZoneState();
            float y = 0f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, content.width, 24f), "Zona");
            Text.Font = GameFont.Small;
            y += 30f;
            Widgets.Label(new Rect(0f, y, content.width, 20f),
                "Asigna una zona existente. No se crean zonas desde esta sección; el gestor vanilla permite crear, borrar y editar áreas.");
            y += 28f;

            if (Widgets.ButtonText(new Rect(0f, y, 220f, 26f), "Mapa: " + MapLabel(detailZoneMap)))
            {
                ShowMapMenu();
            }
            y += 32f;

            Rect areaRect = new Rect(0f, y, 300f, 28f);
            if (detailZoneArea == null)
            {
                Widgets.Label(new Rect(areaRect.x, areaRect.y + 5f, 16f, 16f), "□");
            }
            else
            {
                Widgets.DrawBoxSolid(new Rect(areaRect.x, areaRect.y + 5f, 16f, 16f), detailZoneArea.Color);
            }
            string areaLabel = detailZoneArea == null ? "Sin restricciones" : detailZoneArea.Label;
            if (Widgets.ButtonText(new Rect(areaRect.x + 20f, areaRect.y, areaRect.width - 20f, areaRect.height), areaLabel))
            {
                ShowAreaMenu();
            }
            if (Widgets.ButtonText(new Rect(306f, y, 120f, 28f), "Apply zone"))
            {
                ApplyZone();
            }
            y += 36f;

            if (Widgets.ButtonText(new Rect(0f, y, 150f, 26f), "Manage zones"))
            {
                if (detailZoneMap != null)
                {
                    Find.WindowStack.Add(new Dialog_ManageAreas(detailZoneMap));
                }
            }
            if (Mouse.IsOver(new Rect(0f, y, 150f, 26f)))
            {
                TooltipHandler.TipRegion(new Rect(0f, y, 150f, 26f),
                    "Open the vanilla area manager. Create, delete, expand or reduce areas there; Ordered Work only assigns existing areas.");
            }
            y += 36f;

            PawnWorkOrder entry = OrderedWorkGameComponent.GetOrCreate()?.GetOrCreateEntry(pawn);
            PawnZoneBinding binding = entry?.zones?.FirstOrDefault(z => z.mapUniqueId == (detailZoneMap != null ? detailZoneMap.uniqueID : 0));
            if (binding != null && detailZoneArea == null && !binding.unrestricted)
            {
                GUI.color = new Color(1f, 0.75f, 0.35f, 1f);
                Widgets.Label(new Rect(0f, y, content.width, 20f),
                    "Aviso: la zona guardada ya no existe en este mapa. Se conserva la referencia.");
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
                    .Where(a => a is Area_Allowed)
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
