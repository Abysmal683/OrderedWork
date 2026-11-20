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
    internal sealed partial class SplitMergePanel
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
