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

            if (DrawCollapsibleSection(list, "general-equipment",
                ConfigText("OrderedWork.Config.General.EquipmentSection", "Equipment system")))
            {
                list.CheckboxLabeled(
                    ConfigText("OrderedWork.Config.General.EquipmentEnabled", "Enable equipment system"),
                    ref Settings.equipmentEnabled,
                    ConfigText("OrderedWork.Config.General.EquipmentEnabledTip", "Off: hides the equipment UI and skips equipment jobs. Use this to avoid conflicts with other clothing/loadout mods."));
                list.CheckboxLabeled(
                    ConfigText("OrderedWork.Config.General.EquipmentSearchArea", "Limit search to workbench area"),
                    ref Settings.equipmentSearchAreaEnabled,
                    ConfigText("OrderedWork.Config.General.EquipmentSearchAreaTip", "When enabled, searches around the active workbench/job area. When disabled, searches the map."));
                Rect radiusRect = list.GetRect(24f);
                Settings.equipmentSearchRadius = Mathf.RoundToInt(Widgets.HorizontalSlider(
                    radiusRect,
                    Settings.equipmentSearchRadius,
                    5f,
                    80f,
                    true,
                    string.Format(ConfigText("OrderedWork.Config.General.EquipmentRadius", "Search radius: {0}"), Settings.equipmentSearchRadius)));
            }

            if (DrawCollapsibleSection(list, "general-profiles",
                ConfigText("OrderedWork.Config.General.ProfilesSection", "Profiles")))
            {
                profilesPanel.Draw(list, Settings, activeProfileName,
                    delegate(string profile) { activeProfileName = profile; },
                    profileStatus,
                    delegate(string message) { profileStatus = message; },
                    PersistLivePending);
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
                    list.Label(ConfigText("OrderedWork.Config.General.ResetConfirmInfo", "This clears UI settings, colors, toggles, passion weights and drafts, and rebuilds the active profile from reference.json. Other profile, pending and reference files are not deleted. Pawn work priorities are not touched."));
                    if (UiButtonBar.Button(list.GetRect(28f),
                        ConfigText("OrderedWork.Config.General.ResetConfirm", "Confirm reset config mod all"), true,
                        ConfigText("OrderedWork.Config.General.ResetConfirmTip", "Confirm the full configuration reset."), UiColors.ConfirmDanger))
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
