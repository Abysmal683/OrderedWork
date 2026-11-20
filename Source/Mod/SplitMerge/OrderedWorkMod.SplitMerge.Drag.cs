#nullable disable
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
        internal bool IsValidDropHover(bool originSide, bool mergeMode, Rect row)
        {
            if (splitMerge.DragSide == 0 || splitMerge.DragMergeMode != mergeMode || !splitMerge.DragMoved)
            {
                return false;
            }
            bool wantsOrigin = splitMerge.DragSide == 2;
            return wantsOrigin == originSide && row.Contains(Event.current.mousePosition);
        }

        internal void DrawSplitMergeActions(Listing_Standard list, string actionLabel, string warning, Action onSave, Action onCancel, bool canCancel, string actionTooltip = null)
        {
            Rect actionsRect = list.GetRect(30f);
            float buttonWidth = Mathf.Min(150f, (actionsRect.width - 12f) / 3f);
            Rect actionRect = new Rect(actionsRect.x, actionsRect.y, buttonWidth, 28f);
            Rect cancelRect = new Rect(actionRect.xMax + 6f, actionsRect.y, buttonWidth, 28f);

            if (UiButtonBar.Button(actionRect, actionLabel, string.IsNullOrEmpty(warning), actionTooltip))
            {
                SafeSplitMergeAction(onSave);
            }
            if (UiButtonBar.Button(cancelRect, ConfigText("OrderedWork.Config.SplitMerge.CancelDraft", "Cancel draft"), canCancel, ConfigText("OrderedWork.Config.SplitMerge.CancelDraftTip", "Clear the unsaved draft only.")))
            {
                onCancel();
            }
            if (!string.IsNullOrEmpty(warning))
            {
                DrawInlineWarning(list, warning);
            }
        }

        internal static void DrawInlineWarning(Listing_Standard list, string warning)
        {
            Color previous = GUI.color;
            GUI.color = new Color(1f, 0.74f, 0.40f, 1f);
            list.Label(string.Format(ConfigText("OrderedWork.Config.SplitMerge.WarningPrefix", "Warning: {0}"), warning));
            GUI.color = previous;
        }

        internal void HandleSplitMergeDrag()
        {
            if (splitMerge.DragSide == 0)
            {
                return;
            }

            Event current = Event.current;
            if (current.type == EventType.MouseDrag)
            {
                if (!splitMerge.DragMoved && Mathf.Abs(current.mousePosition.y - splitMerge.DragStartY) > 3f)
                {
                    splitMerge.DragMoved = true;
                }
                if (splitMerge.DragMoved)
                {
                    current.Use();
                }
                return;
            }

            if (current.type != EventType.MouseUp)
            {
                return;
            }

            bool dropInOrigin = splitMerge.OriginArea.Contains(current.mousePosition);
            bool dropInDest = splitMerge.DestArea.Contains(current.mousePosition);

            if (splitMerge.DragSide == 1 && dropInDest)
            {
                AddDraftGiver(splitMerge.DragGiver, splitMerge.DragMergeMode);
                if (splitMerge.DragMergeMode && splitMerge.MergeDropIndex >= 0)
                {
                    ReorderMergeDraft(splitMerge.DragGiver, splitMerge.MergeDropIndex);
                }
            }
            else if (splitMerge.DragSide == 2 && dropInOrigin)
            {
                RemoveDraftGiver(splitMerge.DragGiver, splitMerge.DragMergeMode);
            }
            else if (splitMerge.DragSide == 2 && splitMerge.DragMergeMode && dropInDest && splitMerge.MergeDropIndex >= 0)
            {
                ReorderMergeDraft(splitMerge.DragGiver, splitMerge.MergeDropIndex);
            }

            ResetSplitMergeDrag();
            current.Use();
        }

        private void ReorderMergeDraft(string giverName, int targetIndex)
        {
            int current = splitMerge.MergeDraft.IndexOf(giverName);
            if (current < 0)
            {
                return;
            }
            splitMerge.MergeDraft.RemoveAt(current);
            splitMerge.MergeDraft.Insert(Mathf.Clamp(targetIndex, 0, splitMerge.MergeDraft.Count), giverName);
        }

        private void ResetSplitMergeDrag()
        {
            splitMerge.DragSide = 0;
            splitMerge.DragGiver = null;
            splitMerge.DragMoved = false;
            splitMerge.ScrollDragColumn = 0;
            splitMerge.MergeDropIndex = -1;
        }

        private void AddDraftGiver(string giverName, bool mergeMode)
        {
            string origin = mergeMode ? splitMerge.MergeOrigin : splitMerge.SplitOrigin;
            if (!GiverBelongsTo(giverName, origin))
            {
                return;
            }
            List<string> draft = mergeMode ? splitMerge.MergeDraft : splitMerge.SplitDraft;
            if (!draft.Contains(giverName))
            {
                draft.Add(giverName);
                if (mergeMode) splitMerge.MergeDirty = true; else splitMerge.SplitDirty = true;
            }
        }

        private void RemoveDraftGiver(string giverName, bool mergeMode)
        {
            List<string> draft = mergeMode ? splitMerge.MergeDraft : splitMerge.SplitDraft;
            if (draft.Remove(giverName))
            {
                if (mergeMode) splitMerge.MergeDirty = true; else splitMerge.SplitDirty = true;
            }
        }

        internal void EnsureSplitMergeDefaults()
        {
            string fallback = WorkConfigStore.GetOrderedVisibleWorkTypes(LoadEditingProfile())
                .FirstOrDefault()?.defName ?? "";
            WorkTypeDef selectedGiverDef = DefDatabase<WorkTypeDef>.GetNamedSilentFail(selectedGiverType);
            if (selectedGiverDef == null || !selectedGiverDef.visible)
            {
                selectedGiverType = "";
            }
            if (DefDatabase<WorkTypeDef>.GetNamedSilentFail(splitMerge.SplitOrigin) == null)
            {
                splitMerge.SplitOrigin = fallback;
                splitMerge.SplitDraft.Clear();
            }
            if (DefDatabase<WorkTypeDef>.GetNamedSilentFail(splitMerge.MergeOrigin) == null)
            {
                splitMerge.MergeOrigin = fallback;
                splitMerge.MergeDraft.Clear();
            }
            if (!string.IsNullOrEmpty(splitMerge.MergeTarget)
                && (splitMerge.MergeTarget == splitMerge.MergeOrigin || DefDatabase<WorkTypeDef>.GetNamedSilentFail(splitMerge.MergeTarget) == null))
            {
                splitMerge.MergeTarget = "";
            }
            if (DefDatabase<WorkTypeDef>.GetNamedSilentFail(selectedGiverType) == null)
            {
                selectedGiverType = fallback;
            }
            splitMerge.SplitDraft.RemoveAll(g => !GiverBelongsTo(g, splitMerge.SplitOrigin));
            splitMerge.MergeDraft.RemoveAll(g => !GiverBelongsTo(g, splitMerge.MergeOrigin));
        }
    }
}
