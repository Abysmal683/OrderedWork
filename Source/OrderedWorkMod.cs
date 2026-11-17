using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OrderedWork.Core;
using OrderedWork.Services;
using OrderedWork.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork
{
    public class OrderedWorkMod : Mod
    {
        public static OrderedWorkSettings Settings;

        private const int TabRules = 0;
        private const int TabGeneral = 1;

        // Split/Merge editor geometry: rows are whole multiples of the row height, so a
        // column never draws a partially clipped row and needs no nested clipping group.
        private const float SplitMergeRowHeight = 24f;
        private const int SplitMergeVisibleRows = 10;
        private const float SplitMergeColumnHeight = SplitMergeRowHeight * SplitMergeVisibleRows;
        private const float SplitMergeScrollbarWidth = 6f;

        // Split/Merge/Order editor modes are UI-only.
        private const int EditorSplit = 0;
        private const int EditorMerge = 1;
        private const int EditorOrder = 2;

        private Vector2 scroll;

        // Measured from Listing_Standard.CurHeight after each draw and reused next frame so
        // the outer settings scroll is only as tall as the current tab content.
        private float settingsContentHeight;

        // Tab selection is intentionally UI-only; it is not scribed to settings.
        private int settingsTab = TabRules;
        private int settingsEditorMode = EditorSplit;

        // Order mode origin. Kept as UI-only state; the saved order itself lives in settings.
        private string selectedGiverType = "";
        private string editSplitDefName = "";
        private string editSplitName = "";
        private string editSplitDescription = "";

        // Split/Merge drafts are UI-only and never scribed.
        private string splitOrigin = "";
        private string splitName = "";
        private string splitDescription = "";
        private List<string> splitDraft = new List<string>();
        private float splitOriginScroll;
        private float splitDraftScroll;

        private string mergeOrigin = "";
        private string mergeTarget = "";
        private List<string> mergeDraft = new List<string>();
        private float mergeOriginScroll;
        private float mergeTargetScroll;

        // 0 = nothing, 1 = from the origin column, 2 = from the destination column.
        private int smDragSide;
        private bool smDragMergeMode;
        private string smDragGiver;
        private float smDragStartY;
        private bool smDragMoved;
        private Rect smOriginArea;
        private Rect smDestArea;

        // Per-column scrollbar drag owner (0 = none, 1 = origin column, 2 = destination).
        private int smScrollDragColumn;
        // Merge destination reorder preview: insertion index into mergeDraft, -1 = none.
        private int smMergeDropIndex = -1;

        private int dragKind;
        private int dragIndex = -1;
        private float dragStartMouseY;
        private bool dragMoved;
        private bool confirmResetAll;
        private bool confirmResetOrder;
        private bool splitAllowEmptyOrigin;
        private bool mergeAllowEmptyOrigin;
        private int confirmResetSplitIndex = -1;
        private int confirmResetMergeIndex = -1;

        public OrderedWorkMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<OrderedWorkSettings>();
        }

        public override string SettingsCategory()
        {
            return "Ordered Work";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            EnsureWorkTypeOrder();

            Rect tabsRect = new Rect(inRect.x, inRect.y, inRect.width, 28f);
            DrawSettingsTabs(tabsRect);

            Rect body = new Rect(
                inRect.x,
                tabsRect.yMax + 4f,
                inRect.width,
                Mathf.Max(40f, inRect.height - tabsRect.height - 4f));

            // Use the height measured after last frame's draw. Zero means "not measured yet",
            // so fall back to the body height and let this frame measure the real content.
            float contentHeight = Mathf.Max(body.height, settingsContentHeight);
            Rect viewRect = new Rect(0f, 0f, body.width - 24f, contentHeight);
            Widgets.BeginScrollView(body, ref scroll, viewRect);
            Listing_Standard list = new Listing_Standard();
            // Listing_Standard otherwise starts a second column when content exceeds the first
            // frame's provisional view height, which hid the rest of the tab off-screen.
            list.maxOneColumn = true;
            list.Begin(viewRect);

            if (settingsTab == TabRules)
            {
                DrawRulesTab(list);
            }
            else
            {
                DrawGeneralTab(list);
            }

            list.End();
            settingsContentHeight = list.CurHeight + 8f;
            Widgets.EndScrollView();
        }

        private void DrawSettingsTabs(Rect rect)
        {
            float width = Mathf.Min(180f, (rect.width - 8f) * 0.5f);
            Rect rulesRect = new Rect(rect.x, rect.y, width, 28f);
            Rect generalRect = new Rect(rulesRect.xMax + 6f, rect.y, width, 28f);

            if (Widgets.ButtonText(rulesRect, "Rules"))
            {
                if (settingsTab != TabRules)
                {
                    settingsTab = TabRules;
                    ResetEditorUiState();
                }
            }
            if (Widgets.ButtonText(generalRect, "General"))
            {
                if (settingsTab != TabGeneral)
                {
                    settingsTab = TabGeneral;
                    ResetEditorUiState();
                }
            }

            DrawTabMarker(settingsTab == TabRules ? rulesRect : generalRect);
        }

        private static void DrawTabMarker(Rect rect)
        {
            Widgets.DrawBoxSolid(
                new Rect(rect.x, rect.yMax - 3f, rect.width, 3f),
                new Color(0.45f, 0.72f, 0.95f, 0.95f));
        }

        private void DrawRulesTab(Listing_Standard list)
        {
            EnsureSplitMergeDefaults();

            list.Label("Ordered Work (Core) - changes apply on restart.");
            list.Label("Split copies work tags, skills, life stages and mech availability.");
            list.Gap(6f);

            list.Label("Work type order (top = first). Drag rows to reorder:");
            List<Rect> workRows = new List<Rect>();
            for (int i = 0; i < Settings.workTypeOrder.Count; i++)
            {
                string defName = Settings.workTypeOrder[i];
                WorkTypeDef wt = DefDatabase<WorkTypeDef>.GetNamedSilentFail(defName);
                string label = wt != null ? wt.labelShort : defName;
                Rect row = list.GetRect(24f);
                DrawReorderRow(row, i + 1 + ". " + label, 1, i, WorkTypeTooltip(defName));
                workRows.Add(row);
            }
            HandleReorderDrag(1, workRows, MoveWorkType);

            list.Gap(14f);
            DrawSplitMergeEditor(list);

            list.Gap(10f);
            list.Label("No work type or giver is ever deleted. Back up your save before applying.");
            list.Label("Split/Merge changes take effect after a restart.");
        }

        private void DrawGeneralTab(Listing_Standard list)
        {
            list.Label("Ordered Work (Core) - changes apply on restart.");
            list.Gap(6f);

            list.CheckboxLabeled(
                "Auto recommend new colony",
                ref Settings.autoRecommendNewColony,
                "On: new/default colonies and new pawns get a skill recommendation applied. Off: copy the current vanilla order instead; the top Recommend button still works.");
            list.CheckboxLabeled(
                "Auto-hide scrollbars",
                ref Settings.autoHideScrollbars,
                "Show Work Control scrollbars only while hovering or scrolling, so the matrix keeps its space.");
            list.CheckboxLabeled(
                "Palette stress test (debug)",
                ref Settings.paletteStressTest,
                "Adds 60 visual-only WorkTypes to test the palette's own horizontal scrollbar. They cannot be dragged or assigned.");

            list.Gap(12f);
            list.Label("Technical spike: writes dense rank 1..N to free colonists and logs the result.");
            if (Find.CurrentMap == null)
            {
                list.Label("Load a colony to run the dense-rank test.");
            }
            else if (list.ButtonText("Test dense rank on colonists (log)"))
            {
                ApplyTestDenseRank();
            }

            list.Gap(12f);
            list.Label("Danger zone: reset the mod configuration without touching pawn 1..N priorities.");
            if (!confirmResetAll)
            {
                if (list.ButtonText("Reset config mod all"))
                {
                    confirmResetAll = true;
                }
            }
            else
            {
                list.Label("This clears orders, splits, merges, colors, stress test and panel height, plus any unsaved drafts. Pawn work priorities are not touched.");
                if (list.ButtonText("Confirm reset config mod all"))
                {
                    ResetAllSettings();
                    confirmResetAll = false;
                }
                if (list.ButtonText("Cancel reset"))
                {
                    confirmResetAll = false;
                }
            }

            list.Gap(10f);
            list.Label("No work type or giver is ever deleted. Back up your save before applying.");
        }

        // ---------------------------------------------------------------------
        // Split / Merge editor
        // ---------------------------------------------------------------------

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
            list.CheckboxLabeled(
                "Allow empty origin",
                ref splitAllowEmptyOrigin,
                "If the split takes every origin giver, the origin may be left empty. This is the explicit confirmation.");

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
                });

            DrawSavedSplits(list);
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
            list.CheckboxLabeled(
                "Allow empty origin",
                ref mergeAllowEmptyOrigin,
                "Merge all/selected may empty the origin. This is the explicit confirmation; vanilla origins are hidden rather than deleted.");

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
            if (Widgets.ButtonText(cancelRect, "Cancel draft"))
            {
                mergeDraft.Clear();
                mergeTargetScroll = 0f;
            }
            if (!string.IsNullOrEmpty(selectedWarning))
            {
                DrawInlineWarning(list, "Merge selected: " + selectedWarning);
            }
            if (!string.IsNullOrEmpty(allWarning))
            {
                DrawInlineWarning(list, "Merge all: " + allWarning);
            }

            DrawSavedMerges(list);
            HandleSplitMergeDrag();
        }

        private void DrawOrderEditor(Listing_Standard list)
        {
            EnsureSplitMergeDefaults();

            list.Label("Order mode reorders the WorkGivers of one work type. Top = first.");
            Rect selectorRect = list.GetRect(28f);
            DrawWorkTypeSelector(selectorRect, "Work type: ", selectedGiverType, "Select Work type", delegate(string defName)
            {
                selectedGiverType = defName;
                confirmResetOrder = false;
            });

            List<WorkGiverDef> givers = GiversForWorkType(selectedGiverType);
            if (givers.Count == 0)
            {
                list.Label("This work type has no WorkGivers.");
            }
            else
            {
                List<Rect> giverRows = new List<Rect>();
                for (int i = 0; i < givers.Count; i++)
                {
                    WorkGiverDef giver = givers[i];
                    Rect row = list.GetRect(SplitMergeRowHeight);
                    DrawReorderRow(row, (i + 1) + ". " + giver.defName, 2, i, GiverTooltip(giver));
                    giverRows.Add(row);
                }
                HandleReorderDrag(2, giverRows, delegate(int from, int to)
                {
                    WorkGiverDef item = givers[from];
                    givers.RemoveAt(from);
                    givers.Insert(Mathf.Clamp(to, 0, givers.Count), item);
                    CommitGiverOrder(selectedGiverType, givers);
                });
            }

            list.Gap(10f);
            string selectedLabel = DefDatabase<WorkTypeDef>.GetNamedSilentFail(selectedGiverType)?.labelShort ?? selectedGiverType;
            if (!confirmResetOrder)
            {
                if (list.ButtonText("Reset order"))
                {
                    confirmResetOrder = true;
                }
            }
            else
            {
                list.Label("Remove the saved giver order for " + selectedLabel + "? The original def order returns after a restart; the current session keeps its priority values.");
                if (list.ButtonText("Confirm reset order"))
                {
                    foreach (WorkGiverDef giver in GiversForWorkType(selectedGiverType))
                    {
                        GiverOrderEntry original = Settings.originalGiverOrder.FirstOrDefault(
                            e => e != null && e.giver == giver.defName);
                        if (original != null)
                        {
                            giver.priorityInType = original.order;
                        }
                    }
                    Settings.giverOrder.RemoveAll(e => e != null && e.workType == selectedGiverType);
                    OrderedWorkStartup.ApplyPendingChanges(Settings, this);
                    confirmResetOrder = false;
                }
                if (list.ButtonText("Cancel reset order"))
                {
                    confirmResetOrder = false;
                }
            }
            list.Label("Reset order does not write WorkTypes or pawn priorities now; it only removes the saved override.");
            list.Gap(12f);
            DrawSavedSplitMetadata(list);
        }

        private void DrawSavedSplitMetadata(Listing_Standard list)
        {
            list.Label("Edit saved split metadata (hot):");
            List<WorkTypeDef> splitDefs = Settings.splits
                .Where(s => s != null && !string.IsNullOrEmpty(s.newDefName))
                .Select(s => DefDatabase<WorkTypeDef>.GetNamedSilentFail(s.newDefName))
                .Where(d => d != null)
                .ToList();
            if (splitDefs.Count == 0)
            {
                list.Label("No saved splits with a live WorkType.");
                return;
            }

            WorkTypeDef selected = DefDatabase<WorkTypeDef>.GetNamedSilentFail(editSplitDefName);
            Rect selector = list.GetRect(28f);
            if (Widgets.ButtonText(selector, "Split: " + (selected != null ? selected.labelShort : "(select)") + "  ▼"))
            {
                Find.WindowStack.Add(new WorkTypeDropdownWindow(
                    "Select split",
                    splitDefs,
                    selected,
                    delegate(WorkTypeDef wt)
                    {
                        editSplitDefName = wt.defName;
                        SplitEntry entry = Settings.splits.FirstOrDefault(s => s != null && s.newDefName == wt.defName);
                        editSplitName = entry != null ? entry.label : wt.labelShort;
                        editSplitDescription = entry != null ? entry.description : wt.description;
                    }));
            }

            if (selected != null)
            {
                Rect nameRow = list.GetRect(26f);
                Widgets.Label(new Rect(nameRow.x, nameRow.y, 100f, 24f), "Name");
                editSplitName = Widgets.TextField(new Rect(nameRow.x + 104f, nameRow.y, nameRow.width - 104f, 24f), editSplitName ?? "");
                Rect descRow = list.GetRect(26f);
                Widgets.Label(new Rect(descRow.x, descRow.y, 100f, 24f), "Description");
                editSplitDescription = Widgets.TextField(new Rect(descRow.x + 104f, descRow.y, descRow.width - 104f, 24f), editSplitDescription ?? "");
                if (Widgets.ButtonText(list.GetRect(28f), "Apply split metadata") && !string.IsNullOrWhiteSpace(editSplitName))
                {
                    SplitEntry entry = Settings.splits.FirstOrDefault(s => s != null && s.newDefName == selected.defName);
                    if (entry != null)
                    {
                        entry.label = editSplitName.Trim();
                        entry.description = editSplitDescription.Trim();
                    }
                    selected.labelShort = editSplitName.Trim();
                    selected.pawnLabel = editSplitName.Trim();
                    selected.gerundLabel = editSplitName.Trim();
                    selected.description = editSplitDescription.Trim();
                    WorkOrderService.InvalidateAvailableWorkTypes();
                }
            }
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

        private void DrawSplitMergeActions(Listing_Standard list, string actionLabel, string warning, Action onSave, Action onCancel)
        {
            Rect actionsRect = list.GetRect(30f);
            float buttonWidth = Mathf.Min(150f, (actionsRect.width - 12f) / 3f);
            Rect actionRect = new Rect(actionsRect.x, actionsRect.y, buttonWidth, 28f);
            Rect cancelRect = new Rect(actionRect.xMax + 6f, actionsRect.y, buttonWidth, 28f);

            if (DrawActionButton(actionRect, actionLabel, string.IsNullOrEmpty(warning)))
            {
                onSave();
            }
            if (Widgets.ButtonText(cancelRect, "Cancel draft"))
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
            if (!splitAllowEmptyOrigin && originGivers.Count > 0 && splitDraft.Count >= originGivers.Count)
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
                if (!mergeAllowEmptyOrigin)
                {
                    return "Merge all would leave the origin empty. Enable 'Allow empty origin' to confirm.";
                }
                return null;
            }
            if (mergeDraft.Count == 0)
            {
                return "Drag at least one origin giver into Merge destination.";
            }
            if (!mergeAllowEmptyOrigin && mergeDraft.Count >= originGivers.Count)
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

        private void ResetEditorUiState()
        {
            ResetSplitMergeDrag();
            confirmResetOrder = false;
            confirmResetSplitIndex = -1;
            confirmResetMergeIndex = -1;
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
            }
        }

        private void RemoveDraftGiver(string giverName, bool mergeMode)
        {
            List<string> draft = mergeMode ? mergeDraft : splitDraft;
            draft.Remove(giverName);
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

        private void DrawSavedSplits(Listing_Standard list)
        {
            list.Gap(8f);
            list.Label("Saved splits (applied on restart):");
            if (Settings.splits.Count == 0)
            {
                list.Label("None.");
                return;
            }
            for (int i = 0; i < Settings.splits.Count; i++)
            {
                SplitEntry split = Settings.splits[i];
                if (split == null)
                {
                    continue;
                }
                string label = string.IsNullOrEmpty(split.label) ? split.newDefName : split.label;
                string givers = split.givers != null ? string.Join(", ", split.givers) : "";
                string text = "Saved: " + label + " [" + split.newDefName + "] from " + split.baseWorkType + " - " + givers;
                if (split.givers == null || split.givers.Count == 0)
                {
                    Color previous = GUI.color;
                    GUI.color = new Color(1f, 0.55f, 0.40f, 1f);
                    list.Label("WARNING: empty split. Reset it; it will not be created on restart.");
                    GUI.color = previous;
                }
                Rect row = list.GetRect(30f);
                float buttonWidth = 132f;
                Widgets.LabelEllipses(new Rect(row.x, row.y, Mathf.Max(60f, row.width - buttonWidth * 2f - 18f), 26f), text);

                if (confirmResetSplitIndex == i)
                {
                    Rect confirmRect = new Rect(row.xMax - buttonWidth * 2f - 12f, row.y, buttonWidth, 26f);
                    Rect cancelRect = new Rect(row.xMax - buttonWidth, row.y, buttonWidth, 26f);
                    if (Widgets.ButtonText(confirmRect, "Confirm reset split"))
                    {
                        if (split != null)
                        {
                            WorkTypeDef splitDef = DefDatabase<WorkTypeDef>.GetNamedSilentFail(split.newDefName);
                            WorkTypeDef baseDef = DefDatabase<WorkTypeDef>.GetNamedSilentFail(split.baseWorkType);
                            if (splitDef != null && baseDef != null && split.givers != null)
                            {
                                foreach (string giverName in split.givers)
                                {
                                    WorkGiverDef giver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(giverName);
                                    if (giver != null)
                                    {
                                        giver.workType = baseDef;
                                    }
                                }
                                splitDef.visible = false;
                            }
                            Settings.workTypeOrder.Remove(split.newDefName);
                            Settings.giverOrder.RemoveAll(e => e != null && e.workType == split.newDefName);
                            if (editSplitDefName == split.newDefName)
                            {
                                editSplitDefName = "";
                                editSplitName = "";
                                editSplitDescription = "";
                            }
                        }
                        Settings.splits.RemoveAt(i);
                        OrderedWorkStartup.ApplyPendingChanges(Settings, this);
                        confirmResetSplitIndex = -1;
                        break;
                    }
                    if (Widgets.ButtonText(cancelRect, "Cancel"))
                    {
                        confirmResetSplitIndex = -1;
                    }
                }
                else
                {
                    Rect resetRect = new Rect(row.xMax - buttonWidth, row.y, buttonWidth, 26f);
                    if (Widgets.ButtonText(resetRect, "Reset split"))
                    {
                        confirmResetSplitIndex = i;
                    }
                }
            }
            list.Label("Reset split removes the saved entry; the split work type stops being recreated after a restart. Pawns are not touched now.");
        }

        private void DrawSavedMerges(Listing_Standard list)
        {
            list.Gap(8f);
            list.Label("Saved merges (applied on restart):");
            if (Settings.merges.Count == 0)
            {
                list.Label("None.");
                return;
            }
            for (int i = 0; i < Settings.merges.Count; i++)
            {
                MergeEntry merge = Settings.merges[i];
                if (merge == null)
                {
                    continue;
                }
                string givers = merge.givers != null ? string.Join(", ", merge.givers) : "";
                string text = "Saved: [" + givers + "] -> " + merge.targetWorkType;
                Rect row = list.GetRect(30f);
                float buttonWidth = 138f;
                Widgets.LabelEllipses(new Rect(row.x, row.y, Mathf.Max(60f, row.width - buttonWidth * 2f - 18f), 26f), text);

                if (confirmResetMergeIndex == i)
                {
                    Rect confirmRect = new Rect(row.xMax - buttonWidth * 2f - 12f, row.y, buttonWidth, 26f);
                    Rect cancelRect = new Rect(row.xMax - buttonWidth, row.y, buttonWidth, 26f);
                    if (Widgets.ButtonText(confirmRect, "Confirm reset merge"))
                    {
                        if (merge != null && merge.origins != null)
                        {
                            foreach (GiverOrigin origin in merge.origins)
                            {
                                WorkGiverDef giver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(origin.giver);
                                WorkTypeDef source = DefDatabase<WorkTypeDef>.GetNamedSilentFail(origin.workType);
                                if (giver != null && source != null)
                                {
                                    giver.workType = source;
                                }
                            }
                        }
                        Settings.merges.RemoveAt(i);
                        OrderedWorkStartup.ApplyPendingChanges(Settings, this);
                        confirmResetMergeIndex = -1;
                        break;
                    }
                    if (Widgets.ButtonText(cancelRect, "Cancel"))
                    {
                        confirmResetMergeIndex = -1;
                    }
                }
                else
                {
                    Rect resetRect = new Rect(row.xMax - buttonWidth, row.y, buttonWidth, 26f);
                    if (Widgets.ButtonText(resetRect, "Reset merge"))
                    {
                        confirmResetMergeIndex = i;
                    }
                }
            }
            list.Label("Reset merge removes the saved entry; foreign givers return to their original work type after a restart. Pawns are not touched now.");
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

            splitName = "";
            splitDescription = "";
            splitDraft.Clear();
            splitDraftScroll = 0f;
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

            mergeDraft.Clear();
            mergeTargetScroll = 0f;
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
            while (Settings.splits.Any(s => s != null && s.newDefName == defName))
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

        // ---------------------------------------------------------------------
        // Shared settings helpers
        // ---------------------------------------------------------------------

        private void ResetAllSettings()
        {
            Settings.workTypeOrder = DefDatabase<WorkTypeDef>.AllDefsListForReading
                .Where(w => w != null && w.visible)
                .OrderByDescending(w => w.naturalPriority)
                .Select(w => w.defName)
                .ToList();
            Settings.giverOrder.Clear();
            Settings.splits.Clear();
            Settings.merges.Clear();
            Settings.workTypeColors.Clear();
            Settings.autoRecommendNewColony = true;
            Settings.autoHideScrollbars = true;
            Settings.paletteStressTest = false;
            Settings.workControlHeight = 0f;
            selectedGiverType = Settings.workTypeOrder.FirstOrDefault() ?? "";
            splitOrigin = "";
            splitName = "";
            splitDescription = "";
            splitDraft.Clear();
            splitOriginScroll = 0f;
            splitDraftScroll = 0f;
            mergeOrigin = "";
            mergeTarget = "";
            mergeDraft.Clear();
            mergeOriginScroll = 0f;
            mergeTargetScroll = 0f;
            ResetEditorUiState();
            dragKind = 0;
            dragIndex = -1;
            EnsureSplitMergeDefaults();
        }

        private void DrawReorderRow(Rect row, string label, int kind, int index, string tooltip)
        {
            bool dragging = dragKind == kind;
            bool dragged = dragging && dragIndex == index;
            bool dropTarget = dragging && dragIndex >= 0 && dragIndex != index && Mouse.IsOver(row);

            if (dragged)
            {
                Widgets.DrawBoxSolid(row, new Color(0.22f, 0.42f, 0.62f, 0.55f));
            }
            else if (dropTarget)
            {
                Widgets.DrawBoxSolid(row, new Color(0.55f, 0.72f, 0.35f, 0.35f));
            }
            else if (Mouse.IsOver(row))
            {
                Widgets.DrawHighlight(row);
            }

            Widgets.LabelEllipses(new Rect(row.x + 4f, row.y, row.width - 30f, row.height), label);
            Widgets.Label(new Rect(row.xMax - 24f, row.y, 20f, row.height), "≡");

            if (!dragging && !dragMoved && !string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(row, tooltip);
            }
        }

        private static string WorkTypeTooltip(string defName)
        {
            WorkTypeDef wt = DefDatabase<WorkTypeDef>.GetNamedSilentFail(defName);
            if (wt == null)
            {
                return defName;
            }
            string text = wt.labelShort + "\n\n" + (wt.description ?? "");
            List<WorkGiverDef> givers = DefDatabase<WorkGiverDef>.AllDefsListForReading
                .Where(g => g.workType == wt)
                .OrderByDescending(g => g.priorityInType)
                .ToList();
            if (givers.Count > 0)
            {
                text += "\n\nWorkGivers:";
                foreach (WorkGiverDef giver in givers)
                {
                    text += "\n- " + giver.defName;
                }
            }
            return text;
        }

        private static string GiverTooltip(WorkGiverDef giver)
        {
            if (giver == null)
            {
                return "";
            }
            string text = giver.defName;
            if (giver.workType != null)
            {
                text += "\nWork type: " + giver.workType.labelShort;
            }
            return text;
        }

        private void HandleReorderDrag(int kind, List<Rect> rows, Action<int, int> move)
        {
            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    if (rows[i].Contains(current.mousePosition))
                    {
                        dragKind = kind;
                        dragIndex = i;
                        dragStartMouseY = current.mousePosition.y;
                        dragMoved = false;
                        current.Use();
                        return;
                    }
                }
            }

            if (dragKind != kind || dragIndex < 0)
            {
                return;
            }

            if (current.type == EventType.MouseDrag)
            {
                if (!dragMoved && Mathf.Abs(current.mousePosition.y - dragStartMouseY) > 4f)
                {
                    dragMoved = true;
                }
                if (dragMoved)
                {
                    for (int i = 0; i < rows.Count; i++)
                    {
                        if (i != dragIndex && rows[i].Contains(current.mousePosition))
                        {
                            move(dragIndex, i);
                            dragIndex = i;
                            break;
                        }
                    }
                    current.Use();
                }
            }
            else if (current.type == EventType.MouseUp)
            {
                dragKind = 0;
                dragIndex = -1;
                current.Use();
            }
        }

        private void MoveWorkType(int from, int to)
        {
            List<string> order = Settings.workTypeOrder;
            if (from < 0 || from >= order.Count || to < 0 || to >= order.Count || from == to)
            {
                return;
            }
            string item = order[from];
            order.RemoveAt(from);
            order.Insert(to, item);
        }

        private void CommitGiverOrder(string workType, List<WorkGiverDef> givers)
        {
            for (int i = 0; i < givers.Count; i++)
            {
                givers[i].priorityInType = givers.Count - i;
            }

            Settings.giverOrder.RemoveAll(e => e.workType == workType);
            for (int i = 0; i < givers.Count; i++)
            {
                Settings.giverOrder.Add(new GiverOrderEntry
                {
                    workType = workType,
                    giver = givers[i].defName,
                    order = i
                });
            }
        }

        private void ApplyTestDenseRank()
        {
            try
            {
                if (Find.CurrentMap == null)
                {
                    Log.Warning("[Ordered Work] No map loaded.");
                    return;
                }

                List<string> available = DefDatabase<WorkTypeDef>.AllDefsListForReading
                    .Where(w => w.visible)
                    .Select(w => w.defName)
                    .ToList();

                List<string> order = DenseRank.NormalizeOrder(Settings.workTypeOrder, available);
                if (order.Count == 0)
                {
                    Log.Warning("[Ordered Work] No visible work types available for the dense rank test.");
                    return;
                }

                Dictionary<string, int> ranks = DenseRank.BuildRanks(order, available);
                int max = Math.Max(4, order.Count);

                FieldInfo prioritiesField = typeof(Pawn_WorkSettings).GetField(
                    "priorities",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                bool loggedDirectMode = false;

                int colonists = 0;
                foreach (Pawn pawn in Find.CurrentMap.mapPawns.FreeColonists)
                {
                    Pawn_WorkSettings ws = pawn.workSettings;
                    if (ws == null)
                    {
                        continue;
                    }

                    DefMap<WorkTypeDef, int> priorities = prioritiesField != null
                        ? prioritiesField.GetValue(ws) as DefMap<WorkTypeDef, int>
                        : null;

                    foreach (WorkTypeDef wt in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                    {
                        int rank;
                        int value = ranks.TryGetValue(wt.defName, out rank) ? rank : 0;
                        if (priorities != null)
                        {
                            priorities[wt] = value;
                        }
                        else
                        {
                            ws.SetPriority(wt, value);
                        }
                    }

                    if (priorities != null && !loggedDirectMode)
                    {
                        Log.Message("[Ordered Work] Dense rank test using direct priorities map (bypasses the SetPriority clamp).");
                        loggedDirectMode = true;
                    }

                    ws.Notify_UseWorkPrioritiesChanged();

                    WorkTypeDef firstWorkType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(order[0]);
                    WorkTypeDef lastWorkType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(order[order.Count - 1]);
                    string firstPriority = firstWorkType != null ? ws.GetPriority(firstWorkType).ToString() : "n/a";
                    string lastPriority = lastWorkType != null ? ws.GetPriority(lastWorkType).ToString() : "n/a";
                    Log.Message("[Ordered Work] Dense rank test " + pawn.LabelShort + ": max=" + max + ", count=" + order.Count + ", first=" + order[0] + "=" + firstPriority + ", last=" + order[order.Count - 1] + "=" + lastPriority);
                    colonists++;
                }

                Log.Message("[Ordered Work] Dense rank test applied to " + colonists + " colonist(s).");
            }
            catch (Exception ex)
            {
                Log.Error("[Ordered Work] Dense rank test failed: " + ex);
            }
        }

        private void EnsureWorkTypeOrder()
        {
            if (Settings.workTypeOrder == null)
            {
                Settings.workTypeOrder = new List<string>();
            }
            // Drop stale/hidden split defs and empty saved splits from the visible order.
            HashSet<string> validSplitNames = new HashSet<string>(
                Settings.splits
                    .Where(s => s != null && !string.IsNullOrEmpty(s.newDefName)
                        && s.givers != null && s.givers.Count > 0)
                    .Select(s => s.newDefName));
            for (int i = Settings.workTypeOrder.Count - 1; i >= 0; i--)
            {
                string id = Settings.workTypeOrder[i];
                WorkTypeDef def = DefDatabase<WorkTypeDef>.GetNamedSilentFail(id);
                bool staleSplit = !string.IsNullOrEmpty(id)
                    && id.StartsWith("OrderedWork_", StringComparison.Ordinal)
                    && (!validSplitNames.Contains(id)
                        || GiversForWorkType(id).Count == 0);
                if (def == null || !def.visible || staleSplit)
                {
                    if (staleSplit && def != null)
                    {
                        def.visible = false;
                    }
                    Settings.workTypeOrder.RemoveAt(i);
                }
            }

            if (Settings.workTypeOrder.Count == 0)
            {
                Settings.workTypeOrder = DefDatabase<WorkTypeDef>.AllDefsListForReading
                    .Where(w => w != null && w.visible)
                    .OrderByDescending(w => w.naturalPriority)
                    .Select(w => w.defName)
                    .ToList();
                return;
            }
            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading
                .Where(w => w != null && w.visible)
                .OrderByDescending(w => w.naturalPriority))
            {
                if (!Settings.workTypeOrder.Contains(workType.defName))
                {
                    Settings.workTypeOrder.Add(workType.defName);
                }
            }
        }

        private static string Sanitize(string text)
        {
            char[] chars = text.Trim().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
            return new string(chars);
        }
    }
}
