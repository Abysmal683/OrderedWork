using System.Collections.Generic;
using UnityEngine;

namespace OrderedWork
{
    /// <summary>
    /// Split/Merge UI state. Drawing and host callbacks remain in OrderedWorkMod for now;
    /// this component owns the draft, scroll, drag and metadata state.
    /// </summary>
    internal sealed class SplitMergePanel
    {
        internal int EditorMode;
        internal string SplitOrigin = "";
        internal string SplitName = "";
        internal string SplitDescription = "";
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

        internal void ResetDrafts()
        {
            SplitOrigin = "";
            SplitName = "";
            SplitDescription = "";
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
        }
    }
}