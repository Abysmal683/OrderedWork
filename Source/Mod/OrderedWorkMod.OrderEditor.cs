using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using OrderedWork.Config;
using OrderedWork.Core;
using OrderedWork.Services;
using OrderedWork.UI;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork
{
    public partial class OrderedWorkMod : Mod
    {
        internal void DrawOrderEditor(Listing_Standard list)
        {
            EnsureSplitMergeDefaults();
            list.Label(ConfigText("OrderedWork.Config.Order.Info", "Giver order: choose a WorkType, then drag the destination list. Top = first."));
            Rect selectorRect = list.GetRect(28f);
            DrawWorkTypeSelector(selectorRect, ConfigText("OrderedWork.Config.Order.WorkType", "Work type: "), selectedGiverType, ConfigText("OrderedWork.Config.Order.SelectWorkType", "Select Work type"), delegate(string defName)
            {
                selectedGiverType = defName;
                orderEditorPanel.Reset();
            });

            orderEditorPanel.Draw(list, this, activeProfileName,
                delegate { return selectedGiverType; },
                delegate(string value) { selectedGiverType = value; },
                delegate(string value) { profileStatus = value; });
            list.Gap(10f);
            list.Gap(12f);
            DrawProfileResetChanges(list);
        }

        private void DrawProfileResetChanges(Listing_Standard list)
        {
            list.Gap(14f);
            list.Label(ConfigText("OrderedWork.Config.Order.ResetInfo", "Reset changes against reference.json:"));
            WorkConfigLoadResult active = WorkConfigStore.LoadActiveWithFallback(activeProfileName);
            WorkConfigProfile pending = WorkConfigStore.LoadPending(activeProfileName);
            WorkConfigProfile reference = WorkConfigStore.LoadReference();
            if (active == null || active.Profile == null || reference == null)
            {
                list.Label(ConfigText("OrderedWork.Config.Order.NoActive", "No active profile/reference available."));
                return;
            }
            WorkConfigProfile display = pending ?? (active != null ? active.Profile : null);
            if (display == null)
            {
                list.Label(ConfigText("OrderedWork.Config.Order.NoPending", "No pending/active profile available."));
                return;
            }
            List<string> modified = WorkConfigDiff.GetModifiedDefNames(display, reference);
            if (modified.Count == 0)
            {
                list.Label(ConfigText("OrderedWork.Config.Order.NoModified", "No modified WorkTypes."));
                return;
            }

            foreach (string defName in modified)
            {
                WorkTypeConfig current;
                display.WorkTypes.TryGetValue(defName, out current);
                WorkTypeConfig original;
                reference.WorkTypes.TryGetValue(defName, out original);
                bool isSplit = original == null;
                Rect row = list.GetRect(30f);
                Widgets.LabelEllipses(new Rect(row.x, row.y, row.width - 150f, 26f),
                    (isSplit ? ConfigText("OrderedWork.Config.Order.DeleteSplit", "Delete split: ") : ConfigText("OrderedWork.Config.Order.Restore", "Restore: ")) + defName);
                if (Widgets.ButtonText(new Rect(row.xMax - 138f, row.y, 132f, 26f), isSplit ? ConfigText("OrderedWork.Config.Order.Delete", "Delete") : ConfigText("OrderedWork.Config.Order.RestoreButton", "Restore")))
                {
                    if (isSplit)
                    {
                        WorkTypeConfig removed = display.WorkTypes[defName];
                        if (removed != null && !string.IsNullOrEmpty(removed.Base)
                            && display.WorkTypes.TryGetValue(removed.Base, out WorkTypeConfig baseType)
                            && baseType != null)
                        {
                            // Deleting a split returns its givers to the base so the base
                            // change disappears from the diff as well.
                            if (baseType.Givers == null)
                            {
                                baseType.Givers = new List<GiverConfig>();
                            }
                            WorkTypeDef baseLive = DefDatabase<WorkTypeDef>.GetNamedSilentFail(removed.Base);
                            foreach (GiverConfig giver in removed.Givers ?? new List<GiverConfig>())
                            {
                                if (giver == null)
                                {
                                    continue;
                                }
                                WorkGiverDef live = DefDatabase<WorkGiverDef>.GetNamedSilentFail(giver.DefName);
                                if (live != null && baseLive != null)
                                {
                                    live.workType = baseLive;
                                }
                                if (!baseType.Givers.Any(g => g.DefName == giver.DefName))
                                {
                                    baseType.Givers.Add(new GiverConfig
                                    {
                                        DefName = giver.DefName,
                                        Order = baseType.Givers.Count,
                                        Mod = giver.Mod
                                    });
                                }
                            }
                        }
                        display.WorkTypes.Remove(defName);
                    }
                    else
                    {
                        WorkTypeDef restoredDef = DefDatabase<WorkTypeDef>.GetNamedSilentFail(defName);
                        HashSet<string> targetGivers = new HashSet<string>(
                            (original.Givers ?? new List<GiverConfig>())
                                .Where(g => g != null).Select(g => g.DefName));
                        ReturnSurplusGivers(display, reference, defName, targetGivers, current);
                        foreach (GiverConfig giver in original.Givers ?? new List<GiverConfig>())
                        {
                            if (giver == null)
                            {
                                continue;
                            }
                            RemoveGiverFromOtherTypes(display, giver.DefName, defName);
                            WorkGiverDef live = DefDatabase<WorkGiverDef>.GetNamedSilentFail(giver.DefName);
                            if (live != null && restoredDef != null)
                            {
                                live.workType = restoredDef;
                            }
                        }
                        display.WorkTypes[defName] = CloneWorkType(original);
                    }
                    if (WorkConfigStore.SavePending(display))
                    {
                        WorkConfigStore.SaveProfile(display, true);
                        profileStatus = ConfigText("OrderedWork.Config.Order.ResetApplied", "Reset written to pending and applied now.");
                        OrderedWorkMod.DebugLog("[Reset] " + defName + " profile=" + display.Profile);
                        OrderedWorkStartup.ApplyProfileNow(display, Settings, this);
                        Messages.Message(isSplit ? string.Format(ConfigText("OrderedWork.Config.Order.SplitDeleted", "Split deleted: {0}"), defName) : string.Format(ConfigText("OrderedWork.Config.Order.Restored", "Restored: {0}"), defName), MessageTypeDefOf.TaskCompletion, false);
                        ResetGiverOrderDraft();
                        WorkTypeDef selectedAfterReset = DefDatabase<WorkTypeDef>.GetNamedSilentFail(selectedGiverType);
                        if (selectedAfterReset == null || !selectedAfterReset.visible)
                        {
                            selectedGiverType = WorkConfigStore.GetOrderedVisibleWorkTypes(display)
                                .FirstOrDefault()?.defName ?? "";
                        }
                    }
                    break;
                }
            }
            list.Label(ConfigText("OrderedWork.Config.Order.ResetPending", "Reset writes to pending; it does not mutate live defs until restart."));
        }
    }
}