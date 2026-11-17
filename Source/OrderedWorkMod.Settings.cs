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
            list.CheckboxLabeled(
                "Allow empty origin (Rules-wide)",
                ref Settings.allowEmptyOrigin,
                "Applies to both Split and Merge. Vanilla origins hide; splits delete. Disable to require at least one giver left in the origin.");
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

        private void DrawProfilePanel(Listing_Standard list)
        {
            list.Label("Global JSON profiles (pending edits apply on restart):");
            if (!profileNameInitialized)
            {
                activeProfileName = WorkConfigStore.LoadSelectedProfileName();
                newProfileName = activeProfileName;
                profileNameInitialized = true;
            }

            List<string> profiles = new List<string>();
            try
            {
                Directory.CreateDirectory(WorkConfigStore.ProfilesDirectory);
                profiles = Directory.GetFiles(WorkConfigStore.ProfilesDirectory, "*.json")
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(n => !n.EndsWith(".pending", StringComparison.Ordinal))
                    .Distinct()
                    .OrderBy(n => n, StringComparer.Ordinal)
                    .ToList();
            }
            catch (Exception exception)
            {
                profileStatus = "Could not list profiles: " + exception.Message;
            }

            Rect selector = list.GetRect(28f);
            if (Widgets.ButtonText(selector, "Active profile: " + activeProfileName + "  ▼"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (string profile in profiles)
                {
                    string captured = profile;
                    options.Add(new FloatMenuOption(profile, delegate
                    {
                        activeProfileName = captured;
                        newProfileName = captured;
                        WorkConfigStore.SaveSelectedProfileName(captured);
                        profileStatus = "Selected " + captured + ". Apply on next startup.";
                    }));
                }
                if (options.Count > 0)
                {
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }

            Rect nameRow = list.GetRect(26f);
            Widgets.Label(new Rect(nameRow.x, nameRow.y, 84f, 24f), "Save as");
            newProfileName = Widgets.TextField(
                new Rect(nameRow.x + 88f, nameRow.y, Mathf.Max(80f, nameRow.width - 88f), 24f),
                newProfileName ?? "");

            if (list.ButtonText("Save current as profile"))
            {
                string profile = string.IsNullOrWhiteSpace(newProfileName) ? activeProfileName : newProfileName.Trim();
                if (string.IsNullOrWhiteSpace(profile))
                {
                    profile = "default";
                }
                WorkConfigProfile captured = WorkConfigProfileBuilder.Capture(Settings, profile);
                if (WorkConfigStore.SaveProfile(captured, true))
                {
                    activeProfileName = profile;
                    WorkConfigStore.SaveSelectedProfileName(profile);
                    PersistLivePending();
                    profileStatus = "Saved " + profile + ".json";
                }
                else
                {
                    profileStatus = "Failed to save " + profile + ".json";
                }
            }
            if (list.ButtonText("Write pending for active profile"))
            {
                WorkConfigProfile captured = WorkConfigProfileBuilder.Capture(Settings, activeProfileName);
                profileStatus = WorkConfigStore.SavePending(captured)
                    ? "Wrote " + activeProfileName + ".pending.json. Apply on next startup."
                    : "Failed to write pending profile.";
            }

            list.Label("Reference: " + WorkConfigStore.ReferencePath);
            if (!string.IsNullOrEmpty(profileStatus))
            {
                list.Label(profileStatus);
            }
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
            list.CheckboxLabeled(
                "Keep orders for destroyed pawns",
                ref Settings.keepDestroyedPawnOrders,
                "Off: remove the pawn entry when the game no longer remembers it. On: keep it for mods that can return fully deleted pawns.");

            list.Gap(12f);
            DrawProfilePanel(list);
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

    }
}
