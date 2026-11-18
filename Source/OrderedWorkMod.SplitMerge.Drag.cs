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
    {        private bool IsValidDropHover(bool originSide, bool mergeMode, Rect row)
        {
            if (smDragSide == 0 || smDragMergeMode != mergeMode || !smDragMoved)
            {
                return false;
            }
            bool wantsOrigin = smDragSide == 2;
            return wantsOrigin == originSide && row.Contains(Event.current.mousePosition);
        }

        private void DrawSplitMergeActions(Listing_Standard list, string actionLabel, string warning, Action onSave, Action onCancel, bool canCancel)
        {
            Rect actionsRect = list.GetRect(30f);
            float buttonWidth = Mathf.Min(150f, (actionsRect.width - 12f) / 3f);
            Rect actionRect = new Rect(actionsRect.x, actionsRect.y, buttonWidth, 28f);
            Rect cancelRect = new Rect(actionRect.xMax + 6f, actionsRect.y, buttonWidth, 28f);

            if (DrawActionButton(actionRect, actionLabel, string.IsNullOrEmpty(warning)))
            {
                onSave();
            }
            if (DrawActionButton(cancelRect, "Cancel draft", canCancel))
            {
                onCancel();
            }
            if (!string.IsNullOrEmpty(warning))
            {
                DrawInlineWarning(list, warning);
            }
        }

        private static bool DrawActionButton(Rect rect, string label, bool enabled, string tooltip = null)
        {
            Color background = enabled
                ? new Color(0.17f, 0.17f, 0.19f, 1f)
                : new Color(0.11f, 0.11f, 0.13f, 1f);
            Widgets.DrawBoxSolid(rect, background);
            if (enabled && Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }
            Widgets.DrawBox(rect);

            Color previous = GUI.color;
            GUI.color = enabled ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.75f);
            TextAnchor previousAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, label);
            Text.Anchor = previousAnchor;
            GUI.color = previous;

            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }
            return enabled && Widgets.ButtonInvisible(rect);
        }
        private static void DrawInlineWarning(Listing_Standard list, string warning)
        {
            Color previous = GUI.color;
            GUI.color = new Color(1f, 0.74f, 0.40f, 1f);
            list.Label("Warning: " + warning);
            GUI.color = previous;
        }

        private string SplitValidationWarning()
        {
            if (DefDatabase<WorkTypeDef>.GetNamedSilentFail(splitOrigin) == null)
            {
                return "Select a valid origin work type.";
            }
            if (string.IsNullOrWhiteSpace(splitName))
            {
                return "Enter a name for the new work type.";
            }
            if (splitDraft.Count == 0)
            {
                return "Drag at least one origin giver into Split destination.";
            }
            List<WorkGiverDef> originGivers = GiversForWorkType(splitOrigin);
            if (!Settings.allowEmptyOrigin && originGivers.Count > 0 && splitDraft.Count >= originGivers.Count)
            {
                return "This split would leave the origin empty. Leave at least one giver or enable 'Allow empty origin'.";
            }
            foreach (string giver in splitDraft)
            {
                if (!GiverBelongsTo(giver, splitOrigin))
                {
                    return "Every split giver must belong to the selected origin.";
                }
            }
            return null;
        }

        private string MergeValidationWarning(bool all)
        {
            if (DefDatabase<WorkTypeDef>.GetNamedSilentFail(mergeOrigin) == null)
            {
                return "Select a valid origin work type.";
            }
            if (DefDatabase<WorkTypeDef>.GetNamedSilentFail(mergeTarget) == null)
            {
                return "Select a different target work type.";
            }
            if (mergeTarget == mergeOrigin)
            {
                return "Target must be a different work type.";
            }
            List<WorkGiverDef> originGivers = GiversForWorkType(mergeOrigin);
            if (all)
            {
                if (originGivers.Count == 0)
                {
                    return "Origin work type has no givers to move.";
                }
                if (!Settings.allowEmptyOrigin)
                {
                    return "Merge all would leave the origin empty. Enable 'Allow empty origin' to confirm.";
                }
                return null;
            }
            if (mergeDraft.Count == 0)
            {
                return "Drag at least one origin giver into Merge destination.";
            }
            if (!Settings.allowEmptyOrigin && mergeDraft.Count >= originGivers.Count)
            {
                return "Merge selected would leave the origin empty. Enable 'Allow empty origin' to confirm.";
            }
            foreach (string giver in mergeDraft)
            {
                if (!GiverBelongsTo(giver, mergeOrigin))
                {
                    return "Every merge giver must belong to the selected origin.";
                }
            }
            return null;
        }

        private void HandleSplitMergeDrag()
        {
            if (smDragSide == 0)
            {
                return;
            }

            Event current = Event.current;
            if (current.type == EventType.MouseDrag)
            {
                if (!smDragMoved && Mathf.Abs(current.mousePosition.y - smDragStartY) > 3f)
                {
                    smDragMoved = true;
                }
                if (smDragMoved)
                {
                    current.Use();
                }
                return;
            }

            if (current.type != EventType.MouseUp)
            {
                return;
            }

            bool dropInOrigin = smOriginArea.Contains(current.mousePosition);
            bool dropInDest = smDestArea.Contains(current.mousePosition);

            if (smDragSide == 1 && dropInDest)
            {
                AddDraftGiver(smDragGiver, smDragMergeMode);
                if (smDragMergeMode && smMergeDropIndex >= 0)
                {
                    ReorderMergeDraft(smDragGiver, smMergeDropIndex);
                }
            }
            else if (smDragSide == 2 && dropInOrigin)
            {
                RemoveDraftGiver(smDragGiver, smDragMergeMode);
            }
            else if (smDragSide == 2 && smDragMergeMode && dropInDest && smMergeDropIndex >= 0)
            {
                ReorderMergeDraft(smDragGiver, smMergeDropIndex);
            }

            ResetSplitMergeDrag();
            current.Use();
        }

        private void ReorderMergeDraft(string giverName, int targetIndex)
        {
            int current = mergeDraft.IndexOf(giverName);
            if (current < 0)
            {
                return;
            }
            mergeDraft.RemoveAt(current);
            mergeDraft.Insert(Mathf.Clamp(targetIndex, 0, mergeDraft.Count), giverName);
        }

        private void ResetSplitMergeDrag()
        {
            smDragSide = 0;
            smDragGiver = null;
            smDragMoved = false;
            smScrollDragColumn = 0;
            smMergeDropIndex = -1;
        }

        private void AddDraftGiver(string giverName, bool mergeMode)
        {
            string origin = mergeMode ? mergeOrigin : splitOrigin;
            if (!GiverBelongsTo(giverName, origin))
            {
                return;
            }
            List<string> draft = mergeMode ? mergeDraft : splitDraft;
            if (!draft.Contains(giverName))
            {
                draft.Add(giverName);
                if (mergeMode) mergeDirty = true; else splitDirty = true;
            }
        }

        private void RemoveDraftGiver(string giverName, bool mergeMode)
        {
            List<string> draft = mergeMode ? mergeDraft : splitDraft;
            if (draft.Remove(giverName))
            {
                if (mergeMode) mergeDirty = true; else splitDirty = true;
            }
        }

        private void EnsureSplitMergeDefaults()
        {
            string fallback = WorkConfigStore.GetOrderedVisibleWorkTypes(LoadEditingProfile())
                .FirstOrDefault()?.defName ?? "";
            WorkTypeDef selectedGiverDef = DefDatabase<WorkTypeDef>.GetNamedSilentFail(selectedGiverType);
            if (selectedGiverDef == null || !selectedGiverDef.visible)
            {
                selectedGiverType = "";
            }
            if (DefDatabase<WorkTypeDef>.GetNamedSilentFail(splitOrigin) == null)
            {
                splitOrigin = fallback;
                splitDraft.Clear();
            }
            if (DefDatabase<WorkTypeDef>.GetNamedSilentFail(mergeOrigin) == null)
            {
                mergeOrigin = fallback;
                mergeDraft.Clear();
            }
            if (!string.IsNullOrEmpty(mergeTarget)
                && (mergeTarget == mergeOrigin || DefDatabase<WorkTypeDef>.GetNamedSilentFail(mergeTarget) == null))
            {
                mergeTarget = "";
            }
            if (DefDatabase<WorkTypeDef>.GetNamedSilentFail(selectedGiverType) == null)
            {
                selectedGiverType = fallback;
            }
            splitDraft.RemoveAll(g => !GiverBelongsTo(g, splitOrigin));
            mergeDraft.RemoveAll(g => !GiverBelongsTo(g, mergeOrigin));
        }

    }
}