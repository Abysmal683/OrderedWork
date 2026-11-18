using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OrderedWork.Config;
using OrderedWork.Core;
using OrderedWork.Services;
using OrderedWork.UI;
using RimWorld;
using UnityEngine;
using Verse;
using System.Text;

namespace OrderedWork
{
    public partial class OrderedWorkMod : Mod
    {        private void DrawWorkTypeSelector(Rect rect, string prefix, string selectedDefName, string title, Action<string> onSelect, string excludeDefName = null)
        {
            WorkTypeDef selected = DefDatabase<WorkTypeDef>.GetNamedSilentFail(selectedDefName);
            string label = selected != null ? WorkTypePaletteView.DisplayName(selected) : "(select)";
            if (!Widgets.ButtonText(rect, prefix + label + "  ▼"))
            {
                return;
            }

            List<WorkTypeDef> options = DefDatabase<WorkTypeDef>.AllDefsListForReading
                .Where(w => w != null && w.visible && (string.IsNullOrEmpty(excludeDefName) || w.defName != excludeDefName))
                .OrderByDescending(w => w.naturalPriority)
                .ToList();

            Find.WindowStack.Add(new WorkTypeDropdownWindow(
                title,
                options,
                selected,
                delegate(WorkTypeDef wt)
                {
                    if (wt != null && onSelect != null)
                    {
                        onSelect(wt.defName);
                    }
                }));
        }

        private void SaveSplit()
        {
            if (!string.IsNullOrEmpty(SplitValidationWarning()))
            {
                return;
            }

            string newDefName = BuildSplitDefName(splitName);
            WorkConfigProfile profile = LoadEditingProfile();
            if (profile == null || !profile.WorkTypes.TryGetValue(splitOrigin, out WorkTypeConfig origin) || origin == null)
            {
                return;
            }

            List<string> incoming = splitDraft.Distinct().ToList();
            foreach (string giverName in incoming)
            {
                foreach (WorkTypeConfig workType in profile.WorkTypes.Values)
                {
                    if (workType != null && workType.Givers != null)
                    {
                        workType.Givers.RemoveAll(g => g != null && g.DefName == giverName);
                        for (int i = 0; i < workType.Givers.Count; i++)
                        {
                            workType.Givers[i].Order = i;
                        }
                    }
                }
            }

            WorkTypeConfig split = new WorkTypeConfig
            {
                Vanilla = false,
                Base = splitOrigin,
                Label = splitName.Trim(),
                Description = splitDescription.Trim(),
                Visible = true,
                Order = profile.WorkTypes.Count == 0 ? 0 : profile.WorkTypes.Values.Max(w => w != null ? w.Order : 0) + 1,
                Mod = Content != null && Content.ModMetaData != null ? Content.PackageId : "nightmare123487.OrderedWork",
                Givers = incoming.Select((name, index) => new GiverConfig
                {
                    DefName = name,
                    Order = index,
                    Mod = DefDatabase<WorkGiverDef>.GetNamedSilentFail(name)?.modContentPack?.PackageId
                }).ToList()
            };
            profile.WorkTypes[newDefName] = split;
            if (!origin.Vanilla && (origin.Givers == null || origin.Givers.Count == 0))
            {
                profile.WorkTypes.Remove(splitOrigin);
            }
            if (origin.Vanilla && (origin.Givers == null || origin.Givers.Count == 0))
            {
                origin.Visible = false;
            }
            WorkConfigStore.SaveProfile(profile, true);
            WorkConfigStore.SavePending(profile);
            OrderedWorkStartup.ApplyProfileNow(profile, Settings, this);
            Messages.Message("Split saved: " + splitName + " (restart to apply).", MessageTypeDefOf.TaskCompletion, false);

            splitName = "";
            splitDescription = "";
            splitDraft.Clear();
            splitDraftScroll = 0f;
            splitDirty = false;
        }

        private void SaveMerge(List<string> givers)
        {
            if (givers == null || givers.Count == 0)
            {
                return;
            }
            if (DefDatabase<WorkTypeDef>.GetNamedSilentFail(mergeTarget) == null || mergeTarget == mergeOrigin)
            {
                return;
            }

            WorkConfigProfile profile = LoadEditingProfile();
            if (profile == null
                || !profile.WorkTypes.TryGetValue(mergeOrigin, out WorkTypeConfig origin)
                || origin == null
                || !profile.WorkTypes.TryGetValue(mergeTarget, out WorkTypeConfig target)
                || target == null)
            {
                return;
            }

            List<string> incoming = givers.Distinct().ToList();
            target.Givers = target.Givers ?? new List<GiverConfig>();
            List<string> targetOrder = target.Givers
                .Where(g => g != null).OrderBy(g => g.Order).Select(g => g.DefName).ToList();
            foreach (string giverName in incoming)
            {
                if (!targetOrder.Contains(giverName))
                {
                    targetOrder.Add(giverName);
                }
            }

            foreach (string giverName in incoming)
            {
                foreach (WorkTypeConfig workType in profile.WorkTypes.Values)
                {
                    if (workType != null && workType.Givers != null)
                    {
                        workType.Givers.RemoveAll(g => g != null && g.DefName == giverName);
                        for (int i = 0; i < workType.Givers.Count; i++)
                        {
                            workType.Givers[i].Order = i;
                        }
                    }
                }
                target.Givers.Add(new GiverConfig
                {
                    DefName = giverName,
                    Order = target.Givers.Count,
                    Mod = DefDatabase<WorkGiverDef>.GetNamedSilentFail(giverName)?.modContentPack?.PackageId
                });
            }
            target.Givers = targetOrder.Select((name, index) =>
            {
                GiverConfig giver = target.Givers.FirstOrDefault(g => g != null && g.DefName == name);
                if (giver == null)
                {
                    giver = new GiverConfig { DefName = name };
                }
                giver.Order = index;
                return giver;
            }).ToList();

            // An emptied split cannot exist in the canonical profile. Vanilla types stay
            // hidden until another mod gives them content.
            if (!origin.Vanilla && (origin.Givers == null || origin.Givers.Count == 0))
            {
                profile.WorkTypes.Remove(mergeOrigin);
            }
            if (origin.Vanilla && (origin.Givers == null || origin.Givers.Count == 0))
            {
                origin.Visible = false;
            }

            WorkConfigStore.SaveProfile(profile, true);
            WorkConfigStore.SavePending(profile);
            OrderedWorkStartup.ApplyProfileNow(profile, Settings, this);
            Messages.Message("Merge saved for " + mergeTarget + " (restart to apply).", MessageTypeDefOf.TaskCompletion, false);
            mergeDraft.Clear();
            mergeTargetScroll = 0f;
            mergeDirty = false;
        }

        private string BuildSplitDefName(string label)
        {
            string baseName = Sanitize(label ?? "");
            if (string.IsNullOrEmpty(baseName))
            {
                baseName = "Split";
            }

            WorkConfigProfile currentProfile = LoadEditingProfile();
            int suffix = (currentProfile != null ? currentProfile.WorkTypes.Count : 0) + 1;
            string defName = "OrderedWork_" + baseName + "_" + suffix;
            while ((currentProfile != null && currentProfile.WorkTypes.ContainsKey(defName))
                || DefDatabase<WorkTypeDef>.GetNamedSilentFail(defName) != null)
            {
                suffix++;
                defName = "OrderedWork_" + baseName + "_" + suffix;
            }
            return defName;
        }

        private static bool GiverBelongsTo(string giverName, string workTypeDefName)
        {
            if (string.IsNullOrEmpty(giverName) || string.IsNullOrEmpty(workTypeDefName))
            {
                return false;
            }
            WorkGiverDef giver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(giverName);
            return giver != null && giver.workType != null && giver.workType.defName == workTypeDefName;
        }

        private static List<WorkGiverDef> GiversForWorkType(string workTypeDefName)
        {
            if (string.IsNullOrEmpty(workTypeDefName))
            {
                return new List<WorkGiverDef>();
            }
            return DefDatabase<WorkGiverDef>.AllDefsListForReading
                .Where(g => g != null && g.workType != null && g.workType.defName == workTypeDefName)
                .OrderByDescending(g => g.priorityInType)
                .ToList();
        }

    }
}