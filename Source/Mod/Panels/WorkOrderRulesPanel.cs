using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.UI;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork
{
    /// <summary>
    /// WorkType order draft plus global recommendation pins. It owns the draft state;
    /// the host supplies section drawing, drag visuals and profile selection.
    /// </summary>
    internal sealed class WorkOrderRulesPanel
    {
        private readonly List<string> draft = new List<string>();
        private readonly List<string> baseline = new List<string>();
        private bool initialized;
        private string cachedProfile;

        internal void Draw(Listing_Standard list, OrderedWorkMod host, string activeProfile)
        {
            if (cachedProfile != activeProfile)
            {
                cachedProfile = activeProfile;
                initialized = false;
            }
            WorkConfigProfile profile = WorkConfigStore.LoadEditingProfile(activeProfile);
            List<WorkTypeDef> orderedTypes = WorkConfigStore.GetOrderedVisibleWorkTypes(profile);
            if (!initialized)
            {
                draft.Clear();
                baseline.Clear();
                for (int i = 0; i < orderedTypes.Count; i++)
                {
                    draft.Add(orderedTypes[i].defName);
                    baseline.Add(orderedTypes[i].defName);
                }
                initialized = true;
            }

            List<Rect> rows = new List<Rect>();
            for (int i = 0; i < draft.Count; i++)
            {
                WorkTypeDef type = DefDatabase<WorkTypeDef>.GetNamedSilentFail(draft[i]);
                if (type == null)
                {
                    continue;
                }
                Rect row = list.GetRect(24f);
                UiZebra.Draw(row, i);
                string rowLabel = i + 1 + ". " + type.labelShort;
                if (profile.WorkTypes.TryGetValue(type.defName, out WorkTypeConfig rowConfig) && rowConfig != null && rowConfig.Hidden)
                {
                    rowLabel += " [" + OrderedWorkMod.ConfigText("OrderedWork.Config.Rules.HiddenSuffix", "hidden") + "]";
                }
                host.DrawReorderRow(row, rowLabel, 1, i, OrderedWorkMod.WorkTypeTooltip(type.defName));
                bool changed = i >= baseline.Count || draft[i] != baseline[i];
                if (changed)
                {
                    Widgets.Label(new Rect(row.xMax - 86f, row.y, 60f, 20f),
                        OrderedWorkMod.ConfigText("OrderedWork.Config.Rules.Changed", "Changed"));
                }
                if (Mouse.IsOver(row) && Event.current.type == EventType.MouseDown && Event.current.button == 1
                    && profile.WorkTypes.TryGetValue(type.defName, out WorkTypeConfig hiddenConfig)
                    && hiddenConfig != null && hiddenConfig.Hidden)
                {
                    hiddenConfig.Hidden = false;
                    WorkConfigStore.SaveProfile(profile, true);
                    WorkConfigStore.SavePending(profile);
                    OrderedWorkStartup.ApplyProfileNow(profile, OrderedWorkMod.Settings, host);
                    initialized = false;
                    Event.current.Use();
                }
                rows.Add(row);
            }
            host.HandleReorderDrag(1, rows, Move);

            list.Gap(4f);
            Rect buttons = list.GetRect(28f);
            Rect[] cells = UiFlex.Columns(buttons, 6f, 1f, 1f);
            bool changedOrder = !draft.SequenceEqual(baseline);
            if (UiButtonBar.Button(cells[0],
                OrderedWorkMod.ConfigText("OrderedWork.Config.Rules.ApplyWorkOrder", "Apply work order"), changedOrder,
                changedOrder
                    ? OrderedWorkMod.ConfigText("OrderedWork.Config.Rules.ApplyWorkOrderTip", "Apply the reordered WorkTypes.")
                    : OrderedWorkMod.ConfigText("OrderedWork.Config.Rules.ApplyWorkOrderNoChanges", "No work order changes to apply.")))
            {
                Apply(host, activeProfile);
            }
            if (UiButtonBar.Button(cells[1],
                OrderedWorkMod.ConfigText("OrderedWork.Config.Rules.CancelWorkOrder", "Cancel work order"), changedOrder,
                changedOrder
                    ? OrderedWorkMod.ConfigText("OrderedWork.Config.Rules.CancelWorkOrderTip", "Discard the work order draft.")
                    : OrderedWorkMod.ConfigText("OrderedWork.Config.Rules.CancelWorkOrderNoChanges", "No work order changes to cancel.")))
            {
                draft.Clear();
                draft.AddRange(baseline);
            }

            Rect addHidden = list.GetRect(24f);
            if (Widgets.ButtonText(addHidden,
                OrderedWorkMod.ConfigText("OrderedWork.Config.Rules.AddHidden", "Add hidden WorkType")))
            {
                List<WorkTypeDef> candidates = DefDatabase<WorkTypeDef>.AllDefsListForReading
                    .Where(type => type != null)
                    .Where(type => !profile.WorkTypes.TryGetValue(type.defName, out WorkTypeConfig existing)
                        || existing == null || !existing.Hidden)
                    .OrderBy(type => type.labelShort)
                    .ToList();
                if (candidates.Count > 0)
                {
                    Find.WindowStack.Add(new WorkTypeDropdownWindow(
                        OrderedWorkMod.ConfigText("OrderedWork.Config.Rules.AddHidden", "Add hidden WorkType"),
                        candidates,
                        null,
                        delegate(WorkTypeDef type)
                        {
                            if (type == null)
                            {
                                return;
                            }
                            if (!profile.WorkTypes.TryGetValue(type.defName, out WorkTypeConfig existing) || existing == null)
                            {
                                existing = new WorkTypeConfig
                                {
                                    Vanilla = true,
                                    Label = type.labelShort,
                                    ActionWord = type.gerundLabel,
                                    Description = type.description,
                                    Visible = true,
                                    Order = profile.WorkTypes.Count
                                };
                                profile.WorkTypes[type.defName] = existing;
                            }
                            existing.Hidden = true;
                            existing.Visible = true;
                            WorkConfigStore.SaveProfile(profile, true);
                            WorkConfigStore.SavePending(profile);
                            OrderedWorkStartup.ApplyProfileNow(profile, OrderedWorkMod.Settings, host);
                            initialized = false;
                        }));
                }
            }
            TooltipHandler.TipRegion(addHidden,
                OrderedWorkMod.ConfigText("OrderedWork.Config.Rules.AddHiddenTip", "Mark a WorkType hidden but active. Right-click its order row to remove the hidden mark."));
        }

        private void Move(int from, int to)
        {
            if (from < 0 || from >= draft.Count || to < 0 || to >= draft.Count || from == to)
            {
                return;
            }
            string value = draft[from];
            draft.RemoveAt(from);
            draft.Insert(to, value);
        }

        private void Apply(OrderedWorkMod host, string activeProfile)
        {
            if (cachedProfile != activeProfile)
            {
                cachedProfile = activeProfile;
                initialized = false;
            }
            WorkConfigProfile profile = WorkConfigStore.LoadEditingProfile(activeProfile);
            if (profile == null)
            {
                return;
            }
            for (int i = 0; i < draft.Count; i++)
            {
                if (profile.WorkTypes.TryGetValue(draft[i], out WorkTypeConfig config) && config != null)
                {
                    config.Order = i;
                }
            }
            WorkConfigStore.SaveProfile(profile, true);
            WorkConfigStore.SavePending(profile);
            OrderedWorkStartup.ApplyProfileNow(profile, OrderedWorkMod.Settings, host);
            baseline.Clear();
            baseline.AddRange(draft);
            Messages.Message(OrderedWorkMod.ConfigText("OrderedWork.Config.Helpers.OrderApplied", "Work type order applied."), MessageTypeDefOf.TaskCompletion, false);
        }
    }
}