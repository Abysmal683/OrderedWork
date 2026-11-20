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
        private void DrawSplitMergeEditor(Listing_Standard list)
        {
            list.Label(ConfigText("OrderedWork.Config.SplitMerge.Info", "Split / Merge / Order editor. Drafts are temporary; saved entries apply on restart."));
            list.Gap(28f);

            Rect modeRect = list.GetRect(28f);
            UiTabBar.Draw(modeRect, new[] { ConfigText("OrderedWork.Config.SplitMerge.SplitTab", "Split"), ConfigText("OrderedWork.Config.SplitMerge.MergeTab", "Merge"), ConfigText("OrderedWork.Config.SplitMerge.OrderTab", "Order") }, settingsEditorMode,
                delegate(int index)
                {
                    if (settingsEditorMode == index) return;
                    settingsEditorMode = index;
                    ResetEditorUiState();
                    ResetGiverOrderDraft();
                });
            list.Label(ConfigText("OrderedWork.Config.SplitMerge.Tip", "Tip: Ctrl+wheel or drag a column scrollbar to scroll that column; a plain wheel scrolls the settings."));
            list.Gap(4f);

            switch (settingsEditorMode)
            {
                case EditorMerge:
                    DrawMergeEditor(list);
                    break;
                case EditorOrder:
                    DrawOrderEditor(list);
                    break;
                default:
                    DrawSplitEditor(list);
                    break;
            }
        }

        private void DrawSplitEditor(Listing_Standard list)
        {
            EnsureSplitMergeDefaults();

            Rect originRect = list.GetRect(28f);
            DrawWorkTypeSelector(originRect, ConfigText("OrderedWork.Config.SplitMerge.OriginWorkType", "Origin work type: "), splitOrigin, ConfigText("OrderedWork.Config.SplitMerge.SelectOrigin", "Select origin work type"), delegate(string defName)
            {
                splitOrigin = defName;
                splitDraft.Clear();
                splitOriginScroll = 0f;
                splitDraftScroll = 0f;
            });

            Rect nameRow = list.GetRect(26f);
            Widgets.Label(new Rect(nameRow.x, nameRow.y, 110f, 24f), ConfigText("OrderedWork.Config.SplitMerge.NewName", "New name"));
            splitName = Widgets.TextField(new Rect(nameRow.x + 114f, nameRow.y, Mathf.Max(60f, nameRow.width - 114f), 24f), splitName ?? "");
            Rect descRow = list.GetRect(26f);
            Widgets.Label(new Rect(descRow.x, descRow.y, 110f, 24f), ConfigText("OrderedWork.Config.SplitMerge.Description", "Description"));
            splitDescription = Widgets.TextField(new Rect(descRow.x + 114f, descRow.y, Mathf.Max(60f, descRow.width - 114f), 24f), splitDescription ?? "");
            if (!string.IsNullOrWhiteSpace(splitName))
            {
                list.Label(string.Format(ConfigText("OrderedWork.Config.SplitMerge.DefName", "defName: {0}"), BuildSplitDefName(splitName)));
            }

            list.Gap(4f);
            List<WorkGiverDef> originGivers = GiversForWorkType(splitOrigin);
            List<string> originNames = originGivers.Select(g => g.defName).ToList();
            HashSet<string> draftSet = new HashSet<string>(splitDraft);

            Rect headerRect = list.GetRect(20f);
            float columnWidth = (headerRect.width - 12f) * 0.5f;
            Widgets.Label(new Rect(headerRect.x, headerRect.y, columnWidth, 20f),
                string.Format(ConfigText("OrderedWork.Config.SplitMerge.OriginGivers", "Origin givers ({0})"), originNames.Count));
            Widgets.Label(new Rect(headerRect.x + columnWidth + 12f, headerRect.y, columnWidth, 20f),
                string.Format(ConfigText("OrderedWork.Config.SplitMerge.SplitDest", "Split destination ({0})"), splitDraft.Count));

            Rect columnsRect = list.GetRect(SplitMergeColumnHeight);
            Rect left = new Rect(columnsRect.x, columnsRect.y, columnWidth, SplitMergeColumnHeight);
            Rect right = new Rect(columnsRect.x + columnWidth + 12f, columnsRect.y, columnWidth, SplitMergeColumnHeight);
            Widgets.DrawBoxSolid(left, UiColors.ColumnBackground);
            Widgets.DrawBoxSolid(right, UiColors.ColumnBackground);
            smOriginArea = left;
            smDestArea = right;

            DrawGiverColumn(left, originNames, draftSet, true, false, ref splitOriginScroll, ConfigText("OrderedWork.Config.SplitMerge.InSplit", "(in split)"), 1);
            DrawGiverColumn(right, splitDraft, draftSet, false, false, ref splitDraftScroll, null, 2);

            string warning = SplitValidationWarning();
            DrawSplitMergeActions(list, ConfigText("OrderedWork.Config.SplitMerge.Add", "Add split"), warning,
                delegate
                {
                    SaveSplit();
                },
                delegate
                {
                    splitName = "";
                    splitDescription = "";
                    splitDraft.Clear();
                    splitDraftScroll = 0f;
                    splitDirty = false;
                }, splitDirty);

            HandleSplitMergeDrag();
        }

        private WorkConfigProfile LoadEditingProfile()
        {
            WorkConfigProfile pending = WorkConfigStore.LoadPending(activeProfileName);
            if (pending != null)
            {
                return pending;
            }
            WorkConfigLoadResult active = WorkConfigStore.LoadActiveWithFallback(activeProfileName);
            return active != null ? active.Profile : null;
        }

        private void DrawMergeEditor(Listing_Standard list)
        {
            EnsureSplitMergeDefaults();

            Rect selectorRect = list.GetRect(28f);
            float selectorHalf = (selectorRect.width - 8f) * 0.5f;
            DrawWorkTypeSelector(new Rect(selectorRect.x, selectorRect.y, selectorHalf, 28f),
                ConfigText("OrderedWork.Config.SplitMerge.Origin", "Origin: "), mergeOrigin, ConfigText("OrderedWork.Config.SplitMerge.SelectOrigin", "Select origin work type"), delegate(string defName)
                {
                    mergeOrigin = defName;
                    mergeDraft.Clear();
                    mergeOriginScroll = 0f;
                    mergeTargetScroll = 0f;
                    if (mergeTarget == mergeOrigin)
                    {
                        mergeTarget = "";
                    }
                });
            DrawWorkTypeSelector(new Rect(selectorRect.x + selectorHalf + 8f, selectorRect.y, selectorHalf, 28f),
                ConfigText("OrderedWork.Config.SplitMerge.Target", "Target: "), mergeTarget, ConfigText("OrderedWork.Config.SplitMerge.SelectTarget", "Select merge target"), delegate(string defName)
                {
                    mergeTarget = defName;
                    smMergeDropIndex = -1;
                },
                mergeOrigin);

            list.Gap(4f);
            List<WorkGiverDef> originGivers = GiversForWorkType(mergeOrigin);
            List<string> originNames = originGivers.Select(g => g.defName).ToList();
            HashSet<string> draftSet = new HashSet<string>(mergeDraft);
            List<string> existingTarget = GiversForWorkType(mergeTarget).Select(g => g.defName).ToList();

            Rect headerRect = list.GetRect(20f);
            float columnWidth = (headerRect.width - 12f) * 0.5f;
            Widgets.Label(new Rect(headerRect.x, headerRect.y, columnWidth, 20f),
                string.Format(ConfigText("OrderedWork.Config.SplitMerge.OriginGivers", "Origin givers ({0})"), originNames.Count));
            Widgets.Label(new Rect(headerRect.x + columnWidth + 12f, headerRect.y, columnWidth, 20f),
                string.Format(ConfigText("OrderedWork.Config.SplitMerge.MergeDest", "Destination ({0} fixed + {1} incoming)"), existingTarget.Count, mergeDraft.Count));

            Rect columnsRect = list.GetRect(SplitMergeColumnHeight);
            Rect left = new Rect(columnsRect.x, columnsRect.y, columnWidth, SplitMergeColumnHeight);
            Rect right = new Rect(columnsRect.x + columnWidth + 12f, columnsRect.y, columnWidth, SplitMergeColumnHeight);
            Widgets.DrawBoxSolid(left, UiColors.ColumnBackground);
            Widgets.DrawBoxSolid(right, UiColors.ColumnBackground);
            smOriginArea = left;
            smDestArea = right;
            smMergeDropIndex = -1;

            DrawGiverColumn(left, originNames, draftSet, true, true, ref mergeOriginScroll, ConfigText("OrderedWork.Config.SplitMerge.InMerge", "(in merge)"), 1);
            DrawMergeDestinationColumn(right, existingTarget, mergeDraft, ref mergeTargetScroll);

            if (smDragSide == 2 && smDragMergeMode && smDragMoved && smMergeDropIndex >= 0)
            {
                list.Label(string.Format(ConfigText("OrderedWork.Config.SplitMerge.Drop", "Drop \"{0}\" at destination position {1}."), smDragGiver, existingTarget.Count + smMergeDropIndex + 1));
            }

            Rect actionsRect = list.GetRect(30f);
            float buttonWidth = Mathf.Min(150f, (actionsRect.width - 12f) / 3f);
            Rect selectedRect = new Rect(actionsRect.x, actionsRect.y, buttonWidth, 28f);
            Rect allRect = new Rect(selectedRect.xMax + 6f, actionsRect.y, buttonWidth, 28f);
            Rect cancelRect = new Rect(allRect.xMax + 6f, actionsRect.y, buttonWidth, 28f);

            string selectedWarning = MergeValidationWarning(false);
            string allWarning = MergeValidationWarning(true);
            if (UiButtonBar.Button(selectedRect, ConfigText("OrderedWork.Config.SplitMerge.MergeSelected", "Merge selected"), string.IsNullOrEmpty(selectedWarning)))
            {
                SafeSplitMergeAction(delegate { SaveMerge(mergeDraft); });
            }
            if (UiButtonBar.Button(allRect, ConfigText("OrderedWork.Config.SplitMerge.MergeAll", "Merge all"), string.IsNullOrEmpty(allWarning)))
            {
                SafeSplitMergeAction(delegate { SaveMerge(originNames); });
            }
            if (UiButtonBar.Button(cancelRect, ConfigText("OrderedWork.Config.SplitMerge.CancelDraft", "Cancel draft"), mergeDirty))
            {
                mergeDraft.Clear();
                mergeTargetScroll = 0f;
                mergeDirty = false;
            }
            if (!string.IsNullOrEmpty(selectedWarning))
            {
                DrawInlineWarning(list, string.Format(ConfigText("OrderedWork.Config.SplitMerge.WarningSelected", "Merge selected: {0}"), selectedWarning));
            }
            if (!string.IsNullOrEmpty(allWarning))
            {
                DrawInlineWarning(list, string.Format(ConfigText("OrderedWork.Config.SplitMerge.WarningAll", "Merge all: {0}"), allWarning));
            }

            HandleSplitMergeDrag();
        }

    }
}