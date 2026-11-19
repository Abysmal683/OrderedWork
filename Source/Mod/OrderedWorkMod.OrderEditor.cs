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
        private void DrawGiverOrderDiff(Listing_Standard list)
        {
            WorkConfigLoadResult active = WorkConfigStore.LoadActiveWithFallback(activeProfileName);
            WorkConfigProfile reference = WorkConfigStore.LoadReference();
            if (active == null || active.Profile == null || string.IsNullOrEmpty(selectedGiverType))
            {
                list.Label(ConfigText("OrderedWork.Config.Order.SelectPrompt", "Choose a WorkType below to compare its giver order."));
                return;
            }
            WorkTypeConfig current;
            if (!active.Profile.WorkTypes.TryGetValue(selectedGiverType, out current) || current == null)
            {
                list.Label(ConfigText("OrderedWork.Config.Order.NotInProfile", "Selected WorkType is not in the active profile."));
                return;
            }
            if (!giverOrderDraftInitialized || giverOrderDraftType != selectedGiverType)
            {
                giverOrderDraft = (current.Givers ?? new List<GiverConfig>())
                    .Where(g => g != null).OrderBy(g => g.Order).Select(g => g.DefName).ToList();
                giverOrderInitial = new List<string>(giverOrderDraft);
                giverOrderDraftType = selectedGiverType;
                giverOrderDraftInitialized = true;
                giverOrderDirty = false;
            }

            WorkTypeConfig referenceType = null;
            if (reference != null)
            {
                reference.WorkTypes.TryGetValue(selectedGiverType, out referenceType);
            }
            List<string> referenceGivers = giverOrderInitial.Count > 0
                ? new List<string>(giverOrderInitial)
                : giverOrderDraft.ToList();

            string workLabel = DefDatabase<WorkTypeDef>.GetNamedSilentFail(selectedGiverType)?.labelShort ?? selectedGiverType;
            list.Label(string.Format(ConfigText("OrderedWork.Config.Order.Header", "Giver order — {0}: origin = reference, destination = draft."), workLabel));
            int rowCount = Mathf.Max(referenceGivers.Count, giverOrderDraft.Count);
            Rect columns = list.GetRect(Mathf.Max(120f, 28f + rowCount * SplitMergeRowHeight));
            float columnWidth = (columns.width - 12f) * 0.5f;
            Rect left = new Rect(columns.x, columns.y, columnWidth, columns.height);
            Rect right = new Rect(columns.x + columnWidth + 12f, columns.y, columnWidth, columns.height);
            Widgets.DrawBoxSolid(left, UiColors.ColumnBackground);
            Widgets.DrawBoxSolid(right, UiColors.ColumnBackground);
            Widgets.Label(new Rect(left.x, left.y, left.width, 22f), ConfigText("OrderedWork.Config.Order.Origin", "Origin (last applied)"));
            Widgets.Label(new Rect(right.x, right.y, right.width, 22f), ConfigText("OrderedWork.Config.Order.Destination", "Destination (current)"));

            for (int i = 0; i < referenceGivers.Count; i++)
            {
                Rect row = new Rect(left.x, left.y + 24f + i * SplitMergeRowHeight, left.width, SplitMergeRowHeight - 2f);
                UiZebra.Draw(row, i);
                Widgets.DrawBoxSolid(row, new Color(0.20f, 0.25f, 0.30f, 0.45f));
                Widgets.LabelEllipses(new Rect(row.x + 4f, row.y, row.width - 8f, row.height), (i + 1) + ". " + referenceGivers[i]);
            }

            List<Rect> rows = new List<Rect>();
            for (int i = 0; i < giverOrderDraft.Count; i++)
            {
                Rect row = new Rect(right.x, right.y + 24f + i * SplitMergeRowHeight, right.width, SplitMergeRowHeight - 2f);
                UiZebra.Draw(row, i);
                DrawReorderRow(row, (i + 1) + ". " + giverOrderDraft[i], 5, i, null);
                rows.Add(row);
            }
            HandleReorderDrag(5, rows, MoveGiverOrderDraft);

            Rect buttons = list.GetRect(30f);
            float half = (buttons.width - 8f) * 0.5f;
            bool changed = !giverOrderDraft.SequenceEqual(giverOrderInitial);
            giverOrderDirty = changed;
            if (UiButtonBar.Button(new Rect(buttons.x, buttons.y, half, 28f), ConfigText("OrderedWork.Config.Order.Apply", "Apply giver order"), changed))
            {
                ApplyGiverOrderDraft(active.Profile);
            }
            if (UiButtonBar.Button(new Rect(buttons.x + half + 8f, buttons.y, half, 28f), ConfigText("OrderedWork.Config.Order.Cancel", "Cancel giver order"), changed))
            {
                giverOrderDraft = new List<string>(giverOrderInitial);
                giverOrderDirty = false;
                giverOrderDraftInitialized = true;
            }
        }

        private void DrawOrderEditor(Listing_Standard list)
        {
            EnsureSplitMergeDefaults();
            list.Label(ConfigText("OrderedWork.Config.Order.Info", "Giver order: choose a WorkType, then drag the destination list. Top = first."));
            Rect selectorRect = list.GetRect(28f);
            DrawWorkTypeSelector(selectorRect, ConfigText("OrderedWork.Config.Order.WorkType", "Work type: "), selectedGiverType, ConfigText("OrderedWork.Config.Order.SelectWorkType", "Select Work type"), delegate(string defName)
            {
                selectedGiverType = defName;
                giverOrderDraftInitialized = false;
            });

            DrawGiverOrderDiff(list);
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
                        giverOrderDraftInitialized = false;
                        giverOrderDraft.Clear();
                        giverOrderInitial.Clear();
                        giverOrderDraftType = "";
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