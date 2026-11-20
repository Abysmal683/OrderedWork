using System;
using System.Collections.Generic;
using System.Linq;
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
        private OrderedWorkMod host;

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
                    break;
                default:
                    DrawSplitEditor(list);
                    break;
            }
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
            SplitName = Widgets.TextField(new Rect(nameRow.x + 114f, nameRow.y, Mathf.Max(60f, nameRow.width - 114f), 24f), SplitName ?? "");
            Rect descRow = list.GetRect(26f);
            Widgets.Label(new Rect(descRow.x, descRow.y, 110f, 24f), OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.Description", "Description"));
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

            string warning = host.SplitValidationWarning();
            host.DrawSplitMergeActions(list, OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.Add", "Add split"), warning,
                delegate
                {
                    host.SaveSplit();
                },
                delegate
                {
                    SplitName = "";
                    SplitDescription = "";
                    SplitDraft.Clear();
                    SplitDraftScroll = 0f;
                    SplitDirty = false;
                }, SplitDirty);

            host.HandleSplitMergeDrag();
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
            host.DrawMergeDestinationColumn(right, existingTarget, MergeDraft, ref MergeTargetScroll);

            if (DragSide == 2 && DragMergeMode && DragMoved && MergeDropIndex >= 0)
            {
                list.Label(string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.Drop", "Drop \"{0}\" at destination position {1}."), DragGiver, existingTarget.Count + MergeDropIndex + 1));
            }

            Rect actionsRect = list.GetRect(30f);
            float buttonWidth = Mathf.Min(150f, (actionsRect.width - 12f) / 3f);
            Rect selectedRect = new Rect(actionsRect.x, actionsRect.y, buttonWidth, 28f);
            Rect allRect = new Rect(selectedRect.xMax + 6f, actionsRect.y, buttonWidth, 28f);
            Rect cancelRect = new Rect(allRect.xMax + 6f, actionsRect.y, buttonWidth, 28f);

            string selectedWarning = host.MergeValidationWarning(false);
            string allWarning = host.MergeValidationWarning(true);
            if (UiButtonBar.Button(selectedRect, OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.MergeSelected", "Merge selected"), string.IsNullOrEmpty(selectedWarning)))
            {
                host.SafeSplitMergeAction(delegate { host.SaveMerge(MergeDraft); });
            }
            if (UiButtonBar.Button(allRect, OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.MergeAll", "Merge all"), string.IsNullOrEmpty(allWarning)))
            {
                host.SafeSplitMergeAction(delegate { host.SaveMerge(originNames); });
            }
            if (UiButtonBar.Button(cancelRect, OrderedWorkMod.ConfigText("OrderedWork.Config.SplitMerge.CancelDraft", "Cancel draft"), MergeDirty))
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