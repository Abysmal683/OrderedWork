using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.Core;
using OrderedWork.Data;
using OrderedWork.Localization;
using OrderedWork.Services;
using OrderedWork.UI;
using RimWorld;
using Verse;

namespace OrderedWork.Harmony
{
    [HarmonyLib.HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    internal static class EquipmentGizmoPatch
    {
        [HarmonyLib.HarmonyPostfix]
        private static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            if (__result != null)
            {
                foreach (Gizmo gizmo in __result)
                {
                    yield return gizmo;
                }
            }

            if (__instance == null || !__instance.IsColonistPlayerControlled || __instance.Downed
                || OrderedWorkMod.Settings == null || !OrderedWorkMod.Settings.equipmentEnabled)
            {
                yield break;
            }

            if (Find.Selector.SelectedPawns.Count == 0 || Find.Selector.SelectedPawns[0] != __instance)
            {
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = OrderedWorkText.T("OrderedWork.Equipment.Gizmo", "Equipment"),
                defaultDesc = OrderedWorkText.T("OrderedWork.Equipment.GizmoTip", "Equip the active set or widen the search area."),
                icon = TexButton.Info,
                action = delegate { ShowMenu(__instance); }
            };
        }

        private static void ShowMenu(Pawn source)
        {
            try
            {
                int radius = OrderedWorkMod.Settings?.equipmentSearchRadius ?? 30;
                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption(
                        string.Format(OrderedWorkText.T("OrderedWork.Equipment.GizmoCurrent", "Equip now · radius {0}"), radius),
                        delegate { Run(source, null, false); }),
                    new FloatMenuOption(
                        string.Format(OrderedWorkText.T("OrderedWork.Equipment.GizmoRadius", "Equip now · radius {0}"), 100),
                        delegate { Run(source, 100, false); }),
                    new FloatMenuOption(
                        string.Format(OrderedWorkText.T("OrderedWork.Equipment.GizmoRadius", "Equip now · radius {0}"), 200),
                        delegate { Run(source, 200, false); }),
                    new FloatMenuOption(
                        OrderedWorkText.T("OrderedWork.Equipment.GizmoWholeMap", "Equip now · whole map"),
                        delegate { Run(source, null, true); }),
                    new FloatMenuOption(
                        OrderedWorkText.T("OrderedWork.Equipment.GizmoOpen", "Open Equipment"),
                        delegate { Find.WindowStack.Add(new Dialog_EquipmentEditor(LoadedModManager.GetMod<OrderedWorkMod>())); })
                };
                Find.WindowStack.Add(new FloatMenu(options));
            }
            catch (Exception exception)
            {
                Log.ErrorOnce("[Ordered Work] Equipment gizmo failed: " + exception, 817238);
            }
        }

        private static void Run(Pawn source, int? radius, bool wholeMap)
        {
            List<Pawn> pawns = Find.Selector.SelectedPawns
                .Where(value => value != null && value.IsColonistPlayerControlled && !value.Downed)
                .ToList();
            if (pawns.Count == 0)
            {
                pawns.Add(source);
            }

            List<EquipmentSet> sets = EquipmentSetStore.LoadAll();
            foreach (Pawn pawn in pawns)
            {
                PawnWorkOrder? entry = OrderedWorkGameComponent.GetOrCreate()?.GetEntry(pawn);
                EquipmentCatalogSnapshot catalog = EquipmentCatalogBuilder.Build(pawn);
                EquipmentExecutionService.EquipNow(pawn, entry, catalog, sets, radius, wholeMap);
            }
        }
    }
}
