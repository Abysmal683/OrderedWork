using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.Data;
using OrderedWork.Services;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    public sealed partial class WorkPanel
    {        private void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y, 150f, 26f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Work.Title", "Work"));
            Text.Font = GameFont.Small;

            Rect load = new Rect(rect.xMax - 136f, rect.y + 2f, 64f, 24f);
            Rect save = new Rect(rect.xMax - 68f, rect.y + 2f, 64f, 24f);
            if (UiButtonBar.Button(load, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.Load", "Load"), true, "Load a named Work preset and replace this pawn's Work policy."))
            {
                Find.WindowStack.Add(new Dialog_WorkPreset(true, null, ApplyPreset));
            }
            if (UiButtonBar.Button(save, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.Save", "Save"), true, "Save this pawn's WorkType and WorkGiver order as a named preset."))
            {
                Find.WindowStack.Add(new Dialog_WorkPreset(false, CapturePreset, null, pawn.LabelShort));
            }
        }

        private WorkPreset CapturePreset()
        {
            WorkPreset preset = new WorkPreset
            {
                WorkTypes = new List<string>(workDraft)
            };
            foreach (string workTypeName in workDraft)
            {
                WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(workTypeName);
                if (workType == null)
                {
                    continue;
                }
                preset.WorkTypeLabels[workTypeName] = workType.labelShort;
                int workTypeMod = ModIndex(preset, workType);
                if (workTypeMod >= 0)
                {
                    preset.WorkTypeMods[workTypeName] = workTypeMod;
                }
                List<string>? order;
                if (!giverDrafts.TryGetValue(workTypeName, out order))
                {
                    order = WorkGiverOrderService.GetEffectiveOrder(pawn, workType);
                }
                preset.Givers[workTypeName] = new List<string>(order ?? new List<string>());
                foreach (string giverName in preset.Givers[workTypeName])
                {
                    WorkGiverDef giver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(giverName);
                    int giverMod = ModIndex(preset, giver);
                    if (giverMod >= 0)
                    {
                        preset.GiverMods[giverName] = giverMod;
                    }
                }
            }
            return preset;
        }

        private static int ModIndex(WorkPreset preset, Def def)
        {
            if (def == null || def.modContentPack == null)
            {
                return -1;
            }
            string packageId = def.modContentPack.PackageId;
            if (string.IsNullOrEmpty(packageId))
            {
                return -1;
            }
            int index = preset.Mods.IndexOf(packageId);
            if (index < 0)
            {
                index = preset.Mods.Count;
                preset.Mods.Add(packageId);
            }
            return index;
        }

        private void ApplyPreset(WorkPreset preset)
        {
            if (preset == null)
            {
                return;
            }
            List<string> order = (preset.WorkTypes ?? new List<string>())
                .Where(name =>
                {
                    WorkTypeDef def = DefDatabase<WorkTypeDef>.GetNamedSilentFail(name);
                    return def != null && !WorkOrderService.IsIncapable(pawn, def);
                })
                .Distinct(StringComparer.Ordinal)
                .ToList();
            WorkOrderService.SaveAndApply(pawn, order);
            WorkGiverOrderService.ResetAll(pawn);
            foreach (KeyValuePair<string, List<string>> pair in preset.Givers ?? new Dictionary<string, List<string>>())
            {
                WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(pair.Key);
                if (workType == null || pair.Value == null || pair.Value.Count == 0)
                {
                    continue;
                }
                List<string> valid = pair.Value
                    .Where(name =>
                    {
                        WorkGiverDef giver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(name);
                        return giver != null && giver.workType == workType;
                    })
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                if (valid.Count > 0)
                {
                    WorkGiverOrderService.SaveOrder(pawn, workType, valid);
                }
            }
            workDraft = WorkOrderService.GetOrder(pawn) ?? order;
            selectedWorkType = workDraft.FirstOrDefault();
            giverDrafts.Clear();
            giverBaselines.Clear();
            LoadAllGiverDrafts();
            InvalidateTooltips();
        }

        private void InvalidateTooltips()
        {
            workTypeTooltipCache.Clear();
            giverTooltipCache.Clear();
            skillTooltipCache.Clear();
            skillTooltipCacheTick = -1;
        }

        private void CommitWork()
        {
            InvalidateTooltips();
            WorkOrderService.SaveAndApply(pawn, workDraft);
        }

        private static bool SameOrder(List<string> left, List<string> right)
        {
            if (left == null || right == null || left.Count != right.Count)
            {
                return false;
            }
            for (int i = 0; i < left.Count; i++)
            {
                if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
