using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    public partial class MainTabWindow_WorkControl
    {
        private enum WorkView
        {
            Work,
            Schedule
        }

        private WorkView workView;
        private List<SchedulePreset> schedulePresets;
        private SchedulePreset draftSchedule;
        private string scheduleName = "New schedule";
        private Vector2 scheduleScroll;

        private void DrawViewTabs(float titleY, Rect inRect)
        {
            Rect workRect = new Rect(Pad + 132f, titleY + 2f, 74f, 24f);
            Rect scheduleRect = new Rect(workRect.xMax + 4f, titleY + 2f, 86f, 24f);
            if (Widgets.ButtonText(workRect, "Work") && workView != WorkView.Work)
            {
                workView = WorkView.Work;
            }
            if (Widgets.ButtonText(scheduleRect, "Schedule") && workView != WorkView.Schedule)
            {
                workView = WorkView.Schedule;
                EnsureScheduleState();
            }
        }

        private void DrawScheduleView(Rect inRect)
        {
            EnsureScheduleState();
            List<Pawn> selected = GetSelectedColonists();
            Pawn previewPawn = selected.FirstOrDefault() ?? GetColonistsInDisplayOrder().FirstOrDefault();

            float top = Pad + TitleHeight + 10f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(Pad, top, inRect.width - Pad * 2f, 26f), "Schedule presets");
            Text.Font = GameFont.Small;
            top += 28f;
            Widgets.Label(new Rect(Pad, top, inRect.width - Pad * 2f, 22f),
                "Global 24-slot timetable presets. Select a preset, preview it against the current pawn, then apply to selected pawns.");
            top += 26f;

            float leftWidth = Mathf.Clamp(inRect.width * 0.30f, 240f, 360f);
            float leftX = Pad;
            float rightX = leftX + leftWidth + 8f;
            float panelHeight = Mathf.Max(220f, inRect.height - top - Pad);
            Rect left = new Rect(leftX, top, leftWidth, panelHeight);
            Rect right = new Rect(rightX, top, inRect.width - rightX - Pad, panelHeight);

            DrawSchedulePresetPanel(left, selected);
            DrawScheduleEditor(right, previewPawn, selected);
        }

        private void EnsureScheduleState()
        {
            if (schedulePresets == null)
            {
                schedulePresets = SchedulePresetStore.LoadAll();
            }
            if (draftSchedule == null)
            {
                draftSchedule = new SchedulePreset { Name = scheduleName };
            }
        }

        private void DrawSchedulePresetPanel(Rect rect, List<Pawn> selected)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.10f, 0.10f, 0.12f, 0.45f));
            Rect inner = rect.ContractedBy(6f);

            if (Widgets.ButtonText(new Rect(inner.x, inner.y, 86f, 24f), "New"))
            {
                draftSchedule = new SchedulePreset { Name = scheduleName };
            }
            if (Widgets.ButtonText(new Rect(inner.x + 90f, inner.y, 86f, 26f), "Capture"))
            {
                Pawn pawn = selected.FirstOrDefault();
                if (pawn == null)
                {
                    Messages.Message("Select a colonist to capture their schedule.", MessageTypeDefOf.RejectInput, false);
                }
                else
                {
                    SchedulePreset captured = ScheduleAdapter.CaptureCurrent(pawn, scheduleName);
                    if (captured != null)
                    {
                        draftSchedule = captured;
                    }
                }
            }
            Rect nameRect = new Rect(inner.x, inner.y + 30f, inner.width, 24f);
            scheduleName = Widgets.TextField(nameRect, scheduleName ?? "");
            if (draftSchedule != null)
            {
                draftSchedule.Name = scheduleName;
            }

            float y = inner.y + 62f;
            float listHeight = Mathf.Max(80f, inner.height - 100f);
            Rect listRect = new Rect(inner.x, y, inner.width, listHeight);
            float rowHeight = 26f;
            Rect content = new Rect(0f, 0f, listRect.width - 18f, Mathf.Max(listRect.height, schedulePresets.Count * rowHeight));
            scheduleScroll.y = Mathf.Clamp(scheduleScroll.y, 0f, Mathf.Max(0f, content.height - listRect.height));
            Widgets.BeginScrollView(listRect, ref scheduleScroll, content);
            for (int i = 0; i < schedulePresets.Count; i++)
            {
                SchedulePreset preset = schedulePresets[i];
                Rect row = new Rect(0f, i * rowHeight, content.width, rowHeight - 2f);
                if (Widgets.ButtonText(row, preset.Name))
                {
                    draftSchedule = preset;
                    scheduleName = preset.Name;
                }
            }
            Widgets.EndScrollView();

            if (Widgets.ButtonText(new Rect(inner.x, inner.yMax - 28f, 100f, 24f), "Save preset", true, true, draftSchedule != null))
            {
                SaveDraftSchedule();
            }
            if (Widgets.ButtonText(new Rect(inner.x + 104f, inner.yMax - 28f, 130f, 24f), "Apply to selected", true, true, draftSchedule != null && selected.Count > 0))
            {
                SaveDraftSchedule();
                for (int i = 0; i < selected.Count; i++)
                {
                    ScheduleAdapter.ApplyPreset(selected[i], draftSchedule);
                }
            }
        }

        private void SaveDraftSchedule()
        {
            if (draftSchedule == null)
            {
                return;
            }
            draftSchedule.Name = string.IsNullOrWhiteSpace(scheduleName) ? draftSchedule.Name : scheduleName;
            if (SchedulePresetStore.Save(draftSchedule))
            {
                schedulePresets = SchedulePresetStore.LoadAll();
                Messages.Message("Saved schedule preset: " + draftSchedule.Name, MessageTypeDefOf.TaskCompletion, false);
            }
            else
            {
                Messages.Message("Could not save the schedule preset. Check Player.log.", MessageTypeDefOf.RejectInput, false);
            }
        }

        private void DrawScheduleEditor(Rect rect, Pawn pawn, List<Pawn> selected)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.10f, 0.10f, 0.12f, 0.45f));
            Rect inner = rect.ContractedBy(6f);
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, 22f),
                pawn == null ? "No colonist selected for preview." : "Preview: " + pawn.LabelShort);
            Rect header = new Rect(inner.x, inner.y + 24f, inner.width, 20f);
            Widgets.Label(new Rect(header.x, header.y, 48f, 20f), "Hour");
            Widgets.Label(new Rect(header.x + 52f, header.y, (header.width - 56f) * 0.5f, 20f), "Current vanilla");
            Widgets.Label(new Rect(header.x + 52f + (header.width - 56f) * 0.5f, header.y, (header.width - 56f) * 0.5f, 20f), "Draft");
            Rect rowsRect = new Rect(inner.x, header.yMax + 2f, inner.width, inner.height - 46f);
            float rowHeight = 24f;
            Rect content = new Rect(0f, 0f, rowsRect.width - 18f, SchedulePreset.SlotCount * rowHeight);
            scheduleScroll.y = Mathf.Clamp(scheduleScroll.y, 0f, Mathf.Max(0f, content.height - rowsRect.height));
            Widgets.BeginScrollView(rowsRect, ref scheduleScroll, content);
            for (int hour = 0; hour < SchedulePreset.SlotCount; hour++)
            {
                Rect row = new Rect(0f, hour * rowHeight, content.width, rowHeight - 1f);
                Widgets.Label(new Rect(row.x, row.y, 44f, row.height), hour.ToString("00"));
                TimeAssignmentDef current = pawn != null && pawn.timetable != null ? pawn.timetable.GetAssignment(hour) : null;
                Color previous = GUI.color;
                GUI.color = new Color(0.70f, 0.70f, 0.70f, 1f);
                Widgets.Label(new Rect(row.x + 46f, row.y, (row.width - 50f) * 0.5f, row.height),
                    current != null ? current.LabelCap.ToString() : "—");
                GUI.color = previous;
                Rect draftRect = new Rect(row.x + 46f + (row.width - 50f) * 0.5f, row.y,
                    (row.width - 50f) * 0.5f - 2f, row.height - 2f);
                string draftLabel = DraftLabel(hour);
                if (Widgets.ButtonText(draftRect, draftLabel))
                {
                    ShowAssignmentMenu(hour);
                }
            }
            Widgets.EndScrollView();
        }

        private string DraftLabel(int hour)
        {
            if (draftSchedule == null || draftSchedule.Slots == null || hour >= draftSchedule.Slots.Count)
            {
                return "—";
            }
            TimeAssignmentDef def = SchedulePolicyResolver.ResolveDef(draftSchedule.Slots[hour]);
            return def != null ? def.LabelCap.ToString() : "—";
        }

        private void ShowAssignmentMenu(int hour)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (TimeAssignmentDef def in DefDatabase<TimeAssignmentDef>.AllDefsListForReading
                .OrderBy(d => d.LabelCap.ToString(), StringComparer.OrdinalIgnoreCase))
            {
                TimeAssignmentDef captured = def;
                options.Add(new FloatMenuOption(captured.LabelCap.ToString(), delegate
                {
                    if (draftSchedule == null)
                    {
                        draftSchedule = new SchedulePreset { Name = scheduleName };
                    }
                    draftSchedule.Slots[hour] = captured.defName;
                }));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}
