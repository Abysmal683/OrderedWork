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
        private static void DrawLogCategory(Listing_Standard list, string key, string fallback,
            OrderedWorkLogCategory category)
        {
            bool enabled = (Settings.logCategories & (int)category) != 0;
            bool previous = enabled;
            list.CheckboxLabeled(ConfigText(key, fallback), ref enabled);
            if (enabled != previous)
            {
                Settings.logCategories = enabled
                    ? Settings.logCategories | (int)category
                    : Settings.logCategories & ~(int)category;
            }
        }
        private void DrawGeneralTab(Listing_Standard list)
        {
            if (DrawCollapsibleSection(list, "general-core",
                ConfigText("OrderedWork.Config.General.CoreSection", "Core settings")))
            {
                list.Label(ConfigText("OrderedWork.Config.General.RestartNotice",
                    "Ordered Work (Core) - changes apply on restart."));
                string version = Content != null && Content.ModMetaData != null
                    ? Content.ModMetaData.ModVersion
                    : ConfigText("OrderedWork.Config.General.Unknown", "unknown");
                list.Label(string.Format(ConfigText("OrderedWork.Config.General.Version", "Mod version: {0}"), version));
                list.Gap(6f);
                list.CheckboxLabeled(
                    ConfigText("OrderedWork.Config.General.AutoRecommend", "Auto recommend new colony"),
                    ref Settings.autoRecommendNewColony,
                    ConfigText("OrderedWork.Config.General.AutoRecommendTip", "On: new/default colonies and new pawns get a skill recommendation applied. Off: copy the current vanilla order instead; the top Recommend button still works."));
                list.CheckboxLabeled(
                    ConfigText("OrderedWork.Config.General.AutoHideScrollbars", "Auto-hide scrollbars"),
                    ref Settings.autoHideScrollbars,
                    ConfigText("OrderedWork.Config.General.AutoHideScrollbarsTip", "Show Work Control scrollbars only while hovering or scrolling, so the matrix keeps its space."));
                list.CheckboxLabeled(
                    ConfigText("OrderedWork.Config.General.KeepDestroyed", "Keep orders for destroyed pawns"),
                    ref Settings.keepDestroyedPawnOrders,
                    ConfigText("OrderedWork.Config.General.KeepDestroyedTip", "Off: remove the pawn entry when the game no longer remembers it. On: keep it for mods that can return fully deleted pawns."));
            }
            list.Gap(12f);

            if (DrawCollapsibleSection(list, "general-profiles",
                ConfigText("OrderedWork.Config.General.ProfilesSection", "Profiles")))
            {
                DrawProfilePanel(list);
            }
            list.Gap(12f);

            if (DrawCollapsibleSection(list, "general-diagnostics",
                ConfigText("OrderedWork.Config.General.DiagnosticsSection", "Diagnostics")))
            {
                list.CheckboxLabeled(
                    ConfigText("OrderedWork.Config.General.PaletteStress", "Palette stress test (debug)"),
                    ref Settings.paletteStressTest,
                    ConfigText("OrderedWork.Config.General.PaletteStressTip", "Adds 60 visual-only WorkTypes to test the palette focus."));
                list.Label(ConfigText("OrderedWork.Config.General.LogCategories", "Log categories (Player.log):"));
                DrawLogCategory(list, "OrderedWork.Config.General.LogWorkControl", "Work Control", OrderedWorkLogCategory.WorkControl);
                DrawLogCategory(list, "OrderedWork.Config.General.LogSchedule", "Schedule / focus", OrderedWorkLogCategory.Schedule);
                DrawLogCategory(list, "OrderedWork.Config.General.LogStores", "Stores", OrderedWorkLogCategory.Stores);
                DrawLogCategory(list, "OrderedWork.Config.General.LogPassion", "Passion / VSE", OrderedWorkLogCategory.Passion);
                DrawLogCategory(list, "OrderedWork.Config.General.LogConfig", "Config mod", OrderedWorkLogCategory.Config);
                DrawLogCategory(list, "OrderedWork.Config.General.LogHarmony", "Harmony", OrderedWorkLogCategory.Harmony);
                list.Gap(4f);
                list.Label(ConfigText("OrderedWork.Config.General.SpikeInfo", "WorkGiver order spike: reorders Hauling WorkGivers for one selected pawn only. No defs are changed."));
                list.CheckboxLabeled(
                    ConfigText("OrderedWork.Config.General.SpikeToggle", "Enable WorkGiver order spike (debug)"),
                    ref Settings.workGiverOrderSpike,
                    ConfigText("OrderedWork.Config.General.SpikeToggleTip", "When enabled, the spike patch is allowed to reorder the selected pawn's runtime WorkGiver list."));
                if (UiButtonBar.Button(list.GetRect(28f),
                    ConfigText("OrderedWork.Config.General.SpikeApply", "Apply WorkGiver spike to selected pawn"), true,
                    ConfigText("OrderedWork.Config.General.SpikeApplyTip", "Select exactly one pawn, then reverse that pawn's Hauling WorkGivers and log the result.")))
                {
                    WorkGiverOrderSpike.ApplyToSelectedPawn();
                }
                if (UiButtonBar.Button(list.GetRect(28f),
                    ConfigText("OrderedWork.Config.General.SpikeLog", "Log current WorkGiver order"), true,
                    ConfigText("OrderedWork.Config.General.SpikeLogTip", "Log the selected pawn's current normal and emergency WorkGiver order.")))
                {
                    Pawn selected = Find.Selector != null ? Find.Selector.SingleSelectedThing as Pawn : null;
                    if (selected != null)
                    {
                        WorkGiverOrderSpike.LogCurrentOrder(selected);
                    }
                    else
                    {
                        Log.Warning("[OrderedWork][Spike] Select exactly one pawn before logging the order.");
                    }
                }
                if (UiButtonBar.Button(list.GetRect(28f),
                    ConfigText("OrderedWork.Config.General.SpikeClear", "Clear WorkGiver spike"), true,
                    ConfigText("OrderedWork.Config.General.SpikeClearTip", "Remove the spike order from the selected pawn.")))
                {
                    WorkGiverOrderSpike.ClearSelectedPawn();
                }
                list.Label(ConfigText("OrderedWork.Config.General.DenseInfo", "Technical spike: writes dense rank 1..N to free colonists and logs the result."));
                if (Find.CurrentMap == null)
                {
                    list.Label(ConfigText("OrderedWork.Config.General.LoadColony", "Load a colony to run the dense-rank test."));
                }
                else if (UiButtonBar.Button(list.GetRect(28f),
                    ConfigText("OrderedWork.Config.General.DenseButton", "Test dense rank on colonists (log)"), true,
                    ConfigText("OrderedWork.Config.General.DenseButtonTip", "Run the dense-rank technical spike.")))
                {
                    ApplyTestDenseRank();
                }
            }
            list.Gap(12f);

            if (DrawCollapsibleSection(list, "general-danger",
                ConfigText("OrderedWork.Config.General.DangerSection", "Danger zone")))
            {
                list.Label(ConfigText("OrderedWork.Config.General.ResetInfo", "Reset the mod configuration without touching pawn 1..N priorities."));
                if (!confirmResetAll)
                {
                    if (UiButtonBar.Button(list.GetRect(28f),
                        ConfigText("OrderedWork.Config.General.ResetButton", "Reset config mod all"), true,
                        ConfigText("OrderedWork.Config.General.ResetTip", "Reset all Ordered Work configuration.")))
                    {
                        confirmResetAll = true;
                    }
                }
                else
                {
                    list.Label(ConfigText("OrderedWork.Config.General.ResetConfirmInfo", "This clears orders, splits, merges, colors, stress test and panel height, plus any unsaved drafts. Pawn work priorities are not touched."));
                    if (UiButtonBar.Button(list.GetRect(28f),
                        ConfigText("OrderedWork.Config.General.ResetConfirm", "Confirm reset config mod all"), true,
                        ConfigText("OrderedWork.Config.General.ResetConfirmTip", "Confirm the full configuration reset.")))
                    {
                        ResetAllSettings();
                        confirmResetAll = false;
                    }
                    if (UiButtonBar.Button(list.GetRect(28f),
                        ConfigText("OrderedWork.Config.General.CancelReset", "Cancel reset"), true,
                        ConfigText("OrderedWork.Config.General.CancelResetTip", "Cancel the pending configuration reset.")))
                    {
                        confirmResetAll = false;
                    }
                }
            }

            list.Gap(64f);
            list.Label(ConfigText("OrderedWork.Config.General.BackupWarning", "No work type or giver is ever deleted. Back up your save before applying."));
        }
    }
}
