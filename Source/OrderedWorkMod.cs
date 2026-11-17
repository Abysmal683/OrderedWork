using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OrderedWork.Core;
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

        private Vector2 scroll;

        // Tab selection is intentionally UI-only; it is not scribed to settings.
        private int settingsTab = TabRules;
        private bool settingsMergeMode;

        private string selectedGiverType = "";

        // Split/Merge drafts are UI-only and never scribed.
        private string splitOrigin = "";
        private string splitName = "";
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

        private int dragKind;
        private int dragIndex = -1;
        private float dragStartMouseY;
        private bool dragMoved;
        private bool confirmResetAll;

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

            Rect viewRect = new Rect(0f, 0f, body.width - 24f, 4000f);
            Widgets.BeginScrollView(body, ref scroll, viewRect);
            Listing_Standard list = new Listing_Standard();
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
                    ResetSplitMergeDrag();
                }
            }
            if (Widgets.ButtonText(generalRect, "General"))
            {
                if (settingsTab != TabGeneral)
                {
                    settingsTab = TabGeneral;
                    ResetSplitMergeDrag();
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

            list.Gap(10f);
            list.Label("Giver order (top = first) for one work type:");
            if (string.IsNullOrEmpty(selectedGiverType) || DefDatabase<WorkTypeDef>.GetNamedSilentFail(selectedGiverType) == null)
            {
                selectedGiverType = Settings.workTypeOrder.FirstOrDefault() ?? "";
            }
            string selectedLabel = DefDatabase<WorkTypeDef>.GetNamedSilentFail(selectedGiverType)?.labelShort ?? selectedGiverType;
            Rect selectorRect = list.GetRect(28f);
            if (Widgets.ButtonText(selectorRect, "Work type: " + selectedLabel + "  ▼"))
            {
                List<WorkTypeDef> options = new List<WorkTypeDef>();
                foreach (string id in Settings.workTypeOrder)
                {
                    WorkTypeDef wt = DefDatabase<WorkTypeDef>.GetNamedSilentFail(id);
                    if (wt != null)
                    {
                        options.Add(wt);
                    }
                }
                Find.WindowStack.Add(new WorkTypeDropdownWindow(
                    "Select Work type",
                    options,
                    DefDatabase<WorkTypeDef>.GetNamedSilentFail(selectedGiverType),
                    delegate(WorkTypeDef wt) { selectedGiverType = wt.defName; }));
            }
            if (!string.IsNullOrEmpty(selectedGiverType))
            {
                List<WorkGiverDef> givers = GiversForWorkType(selectedGiverType);
                List<Rect> giverRows = new List<Rect>();
                for (int i = 0; i < givers.Count; i++)
                {
                    WorkGiverDef giver = givers[i];
                    Rect row = list.GetRect(24f);
                    DrawReorderRow(row, i + 1 + ". " + giver.defName, 2, i, GiverTooltip(giver));
                    giverRows.Add(row);
                }
                HandleReorderDrag(2, giverRows, (from, to) =>
                {
                    WorkGiverDef item = givers[from];
                    givers.RemoveAt(from);
                    givers.Insert(Mathf.Clamp(to, 0, givers.Count), item);
                    CommitGiverOrder(selectedGiverType, givers);
                });
            }

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
            list.Label("Split / Merge editor. Drafts are temporary; saved entries apply on restart.");

            Rect modeRect = list.GetRect(28f);
            float modeHalf = (modeRect.width - 8f) * 0.5f;
            Rect splitRect = new Rect(modeRect.x, modeRect.y, modeHalf, 28f);
            Rect mergeRect = new Rect(splitRect.xMax + 8f, modeRect.y, modeHalf, 28f);
            if (Widgets.ButtonText(splitRect, "Split mode"))
            {
                if (settingsMergeMode)
                {
                    settingsMergeMode = false;
                    ResetSplitMergeDrag();
                }
            }
            if (Widgets.ButtonText(mergeRect, "Merge mode"))
            {
                if (!settingsMergeMode)
                {
                    settingsMergeMode = true;
                    ResetSplitMergeDrag();
                }
            }
            DrawTabMarker(settingsMergeMode ? mergeRect : splitRect);
            list.Gap(4f);

            if (settingsMergeMode)
            {
                DrawMergeEditor(list);
            }
            else
            {
                DrawSplitEditor(list);
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

            DrawGiverColumn(left, originNames, draftSet, true, false, ref splitOriginScroll, "(in split)");
            DrawGiverColumn(right, splitDraft, draftSet, false, false, ref splitDraftScroll, null);

            string warning = SplitValidationWarning();
            DrawSplitMergeActions(list, "Add split", warning,
                delegate
                {
                    SaveSplit();
                },
                delegate
                {
                    splitName = "";
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
                },
                mergeOrigin);

            list.Gap(4f);
            List<WorkGiverDef> originGivers = GiversForWorkType(mergeOrigin);
            List<string> originNames = originGivers.Select(g => g.defName).ToList();
            HashSet<string> draftSet = new HashSet<string>(mergeDraft);

            Rect headerRect = list.GetRect(20f);
            float columnWidth = (headerRect.width - 12f) * 0.5f;
            Widgets.Label(new Rect(headerRect.x, headerRect.y, columnWidth, 20f),
                "Origin givers (" + originNames.Count + ")");
            Widgets.Label(new Rect(headerRect.x + columnWidth + 12f, headerRect.y, columnWidth, 20f),
                "Merge destination (" + mergeDraft.Count + ")");

            Rect columnsRect = list.GetRect(SplitMergeColumnHeight);
            Rect left = new Rect(columnsRect.x, columnsRect.y, columnWidth, SplitMergeColumnHeight);
            Rect right = new Rect(columnsRect.x + columnWidth + 12f, columnsRect.y, columnWidth, SplitMergeColumnHeight);
            smOriginArea = left;
            smDestArea = right;

            DrawGiverColumn(left, originNames, draftSet, true, true, ref mergeOriginScroll, "(in merge)");
            DrawGiverColumn(right, mergeDraft, draftSet, false, true, ref mergeTargetScroll, null);

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

        private void DrawGiverColumn(
            Rect area,
            List<string> names,
            HashSet<string> draftNames,
            bool originSide,
            bool mergeMode,
            ref float scroll,
            string draftMarker)
        {
            Widgets.DrawBoxSolid(area, new Color(0.10f, 0.10f, 0.12f, 0.45f));

            if (names == null || names.Count == 0)
            {
                Widgets.Label(
                    new Rect(area.x + 6f, area.y + 4f, area.width - 12f, area.height - 8f),
                    originSide ? "No givers on this work type." : "Drag origin givers here.");
                return;
            }

            float maxScroll = Mathf.Max(0f, names.Count * SplitMergeRowHeight - area.height);
            Event current = Event.current;
            if (Mouse.IsOver(area) && current.type == EventType.ScrollWheel)
            {
                float delta = current.delta.y;
                if (Mathf.Abs(delta) > 0.001f)
                {
                    scroll += delta > 0f ? -SplitMergeRowHeight : SplitMergeRowHeight;
                }
                current.Use();
            }

            // Keep scroll on whole rows so no row is ever drawn half-clipped.
            scroll = Mathf.Clamp(scroll, 0f, maxScroll);
            scroll = Mathf.Floor(scroll / SplitMergeRowHeight) * SplitMergeRowHeight;

            for (int i = 0; i < names.Count; i++)
            {
                float y = area.y + i * SplitMergeRowHeight - scroll;
                if (y + SplitMergeRowHeight <= area.y || y >= area.yMax)
                {
                    continue;
                }

                string name = names[i];
                Rect row = new Rect(area.x + 3f, y, area.width - 6f, SplitMergeRowHeight - 2f);
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
        }

        private void DrawSplitMergeRow(Rect row, string giverName, bool inDraft, bool dragged, bool dropTarget, string draftMarker)
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
            Widgets.Label(new Rect(row.xMax - 20f, row.y, 16f, row.height), "≡");
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
            if (all)
            {
                if (GiversForWorkType(mergeOrigin).Count == 0)
                {
                    return "Origin work type has no givers to move.";
                }
                return null;
            }
            if (mergeDraft.Count == 0)
            {
                return "Drag at least one origin giver into Merge destination.";
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
            }
            else if (smDragSide == 2 && dropInOrigin)
            {
                RemoveDraftGiver(smDragGiver, smDragMergeMode);
            }

            ResetSplitMergeDrag();
            current.Use();
        }

        private void ResetSplitMergeDrag()
        {
            smDragSide = 0;
            smDragGiver = null;
            smDragMoved = false;
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
            list.Label("Saved splits (read-only; applied on restart):");
            if (Settings.splits.Count == 0)
            {
                list.Label("None.");
                return;
            }
            foreach (SplitEntry split in Settings.splits)
            {
                if (split == null)
                {
                    continue;
                }
                string label = string.IsNullOrEmpty(split.label) ? split.newDefName : split.label;
                string givers = split.givers != null ? string.Join(", ", split.givers) : "";
                list.Label("Saved: " + label + " [" + split.newDefName + "] from " + split.baseWorkType + " - " + givers);
            }
        }

        private void DrawSavedMerges(Listing_Standard list)
        {
            list.Gap(8f);
            list.Label("Saved merges (read-only; applied on restart):");
            if (Settings.merges.Count == 0)
            {
                list.Label("None.");
                return;
            }
            foreach (MergeEntry merge in Settings.merges)
            {
                if (merge == null)
                {
                    continue;
                }
                string givers = merge.givers != null ? string.Join(", ", merge.givers) : "";
                list.Label("Saved: [" + givers + "] -> " + merge.targetWorkType);
            }
        }

        private void SaveSplit()
        {
            if (!string.IsNullOrEmpty(SplitValidationWarning()))
            {
                return;
            }

            Settings.splits.Add(new SplitEntry
            {
                newDefName = BuildSplitDefName(splitName),
                label = splitName.Trim(),
                baseWorkType = splitOrigin,
                givers = splitDraft.Distinct().ToList()
            });

            splitName = "";
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

            Settings.merges.Add(new MergeEntry
            {
                givers = givers.Distinct().ToList(),
                targetWorkType = mergeTarget
            });

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
            splitDraft.Clear();
            splitOriginScroll = 0f;
            splitDraftScroll = 0f;
            mergeOrigin = "";
            mergeTarget = "";
            mergeDraft.Clear();
            mergeOriginScroll = 0f;
            mergeTargetScroll = 0f;
            ResetSplitMergeDrag();
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
            if (Settings.workTypeOrder.Count == 0)
            {
                Settings.workTypeOrder = DefDatabase<WorkTypeDef>.AllDefsListForReading
                    .OrderByDescending(w => w.naturalPriority)
                    .Select(w => w.defName)
                    .ToList();
                return;
            }
            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading.OrderByDescending(w => w.naturalPriority))
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
