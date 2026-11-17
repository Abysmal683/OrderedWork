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
    {
        private void DrawSplitMergeEditor(Listing_Standard list)
        {
            list.Label("Split / Merge / Order editor. Drafts are temporary; saved entries apply on restart.");

            Rect modeRect = list.GetRect(28f);
            float modeThird = (modeRect.width - 16f) / 3f;
            Rect splitRect = new Rect(modeRect.x, modeRect.y, modeThird, 28f);
            Rect mergeRect = new Rect(splitRect.xMax + 8f, modeRect.y, modeThird, 28f);
            Rect orderRect = new Rect(mergeRect.xMax + 8f, modeRect.y, modeThird, 28f);

            if (Widgets.ButtonText(splitRect, "Split mode") && settingsEditorMode != EditorSplit)
            {
                settingsEditorMode = EditorSplit;
                ResetEditorUiState();
            }
            if (Widgets.ButtonText(mergeRect, "Merge mode") && settingsEditorMode != EditorMerge)
            {
                settingsEditorMode = EditorMerge;
                ResetEditorUiState();
            }
            if (Widgets.ButtonText(orderRect, "Order mode") && settingsEditorMode != EditorOrder)
            {
                settingsEditorMode = EditorOrder;
                ResetEditorUiState();
            }

            Rect marker = settingsEditorMode == EditorMerge ? mergeRect
                : settingsEditorMode == EditorOrder ? orderRect
                : splitRect;
            DrawTabMarker(marker);
            list.Label("Tip: Ctrl+wheel or drag a column scrollbar to scroll that column; a plain wheel scrolls the settings.");
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
            DrawWorkTypeSelector(originRect, "Origin work type: ", splitOrigin, "Select origin work type", delegate(string defName)
            {
                splitOrigin = defName;
                splitDraft.Clear();
                splitOriginScroll = 0f;
                splitDraftScroll = 0f;
            });

            Rect nameRow = list.GetRect(26f);
            Widgets.Label(new Rect(nameRow.x, nameRow.y, 110f, 24f), "New name");
            splitName = Widgets.TextField(new Rect(nameRow.x + 114f, nameRow.y, Mathf.Max(60f, nameRow.width - 114f), 24f), splitName ?? "");
            Rect descRow = list.GetRect(26f);
            Widgets.Label(new Rect(descRow.x, descRow.y, 110f, 24f), "Description");
            splitDescription = Widgets.TextField(new Rect(descRow.x + 114f, descRow.y, Mathf.Max(60f, descRow.width - 114f), 24f), splitDescription ?? "");
            if (!string.IsNullOrWhiteSpace(splitName))
            {
                list.Label("defName: " + BuildSplitDefName(splitName));
            }

            list.Gap(4f);
            List<WorkGiverDef> originGivers = GiversForWorkType(splitOrigin);
            List<string> originNames = originGivers.Select(g => g.defName).ToList();
            HashSet<string> draftSet = new HashSet<string>(splitDraft);

            Rect headerRect = list.GetRect(20f);
            float columnWidth = (headerRect.width - 12f) * 0.5f;
            Widgets.Label(new Rect(headerRect.x, headerRect.y, columnWidth, 20f),
                "Origin givers (" + originNames.Count + ")");
            Widgets.Label(new Rect(headerRect.x + columnWidth + 12f, headerRect.y, columnWidth, 20f),
                "Split destination (" + splitDraft.Count + ")");

            Rect columnsRect = list.GetRect(SplitMergeColumnHeight);
            Rect left = new Rect(columnsRect.x, columnsRect.y, columnWidth, SplitMergeColumnHeight);
            Rect right = new Rect(columnsRect.x + columnWidth + 12f, columnsRect.y, columnWidth, SplitMergeColumnHeight);
            smOriginArea = left;
            smDestArea = right;

            DrawGiverColumn(left, originNames, draftSet, true, false, ref splitOriginScroll, "(in split)", 1);
            DrawGiverColumn(right, splitDraft, draftSet, false, false, ref splitDraftScroll, null, 2);

            string warning = SplitValidationWarning();
            DrawSplitMergeActions(list, "Add split", warning,
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

        private void DrawMergeEditor(Listing_Standard list)
        {
            EnsureSplitMergeDefaults();

            Rect selectorRect = list.GetRect(28f);
            float selectorHalf = (selectorRect.width - 8f) * 0.5f;
            DrawWorkTypeSelector(new Rect(selectorRect.x, selectorRect.y, selectorHalf, 28f),
                "Origin: ", mergeOrigin, "Select origin work type", delegate(string defName)
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
                "Target: ", mergeTarget, "Select merge target", delegate(string defName)
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
                "Origin givers (" + originNames.Count + ")");
            Widgets.Label(new Rect(headerRect.x + columnWidth + 12f, headerRect.y, columnWidth, 20f),
                "Destination (" + existingTarget.Count + " fixed + " + mergeDraft.Count + " incoming)");

            Rect columnsRect = list.GetRect(SplitMergeColumnHeight);
            Rect left = new Rect(columnsRect.x, columnsRect.y, columnWidth, SplitMergeColumnHeight);
            Rect right = new Rect(columnsRect.x + columnWidth + 12f, columnsRect.y, columnWidth, SplitMergeColumnHeight);
            smOriginArea = left;
            smDestArea = right;
            smMergeDropIndex = -1;

            DrawGiverColumn(left, originNames, draftSet, true, true, ref mergeOriginScroll, "(in merge)", 1);
            DrawMergeDestinationColumn(right, existingTarget, mergeDraft, ref mergeTargetScroll);

            if (smDragSide == 2 && smDragMergeMode && smDragMoved && smMergeDropIndex >= 0)
            {
                list.Label("Drop \"" + smDragGiver + "\" at destination position " + (existingTarget.Count + smMergeDropIndex + 1) + ".");
            }

            Rect actionsRect = list.GetRect(30f);
            float buttonWidth = Mathf.Min(150f, (actionsRect.width - 12f) / 3f);
            Rect selectedRect = new Rect(actionsRect.x, actionsRect.y, buttonWidth, 28f);
            Rect allRect = new Rect(selectedRect.xMax + 6f, actionsRect.y, buttonWidth, 28f);
            Rect cancelRect = new Rect(allRect.xMax + 6f, actionsRect.y, buttonWidth, 28f);

            string selectedWarning = MergeValidationWarning(false);
            string allWarning = MergeValidationWarning(true);
            if (DrawActionButton(selectedRect, "Merge selected", string.IsNullOrEmpty(selectedWarning)))
            {
                SaveMerge(mergeDraft);
            }
            if (DrawActionButton(allRect, "Merge all", string.IsNullOrEmpty(allWarning)))
            {
                SaveMerge(originNames);
            }
            if (DrawActionButton(cancelRect, "Cancel draft", mergeDirty))
            {
                mergeDraft.Clear();
                mergeTargetScroll = 0f;
                mergeDirty = false;
            }
            if (!string.IsNullOrEmpty(selectedWarning))
            {
                DrawInlineWarning(list, "Merge selected: " + selectedWarning);
            }
            if (!string.IsNullOrEmpty(allWarning))
            {
                DrawInlineWarning(list, "Merge all: " + allWarning);
            }

            HandleSplitMergeDrag();
        }

        private void DrawGiverColumn(
            Rect area,
            List<string> names,
            HashSet<string> draftNames,
            bool originSide,
            bool mergeMode,
            ref float scroll,
            string draftMarker,
            int columnId)
        {
            Widgets.DrawBoxSolid(area, new Color(0.10f, 0.10f, 0.12f, 0.45f));

            bool hasRows = names != null && names.Count > 0;
            float maxScroll = hasRows
                ? Mathf.Max(0f, names.Count * SplitMergeRowHeight - area.height)
                : 0f;
            float contentWidth = area.width - (maxScroll > 0f ? SplitMergeScrollbarWidth + 2f : 0f);
            Rect track = new Rect(area.xMax - SplitMergeScrollbarWidth, area.y, SplitMergeScrollbarWidth, area.height);

            HandleColumnWheel(area, track, maxScroll, ref scroll);
            HandleColumnScrollbarInput(columnId, track, area.height, maxScroll, ref scroll);
            ClampColumnScroll(maxScroll, ref scroll);

            if (!hasRows)
            {
                Widgets.Label(
                    new Rect(area.x + 6f, area.y + 4f, contentWidth - 12f, area.height - 8f),
                    originSide ? "No givers on this work type." : "Drag origin givers here.");
                DrawColumnScrollbar(track, area.height, maxScroll, scroll, columnId);
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
                bool inDraft = originSide && draftNames.Contains(name);
                bool dragged = smDragSide != 0
                    && smDragMergeMode == mergeMode
                    && smDragGiver == name
                    && smDragSide == (originSide ? 1 : 2);
                bool dropTarget = IsValidDropHover(originSide, mergeMode, row);

                DrawSplitMergeRow(row, name, inDraft, dragged, dropTarget, inDraft ? draftMarker : null);

                if (!dragged && smDragSide == 0)
                {
                    TooltipHandler.TipRegion(row, GiverTooltip(DefDatabase<WorkGiverDef>.GetNamedSilentFail(name)));
                }

                if (current.type == EventType.MouseDown
                    && current.button == 0
                    && row.Contains(current.mousePosition))
                {
                    smDragSide = originSide ? 1 : 2;
                    smDragMergeMode = mergeMode;
                    smDragGiver = name;
                    smDragStartY = current.mousePosition.y;
                    smDragMoved = false;
                    current.Use();
                }
            }

            DrawColumnScrollbar(track, area.height, maxScroll, scroll, columnId);
        }

        // Merge destination column: existing target givers are drawn first and are fixed; the
        // incoming origin givers follow and can be dragged/reordered. Their resulting order is
        // saved as MergeEntry.targetOrder and applied after workType assignment on restart.
        private void DrawMergeDestinationColumn(Rect area, List<string> existingNames, List<string> incomingNames, ref float scroll)
        {
            Widgets.DrawBoxSolid(area, new Color(0.10f, 0.10f, 0.12f, 0.45f));

            int fixedCount = existingNames != null ? existingNames.Count : 0;
            int incomingCount = incomingNames != null ? incomingNames.Count : 0;
            int total = fixedCount + incomingCount;
            float maxScroll = total > 0
                ? Mathf.Max(0f, total * SplitMergeRowHeight - area.height)
                : 0f;
            float contentWidth = area.width - (maxScroll > 0f ? SplitMergeScrollbarWidth + 2f : 0f);
            Rect track = new Rect(area.xMax - SplitMergeScrollbarWidth, area.y, SplitMergeScrollbarWidth, area.height);

            HandleColumnWheel(area, track, maxScroll, ref scroll);
            HandleColumnScrollbarInput(2, track, area.height, maxScroll, ref scroll);
            ClampColumnScroll(maxScroll, ref scroll);

            if (total == 0)
            {
                Widgets.Label(
                    new Rect(area.x + 6f, area.y + 4f, contentWidth - 12f, area.height - 8f),
                    "Select a target, then drag origin givers here.");
                DrawColumnScrollbar(track, area.height, maxScroll, scroll, 2);
                return;
            }

            Event current = Event.current;
            for (int i = 0; i < total; i++)
            {
                float y = area.y + i * SplitMergeRowHeight - scroll;
                if (y + SplitMergeRowHeight <= area.y || y >= area.yMax)
                {
                    continue;
                }

                bool isFixed = i < fixedCount;
                string name = isFixed ? existingNames[i] : incomingNames[i - fixedCount];
                Rect row = new Rect(area.x + 3f, y, contentWidth - 6f, SplitMergeRowHeight - 2f);
                bool dragged = !isFixed
                    && smDragSide == 2
                    && smDragMergeMode
                    && smDragGiver == name;
                bool dropTarget = !isFixed
                    && smDragSide != 0
                    && smDragMergeMode
                    && smDragMoved
                    && row.Contains(current.mousePosition);

                if (isFixed)
                {
                    DrawSplitMergeRow(row, (i + 1) + ". " + name, true, false, false, "(fixed)", false);
                }
                else
                {
                    int position = fixedCount + (i - fixedCount) + 1;
                    DrawSplitMergeRow(row, position + ". " + name, false, dragged, dropTarget, null);
                }

                if (!dragged && smDragSide == 0)
                {
                    TooltipHandler.TipRegion(row, GiverTooltip(DefDatabase<WorkGiverDef>.GetNamedSilentFail(name)));
                }

                if (!isFixed
                    && current.type == EventType.MouseDown
                    && current.button == 0
                    && row.Contains(current.mousePosition))
                {
                    smDragSide = 2;
                    smDragMergeMode = true;
                    smDragGiver = name;
                    smDragStartY = current.mousePosition.y;
                    smDragMoved = false;
                    current.Use();
                }
            }

            // Work out where a dragged merge row would be inserted, including the empty area of
            // the column. A drop above all incoming rows inserts at 0; below them appends.
            if ((smDragSide == 1 || smDragSide == 2) && smDragMergeMode && smDragMoved && Mouse.IsOver(area))
            {
                int hovered = -1;
                for (int j = 0; j < incomingCount; j++)
                {
                    float y = area.y + (fixedCount + j) * SplitMergeRowHeight - scroll;
                    Rect row = new Rect(area.x + 3f, y, contentWidth - 6f, SplitMergeRowHeight - 2f);
                    if (row.Contains(current.mousePosition))
                    {
                        hovered = j;
                        break;
                    }
                }
                if (hovered >= 0)
                {
                    smMergeDropIndex = hovered;
                }
                else
                {
                    float incomingTop = area.y + fixedCount * SplitMergeRowHeight - scroll;
                    smMergeDropIndex = current.mousePosition.y < incomingTop ? 0 : incomingCount;
                }
            }

            DrawColumnScrollbar(track, area.height, maxScroll, scroll, 2);
        }

        private static void HandleColumnWheel(Rect area, Rect track, float maxScroll, ref float scroll)
        {
            if (maxScroll <= 0f)
            {
                return;
            }

            Event current = Event.current;
            if (current.type != EventType.ScrollWheel)
            {
                return;
            }

            bool ctrl = current.control || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool overTrack = track.Contains(current.mousePosition);
            bool ctrlOverColumn = ctrl && Mouse.IsOver(area);
            if (!overTrack && !ctrlOverColumn)
            {
                // Not ours: leave the event alone so the outer settings scroll handles the wheel.
                return;
            }

            float delta = current.delta.y;
            if (Mathf.Abs(delta) > 0.001f)
            {
                scroll += delta > 0f ? -SplitMergeRowHeight : SplitMergeRowHeight;
            }
            current.Use();
        }

        private void HandleColumnScrollbarInput(int columnId, Rect track, float visibleHeight, float maxScroll, ref float scroll)
        {
            if (maxScroll <= 0f)
            {
                if (smScrollDragColumn == columnId)
                {
                    smScrollDragColumn = 0;
                }
                return;
            }

            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && track.Contains(current.mousePosition))
            {
                smScrollDragColumn = columnId;
                UpdateColumnScrollFromMouse(track, visibleHeight, maxScroll, current.mousePosition.y, ref scroll);
                current.Use();
            }
            else if (smScrollDragColumn == columnId && current.type == EventType.MouseDrag)
            {
                UpdateColumnScrollFromMouse(track, visibleHeight, maxScroll, current.mousePosition.y, ref scroll);
                current.Use();
            }
            else if (smScrollDragColumn == columnId && current.type == EventType.MouseUp)
            {
                smScrollDragColumn = 0;
                current.Use();
            }
        }

        private static void UpdateColumnScrollFromMouse(Rect track, float visibleHeight, float maxScroll, float mouseY, ref float scroll)
        {
            float contentHeight = visibleHeight + maxScroll;
            float thumbHeight = Mathf.Clamp(track.height * (visibleHeight / contentHeight), 20f, track.height);
            float travel = track.height - thumbHeight;
            float t = travel <= 0f ? 0f : (mouseY - track.y - thumbHeight * 0.5f) / travel;
            scroll = Mathf.Clamp01(t) * maxScroll;
        }

        private static void ClampColumnScroll(float maxScroll, ref float scroll)
        {
            // Keep scroll on whole rows so no row is ever drawn half-clipped.
            scroll = Mathf.Clamp(scroll, 0f, maxScroll);
            scroll = Mathf.Floor(scroll / SplitMergeRowHeight) * SplitMergeRowHeight;
            scroll = Mathf.Clamp(scroll, 0f, maxScroll);
        }

        private void DrawColumnScrollbar(Rect track, float visibleHeight, float maxScroll, float scroll, int columnId)
        {
            if (maxScroll <= 0f)
            {
                return;
            }

            Widgets.DrawBoxSolid(track, new Color(0.05f, 0.05f, 0.06f, 0.80f));
            float contentHeight = visibleHeight + maxScroll;
            float thumbHeight = Mathf.Clamp(track.height * (visibleHeight / contentHeight), 20f, track.height);
            float travel = track.height - thumbHeight;
            float t = Mathf.Clamp01(scroll / maxScroll);
            Rect thumb = new Rect(track.x + 1f, track.y + travel * t, track.width - 2f, thumbHeight);
            bool active = smScrollDragColumn == columnId;
            Color color = active || Mouse.IsOver(thumb)
                ? new Color(0.72f, 0.80f, 0.92f, 1f)
                : new Color(0.48f, 0.53f, 0.60f, 0.95f);
            Widgets.DrawBoxSolid(thumb, color);
            TooltipHandler.TipRegion(track, "Scrollbar: drag it, or Ctrl+wheel over this column. A plain wheel scrolls the settings.");
        }

        private void DrawSplitMergeRow(Rect row, string giverName, bool inDraft, bool dragged, bool dropTarget, string draftMarker, bool showHandle = true)
        {
            if (dragged)
            {
                Widgets.DrawBoxSolid(row, new Color(0.22f, 0.42f, 0.62f, 0.60f));
            }
            else if (dropTarget)
            {
                // Blue/green drop tint: reads as "accepts this drag" against the dark list.
                Widgets.DrawBoxSolid(row, new Color(0.28f, 0.74f, 0.58f, 0.45f));
            }
            else if (Mouse.IsOver(row))
            {
                Widgets.DrawHighlight(row);
            }
            else if (inDraft)
            {
                Widgets.DrawBoxSolid(row, new Color(0.30f, 0.30f, 0.30f, 0.30f));
            }

            Color previous = GUI.color;
            if (inDraft && !dragged)
            {
                GUI.color = new Color(0.62f, 0.62f, 0.62f, 0.90f);
            }

            string text = string.IsNullOrEmpty(draftMarker) ? giverName : giverName + "  " + draftMarker;
            Widgets.LabelEllipses(new Rect(row.x + 4f, row.y, row.width - 26f, row.height), text);
            GUI.color = previous;
            if (showHandle)
            {
                Widgets.Label(new Rect(row.xMax - 20f, row.y, 16f, row.height), "≡");
            }
        }

        private bool IsValidDropHover(bool originSide, bool mergeMode, Rect row)
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

        private static bool DrawActionButton(Rect rect, string label, bool enabled)
        {
            if (enabled)
            {
                return Widgets.ButtonText(rect, label);
            }

            Widgets.DrawBoxSolid(rect, new Color(0.22f, 0.22f, 0.22f, 0.55f));
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.40f);
            TextAnchor previousAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, label);
            Text.Anchor = previousAnchor;
            GUI.color = previous;
            return false;
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
            string fallback = Settings.workTypeOrder.FirstOrDefault() ?? "";
            WorkTypeDef selectedGiverDef = DefDatabase<WorkTypeDef>.GetNamedSilentFail(selectedGiverType);
            if (selectedGiverDef == null || !selectedGiverDef.visible)
            {
                selectedGiverType = "";
            }
            WorkTypeDef editDef = DefDatabase<WorkTypeDef>.GetNamedSilentFail(editSplitDefName);
            if (editDef == null || !editDef.visible)
            {
                editSplitDefName = "";
                editSplitName = "";
                editSplitDescription = "";
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
                confirmResetOrder = false;
            }
            splitDraft.RemoveAll(g => !GiverBelongsTo(g, splitOrigin));
            mergeDraft.RemoveAll(g => !GiverBelongsTo(g, mergeOrigin));
        }

        private void DrawWorkTypeSelector(Rect rect, string prefix, string selectedDefName, string title, Action<string> onSelect, string excludeDefName = null)
        {
            WorkTypeDef selected = DefDatabase<WorkTypeDef>.GetNamedSilentFail(selectedDefName);
            string label = selected != null ? WorkTypePaletteView.DisplayName(selected) : "(select)";
            if (!Widgets.ButtonText(rect, prefix + label + "  ▼"))
            {
                return;
            }

            List<WorkTypeDef> options = DefDatabase<WorkTypeDef>.AllDefsListForReading
                .Where(w => w != null && w.visible && (string.IsNullOrEmpty(excludeDefName) || w.defName != excludeDefName))
                .OrderByDescending(w => w.naturalPriority)
                .ToList();

            Find.WindowStack.Add(new WorkTypeDropdownWindow(
                title,
                options,
                selected,
                delegate(WorkTypeDef wt)
                {
                    if (wt != null && onSelect != null)
                    {
                        onSelect(wt.defName);
                    }
                }));
        }

        private void SaveSplit()
        {
            if (!string.IsNullOrEmpty(SplitValidationWarning()))
            {
                return;
            }

            string newDefName = BuildSplitDefName(splitName);
            Settings.splits.Add(new SplitEntry
            {
                newDefName = newDefName,
                label = splitName.Trim(),
                description = splitDescription.Trim(),
                baseWorkType = splitOrigin,
                givers = splitDraft.Distinct().ToList()
            });
            if (!Settings.workTypeOrder.Contains(newDefName))
            {
                Settings.workTypeOrder.Add(newDefName);
            }
            OrderedWorkStartup.ApplyPendingChanges(Settings, this);
            PersistLivePending();
            ApplyLiveProfileFromSettings();

            splitName = "";
            splitDescription = "";
            splitDraft.Clear();
            splitDraftScroll = 0f;
            splitDirty = false;
        }

        private void SaveMerge(List<string> givers)
        {
            if (givers == null || givers.Count == 0)
            {
                return;
            }
            if (DefDatabase<WorkTypeDef>.GetNamedSilentFail(mergeTarget) == null || mergeTarget == mergeOrigin)
            {
                return;
            }

            List<string> incoming = givers.Distinct().ToList();
            List<string> targetOrder = new List<string>();
            foreach (WorkGiverDef existing in GiversForWorkType(mergeTarget))
            {
                if (existing != null && !targetOrder.Contains(existing.defName))
                {
                    targetOrder.Add(existing.defName);
                }
            }
            foreach (string giverName in incoming)
            {
                if (!targetOrder.Contains(giverName))
                {
                    targetOrder.Add(giverName);
                }
            }

            List<GiverOrigin> origins = new List<GiverOrigin>();
            foreach (string giverName in incoming)
            {
                WorkGiverDef giver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(giverName);
                if (giver != null && giver.workType != null)
                {
                    origins.Add(new GiverOrigin
                    {
                        giver = giverName,
                        workType = giver.workType.defName
                    });
                }
            }
            Settings.merges.Add(new MergeEntry
            {
                givers = incoming,
                origins = origins,
                targetWorkType = mergeTarget,
                targetOrder = targetOrder
            });
            OrderedWorkStartup.ApplyPendingChanges(Settings, this);
            PersistLivePending();
            ApplyLiveProfileFromSettings();

            mergeDraft.Clear();
            mergeTargetScroll = 0f;
            mergeDirty = false;
        }

        private string BuildSplitDefName(string label)
        {
            string baseName = Sanitize(label ?? "");
            if (string.IsNullOrEmpty(baseName))
            {
                baseName = "Split";
            }

            int suffix = Settings.splits.Count + 1;
            string defName = "OrderedWork_" + baseName + "_" + suffix;
            while (Settings.splits.Any(s => s != null && s.newDefName == defName)
                || DefDatabase<WorkTypeDef>.GetNamedSilentFail(defName) != null)
            {
                suffix++;
                defName = "OrderedWork_" + baseName + "_" + suffix;
            }
            return defName;
        }

        private static bool GiverBelongsTo(string giverName, string workTypeDefName)
        {
            if (string.IsNullOrEmpty(giverName) || string.IsNullOrEmpty(workTypeDefName))
            {
                return false;
            }
            WorkGiverDef giver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(giverName);
            return giver != null && giver.workType != null && giver.workType.defName == workTypeDefName;
        }

        private static List<WorkGiverDef> GiversForWorkType(string workTypeDefName)
        {
            if (string.IsNullOrEmpty(workTypeDefName))
            {
                return new List<WorkGiverDef>();
            }
            return DefDatabase<WorkGiverDef>.AllDefsListForReading
                .Where(g => g != null && g.workType != null && g.workType.defName == workTypeDefName)
                .OrderByDescending(g => g.priorityInType)
                .ToList();
        }

    }
}
