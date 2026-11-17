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

namespace OrderedWork
{
    public partial class OrderedWorkMod : Mod
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
        private float smOriginMaxScroll;
        private float smDestMaxScroll;
        private Rect passionWeightsArea;

        // Per-column scrollbar drag owner (0 = none, 1 = origin column, 2 = destination).
        private int smScrollDragColumn;
        // Merge destination reorder preview: insertion index into mergeDraft, -1 = none.
        private int smMergeDropIndex = -1;

        private int dragKind;
        private int dragIndex = -1;
        private float dragStartMouseY;
        private bool dragMoved;
        private bool confirmResetAll;
        private string activeProfileName = "default";
        private List<string> giverOrderDraft = new List<string>();
        private List<string> giverOrderInitial = new List<string>();
        private string giverOrderDraftType = "";
        private bool giverOrderDraftInitialized;
        private bool splitDirty;
        private bool giverOrderDirty;
        private bool mergeDirty;
        private string newProfileName = "";
        private bool profileNameInitialized;
        private string profileStatus = "";

        public OrderedWorkMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<OrderedWorkSettings>();
        }

        public override string SettingsCategory()
        {
            return "Ordered Work";
        }

        private void HandleNestedSettingsWheel()
        {
            Event current = Event.current;
            if (current.type != EventType.ScrollWheel)
            {
                return;
            }
            bool shift = current.shift
                || Input.GetKey(KeyCode.LeftShift)
                || Input.GetKey(KeyCode.RightShift);
            if (!shift || settingsTab != TabRules)
            {
                return;
            }

            Vector2 mouse = current.mousePosition;
            if (passionWeightsArea.Contains(mouse))
            {
                int count = PassionWeightCatalog.Registered.Count;
                float max = Mathf.Max(0f, count * PassionRowHeight - passionWeightsArea.height);
                if (max > 0f)
                {
                    passionWeightScroll.y = Mathf.Clamp(
                        passionWeightScroll.y + current.delta.y * 20f, 0f, max);
                    current.Use();
                }
                return;
            }

            if (settingsEditorMode == EditorSplit)
            {
                if (ScrollColumnWheel(smOriginArea, smOriginMaxScroll, ref splitOriginScroll, current))
                {
                    return;
                }
                ScrollColumnWheel(smDestArea, smDestMaxScroll, ref splitDraftScroll, current);
                return;
            }
            if (settingsEditorMode == EditorMerge)
            {
                if (ScrollColumnWheel(smOriginArea, smOriginMaxScroll, ref mergeOriginScroll, current))
                {
                    return;
                }
                ScrollColumnWheel(smDestArea, smDestMaxScroll, ref mergeTargetScroll, current);
            }
        }

        private static bool ScrollColumnWheel(Rect area, float maxScroll, ref float scroll, Event current)
        {
            if (maxScroll <= 0f || !area.Contains(current.mousePosition))
            {
                return false;
            }
            scroll = Mathf.Clamp(scroll + (current.delta.y > 0f ? -SplitMergeRowHeight : SplitMergeRowHeight),
                0f, maxScroll);
            current.Use();
            return true;
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            EnsureProfileNameInitialized();

            Rect tabsRect = new Rect(inRect.x, inRect.y, inRect.width, 28f);
            DrawSettingsTabs(tabsRect);

            Rect body = new Rect(
                inRect.x,
                tabsRect.yMax + 4f,
                inRect.width,
                Mathf.Max(40f, inRect.height - tabsRect.height - 4f));

            // Use the height measured after last frame's draw. Zero means "not measured yet",
            // so fall back to the body height and let this frame measure the real content.
            HandleNestedSettingsWheel();
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







        // ---------------------------------------------------------------------
        // Split / Merge editor
        // ---------------------------------------------------------------------





























        private void ResetEditorUiState()
        {
            ResetSplitMergeDrag();
        }










        // ---------------------------------------------------------------------
        // Shared settings helpers
        // ---------------------------------------------------------------------

        private void ResetAllSettings()
        {
            Settings.workTypeColors.Clear();
            Settings.autoRecommendNewColony = true;
            Settings.autoHideScrollbars = true;
            Settings.paletteStressTest = false;
            Settings.workControlHeight = 0f;
            selectedGiverType = WorkConfigStore.GetOrderedVisibleWorkTypes(
                WorkConfigStore.LoadEditingProfile(activeProfileName)).FirstOrDefault()?.defName ?? "";
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









    }
}
