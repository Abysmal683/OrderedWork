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


    }
}
