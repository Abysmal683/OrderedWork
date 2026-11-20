#nullable disable
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
    }
}
