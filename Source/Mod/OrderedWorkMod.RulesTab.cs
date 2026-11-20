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
        private void DrawAllowEmptyOriginToggle(Listing_Standard list)
        {
            bool enabled = Settings.allowEmptyOrigin;
            bool previous = enabled;
            list.CheckboxLabeled(
                ConfigText("OrderedWork.Config.Rules.AllowEmpty", "Allow empty origin (Rules-wide)"),
                ref enabled,
                ConfigText("OrderedWork.Config.Rules.AllowEmptyTip", "Applies to both Split and Merge. Vanilla origins hide; splits delete. Disable to require at least one giver left in the origin."));
            if (enabled == previous)
            {
                return;
            }
            if (!enabled)
            {
                WorkConfigProfile profile = WorkConfigStore.LoadEditingProfile(activeProfileName);
                int empty = profile == null ? 0 : profile.WorkTypes.Values.Count(w => w != null
                    && (w.Givers == null || w.Givers.Count == 0));
                if (empty > 0)
                {
                    Messages.Message(string.Format(
                        ConfigText("OrderedWork.Config.Rules.AllowEmptyBlocked",
                            "Cannot disable empty-origin support: {0} WorkTypes have no Givers. Restore them first."),
                        empty), MessageTypeDefOf.RejectInput, false);
                    return;
                }
            }
            Settings.allowEmptyOrigin = enabled;
        }
        private void DrawRulesTab(Listing_Standard list)
        {
            EnsureSplitMergeDefaults();

            if (DrawCollapsibleSection(list, "rules-core",
                ConfigText("OrderedWork.Config.Rules.Section", "Rules")))
            {
                list.Label(ConfigText("OrderedWork.Config.Rules.RestartNotice",
                    "Ordered Work (Core) - changes apply on restart."));
                list.Label(ConfigText("OrderedWork.Config.Rules.SplitNotice",
                    "Split copies work tags, skills, life stages and mech availability."));
            }
            list.Gap(6f);

            if (DrawCollapsibleSection(list, "work-order",
                ConfigText("OrderedWork.Config.Rules.WorkOrderSection", "Work types and order")))
            {
                list.Label(ConfigText("OrderedWork.Config.Rules.WorkOrderInfo",
                    "Work type order (top = first). Drag rows to reorder:"));
                workOrderRulesPanel.Draw(list, this, activeProfileName);
            }            if (DrawCollapsibleSection(list, "recommendation-pins",
                ConfigText("OrderedWork.Config.Rules.PinsSection", "Recommendation pins")))
            {
                list.CheckboxLabeled(
                    ConfigText("OrderedWork.Config.Rules.PrioritizePassion", "Prioritize low-level passion over work without skill"),
                    ref Settings.prioritizeLowSkillPassion,
                    ConfigText("OrderedWork.Config.Rules.PrioritizePassionTip", "When on, a low skill with Minor/Major passion can rank above a WorkType without a relevant skill. When off, only level and naturalPriority decide."));
                list.Label(ConfigText("OrderedWork.Config.Rules.PinsInfo",
                    "Automatic recommendation pins (top = first). Drag to reorder:"));
                List<Rect> pinRows = new List<Rect>();
                for (int i = 0; i < Settings.recommendationPins.Count; i++)
                {
                    string id = Settings.recommendationPins[i];
                    WorkTypeDef pin = DefDatabase<WorkTypeDef>.GetNamedSilentFail(id);
                    string label = pin != null ? pin.labelShort : id;
                    Rect row = list.GetRect(24f);
                    DrawReorderRow(row, i + 1 + ". " + label, 6, i, WorkTypeTooltip(id));
                    Rect pinRemove = new Rect(row.xMax - 22f, row.y + 2f, 18f, 20f);
                    if (Widgets.ButtonText(pinRemove, "x"))
                    {
                        Settings.recommendationPins.RemoveAt(i);
                        break;
                    }
                    pinRows.Add(row);
                }
                HandleReorderDrag(6, pinRows, MoveRecommendationPin);
                Rect pinButtonRect = list.GetRect(24f);
                if (Widgets.ButtonText(pinButtonRect,
                    ConfigText("OrderedWork.Config.Rules.AddPin", "Add recommendation pin")))
                {
                    List<WorkTypeDef> candidates = WorkConfigStore.GetOrderedVisibleWorkTypes(
                            WorkConfigStore.LoadEditingProfile(activeProfileName))
                        .Where(w => !Settings.recommendationPins.Contains(w.defName))
                        .ToList();
                    if (candidates.Count > 0)
                    {
                        Find.WindowStack.Add(new WorkTypeDropdownWindow(
                            ConfigText("OrderedWork.Config.Rules.AddPinTitle", "Add recommendation pin"),
                            candidates,
                            null,
                            delegate(WorkTypeDef wt)
                            {
                                if (wt != null && !Settings.recommendationPins.Contains(wt.defName))
                                {
                                    Settings.recommendationPins.Add(wt.defName);
                                }
                            }));
                    }
                }
                TooltipHandler.TipRegion(
                    pinButtonRect,
                    ConfigText("OrderedWork.Config.Rules.DefaultPinsTip",
                        "Default pins: Patient, PatientBedRest, Firefighter. Pins are global and applied in this order before scoring."));
            }

            if (DrawCollapsibleSection(list, "recommendation-weights",
                ConfigText("OrderedWork.Config.Rules.WeightsSection", "Recommendation passion weights")))
            {
                PassionWeightsView.Draw(list, Settings, false);
            }

            if (DrawCollapsibleSection(list, "split-merge-order",
                ConfigText("OrderedWork.Config.Rules.SplitMergeSection", "Split / Merge / Order")))
            {
                DrawAllowEmptyOriginToggle(list);
                splitMerge.Draw(list, this);
            }

            list.Gap(10f);
            list.Label(ConfigText("OrderedWork.Config.Rules.NoDelete",
                "No work type or giver is ever deleted. Back up your save before applying."));
            list.Label(ConfigText("OrderedWork.Config.Rules.RestartApply",
                "Split/Merge changes take effect after a restart."));
        }
    }
}
