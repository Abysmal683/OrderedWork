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
            host.CommitProfile(profile);
            Messages.Message(string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.MergeSaved", "Merge saved for {0} (restart to apply)."), MergeTarget), MessageTypeDefOf.TaskCompletion, false);
            MergeDraft.Clear();
            host.ResetGiverOrderDraft();
            MergeTargetScroll = 0f;
            MergeDirty = false;
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





    }
}
