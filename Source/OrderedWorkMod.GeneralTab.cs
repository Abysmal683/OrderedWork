using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.Core;
using OrderedWork.Services;
using OrderedWork.UI;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork
{
    public partial class OrderedWorkMod : Mod
    {
        private void DrawGeneralTab(Listing_Standard list)
        {
            if (DrawCollapsibleSection(list, "general-core", "Core settings"))
            {
                list.Label("Ordered Work (Core) - changes apply on restart.");
                string version = Content != null && Content.ModMetaData != null
                    ? Content.ModMetaData.ModVersion
                    : "unknown";
                list.Label("Mod version: " + version);
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
                    "Keep orders for destroyed pawns",
                    ref Settings.keepDestroyedPawnOrders,
                    "Off: remove the pawn entry when the game no longer remembers it. On: keep it for mods that can return fully deleted pawns.");
            }
            list.Gap(12f);

            if (DrawCollapsibleSection(list, "general-profiles", "Profiles"))
            {
                DrawProfilePanel(list);
            }
            list.Gap(12f);

            if (DrawCollapsibleSection(list, "general-diagnostics", "Diagnostics"))
            {
                list.CheckboxLabeled(
                    "Palette stress test (debug)",
                    ref Settings.paletteStressTest,
                    "Adds 60 visual-only WorkTypes to test the palette focus.");
                list.CheckboxLabeled(
                    "Verbose diagnostics in Player.log",
                    ref Settings.verboseLogging,
                    "Enables temporary Order/Reset/Scroll debug lines. Disable for normal play.");
                list.Label("Technical spike: writes dense rank 1..N to free colonists and logs the result.");
                if (Find.CurrentMap == null)
                {
                    list.Label("Load a colony to run the dense-rank test.");
                }
                else if (UiButtonBar.Button(list.GetRect(28f), "Test dense rank on colonists (log)", true, "Run the dense-rank technical spike."))
                {
                    ApplyTestDenseRank();
                }
            }
            list.Gap(12f);

            if (DrawCollapsibleSection(list, "general-danger", "Danger zone"))
            {
                list.Label("Reset the mod configuration without touching pawn 1..N priorities.");
                if (!confirmResetAll)
                {
                    if (UiButtonBar.Button(list.GetRect(28f), "Reset config mod all", true, "Reset all Ordered Work configuration."))
                    {
                        confirmResetAll = true;
                    }
                }
                else
                {
                    list.Label("This clears orders, splits, merges, colors, stress test and panel height, plus any unsaved drafts. Pawn work priorities are not touched.");
                    if (UiButtonBar.Button(list.GetRect(28f), "Confirm reset config mod all", true, "Confirm the full configuration reset."))
                    {
                        ResetAllSettings();
                        confirmResetAll = false;
                    }
                    if (UiButtonBar.Button(list.GetRect(28f), "Cancel reset", true, "Cancel the pending configuration reset."))
                    {
                        confirmResetAll = false;
                    }
                }
            }

            list.Gap(64f);
            list.Label("No work type or giver is ever deleted. Back up your save before applying.");
        }
    }
}