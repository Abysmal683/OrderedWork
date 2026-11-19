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
        private void DrawRulesTab(Listing_Standard list)
        {
            EnsureSplitMergeDefaults();

            if (DrawCollapsibleSection(list, "rules-core", "Rules"))
            {
                list.Label("Ordered Work (Core) - changes apply on restart.");
                list.Label("Split copies work tags, skills, life stages and mech availability.");
            }
            list.Gap(6f);

            if (DrawCollapsibleSection(list, "work-order", "Work types and order"))
            {
                list.Label("Work type order (top = first). Drag rows to reorder:");
                WorkConfigProfile rulesProfile = WorkConfigStore.LoadEditingProfile(activeProfileName);
                List<WorkTypeDef> orderedTypes = WorkConfigStore.GetOrderedVisibleWorkTypes(rulesProfile);
                if (!workOrderDraftInitialized)
                {
                    workOrderDraft.Clear();
                    workOrderBaseline.Clear();
                    for (int i = 0; i < orderedTypes.Count; i++)
                    {
                        workOrderDraft.Add(orderedTypes[i].defName);
                        workOrderBaseline.Add(orderedTypes[i].defName);
                    }
                    workOrderDraftInitialized = true;
                }
                List<Rect> workRows = new List<Rect>();
                for (int i = 0; i < workOrderDraft.Count; i++)
                {
                    WorkTypeDef wt = DefDatabase<WorkTypeDef>.GetNamedSilentFail(workOrderDraft[i]);
                    if (wt == null) continue;
                    Rect row = list.GetRect(24f);
                    UiZebra.Draw(row, i);
                    DrawReorderRow(row, i + 1 + ". " + wt.labelShort, 1, i, WorkTypeTooltip(wt.defName));
                    bool changed = i >= workOrderBaseline.Count || workOrderDraft[i] != workOrderBaseline[i];
                    if (changed)
                    {
                        Widgets.Label(new Rect(row.xMax - 86f, row.y, 60f, 20f), "Changed");
                    }
                    workRows.Add(row);
                }
                HandleReorderDrag(1, workRows, MoveWorkOrderDraft);
                list.Gap(4f);
                Rect workButtons = list.GetRect(28f);
                Rect[] workButtonCells = UiFlex.Columns(workButtons, 6f, 1f, 1f);
                bool workOrderChanged = !workOrderDraft.SequenceEqual(workOrderBaseline);
                if (UiButtonBar.Button(workButtonCells[0], "Apply work order", workOrderChanged, workOrderChanged ? "Apply the reordered WorkTypes." : "No work order changes to apply."))
                {
                    ApplyWorkOrderDraft();
                }
                if (UiButtonBar.Button(workButtonCells[1], "Cancel work order", workOrderChanged, workOrderChanged ? "Discard the work order draft." : "No work order changes to cancel."))
                {
                    workOrderDraft.Clear();
                    workOrderDraft.AddRange(workOrderBaseline);
                }
            }
            if (DrawCollapsibleSection(list, "recommendation-pins", "Recommendation pins"))
            {
                list.CheckboxLabeled(
                    "Prioritize low-level passion over work without skill",
                    ref Settings.prioritizeLowSkillPassion,
                    "When on, a low skill with Minor/Major passion can rank above a WorkType without a relevant skill. When off, only level and naturalPriority decide.");
                list.Label("Automatic recommendation pins (top = first). Drag to reorder:");
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
                if (Widgets.ButtonText(pinButtonRect, "Add recommendation pin"))
                {
                    List<WorkTypeDef> candidates = WorkConfigStore.GetOrderedVisibleWorkTypes(
                            WorkConfigStore.LoadEditingProfile(activeProfileName))
                        .Where(w => !Settings.recommendationPins.Contains(w.defName))
                        .ToList();
                    if (candidates.Count > 0)
                    {
                        Find.WindowStack.Add(new WorkTypeDropdownWindow(
                            "Add recommendation pin",
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
                    "Default pins: Patient, PatientBedRest, Firefighter. Pins are global and applied in this order before scoring.");
            }

            if (DrawCollapsibleSection(list, "recommendation-weights", "Recommendation passion weights"))
            {
                PassionWeightsView.Draw(list, Settings, ref passionWeightScroll, false);
            }

            if (DrawCollapsibleSection(list, "split-merge-order", "Split / Merge / Order"))
            {
                list.CheckboxLabeled(
                    "Allow empty origin (Rules-wide)",
                    ref Settings.allowEmptyOrigin,
                    "Applies to both Split and Merge. Vanilla origins hide; splits delete. Disable to require at least one giver left in the origin.");
                DrawSplitMergeEditor(list);
            }

            list.Gap(10f);
            list.Label("No work type or giver is ever deleted. Back up your save before applying.");
            list.Label("Split/Merge changes take effect after a restart.");
        }
    }
}