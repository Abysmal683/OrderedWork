using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.UI;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork
{
    /// <summary>
    /// Split/Merge UI state. Drawing and host callbacks remain in OrderedWorkMod for now;
    /// this component owns the draft, scroll, drag and metadata state.
    /// </summary>
    internal sealed class SplitMergePanel
    {
        internal const int EditorSplit = 0;
        internal const int EditorMerge = 1;
        internal const int EditorOrder = 2;
        private const float SplitMergeRowHeight = 24f;
        private const float SplitMergeColumnHeight = 240f;
        private const float SplitMergeScrollbarWidth = 6f;
        private const int MaxVisibleListRows = 10;
        private OrderedWorkMod host;

        internal int EditorMode;
        internal string SplitOrigin = "";
        internal string SplitName = "";
        internal string SplitDescription = "";
        internal string SplitActionWord = "";
        internal readonly List<string> SplitDraft = new List<string>();
        internal float SplitOriginScroll;
        internal float SplitDraftScroll;
        internal string MergeOrigin = "";
        internal string MergeTarget = "";
        internal readonly List<string> MergeDraft = new List<string>();
        internal float MergeOriginScroll;
        internal float MergeTargetScroll;
        internal int DragSide;
        internal bool DragMergeMode;
        internal string DragGiver;
        internal float DragStartY;
        internal bool DragMoved;
        internal Rect OriginArea;
        internal Rect DestArea;
        internal int ScrollDragColumn;
        internal int MergeDropIndex = -1;
        internal bool SplitDirty;
        internal bool MergeDirty;
        internal string MetaDefName = "";
        internal string MetaLabel = "";
        internal string MetaDescription = "";
        internal string MetaActionWord = "";
        private string splitPendingDelete = "";
        private string mergePendingRevert = "";
        private string orderPendingRevert = "";
        private float splitListScroll;
        private float mergeListScroll;
        private float orderListScroll;

        internal void Draw(Listing_Standard list, OrderedWorkMod owner)
        {
            host = owner;
            list.Label(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.Info", "Split / Merge / Order editor. Drafts are temporary; saved entries apply on restart."));
            list.Gap(28f);

            Rect modeRect = list.GetRect(28f);
            UiTabBar.Draw(modeRect, new[]
            {
                OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.SplitTab", "Split"),
                OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.MergeTab", "Merge"),
                OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.OrderTab", "Order")
            }, EditorMode, delegate(int index)
            {
                if (EditorMode == index)
                {
                    return;
                }
                EditorMode = index;
                host.ResetEditorUiState();
                host.ResetGiverOrderDraft();
            });
            list.Label(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.Tip", "Tip: Ctrl+wheel or drag a column scrollbar to scroll that column; a plain wheel scrolls the settings."));
            list.Gap(4f);

            switch (EditorMode)
            {
                case EditorMerge:
                    DrawMergeEditor(list);
                    break;
                case EditorOrder:
                    host.DrawOrderEditor(list);
                    DrawOrderCandidates(list);
                    break;
                default:
                    DrawSplitEditor(list);
                    break;
            }
        }


        internal void SaveSplit()
        {
            if (!string.IsNullOrEmpty(SplitValidationWarning()))
            {
                return;
            }

            string newDefName = host.BuildSplitDefName(SplitName);
            WorkConfigProfile profile = host.LoadEditingProfile();
            if (profile == null || !profile.WorkTypes.TryGetValue(SplitOrigin, out WorkTypeConfig origin) || origin == null)
            {
                return;
            }

            List<string> incoming = SplitDraft.Distinct().ToList();
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
                Base = SplitOrigin,
                Label = SplitName.Trim(),
                ActionWord = SplitActionWord.Trim(),
                Description = SplitDescription.Trim(),
                Visible = true,
                Order = profile.WorkTypes.Count == 0 ? 0 : profile.WorkTypes.Values.Max(w => w != null ? w.Order : 0) + 1,
                Mod = host.Content != null && host.Content.ModMetaData != null ? host.Content.PackageId : "nightmare123487.OrderedWork",
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
                profile.WorkTypes.Remove(SplitOrigin);
            }
            if (origin.Vanilla && (origin.Givers == null || origin.Givers.Count == 0))
            {
                origin.Visible = false;
            }
            WorkConfigStore.SaveProfile(profile, true);
            WorkConfigStore.SavePending(profile);
            OrderedWorkStartup.ApplyProfileNow(profile, OrderedWorkMod.Settings, host);
            Messages.Message(string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.SplitSaved", "Split saved: {0} (restart to apply)."), SplitName), MessageTypeDefOf.TaskCompletion, false);
            host.ResetGiverOrderDraft();

            SplitName = "";
            SplitDescription = "";
            SplitActionWord = "";
            SplitDraft.Clear();
            SplitDraftScroll = 0f;
            SplitDirty = false;
        }

        internal void SaveMerge(List<string> givers)
        {
            if (givers == null || givers.Count == 0)
            {
                return;
            }
            if (DefDatabase<WorkTypeDef>.GetNamedSilentFail(MergeTarget) == null || MergeTarget == MergeOrigin)
            {
                return;
            }

            WorkConfigProfile profile = host.LoadEditingProfile();
            if (profile == null
                || !profile.WorkTypes.TryGetValue(MergeOrigin, out WorkTypeConfig origin)
                || origin == null
                || !profile.WorkTypes.TryGetValue(MergeTarget, out WorkTypeConfig target)
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
                profile.WorkTypes.Remove(MergeOrigin);
            }
            if (origin.Vanilla && (origin.Givers == null || origin.Givers.Count == 0))
            {
                origin.Visible = false;
            }

            WorkConfigStore.SaveProfile(profile, true);
            WorkConfigStore.SavePending(profile);
            OrderedWorkStartup.ApplyProfileNow(profile, OrderedWorkMod.Settings, host);
            Messages.Message(string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.MergeSaved", "Merge saved for {0} (restart to apply)."), MergeTarget), MessageTypeDefOf.TaskCompletion, false);
            MergeDraft.Clear();
            host.ResetGiverOrderDraft();
            MergeTargetScroll = 0f;
            MergeDirty = false;
        }


        private void RevertMerge(string defName)
        {
            WorkConfigProfile profile = host.LoadEditingProfile();
            WorkConfigProfile reference = WorkConfigStore.LoadReference();
            if (profile == null || reference == null || string.IsNullOrEmpty(defName)
                || !profile.WorkTypes.TryGetValue(defName, out WorkTypeConfig current)
                || current == null || current.Givers == null
                || !reference.WorkTypes.TryGetValue(defName, out WorkTypeConfig baseline)
                || baseline == null || baseline.Givers == null)
            {
                return;
            }

            HashSet<string> referenceNames = new HashSet<string>(
                baseline.Givers.Where(g => g != null).Select(g => g.DefName), StringComparer.Ordinal);
            HashSet<string> currentNames = new HashSet<string>(
                current.Givers.Where(g => g != null).Select(g => g.DefName), StringComparer.Ordinal);
            List<GiverConfig> extras = current.Givers
                .Where(g => g != null && !referenceNames.Contains(g.DefName))
                .ToList();
            foreach (GiverConfig missing in baseline.Givers.Where(g => g != null && !currentNames.Contains(g.DefName)))
            {
                KeyValuePair<string, WorkTypeConfig> ownerEntry = profile.WorkTypes
                    .FirstOrDefault(pair => pair.Key != defName && pair.Value != null && pair.Value.Givers != null
                        && pair.Value.Givers.Any(g => g != null && g.DefName == missing.DefName));
                if (string.IsNullOrEmpty(ownerEntry.Key) || ownerEntry.Value == null)
                {
                    continue;
                }
                ownerEntry.Value.Givers.RemoveAll(g => g != null && g.DefName == missing.DefName);
                WorkTypeConfig ownerReference;
                if (reference.WorkTypes.TryGetValue(ownerEntry.Key, out ownerReference))
                {
                    RestoreGiverOrder(ownerEntry.Value, ownerReference);
                }
            }
            foreach (GiverConfig extra in extras)
            {
                KeyValuePair<string, WorkTypeConfig> ownerEntry = reference.WorkTypes
                    .FirstOrDefault(pair => pair.Value != null && pair.Value.Givers != null
                        && pair.Value.Givers.Any(g => g != null && g.DefName == extra.DefName));
                if (string.IsNullOrEmpty(ownerEntry.Key) || ownerEntry.Key == defName
                    || !profile.WorkTypes.TryGetValue(ownerEntry.Key, out WorkTypeConfig owner)
                    || owner == null)
                {
                    continue;
                }
                owner.Givers = owner.Givers ?? new List<GiverConfig>();
                if (!owner.Givers.Any(g => g != null && g.DefName == extra.DefName))
                {
                    owner.Givers.Add(new GiverConfig
                    {
                        DefName = extra.DefName,
                        Order = owner.Givers.Count,
                        Mod = extra.Mod
                    });
                }
                RestoreGiverOrder(owner, ownerEntry.Value);
            }

            current.Givers = baseline.Givers
                .Where(g => g != null)
                .Select((g, index) => new GiverConfig
                {
                    DefName = g.DefName,
                    Order = index,
                    Mod = g.Mod
                })
                .ToList();
            WorkConfigStore.SaveProfile(profile, true);
            WorkConfigStore.SavePending(profile);
            OrderedWorkStartup.ApplyProfileNow(profile, OrderedWorkMod.Settings, host);
            Messages.Message(string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.MergeReverted",
                "Merge reverted for {0}."), defName), MessageTypeDefOf.TaskCompletion, false);
            mergePendingRevert = "";
        }

        private void RevertOrder(string defName)
        {
            WorkConfigProfile profile = host.LoadEditingProfile();
            WorkConfigProfile reference = WorkConfigStore.LoadReference();
            if (profile == null || reference == null || string.IsNullOrEmpty(defName)
                || !profile.WorkTypes.TryGetValue(defName, out WorkTypeConfig current)
                || current == null
                || !reference.WorkTypes.TryGetValue(defName, out WorkTypeConfig baseline)
                || baseline == null || baseline.Givers == null)
            {
                return;
            }

            Dictionary<string, GiverConfig> byName = (current.Givers ?? new List<GiverConfig>())
                .Where(g => g != null)
                .GroupBy(g => g.DefName, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            current.Givers = baseline.Givers
                .Where(g => g != null)
                .Select((g, index) =>
                {
                    GiverConfig existing;
                    if (byName.TryGetValue(g.DefName, out existing))
                    {
                        existing.Order = index;
                        return existing;
                    }
                    return new GiverConfig { DefName = g.DefName, Order = index, Mod = g.Mod };
                })
                .ToList();
            WorkConfigStore.SaveProfile(profile, true);
            WorkConfigStore.SavePending(profile);
            OrderedWorkStartup.ApplyProfileNow(profile, OrderedWorkMod.Settings, host);
            Messages.Message(string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.OrderReverted",
                "Giver order restored for {0}."), defName), MessageTypeDefOf.TaskCompletion, false);
            orderPendingRevert = "";
        }

        private static void RestoreGiverOrder(WorkTypeConfig target, WorkTypeConfig reference)
        {
            if (target == null || reference == null || reference.Givers == null)
            {
                return;
            }
            Dictionary<string, int> order = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < reference.Givers.Count; i++)
            {
                GiverConfig giver = reference.Givers[i];
                if (giver != null && !order.ContainsKey(giver.DefName))
                {
                    order[giver.DefName] = i;
                }
            }
            target.Givers = (target.Givers ?? new List<GiverConfig>())
                .Where(g => g != null)
                .OrderBy(g =>
                {
                    int index;
                    return order.TryGetValue(g.DefName, out index) ? index : int.MaxValue;
                })
                .ThenBy(g => g.Order)
                .ToList();
            for (int i = 0; i < target.Givers.Count; i++)
            {
                target.Givers[i].Order = i;
            }
        }

        private void DrawScrollableList(Listing_Standard list, int count, ref float scroll, string owner,
            Action<Rect, int> drawRow)
        {
            int visible = Math.Min(count, MaxVisibleListRows);
            Rect outRect = list.GetRect(visible * SplitMergeRowHeight);
            float barWidth = WorkGridScrollView.Thickness;
            Rect viewport = new Rect(outRect.x, outRect.y, outRect.width - barWidth - 2f, outRect.height);
            Rect content = new Rect(0f, 0f, viewport.width, count * SplitMergeRowHeight);
            Rect track = new Rect(viewport.xMax + 2f, outRect.y, barWidth, outRect.height);
            float maxScroll = Mathf.Max(0f, content.height - viewport.height);
            scroll = Mathf.Clamp(scroll, 0f, maxScroll);
            OrderedWorkMod.HandleCtrlWheel(viewport, maxScroll, ref scroll, SplitMergeRowHeight);
            Vector2 scrollVector = new Vector2(0f, scroll);
            WorkGridScrollView.HandleVertical(track, viewport, content, ref scrollVector, owner);

            GUI.BeginGroup(viewport);
            for (int i = 0; i < count; i++)
            {
                float y = i * SplitMergeRowHeight - scrollVector.y;
                if (y + SplitMergeRowHeight <= 0f || y >= viewport.height)
                {
                    continue;
                }
                drawRow(new Rect(0f, y, viewport.width, SplitMergeRowHeight - 2f), i);
            }
            GUI.EndGroup();

            scroll = Mathf.Clamp(scrollVector.y, 0f, maxScroll);
            WorkGridScrollView.DrawVertical(track, viewport, content, ref scrollVector);
        }

        private void DrawExistingSplits(Listing_Standard list)
        {
            WorkConfigProfile profile = host.LoadEditingProfile();
            if (profile == null)
            {
                return;
            }

            List<KeyValuePair<string, WorkTypeConfig>> splits = profile.WorkTypes
                .Where(pair => pair.Value != null && !pair.Value.Vanilla && !string.IsNullOrEmpty(pair.Value.Base))
                .OrderBy(pair => pair.Value.Label ?? pair.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (splits.Count == 0)
            {
                list.Label(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.NoSplits", "No splits created yet."));
                return;
            }

            list.Label(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.Splits", "Existing splits"));
            DrawScrollableList(list, splits.Count, ref splitListScroll, "split-list", delegate(Rect row, int index)
            {
                KeyValuePair<string, WorkTypeConfig> pair = splits[index];
                UiZebra.Draw(row, index);
                Widgets.LabelEllipses(new Rect(row.x + 4f, row.y, row.width - 184f, 22f),
                    (pair.Value.Label ?? pair.Key) + " · " + pair.Value.Base + " · " + (pair.Value.Givers != null ? pair.Value.Givers.Count : 0));
                if (Widgets.ButtonText(new Rect(row.xMax - 174f, row.y, 80f, 22f),
                    OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.Edit", "Edit")))
                {
                    MetaDefName = pair.Key;
                    MetaLabel = pair.Value.Label ?? "";
                    MetaDescription = pair.Value.Description ?? "";
                    MetaActionWord = pair.Value.ActionWord ?? "";
                    splitPendingDelete = "";
                    SplitDirty = true;
                }
                bool confirming = splitPendingDelete == pair.Key;
                if (Widgets.ButtonText(new Rect(row.xMax - 88f, row.y, 84f, 22f),
                    confirming
                        ? OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.ConfirmDelete", "Confirm")
                        : OrderedWorkMod.ConfigText("OrderedWork.Config.Common.Delete", "Delete")))
                {
                    if (!confirming)
                    {
                        splitPendingDelete = pair.Key;
                    }
                    else
                    {
                        string defName = pair.Key;
                        host.SafeSplitMergeAction(delegate { DeleteSplit(defName); });
                    }
                }
            });

            if (!string.IsNullOrEmpty(MetaDefName))
            {
                DrawSplitMetadataEditor(list);
            }

        }

        private void DrawSplitMetadataEditor(Listing_Standard list)
        {
            list.Gap(6f);
            list.Label(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.Metadata", "Split metadata") + ": " + MetaDefName);
            MetaLabel = UiFieldRow.Text(list.GetRect(26f),
                OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMeta.Label", "Label"), MetaLabel, 86f,
                OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.NameTip", "Name shown for the new WorkType and used to build its stable defName."));
            MetaActionWord = UiFieldRow.Text(list.GetRect(26f),
                OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMeta.ActionWord", "Action word"), MetaActionWord, 86f,
                OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.ActionWordTip", "Verb shown in action contexts for this WorkType, for example Hauling. Defaults to the name."));
            MetaDescription = UiFieldRow.Text(list.GetRect(26f),
                OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMeta.Description", "Description"), MetaDescription, 86f,
                OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.DescriptionTip", "Description shown in tooltips for this WorkType."));
            Rect buttons = list.GetRect(26f);
            Rect[] cells = UiFlex.Columns(buttons, 6f, 1f, 1f);
            if (UiButtonBar.Button(cells[0], OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.SaveMetadata", "Save metadata"), true))
            {
                host.SafeSplitMergeAction(delegate { SaveSplitMetadata(); });
            }
            if (UiButtonBar.Button(cells[1], OrderedWorkMod.ConfigText("OrderedWork.Config.Common.Cancel", "Cancel"), true))
            {
                ResetMetadata();
            }
        }

        private void SaveSplitMetadata()
        {
            WorkConfigProfile profile = host.LoadEditingProfile();
            if (profile == null || string.IsNullOrEmpty(MetaDefName)
                || !profile.WorkTypes.TryGetValue(MetaDefName, out WorkTypeConfig split)
                || split == null || split.Vanilla || string.IsNullOrEmpty(split.Base))
            {
                return;
            }
            split.Label = string.IsNullOrWhiteSpace(MetaLabel) ? MetaDefName : MetaLabel.Trim();
            split.ActionWord = MetaActionWord != null ? MetaActionWord.Trim() : "";
            split.Description = MetaDescription != null ? MetaDescription.Trim() : "";
            WorkConfigStore.SaveProfile(profile, true);
            WorkConfigStore.SavePending(profile);
            OrderedWorkStartup.ApplyProfileNow(profile, OrderedWorkMod.Settings, host);
            Messages.Message(string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.MetadataSaved", "Split metadata saved: {0}"), MetaDefName),
                MessageTypeDefOf.TaskCompletion, false);
            ResetMetadata();
        }

        private void DeleteSplit(string defName)
        {
            WorkConfigProfile profile = host.LoadEditingProfile();
            if (profile == null || string.IsNullOrEmpty(defName)
                || !profile.WorkTypes.TryGetValue(defName, out WorkTypeConfig split)
                || split == null || split.Vanilla || string.IsNullOrEmpty(split.Base))
            {
                return;
            }
            if (!profile.WorkTypes.TryGetValue(split.Base, out WorkTypeConfig origin) || origin == null)
            {
                Messages.Message(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.BaseMissing",
                    "Cannot delete this split: its base WorkType is missing."), MessageTypeDefOf.RejectInput, false);
                return;
            }
            origin.Givers = origin.Givers ?? new List<GiverConfig>();
            foreach (GiverConfig giver in split.Givers ?? new List<GiverConfig>())
            {
                if (giver != null && origin.Givers.All(g => g == null || g.DefName != giver.DefName))
                {
                    origin.Givers.Add(new GiverConfig
                    {
                        DefName = giver.DefName,
                        Order = origin.Givers.Count,
                        Mod = giver.Mod
                    });
                }
            }
            for (int i = 0; i < origin.Givers.Count; i++)
            {
                origin.Givers[i].Order = i;
            }
            profile.WorkTypes.Remove(defName);
            WorkConfigStore.SaveProfile(profile, true);
            WorkConfigStore.SavePending(profile);
            OrderedWorkStartup.ApplyProfileNow(profile, OrderedWorkMod.Settings, host);
            Messages.Message(string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.Deleted", "Split deleted and Givers returned: {0}"), defName),
                MessageTypeDefOf.TaskCompletion, false);
            splitPendingDelete = "";
            ResetMetadata();
            RevertSplitDraft();
        }

        private void RevertSplitDraft()
        {
            SplitOrigin = "";
            SplitName = "";
            SplitDescription = "";
            SplitActionWord = "";
            SplitDraft.Clear();
            SplitOriginScroll = 0f;
            SplitDraftScroll = 0f;
            SplitDirty = false;
            splitPendingDelete = "";
            ResetMetadata();
        }
        internal void DrawSplitEditor(Listing_Standard list)
        {
            host.EnsureSplitMergeDefaults();

            Rect originRect = list.GetRect(28f);
            host.DrawWorkTypeSelector(originRect, OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.OriginWorkType", "Origin work type: "), SplitOrigin, OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.SelectOrigin", "Select origin work type"), delegate(string defName)
            {
                SplitOrigin = defName;
                SplitDraft.Clear();
                SplitOriginScroll = 0f;
                SplitDraftScroll = 0f;
            });

            Rect nameRow = list.GetRect(26f);
            Widgets.Label(new Rect(nameRow.x, nameRow.y, 110f, 24f), OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.NewName", "New name"));
            TooltipHandler.TipRegion(nameRow, OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.NameTip", "Name shown for the new WorkType and used to build its stable defName."));
            SplitName = Widgets.TextField(new Rect(nameRow.x + 114f, nameRow.y, Mathf.Max(60f, nameRow.width - 114f), 24f), SplitName ?? "");
            Rect actionRow = list.GetRect(26f);
            Widgets.Label(new Rect(actionRow.x, actionRow.y, 110f, 24f), OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.ActionWord", "Action word"));
            TooltipHandler.TipRegion(actionRow, OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.ActionWordTip", "Verb shown in action contexts for this WorkType, for example Hauling. Defaults to the name."));
            SplitActionWord = Widgets.TextField(new Rect(actionRow.x + 114f, actionRow.y, Mathf.Max(60f, actionRow.width - 114f), 24f), SplitActionWord ?? "");
            Rect descRow = list.GetRect(26f);
            Widgets.Label(new Rect(descRow.x, descRow.y, 110f, 24f), OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.Description", "Description"));
            TooltipHandler.TipRegion(descRow, OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.DescriptionTip", "Description shown in tooltips for this WorkType."));
            SplitDescription = Widgets.TextField(new Rect(descRow.x + 114f, descRow.y, Mathf.Max(60f, descRow.width - 114f), 24f), SplitDescription ?? "");
            if (!string.IsNullOrWhiteSpace(SplitName))
            {
                list.Label(string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.DefName", "defName: {0}"), host.BuildSplitDefName(SplitName)));
            }

            list.Gap(4f);
            List<WorkGiverDef> originGivers = OrderedWorkMod.GiversForWorkType(SplitOrigin);
            List<string> originNames = originGivers.Select(g => g.defName).ToList();
            HashSet<string> draftSet = new HashSet<string>(SplitDraft);

            Rect headerRect = list.GetRect(20f);
            float columnWidth = (headerRect.width - 12f) * 0.5f;
            Widgets.Label(new Rect(headerRect.x, headerRect.y, columnWidth, 20f),
                string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.OriginGivers", "Origin givers ({0})"), originNames.Count));
            Widgets.Label(new Rect(headerRect.x + columnWidth + 12f, headerRect.y, columnWidth, 20f),
                string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.SplitDest", "Split destination ({0})"), SplitDraft.Count));

            Rect columnsRect = list.GetRect(SplitMergeColumnHeight);
            Rect left = new Rect(columnsRect.x, columnsRect.y, columnWidth, SplitMergeColumnHeight);
            Rect right = new Rect(columnsRect.x + columnWidth + 12f, columnsRect.y, columnWidth, SplitMergeColumnHeight);
            Widgets.DrawBoxSolid(left, UiColors.ColumnBackground);
            Widgets.DrawBoxSolid(right, UiColors.ColumnBackground);
            OriginArea = left;
            DestArea = right;

            DrawGiverColumn(left, originNames, draftSet, true, false, ref SplitOriginScroll, OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.InSplit", "(in split)"), 1);
            DrawGiverColumn(right, SplitDraft, draftSet, false, false, ref SplitDraftScroll, null, 2);

            string warning = SplitValidationWarning();
            host.DrawSplitMergeActions(list, OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.Add", "Add split"), warning,
                delegate
                {
                    SaveSplit();
                },
                delegate
                {
                    SplitName = "";
                    SplitDescription = "";
            SplitActionWord = "";
                    SplitDraft.Clear();
                    SplitDraftScroll = 0f;
                    SplitDirty = false;
                }, SplitDirty, OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.AddTip", "Create a new WorkType from the selected origin Givers."));

            host.HandleSplitMergeDrag();
            list.Gap(8f);
            DrawExistingSplits(list);
        }


        private void DrawOrderCandidates(Listing_Standard list)
        {
            WorkConfigProfile profile = host.LoadEditingProfile();
            WorkConfigProfile reference = WorkConfigStore.LoadReference();
            if (profile == null || reference == null)
            {
                return;
            }

            List<KeyValuePair<string, WorkTypeConfig>> candidates = new List<KeyValuePair<string, WorkTypeConfig>>();
            foreach (KeyValuePair<string, WorkTypeConfig> pair in profile.WorkTypes)
            {
                WorkTypeConfig current = pair.Value;
                WorkTypeConfig baseline;
                if (current == null || current.Givers == null
                    || !reference.WorkTypes.TryGetValue(pair.Key, out baseline)
                    || baseline == null || baseline.Givers == null)
                {
                    continue;
                }
                List<string> currentOrder = current.Givers
                    .Where(g => g != null).OrderBy(g => g.Order).Select(g => g.DefName).ToList();
                List<string> referenceOrder = baseline.Givers
                    .Where(g => g != null).OrderBy(g => g.Order).Select(g => g.DefName).ToList();
                if (currentOrder.Count == referenceOrder.Count
                    && currentOrder.OrderBy(id => id, StringComparer.Ordinal)
                        .SequenceEqual(referenceOrder.OrderBy(id => id, StringComparer.Ordinal), StringComparer.Ordinal)
                    && !currentOrder.SequenceEqual(referenceOrder, StringComparer.Ordinal))
                {
                    candidates.Add(pair);
                }
            }

            if (candidates.Count == 0)
            {
                list.Label(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.OrderNone",
                    "No WorkTypes with out-of-order Givers."));
                return;
            }

            list.Label(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.OrderCandidates",
                "WorkTypes with out-of-order Givers"));
            DrawScrollableList(list, candidates.Count, ref orderListScroll, "order-list", delegate(Rect row, int i)
            {
                KeyValuePair<string, WorkTypeConfig> pair = candidates[i];
                WorkTypeConfig current = pair.Value;
                List<string> currentOrder = current.Givers
                    .Where(g => g != null).OrderBy(g => g.Order).Select(g => g.DefName).ToList();
                UiZebra.Draw(row, i);
                string label = (current.Label ?? DefDatabase<WorkTypeDef>.GetNamedSilentFail(pair.Key)?.labelShort ?? pair.Key)
                    + " · " + currentOrder.Count + " " + OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.GiversSuffix", "Givers");
                Widgets.LabelEllipses(new Rect(row.x + 4f, row.y, row.width - 184f, 22f), label);
                if (Widgets.ButtonText(new Rect(row.xMax - 174f, row.y, 80f, 22f),
                    OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.Select", "Select")))
                {
                    host.SelectedGiverType = pair.Key;
                    host.ResetGiverOrderDraft();
                    orderPendingRevert = "";
                }
                bool confirming = orderPendingRevert == pair.Key;
                if (Widgets.ButtonText(new Rect(row.xMax - 88f, row.y, 84f, 22f),
                    confirming
                        ? OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.ConfirmRevert", "Confirm")
                        : OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.Revert", "Revert")))
                {
                    if (!confirming)
                    {
                        orderPendingRevert = pair.Key;
                    }
                    else
                    {
                        string defName = pair.Key;
                        host.SafeSplitMergeAction(delegate { RevertOrder(defName); });
                    }
                }
            });
        }
        private void DrawMergeCandidates(Listing_Standard list)
        {
            WorkConfigProfile profile = host.LoadEditingProfile();
            WorkConfigProfile reference = WorkConfigStore.LoadReference();
            if (profile == null || reference == null)
            {
                return;
            }

            List<KeyValuePair<string, WorkTypeConfig>> candidates = new List<KeyValuePair<string, WorkTypeConfig>>();
            foreach (KeyValuePair<string, WorkTypeConfig> pair in profile.WorkTypes)
            {
                WorkTypeConfig current = pair.Value;
                if (current == null || current.Givers == null || !string.IsNullOrEmpty(current.Base))
                {
                    continue;
                }
                WorkTypeConfig baseline;
                reference.WorkTypes.TryGetValue(pair.Key, out baseline);
                HashSet<string> currentNames = new HashSet<string>(
                    current.Givers.Where(g => g != null).Select(g => g.DefName), StringComparer.Ordinal);
                HashSet<string> referenceNames = new HashSet<string>(
                    (baseline != null && baseline.Givers != null ? baseline.Givers : new List<GiverConfig>())
                        .Where(g => g != null).Select(g => g.DefName), StringComparer.Ordinal);
                int missing = referenceNames.Count(name => !currentNames.Contains(name));
                int extra = currentNames.Count(name => !referenceNames.Contains(name));
                if (missing > 0 || extra > 0)
                {
                    candidates.Add(pair);
                }
            }

            if (candidates.Count == 0)
            {
                list.Label(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.MergeNone",
                    "No WorkTypes with giver differences."));
                return;
            }

            list.Label(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.MergeCandidates",
                "Giver reconciliation against the vanilla reference"));
            DrawScrollableList(list, candidates.Count, ref mergeListScroll, "merge-list", delegate(Rect row, int i)
            {
                KeyValuePair<string, WorkTypeConfig> pair = candidates[i];
                WorkTypeConfig current = pair.Value;
                WorkTypeConfig baseline;
                reference.WorkTypes.TryGetValue(pair.Key, out baseline);
                HashSet<string> currentNames = new HashSet<string>(
                    current.Givers.Where(g => g != null).Select(g => g.DefName), StringComparer.Ordinal);
                HashSet<string> referenceNames = new HashSet<string>(
                    (baseline != null && baseline.Givers != null ? baseline.Givers : new List<GiverConfig>())
                        .Where(g => g != null).Select(g => g.DefName), StringComparer.Ordinal);
                int missing = referenceNames.Count(name => !currentNames.Contains(name));
                int extra = currentNames.Count(name => !referenceNames.Contains(name));
                UiZebra.Draw(row, i);
                string label = (current.Label ?? DefDatabase<WorkTypeDef>.GetNamedSilentFail(pair.Key)?.labelShort ?? pair.Key)
                    + " · " + string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.MergeMissing", "missing: {0}"), missing)
                    + " · " + string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.MergeExtra", "extra: {0}"), extra);
                Widgets.LabelEllipses(new Rect(row.x + 4f, row.y, row.width - 184f, 22f), label);
                if (Widgets.ButtonText(new Rect(row.xMax - 174f, row.y, 80f, 22f),
                    OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.Select", "Select")))
                {
                    MergeOrigin = pair.Key;
                    MergeDraft.Clear();
                    MergeOriginScroll = 0f;
                    MergeTargetScroll = 0f;
                    MergeDropIndex = -1;
                    mergePendingRevert = "";
                    if (MergeTarget == MergeOrigin)
                    {
                        MergeTarget = "";
                    }
                }
                bool confirming = mergePendingRevert == pair.Key;
                if (Widgets.ButtonText(new Rect(row.xMax - 88f, row.y, 84f, 22f),
                    confirming
                        ? OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.ConfirmRevert", "Confirm")
                        : OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.Revert", "Revert")))
                {
                    if (!confirming)
                    {
                        mergePendingRevert = pair.Key;
                    }
                    else
                    {
                        string defName = pair.Key;
                        host.SafeSplitMergeAction(delegate { RevertMerge(defName); });
                    }
                }
            });
        }
        internal void DrawMergeEditor(Listing_Standard list)
        {
            host.EnsureSplitMergeDefaults();

            Rect selectorRect = list.GetRect(28f);
            float selectorHalf = (selectorRect.width - 8f) * 0.5f;
            host.DrawWorkTypeSelector(new Rect(selectorRect.x, selectorRect.y, selectorHalf, 28f),
                OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.Origin", "Origin: "), MergeOrigin, OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.SelectOrigin", "Select origin work type"), delegate(string defName)
                {
                    MergeOrigin = defName;
                    MergeDraft.Clear();
                    MergeOriginScroll = 0f;
                    MergeTargetScroll = 0f;
                    if (MergeTarget == MergeOrigin)
                    {
                        MergeTarget = "";
                    }
                });
            host.DrawWorkTypeSelector(new Rect(selectorRect.x + selectorHalf + 8f, selectorRect.y, selectorHalf, 28f),
                OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.Target", "Target: "), MergeTarget, OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.SelectTarget", "Select merge target"), delegate(string defName)
                {
                    MergeTarget = defName;
                    MergeDropIndex = -1;
                },
                MergeOrigin);

            list.Gap(4f);
            List<WorkGiverDef> originGivers = OrderedWorkMod.GiversForWorkType(MergeOrigin);
            List<string> originNames = originGivers.Select(g => g.defName).ToList();
            HashSet<string> draftSet = new HashSet<string>(MergeDraft);
            List<string> existingTarget = OrderedWorkMod.GiversForWorkType(MergeTarget).Select(g => g.defName).ToList();

            Rect headerRect = list.GetRect(20f);
            float columnWidth = (headerRect.width - 12f) * 0.5f;
            Widgets.Label(new Rect(headerRect.x, headerRect.y, columnWidth, 20f),
                string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.OriginGivers", "Origin givers ({0})"), originNames.Count));
            Widgets.Label(new Rect(headerRect.x + columnWidth + 12f, headerRect.y, columnWidth, 20f),
                string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.MergeDest", "Destination ({0} fixed + {1} incoming)"), existingTarget.Count, MergeDraft.Count));

            Rect columnsRect = list.GetRect(SplitMergeColumnHeight);
            Rect left = new Rect(columnsRect.x, columnsRect.y, columnWidth, SplitMergeColumnHeight);
            Rect right = new Rect(columnsRect.x + columnWidth + 12f, columnsRect.y, columnWidth, SplitMergeColumnHeight);
            Widgets.DrawBoxSolid(left, UiColors.ColumnBackground);
            Widgets.DrawBoxSolid(right, UiColors.ColumnBackground);
            OriginArea = left;
            DestArea = right;
            MergeDropIndex = -1;

            DrawGiverColumn(left, originNames, draftSet, true, true, ref MergeOriginScroll, OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.InMerge", "(in merge)"), 1);
            DrawMergeDestinationColumn(right, existingTarget, MergeDraft, ref MergeTargetScroll);

            if (DragSide == 2 && DragMergeMode && DragMoved && MergeDropIndex >= 0)
            {
                list.Label(string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.Drop", "Drop \"{0}\" at destination position {1}."), DragGiver, existingTarget.Count + MergeDropIndex + 1));
            }

            Rect actionsRect = list.GetRect(30f);
            float buttonWidth = Mathf.Min(150f, (actionsRect.width - 12f) / 3f);
            Rect selectedRect = new Rect(actionsRect.x, actionsRect.y, buttonWidth, 28f);
            Rect allRect = new Rect(selectedRect.xMax + 6f, actionsRect.y, buttonWidth, 28f);
            Rect cancelRect = new Rect(allRect.xMax + 6f, actionsRect.y, buttonWidth, 28f);

            string selectedWarning = MergeValidationWarning(false);
            string allWarning = MergeValidationWarning(true);
            if (UiButtonBar.Button(selectedRect, OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.MergeSelected", "Merge selected"), string.IsNullOrEmpty(selectedWarning), OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.MergeSelectedTip", "Move the selected origin Givers into the target.")))
            {
                host.SafeSplitMergeAction(delegate { SaveMerge(MergeDraft); });
            }
            if (UiButtonBar.Button(allRect, OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.MergeAll", "Merge all"), string.IsNullOrEmpty(allWarning), OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.MergeAllTip", "Move every origin Giver into the target.")))
            {
                host.SafeSplitMergeAction(delegate { SaveMerge(originNames); });
            }
            if (UiButtonBar.Button(cancelRect, OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.CancelDraft", "Cancel draft"), MergeDirty, OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.CancelDraftTip", "Clear the unsaved draft only.")))
            {
                MergeDraft.Clear();
                MergeTargetScroll = 0f;
                MergeDirty = false;
            }
            if (!string.IsNullOrEmpty(selectedWarning))
            {
                OrderedWorkMod.DrawInlineWarning(list, string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.WarningSelected", "Merge selected: {0}"), selectedWarning));
            }
            if (!string.IsNullOrEmpty(allWarning))
            {
                OrderedWorkMod.DrawInlineWarning(list, string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.WarningAll", "Merge all: {0}"), allWarning));
            }

            host.HandleSplitMergeDrag();
            list.Gap(8f);
            DrawMergeCandidates(list);
        }

        internal string SplitValidationWarning()
        {
            if (DefDatabase<WorkTypeDef>.GetNamedSilentFail(SplitOrigin) == null)
            {
                return OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.WarnSelectOrigin", "Select a valid origin work type.");
            }
            if (string.IsNullOrWhiteSpace(SplitName))
            {
                return OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.WarnEnterName", "Enter a name for the new work type.");
            }
            if (SplitDraft.Count == 0)
            {
                return OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.WarnDragOne", "Drag at least one origin giver into Split destination.");
            }
            List<WorkGiverDef> originGivers = OrderedWorkMod.GiversForWorkType(SplitOrigin);
            if (!OrderedWorkMod.Settings.allowEmptyOrigin && originGivers.Count > 0 && SplitDraft.Count >= originGivers.Count)
            {
                return OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.WarnLeaveOrigin", "This split would leave the origin empty. Leave at least one giver or enable 'Allow empty origin'.");
            }
            foreach (string giver in SplitDraft)
            {
                if (!OrderedWorkMod.GiverBelongsTo(giver, SplitOrigin))
                {
                    return OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.WarnGiversBelong", "Every split giver must belong to the selected origin.");
                }
            }
            return null;
        }

        internal string MergeValidationWarning(bool all)
        {
            if (DefDatabase<WorkTypeDef>.GetNamedSilentFail(MergeOrigin) == null)
            {
                return OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.WarnSelectOrigin", "Select a valid origin work type.");
            }
            if (DefDatabase<WorkTypeDef>.GetNamedSilentFail(MergeTarget) == null)
            {
                return OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.WarnDifferentTarget", "Select a different target work type.");
            }
            if (MergeTarget == MergeOrigin)
            {
                return OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.WarnTargetDifferent", "Target must be a different work type.");
            }
            List<WorkGiverDef> originGivers = OrderedWorkMod.GiversForWorkType(MergeOrigin);
            if (all)
            {
                if (originGivers.Count == 0)
                {
                    return OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.WarnNoGivers", "Origin work type has no givers to move.");
                }
                if (!OrderedWorkMod.Settings.allowEmptyOrigin)
                {
                    return OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.WarnMergeAllEmpty", "Merge all would leave the origin empty. Enable 'Allow empty origin' to confirm.");
                }
                return null;
            }
            if (MergeDraft.Count == 0)
            {
                return OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.WarnDragMerge", "Drag at least one origin giver into Merge destination.");
            }
            if (!OrderedWorkMod.Settings.allowEmptyOrigin && MergeDraft.Count >= originGivers.Count)
            {
                return OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.WarnMergeSelectedEmpty", "Merge selected would leave the origin empty. Enable 'Allow empty origin' to confirm.");
            }
            foreach (string giver in MergeDraft)
            {
                if (!OrderedWorkMod.GiverBelongsTo(giver, MergeOrigin))
                {
                    return OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.WarnMergeGiversBelong", "Every merge giver must belong to the selected origin.");
                }
            }
            return null;
        }




        internal void DrawMergeDestinationColumn(Rect area, List<string> existingNames, List<string> incomingNames, ref float scroll)
        {
            Widgets.DrawBoxSolid(area, UiColors.ColumnBackground);

            int fixedCount = existingNames != null ? existingNames.Count : 0;
            int incomingCount = incomingNames != null ? incomingNames.Count : 0;
            int total = fixedCount + incomingCount;
            float maxScroll = total > 0
                ? Mathf.Max(0f, total * SplitMergeRowHeight - area.height)
                : 0f;
            float contentWidth = area.width - (maxScroll > 0f ? SplitMergeScrollbarWidth + 2f : 0f);
            Rect track = new Rect(area.xMax - SplitMergeScrollbarWidth, area.y, SplitMergeScrollbarWidth, area.height);

            OrderedWorkMod.HandleColumnWheel(area, track, maxScroll, ref scroll);
            host.HandleColumnScrollbarInput(2, track, area.height, maxScroll, ref scroll);
            OrderedWorkMod.ClampColumnScroll(maxScroll, ref scroll);

            if (total == 0)
            {
                Widgets.Label(
                    new Rect(area.x + 6f, area.y + 4f, contentWidth - 12f, area.height - 8f),
                    OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.DragTarget", "Select a target, then drag origin givers here."));
                host.DrawColumnScrollbar(track, area.height, maxScroll, scroll, 2);
                return;
            }

            Event current = Event.current;
            for (int i = 0; i < total; i++)
            {
                float y = area.y + i * SplitMergeRowHeight - scroll;
                if (y + SplitMergeRowHeight <= area.y || y >= area.yMax)
                {
                    continue;
                }

                bool isFixed = i < fixedCount;
                string name = isFixed ? existingNames[i] : incomingNames[i - fixedCount];
                Rect row = new Rect(area.x + 3f, y, contentWidth - 6f, SplitMergeRowHeight - 2f);
                UiZebra.Draw(row, i);
                bool dragged = !isFixed
                    && DragSide == 2
                    && DragMergeMode
                    && DragGiver == name;
                bool dropTarget = !isFixed
                    && DragSide != 0
                    && DragMergeMode
                    && DragMoved
                    && row.Contains(current.mousePosition);

                if (isFixed)
                {
                    host.DrawSplitMergeRow(row, (i + 1) + ". " + name, true, false, false, OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.Fixed", "(fixed)"), false);
                }
                else
                {
                    int position = fixedCount + (i - fixedCount) + 1;
                    host.DrawSplitMergeRow(row, position + ". " + name, false, dragged, dropTarget, null);
                }

                if (!dragged && DragSide == 0)
                {
                    TooltipHandler.TipRegion(row, OrderedWorkMod.GiverTooltip(DefDatabase<WorkGiverDef>.GetNamedSilentFail(name)));
                }

                if (!isFixed
                    && current.type == EventType.MouseDown
                    && current.button == 0
                    && row.Contains(current.mousePosition))
                {
                    DragSide = 2;
                    DragMergeMode = true;
                    DragGiver = name;
                    DragStartY = current.mousePosition.y;
                    DragMoved = false;
                    current.Use();
                }
            }

            // Work out where a dragged merge row would be inserted, including the empty area of
            // the column. A drop above all incoming rows inserts at 0; below them appends.
            if ((DragSide == 1 || DragSide == 2) && DragMergeMode && DragMoved && Mouse.IsOver(area))
            {
                int hovered = -1;
                for (int j = 0; j < incomingCount; j++)
                {
                    float y = area.y + (fixedCount + j) * SplitMergeRowHeight - scroll;
                    Rect row = new Rect(area.x + 3f, y, contentWidth - 6f, SplitMergeRowHeight - 2f);
                    if (row.Contains(current.mousePosition))
                    {
                        hovered = j;
                        break;
                    }
                }
                if (hovered >= 0)
                {
                    MergeDropIndex = hovered;
                }
                else
                {
                    float incomingTop = area.y + fixedCount * SplitMergeRowHeight - scroll;
                    MergeDropIndex = current.mousePosition.y < incomingTop ? 0 : incomingCount;
                }
            }

            host.DrawColumnScrollbar(track, area.height, maxScroll, scroll, 2);
        }

        internal void DrawGiverColumn(
            Rect area,
            List<string> names,
            HashSet<string> draftNames,
            bool originSide,
            bool mergeMode,
            ref float scroll,
            string draftMarker,
            int columnId)
        {
            Widgets.DrawBoxSolid(area, UiColors.ColumnBackground);

            bool hasRows = names != null && names.Count > 0;
            float maxScroll = hasRows
                ? Mathf.Max(0f, names.Count * SplitMergeRowHeight - area.height)
                : 0f;
            float contentWidth = area.width - (maxScroll > 0f ? SplitMergeScrollbarWidth + 2f : 0f);
            Rect track = new Rect(area.xMax - SplitMergeScrollbarWidth, area.y, SplitMergeScrollbarWidth, area.height);

            OrderedWorkMod.HandleColumnWheel(area, track, maxScroll, ref scroll);
            host.HandleColumnScrollbarInput(columnId, track, area.height, maxScroll, ref scroll);
            OrderedWorkMod.ClampColumnScroll(maxScroll, ref scroll);

            if (!hasRows)
            {
                Widgets.Label(
                    new Rect(area.x + 6f, area.y + 4f, contentWidth - 12f, area.height - 8f),
                    originSide ? OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.NoGivers", "No givers on this work type.") : OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.DragOrigin", "Drag origin givers here."));
                host.DrawColumnScrollbar(track, area.height, maxScroll, scroll, columnId);
                return;
            }

            Event current = Event.current;
            for (int i = 0; i < names.Count; i++)
            {
                float y = area.y + i * SplitMergeRowHeight - scroll;
                if (y + SplitMergeRowHeight <= area.y || y >= area.yMax)
                {
                    continue;
                }

                string name = names[i];
                Rect row = new Rect(area.x + 3f, y, contentWidth - 6f, SplitMergeRowHeight - 2f);
                UiZebra.Draw(row, i);
                bool inDraft = originSide && draftNames.Contains(name);
                bool dragged = DragSide != 0
                    && DragMergeMode == mergeMode
                    && DragGiver == name
                    && DragSide == (originSide ? 1 : 2);
                bool dropTarget = host.IsValidDropHover(originSide, mergeMode, row);

                host.DrawSplitMergeRow(row, name, inDraft, dragged, dropTarget, inDraft ? draftMarker : null);

                if (!dragged && DragSide == 0)
                {
                    TooltipHandler.TipRegion(row, OrderedWorkMod.GiverTooltip(DefDatabase<WorkGiverDef>.GetNamedSilentFail(name)));
                }

                if (current.type == EventType.MouseDown
                    && current.button == 0
                    && row.Contains(current.mousePosition))
                {
                    DragSide = originSide ? 1 : 2;
                    DragMergeMode = mergeMode;
                    DragGiver = name;
                    DragStartY = current.mousePosition.y;
                    DragMoved = false;
                    current.Use();
                }
            }

            host.DrawColumnScrollbar(track, area.height, maxScroll, scroll, columnId);
        }

        internal void ResetDrafts()
        {
            SplitOrigin = "";
            SplitName = "";
            SplitDescription = "";
            SplitActionWord = "";
            SplitDraft.Clear();
            SplitOriginScroll = 0f;
            SplitDraftScroll = 0f;
            MergeOrigin = "";
            MergeTarget = "";
            MergeDraft.Clear();
            MergeOriginScroll = 0f;
            MergeTargetScroll = 0f;
            SplitDirty = false;
            MergeDirty = false;
            ResetDrag();
        }

        internal void ResetDrag()
        {
            DragSide = 0;
            DragGiver = null;
            DragMoved = false;
            ScrollDragColumn = 0;
            MergeDropIndex = -1;
        }

        internal void ResetMetadata()
        {
            MetaDefName = "";
            MetaLabel = "";
            MetaDescription = "";
            MetaActionWord = "";
        }
    }
}