using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.Core;
using OrderedWork.Data;
using OrderedWork.Services;
using RimWorld;
using Verse;

namespace OrderedWork.UI
{
    public partial class Dialog_WorkControlDetails
    {        private WorkFocusPreset BuildFocusPreset()
        {
            WorkFocusPreset preset = new WorkFocusPreset
            {
                Name = pawn != null ? pawn.LabelShort : "focus"
            };
            for (int hour = 0; hour < SchedulePreset.SlotCount; hour++)
            {
                preset.Slots[hour] = new List<string>(GetFocus(hour));
            }
            preset.Normalize();
            return preset;
        }

        private void ApplyFocusPreset(WorkFocusPreset preset)
        {
            PawnWorkOrder entry = DetailEntry();
            if (entry == null || preset == null)
            {
                return;
            }

            List<string> baseOrder = WorkOrderService.GetOrder(pawn) ?? new List<string>();
            HashSet<string> eligible = new HashSet<string>(baseOrder, StringComparer.Ordinal);
            List<PawnHourFocusOverride> next = new List<PawnHourFocusOverride>();
            int omitted = 0;
            int total = 0;
            for (int hour = 0; hour < SchedulePreset.SlotCount; hour++)
            {
                List<string> valid = new List<string>();
                foreach (string id in preset.FocusAt(hour))
                {
                    WorkTypeDef type = DefDatabase<WorkTypeDef>.GetNamedSilentFail(id);
                    if (!eligible.Contains(id) || type == null || WorkOrderService.IsIncapable(pawn, type))
                    {
                        omitted++;
                        continue;
                    }
                    valid.Add(id);
                }
                valid = HourFocusStack.NormalizeFocus(valid);
                if (valid.Count > 0)
                {
                    next.Add(new PawnHourFocusOverride
                    {
                        hour = hour,
                        workTypeDefNames = valid,
                        source = PolicySource.Manual
                    });
                    total += valid.Count;
                }
            }

            if (total == 0)
            {
                Messages.Message(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.LoadCancelled",
                    "Focus preset not loaded: no entry is valid for this pawn."), MessageTypeDefOf.RejectInput, false);
                return;
            }

            entry.hourFocusOverrides.Clear();
            entry.hourFocusOverrides.AddRange(next);
            entry.NormalizePolicy();
            detailFocusSelected = -1;
            string message = OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.PresetLoaded", "Focus preset loaded: ")
                + preset.Name;
            if (omitted > 0)
            {
                message += " · " + OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.Omitted", "omitted: ") + omitted;
            }
            Messages.Message(message, MessageTypeDefOf.TaskCompletion, false);
        }
    }
}
