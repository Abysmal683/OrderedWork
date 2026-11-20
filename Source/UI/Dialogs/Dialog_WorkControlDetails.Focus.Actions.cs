#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.Core;
using OrderedWork.Data;
using OrderedWork.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    public partial class Dialog_WorkControlDetails
    {        private void ClearFocusHour(int hour)
        {
            List<string> focus = GetFocus(hour);
            if (focus.Count == 0)
            {
                Messages.Message(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.NothingToClear",
                    "Nothing to clear in this hour."), MessageTypeDefOf.RejectInput, false);
                return;
            }
            List<string> names = focus.Select(id =>
            {
                WorkTypeDef type = DefDatabase<WorkTypeDef>.GetNamedSilentFail(id);
                return type != null ? WorkTypePaletteView.DisplayName(type) : id;
            }).ToList();
            SetFocus(hour, new List<string>());
            detailFocusSelected = -1;
            Messages.Message(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.ClearedHour", "Hour cleared: ")
                + hour.ToString("00") + ":00 · " + names.Count + " · " + string.Join(", ", names),
                MessageTypeDefOf.TaskCompletion, false);
        }

        private void ClearAllFocus()
        {
            PawnWorkOrder entry = DetailEntry();
            if (entry == null || entry.hourFocusOverrides.Count == 0)
            {
                Messages.Message(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.NothingToClear",
                    "Nothing to clear in this hour."), MessageTypeDefOf.RejectInput, false);
                return;
            }
            int hours = entry.hourFocusOverrides.Count;
            int entries = entry.hourFocusOverrides.Sum(v => v.workTypeDefNames != null ? v.workTypeDefNames.Count : 0);
            entry.hourFocusOverrides.Clear();
            detailFocusSelected = -1;
            Messages.Message(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.ClearedAll", "All focus cleared: ")
                + string.Format(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.ClearedAllFormat", "{0} hours, {1} entries"), hours, entries),
                MessageTypeDefOf.TaskCompletion, false);
        }
        private void StepFocusHour(int delta)
        {
            detailFocusHour = (detailFocusHour + delta + SchedulePreset.SlotCount) % SchedulePreset.SlotCount;
            detailFocusSelected = -1;
        }

        private void OpenFocusPicker(int replaceIndex)
        {
            List<string> focus = GetFocus(detailFocusHour);
            List<WorkTypeDef> candidates = WorkOrderService.GetOrder(pawn)
                .Select(id => DefDatabase<WorkTypeDef>.GetNamedSilentFail(id))
                .Where(type => type != null && !WorkOrderService.IsIncapable(pawn, type))
                .Where(type => replaceIndex >= 0
                    ? !focus.Where((id, index) => index != replaceIndex).Contains(type.defName)
                    : !focus.Contains(type.defName))
                .ToList();
            if (candidates.Count == 0)
            {
                Messages.Message(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.NoCandidates",
                    "No eligible WorkTypes in this pawn's work list."), MessageTypeDefOf.RejectInput, false);
                return;
            }
            Find.WindowStack.Add(new WorkTypeDropdownWindow(
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.Pick", "Choose a WorkType"),
                candidates,
                replaceIndex >= 0 && replaceIndex < focus.Count
                    ? DefDatabase<WorkTypeDef>.GetNamedSilentFail(focus[replaceIndex])
                    : null,
                delegate(WorkTypeDef selected)
                {
                    if (selected == null)
                    {
                        return;
                    }
                    List<string> next = new List<string>(focus);
                    if (replaceIndex >= 0 && replaceIndex < next.Count)
                    {
                        next[replaceIndex] = selected.defName;
                    }
                    else if (next.Count < HourFocusStack.MaxFocus)
                    {
                        next.Add(selected.defName);
                    }
                    SetFocus(detailFocusHour, next);
                    detailFocusSelected = replaceIndex >= 0 ? replaceIndex : Mathf.Min(next.Count - 1, HourFocusStack.MaxFocus - 1);
                    Messages.Message(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.Modified", "Focus updated: ")
                        + detailFocusHour.ToString("00") + ":00 · " + WorkTypePaletteView.DisplayName(selected),
                        MessageTypeDefOf.TaskCompletion, false);
                }));
        }

        private void MoveFocusEntry(int from, int to)
        {
            List<string> focus = GetFocus(detailFocusHour);
            if (from < 0 || to < 0 || from >= focus.Count || to >= focus.Count)
            {
                return;
            }
            string value = focus[from];
            focus.RemoveAt(from);
            focus.Insert(to, value);
            SetFocus(detailFocusHour, focus);
            detailFocusSelected = to;
        }

        private void RemoveFocusEntry(int index)
        {
            List<string> focus = GetFocus(detailFocusHour);
            if (index < 0 || index >= focus.Count)
            {
                return;
            }
            string removedId = focus[index];
            WorkTypeDef removedType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(removedId);
            focus.RemoveAt(index);
            SetFocus(detailFocusHour, focus);
            detailFocusSelected = -1;
            Messages.Message(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.Removed", "Focus removed: ")
                + detailFocusHour.ToString("00") + ":00 · "
                + (removedType != null ? WorkTypePaletteView.DisplayName(removedType) : removedId),
                MessageTypeDefOf.TaskCompletion, false);
        }

        private void CopyFocusHour()
        {
            List<string> current = GetFocus(detailFocusHour);
            if (current.Count == 0)
            {
                Messages.Message(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.CopyEmpty",
                    "No focus to copy in this hour."), MessageTypeDefOf.RejectInput, false);
                return;
            }
            detailFocusClipboard = new List<string>(current);
            Messages.Message(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.Copied", "Hour copied: ")
                + detailFocusHour.ToString("00") + ":00", MessageTypeDefOf.TaskCompletion, false);
        }

        private void PasteFocusHour()
        {
            if (detailFocusClipboard == null || detailFocusClipboard.Count == 0)
            {
                Messages.Message(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.ClipboardEmpty",
                    "No copied hour focus."), MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<string> baseOrder = WorkOrderService.GetOrder(pawn) ?? new List<string>();
            HashSet<string> eligible = new HashSet<string>(baseOrder, StringComparer.Ordinal);
            List<string> valid = new List<string>();
            int omitted = 0;
            foreach (string id in detailFocusClipboard)
            {
                WorkTypeDef type = DefDatabase<WorkTypeDef>.GetNamedSilentFail(id);
                if (!eligible.Contains(id) || type == null || WorkOrderService.IsIncapable(pawn, type))
                {
                    omitted++;
                    continue;
                }
                valid.Add(id);
            }

            if (valid.Count == 0)
            {
                Messages.Message(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.PasteCancelled",
                    "Nothing pasted: no copied WorkType is valid for this pawn."), MessageTypeDefOf.RejectInput, false);
                return;
            }

            SetFocus(detailFocusHour, valid);
            string message = OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.Pasted", "Hour pasted: ")
                + detailFocusHour.ToString("00") + ":00";
            if (omitted > 0)
            {
                message += " · " + OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.Omitted", "omitted: ") + omitted;
            }
            Messages.Message(message, MessageTypeDefOf.TaskCompletion, false);
        }

        private List<string> GetFocus(int hour)
        {
            PawnWorkOrder entry = DetailEntry();
            if (entry == null || entry.hourFocusOverrides == null)
            {
                return new List<string>();
            }
            PawnHourFocusOverride value = entry.hourFocusOverrides.FirstOrDefault(v => v != null && v.hour == hour);
            return value != null ? HourFocusStack.NormalizeFocus(value.workTypeDefNames) : new List<string>();
        }

        private void SetFocus(int hour, List<string> focus)
        {
            PawnWorkOrder entry = DetailEntry();
            if (entry == null)
            {
                return;
            }
            List<string> normalized = HourFocusStack.NormalizeFocus(focus);
            PawnHourFocusOverride value = entry.hourFocusOverrides.FirstOrDefault(v => v != null && v.hour == hour);
            if (normalized.Count == 0)
            {
                if (value != null)
                {
                    entry.hourFocusOverrides.Remove(value);
                }
                return;
            }
            if (value == null)
            {
                value = new PawnHourFocusOverride { hour = hour, source = PolicySource.Manual };
                entry.hourFocusOverrides.Add(value);
            }
            value.workTypeDefNames = normalized;
            entry.NormalizePolicy();
        }

        private PawnWorkOrder DetailEntry()
        {
            if (detailFocusEntry == null || detailFocusEntry.pawn != pawn)
            {
                OrderedWorkGameComponent component = OrderedWorkGameComponent.GetOrCreate();
                detailFocusEntry = component != null ? component.GetOrCreateEntry(pawn) : null;
            }
            return detailFocusEntry;
        }

        private string FocusTooltip(int hour, List<string> focus)
        {
            if (focus == null || focus.Count == 0)
            {
                return hour.ToString("00") + ":00 · —";
            }
            List<string> lines = new List<string> { hour.ToString("00") + ":00" };
            for (int i = 0; i < focus.Count; i++)
            {
                WorkTypeDef type = DefDatabase<WorkTypeDef>.GetNamedSilentFail(focus[i]);
                lines.Add((i + 1) + ". " + (type != null ? WorkTypePaletteView.DisplayName(type) : focus[i]));
            }
            return string.Join("\n", lines);
        }
    }
}