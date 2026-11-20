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
    internal sealed partial class SplitMergePanel
    {
        internal void SaveSplit()
        {
            if (!string.IsNullOrEmpty(SplitValidationWarning()))
            {
                return;
            }

            string newDefName = host.BuildSplitDefName(SplitName);
            WorkConfigProfile? profile = host.LoadEditingProfile();
            string mod = host.Content != null && host.Content.ModMetaData != null
                ? host.Content.PackageId
                : "nightmare123487.OrderedWork";
            if (profile == null || !WorkConfigReconciler.CreateSplit(profile, SplitOrigin, newDefName, SplitDraft,
                SplitName.Trim(), SplitActionWord.Trim(), SplitDescription.Trim(), mod, out _))
            {
                return;
            }            host.CommitProfile(profile);
            Messages.Message(string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.SplitSaved", "Split saved: {0} (restart to apply)."), SplitName), MessageTypeDefOf.TaskCompletion, false);
            host.ResetGiverOrderDraft();

            SplitName = "";
            SplitDescription = "";
            SplitActionWord = "";
            SplitDraft.Clear();
            SplitDraftScroll = 0f;
            SplitDirty = false;
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
            WorkConfigProfile? profile = host.LoadEditingProfile();
            if (profile == null || string.IsNullOrEmpty(MetaDefName)
                || !profile.WorkTypes.TryGetValue(MetaDefName, out WorkTypeConfig? split)
                || split == null || split.Vanilla || string.IsNullOrEmpty(split.Base))
            {
                return;
            }
            split.Label = string.IsNullOrWhiteSpace(MetaLabel) ? MetaDefName : MetaLabel.Trim();
            split.ActionWord = MetaActionWord != null ? MetaActionWord.Trim() : "";
            split.Description = MetaDescription != null ? MetaDescription.Trim() : "";
            host.CommitProfile(profile);
            Messages.Message(string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.MetadataSaved", "Split metadata saved: {0}"), MetaDefName),
                MessageTypeDefOf.TaskCompletion, false);
            ResetMetadata();
        }

        private void DeleteSplit(string defName)
        {
            WorkConfigProfile? profile = host.LoadEditingProfile();
            if (profile == null || string.IsNullOrEmpty(defName)
                || !profile.WorkTypes.TryGetValue(defName, out WorkTypeConfig? split)
                || split == null || split.Vanilla || string.IsNullOrEmpty(split.Base))
            {
                return;
            }
            if (!profile.WorkTypes.TryGetValue(split.Base, out WorkTypeConfig? origin) || origin == null)
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
            host.CommitProfile(profile);
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

            string? warning = SplitValidationWarning();
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



        internal string? SplitValidationWarning()
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


    }
}
