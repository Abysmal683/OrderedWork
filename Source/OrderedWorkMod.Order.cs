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
    {
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
            List<string> referenceGivers = referenceType != null && referenceType.Givers != null
                ? referenceType.Givers.OrderBy(g => g.Order).Select(g => g.DefName).ToList()
                : giverOrderDraft.ToList();

            string workLabel = DefDatabase<WorkTypeDef>.GetNamedSilentFail(selectedGiverType)?.labelShort ?? selectedGiverType;
            list.Label("Giver order — " + workLabel + ": origin = reference, destination = draft.");
            int rowCount = Mathf.Max(referenceGivers.Count, giverOrderDraft.Count);
            Rect columns = list.GetRect(Mathf.Max(120f, 28f + rowCount * SplitMergeRowHeight));
            float columnWidth = (columns.width - 12f) * 0.5f;
            Rect left = new Rect(columns.x, columns.y, columnWidth, columns.height);
            Rect right = new Rect(columns.x + columnWidth + 12f, columns.y, columnWidth, columns.height);
            Widgets.Label(new Rect(left.x, left.y, left.width, 22f), "Origin (reference)");
            Widgets.Label(new Rect(right.x, right.y, right.width, 22f), "Destination (current)");

            for (int i = 0; i < referenceGivers.Count; i++)
            {
                Rect row = new Rect(left.x, left.y + 24f + i * SplitMergeRowHeight, left.width, SplitMergeRowHeight - 2f);
                Widgets.DrawBoxSolid(row, new Color(0.20f, 0.25f, 0.30f, 0.45f));
                Widgets.LabelEllipses(new Rect(row.x + 4f, row.y, row.width - 8f, row.height), (i + 1) + ". " + referenceGivers[i]);
            }

            List<Rect> rows = new List<Rect>();
            for (int i = 0; i < giverOrderDraft.Count; i++)
            {
                Rect row = new Rect(right.x, right.y + 24f + i * SplitMergeRowHeight, right.width, SplitMergeRowHeight - 2f);
                DrawReorderRow(row, (i + 1) + ". " + giverOrderDraft[i], 5, i, null);
                rows.Add(row);
            }
            HandleReorderDrag(5, rows, MoveGiverOrderDraft);

            Rect buttons = list.GetRect(30f);
            float half = (buttons.width - 8f) * 0.5f;
            bool changed = giverOrderDirty;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && changed;
            if (Widgets.ButtonText(new Rect(buttons.x, buttons.y, half, 28f), "Apply giver order"))
            {
                ApplyGiverOrderDraft(active.Profile);
            }
            if (Widgets.ButtonText(new Rect(buttons.x + half + 8f, buttons.y, half, 28f), "Cancel giver order"))
            {
                giverOrderDraft = new List<string>(giverOrderInitial);
                giverOrderDirty = false;
                giverOrderDraftInitialized = true;
            }
            GUI.enabled = previousEnabled;
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
            giverOrderDraftInitialized = false;
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
                confirmResetOrder = false;
            });

            DrawGiverOrderDiff(list);
            list.Gap(10f);

            string selectedLabel = DefDatabase<WorkTypeDef>.GetNamedSilentFail(selectedGiverType)?.labelShort ?? selectedGiverType;
            if (!confirmResetOrder)
            {
                if (list.ButtonText("Reset order"))
                {
                    confirmResetOrder = true;
                }
            }
            else
            {
                list.Label("Remove the saved giver order for " + selectedLabel + "? The original def order returns after a restart; the current session keeps its priority values.");
                if (list.ButtonText("Confirm reset order"))
                {
                    foreach (WorkGiverDef giver in GiversForWorkType(selectedGiverType))
                    {
                        GiverOrderEntry original = Settings.originalGiverOrder.FirstOrDefault(
                            e => e != null && e.giver == giver.defName);
                        if (original != null)
                        {
                            giver.priorityInType = original.order;
                        }
                    }
                    Settings.giverOrder.RemoveAll(e => e != null && e.workType == selectedGiverType);
                    OrderedWorkStartup.ApplyPendingChanges(Settings, this);
                    PersistLivePending();
                    confirmResetOrder = false;
                }
                if (list.ButtonText("Cancel reset order"))
                {
                    confirmResetOrder = false;
                }
            }
            list.Label("Reset order removes the saved override; restart restores the def order.");
            list.Gap(12f);
            DrawSavedSplitMetadata(list);
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
                            foreach (GiverConfig giver in removed.Givers ?? new List<GiverConfig>())
                            {
                                if (giver != null && !baseType.Givers.Any(g => g.DefName == giver.DefName))
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
                        display.WorkTypes[defName] = CloneWorkType(original);
                    }
                    if (WorkConfigStore.SavePending(display))
                    {
                        profileStatus = "Reset written to pending and applied now.";
                        OrderedWorkStartup.ApplyProfileNow(display, Settings, this);
                    }
                    break;
                }
            }
            list.Label("Reset writes to pending; it does not mutate live defs until restart.");
        }

        private static void RemoveGiverFromOtherTypes(WorkConfigProfile profile, string giverDefName, string exceptDefName)
        {
            if (profile == null || profile.WorkTypes == null || string.IsNullOrEmpty(giverDefName))
            {
                return;
            }
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

        private void DrawSavedSplitMetadata(Listing_Standard list)
        {
            list.Label("Edit saved split metadata (hot):");
            List<WorkTypeDef> splitDefs = Settings.splits
                .Where(s => s != null && !string.IsNullOrEmpty(s.newDefName))
                .Select(s => DefDatabase<WorkTypeDef>.GetNamedSilentFail(s.newDefName))
                .Where(d => d != null)
                .ToList();
            if (splitDefs.Count == 0)
            {
                list.Label("No saved splits with a live WorkType.");
                return;
            }

            WorkTypeDef selected = DefDatabase<WorkTypeDef>.GetNamedSilentFail(editSplitDefName);
            Rect selector = list.GetRect(28f);
            if (Widgets.ButtonText(selector, "Split: " + (selected != null ? selected.labelShort : "(select)") + "  ▼"))
            {
                Find.WindowStack.Add(new WorkTypeDropdownWindow(
                    "Select split",
                    splitDefs,
                    selected,
                    delegate(WorkTypeDef wt)
                    {
                        editSplitDefName = wt.defName;
                        SplitEntry entry = Settings.splits.FirstOrDefault(s => s != null && s.newDefName == wt.defName);
                        editSplitName = entry != null ? entry.label : wt.labelShort;
                        editSplitDescription = entry != null ? entry.description : wt.description;
                    }));
            }

            if (selected != null)
            {
                Rect nameRow = list.GetRect(26f);
                Widgets.Label(new Rect(nameRow.x, nameRow.y, 100f, 24f), "Name");
                editSplitName = Widgets.TextField(new Rect(nameRow.x + 104f, nameRow.y, nameRow.width - 104f, 24f), editSplitName ?? "");
                Rect descRow = list.GetRect(26f);
                Widgets.Label(new Rect(descRow.x, descRow.y, 100f, 24f), "Description");
                editSplitDescription = Widgets.TextField(new Rect(descRow.x + 104f, descRow.y, descRow.width - 104f, 24f), editSplitDescription ?? "");
                if (Widgets.ButtonText(list.GetRect(28f), "Apply split metadata") && !string.IsNullOrWhiteSpace(editSplitName))
                {
                    SplitEntry entry = Settings.splits.FirstOrDefault(s => s != null && s.newDefName == selected.defName);
                    if (entry != null)
                    {
                        entry.label = editSplitName.Trim();
                        entry.description = editSplitDescription.Trim();
                    }
                    selected.labelShort = editSplitName.Trim();
                    selected.pawnLabel = editSplitName.Trim();
                    selected.gerundLabel = editSplitName.Trim();
                    selected.description = editSplitDescription.Trim();
                    WorkOrderService.InvalidateAvailableWorkTypes();
                }
            }
        }

    }
}
