using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OrderedWork.Config;
using OrderedWork.Core;
using OrderedWork.Services;
using OrderedWork.UI;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;
using System.Text;

namespace OrderedWork
{
    public partial class OrderedWorkMod : Mod
    {
        private void ApplyLiveProfileFromSettings()
        {
            if (Settings == null || string.IsNullOrEmpty(activeProfileName))
            {
                return;
            }
            WorkConfigProfile profile = WorkConfigProfileBuilder.Capture(Settings, activeProfileName);
            WorkConfigStore.SaveProfile(profile, true);
            WorkConfigStore.SavePending(profile);
            OrderedWorkStartup.ApplyProfileNow(profile, Settings, this);
            giverOrderDraftInitialized = false;
            giverOrderDraft.Clear();
            giverOrderInitial.Clear();
            giverOrderDraftType = "";
        }

        private void DrawGiverOrderDiff(Listing_Standard list)
        {
            WorkConfigLoadResult active = WorkConfigStore.LoadActiveWithFallback(activeProfileName);
            WorkConfigProfile reference = WorkConfigStore.LoadReference();
            if (active == null || active.Profile == null || string.IsNullOrEmpty(selectedGiverType))
            {
                list.Label("Choose a WorkType below to compare its giver order.");
                return;
            }
            WorkTypeConfig current;
            if (!active.Profile.WorkTypes.TryGetValue(selectedGiverType, out current) || current == null)
            {
                list.Label("Selected WorkType is not in the active profile.");
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
            list.Label("Giver order — " + workLabel + ": origin = reference, destination = draft.");
            int rowCount = Mathf.Max(referenceGivers.Count, giverOrderDraft.Count);
            Rect columns = list.GetRect(Mathf.Max(120f, 28f + rowCount * SplitMergeRowHeight));
            float columnWidth = (columns.width - 12f) * 0.5f;
            Rect left = new Rect(columns.x, columns.y, columnWidth, columns.height);
            Rect right = new Rect(columns.x + columnWidth + 12f, columns.y, columnWidth, columns.height);
            Widgets.DrawBoxSolid(left, UiColors.ColumnBackground);
            Widgets.DrawBoxSolid(right, UiColors.ColumnBackground);
            Widgets.Label(new Rect(left.x, left.y, left.width, 22f), "Origin (last applied)");
            Widgets.Label(new Rect(right.x, right.y, right.width, 22f), "Destination (current)");

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
            if (DrawActionButton(new Rect(buttons.x, buttons.y, half, 28f), "Apply giver order", changed))
            {
                ApplyGiverOrderDraft(active.Profile);
            }
            if (DrawActionButton(new Rect(buttons.x + half + 8f, buttons.y, half, 28f), "Cancel giver order", changed))
            {
                giverOrderDraft = new List<string>(giverOrderInitial);
                giverOrderDirty = false;
                giverOrderDraftInitialized = true;
            }
        }

        private void ResetGiverOrderDraft()
        {
            giverOrderDraftInitialized = false;
            giverOrderDirty = false;
            giverOrderDraft.Clear();
            giverOrderInitial.Clear();
            giverOrderDraftType = "";
        }

        private void MoveGiverOrderDraft(int from, int to)
        {
            if (from < 0 || from >= giverOrderDraft.Count || to < 0 || to >= giverOrderDraft.Count || from == to)
            {
                return;
            }
            string item = giverOrderDraft[from];
            giverOrderDraft.RemoveAt(from);
            giverOrderDraft.Insert(to, item);
            giverOrderDirty = true;
        }

        private void ApplyGiverOrderDraft(WorkConfigProfile profile)
        {
            if (profile == null || string.IsNullOrEmpty(selectedGiverType)
                || !profile.WorkTypes.TryGetValue(selectedGiverType, out WorkTypeConfig current)
                || current == null)
            {
                return;
            }
            current.Givers = current.Givers ?? new List<GiverConfig>();
            for (int i = 0; i < giverOrderDraft.Count; i++)
            {
                GiverConfig giver = current.Givers.FirstOrDefault(g => g != null && g.DefName == giverOrderDraft[i]);
                if (giver != null)
                {
                    giver.Order = i;
                }
            }
            current.Givers = current.Givers.OrderBy(g => g.Order).ToList();
            WorkConfigStore.SaveProfile(profile, true);
            WorkConfigStore.SavePending(profile);
            OrderedWorkStartup.ApplyProfileNow(profile, Settings, this);
            profileStatus = "Giver order for " + selectedGiverType + " applied.";
            Messages.Message("Giver order saved for " + selectedGiverType + ".", MessageTypeDefOf.TaskCompletion, false);
            giverOrderDraftInitialized = false;
            giverOrderDirty = false;
        }

        private void DrawOrderEditor(Listing_Standard list)
        {
            EnsureSplitMergeDefaults();
            list.Label("Giver order: choose a WorkType, then drag the destination list. Top = first.");
            Rect selectorRect = list.GetRect(28f);
            DrawWorkTypeSelector(selectorRect, "Work type: ", selectedGiverType, "Select Work type", delegate(string defName)
            {
                selectedGiverType = defName;
                giverOrderDraftInitialized = false;
            });

            DrawGiverOrderDiff(list);
            list.Gap(10f);


            list.Gap(12f);
            DrawProfileResetChanges(list);
        }

        private void PersistLivePending()
        {
            if (Settings == null || string.IsNullOrEmpty(activeProfileName))
            {
                return;
            }
            WorkConfigProfile captured = WorkConfigProfileBuilder.Capture(Settings, activeProfileName);
            WorkConfigStore.SavePending(captured);
        }

        private void DrawProfileResetChanges(Listing_Standard list)
        {
            list.Gap(14f);
            list.Label("Reset changes against reference.json:");
            WorkConfigLoadResult active = WorkConfigStore.LoadActiveWithFallback(activeProfileName);
            WorkConfigProfile pending = WorkConfigStore.LoadPending(activeProfileName);
            WorkConfigProfile reference = WorkConfigStore.LoadReference();
            if (active == null || active.Profile == null || reference == null)
            {
                list.Label("No active profile/reference available.");
                return;
            }
            WorkConfigProfile display = pending ?? (active != null ? active.Profile : null);
            if (display == null)
            {
                list.Label("No pending/active profile available.");
                return;
            }
            List<string> modified = WorkConfigDiff.GetModifiedDefNames(display, reference);
            if (modified.Count == 0)
            {
                list.Label("No modified WorkTypes.");
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
                    (isSplit ? "Delete split: " : "Restore: ") + defName);
                if (Widgets.ButtonText(new Rect(row.xMax - 138f, row.y, 132f, 26f), isSplit ? "Delete" : "Restore"))
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
                        profileStatus = "Reset written to pending and applied now.";
                        OrderedWorkMod.DebugLog("[Reset] " + defName + " profile=" + display.Profile);
                        OrderedWorkStartup.ApplyProfileNow(display, Settings, this);
                        Messages.Message(isSplit ? "Split deleted: " + defName : "Restored: " + defName, MessageTypeDefOf.TaskCompletion, false);
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
            list.Label("Reset writes to pending; it does not mutate live defs until restart.");
        }

        private static void ReturnSurplusGivers(
            WorkConfigProfile display,
            WorkConfigProfile reference,
            string targetDefName,
            HashSet<string> targetGivers,
            WorkTypeConfig currentTarget)
        {
            if (display == null || reference == null || currentTarget == null || currentTarget.Givers == null)
            {
                return;
            }
            foreach (GiverConfig giver in currentTarget.Givers.ToList())
            {
                if (giver == null || targetGivers.Contains(giver.DefName))
                {
                    continue;
                }
                string ownerName = null;
                foreach (KeyValuePair<string, WorkTypeConfig> entry in reference.WorkTypes)
                {
                    if (entry.Value != null && entry.Value.Givers != null
                        && entry.Value.Givers.Any(g => g != null && g.DefName == giver.DefName))
                    {
                        ownerName = entry.Key;
                        break;
                    }
                }
                WorkTypeConfig ownerConfig;
                if (string.IsNullOrEmpty(ownerName) || ownerName == targetDefName
                    || !display.WorkTypes.TryGetValue(ownerName, out ownerConfig)
                    || ownerConfig == null)
                {
                    OrderedWorkMod.DebugLog("[Reset] surplus giver dropped " + giver.DefName);
                    continue;
                }
                ownerConfig.Givers = ownerConfig.Givers ?? new List<GiverConfig>();
                if (!ownerConfig.Givers.Any(g => g != null && g.DefName == giver.DefName))
                {
                    ownerConfig.Givers.Add(new GiverConfig
                    {
                        DefName = giver.DefName,
                        Order = ownerConfig.Givers.Count,
                        Mod = giver.Mod
                    });
                }
                WorkGiverDef live = DefDatabase<WorkGiverDef>.GetNamedSilentFail(giver.DefName);
                WorkTypeDef ownerDef = DefDatabase<WorkTypeDef>.GetNamedSilentFail(ownerName);
                if (live != null && ownerDef != null)
                {
                    live.workType = ownerDef;
                }
            }
        }

        private static void RemoveGiverFromOtherTypes(WorkConfigProfile profile, string giverDefName, string exceptDefName)
        {
            if (profile == null || profile.WorkTypes == null || string.IsNullOrEmpty(giverDefName))
            {
                return;
            }
            List<string> emptySplits = new List<string>();
            foreach (KeyValuePair<string, WorkTypeConfig> entry in profile.WorkTypes)
            {
                if (entry.Key == exceptDefName || entry.Value == null || entry.Value.Givers == null)
                {
                    continue;
                }
                entry.Value.Givers.RemoveAll(g => g != null && g.DefName == giverDefName);
                for (int i = 0; i < entry.Value.Givers.Count; i++)
                {
                    entry.Value.Givers[i].Order = i;
                }
                if (!entry.Value.Vanilla
                    && !string.IsNullOrEmpty(entry.Value.Base)
                    && entry.Value.Givers.Count == 0)
                {
                    emptySplits.Add(entry.Key);
                }
            }
            foreach (string emptySplit in emptySplits)
            {
                profile.WorkTypes.Remove(emptySplit);
                OrderedWorkMod.DebugLog("[Reset] removed empty split " + emptySplit);
            }
        }

        private static WorkTypeConfig CloneWorkType(WorkTypeConfig source)
        {
            if (source == null)
            {
                return null;
            }
            return new WorkTypeConfig
            {
                Vanilla = source.Vanilla,
                Base = source.Base,
                Label = source.Label,
                Description = source.Description,
                Visible = source.Visible,
                Order = source.Order,
                Mod = source.Mod,
                Givers = source.Givers == null
                    ? new List<GiverConfig>()
                    : source.Givers.Select(g => new GiverConfig
                    {
                        DefName = g.DefName,
                        Order = g.Order,
                        Mod = g.Mod
                    }).ToList()
            };
        }


    }
}