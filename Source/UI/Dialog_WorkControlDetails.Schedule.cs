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
    public partial class Dialog_WorkControlDetails
    {
        private SchedulePreset detailSchedule;
        private List<SchedulePreset> detailPresets;
        private TimeAssignmentDef detailBrush;
        private string detailScheduleName = "New schedule";

        private void DrawScheduleTab(Rect content)
        {
            EnsureDetailSchedule();
            float y = 0f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, content.width, 24f), "Horario");
            Text.Font = GameFont.Small;
            y += 28f;
            Widgets.Label(new Rect(0f, y, content.width, 20f),
                "Origen = timetable vanilla actual. Modif = preset editable. La tercera fila queda para prioridad de trabajo por horario (override, no preset).");
            y += 28f;

            float line = 24f;
            Rect presetRect = new Rect(0f, y, 150f, line);
            if (Widgets.ButtonText(presetRect, detailSchedule != null ? detailSchedule.Name : "Preset..."))
            {
                ShowPresetMenu();
            }
            if (Widgets.ButtonText(new Rect(154f, y, 82f, line), "Copy current"))
            {
                detailSchedule = ScheduleAdapter.CaptureCurrent(pawn, detailScheduleName);
                if (detailSchedule != null)
                {
                    detailSchedule.Name = detailScheduleName;
                }
            }
            detailScheduleName = Widgets.TextField(new Rect(240f, y, 170f, line), detailScheduleName ?? "");
            if (Widgets.ButtonText(new Rect(414f, y, 70f, line), "Save"))
            {
                SaveDetailSchedule();
            }
            if (Widgets.ButtonText(new Rect(488f, y, 110f, line), "Apply to pawn"))
            {
                if (SaveDetailSchedule())
                {
                    ScheduleAdapter.ApplyPreset(pawn, detailSchedule);
                }
            }
            y += line + 8f;

            Rect brushRect = new Rect(0f, y, 210f, line);
            DrawBrush(brushRect);
            y += line + 8f;

            float hourWidth = Mathf.Max(22f, (content.width - 76f) / SchedulePreset.SlotCount);
            DrawScheduleRow(content, y, hourWidth, "Origen", false, true);
            y += 42f;
            DrawScheduleRow(content, y, hourWidth, "Modif", true, false);
            y += 42f;
            DrawScheduleRow(content, y, hourWidth, "Prioridad", false, false);
            y += 42f;

            Widgets.Label(new Rect(76f, y, content.width - 76f, 20f),
                "Trabajo prioritario por horario: reservado para la capa de override por WorkType. No se guarda en el preset.");
        }

        private void EnsureDetailSchedule()
        {
            if (detailPresets == null)
            {
                detailPresets = SchedulePresetStore.LoadAll();
            }
            if (detailBrush == null)
            {
                detailBrush = DefDatabase<TimeAssignmentDef>.AllDefsListForReading
                    .OrderBy(d => d.LabelCap.ToString(), StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
            }
            if (detailSchedule == null)
            {
                detailSchedule = ScheduleAdapter.CaptureCurrent(pawn, detailScheduleName);
                if (detailSchedule == null)
                {
                    detailSchedule = new SchedulePreset { Name = detailScheduleName };
                }
            }
        }

        private void DrawBrush(Rect rect)
        {
            if (detailBrush == null)
            {
                Widgets.Label(rect, "No assignment defs");
                return;
            }
            Color color = ScheduleColor(detailBrush.defName);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y + 4f, 16f, 16f), color);
            if (Widgets.ButtonText(new Rect(rect.x + 20f, rect.y, rect.width - 20f, rect.height),
                detailBrush.LabelCap.ToString()))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (TimeAssignmentDef def in DefDatabase<TimeAssignmentDef>.AllDefsListForReading
                    .OrderBy(d => d.LabelCap.ToString(), StringComparer.OrdinalIgnoreCase))
                {
                    TimeAssignmentDef captured = def;
                    options.Add(new FloatMenuOption(captured.LabelCap.ToString(), delegate
                    {
                        detailBrush = captured;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            TooltipHandler.TipRegion(rect, "Active schedule brush. Click or drag over the Modif row to paint this assignment.");
        }

        private void DrawScheduleRow(Rect content, float y, float hourWidth, string label, bool editable, bool origin)
        {
            Widgets.Label(new Rect(0f, y, 72f, 24f), label);
            for (int hour = 0; hour < SchedulePreset.SlotCount; hour++)
            {
                Rect cell = new Rect(76f + hour * hourWidth, y, hourWidth - 1f, 30f);
                TimeAssignmentDef def = origin
                    ? (pawn != null && pawn.timetable != null ? pawn.timetable.GetAssignment(hour) : null)
                    : (editable ? ResolveDraft(hour) : null);
                if (editable && Mouse.IsOver(cell)
                    && (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag))
                {
                    detailSchedule.Slots[hour] = detailBrush != null ? detailBrush.defName : "";
                }
                Color color = def != null ? ScheduleColor(def.defName) : new Color(0.18f, 0.18f, 0.20f, 1f);
                if (origin)
                {
                    color = new Color(color.r, color.g, color.b, 0.55f);
                }
                Widgets.DrawBoxSolid(cell, color);
                if (def != null)
                {
                    string text = def.LabelCap.ToString();
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(cell, text);
                    Text.Anchor = TextAnchor.UpperLeft;
                }
                TooltipHandler.TipRegion(cell, def != null
                    ? def.LabelCap + "\n" + def.defName
                    : (editable ? "Click to paint " + (detailBrush != null ? detailBrush.LabelCap.ToString() : "—") : "—"));
            }
        }

        private TimeAssignmentDef ResolveDraft(int hour)
        {
            if (detailSchedule == null || detailSchedule.Slots == null || hour >= detailSchedule.Slots.Count)
            {
                return null;
            }
            return SchedulePolicyResolver.ResolveDef(detailSchedule.Slots[hour]);
        }

        private static Color ScheduleColor(string defName)
        {
            if (defName == "Work") return new Color(0.30f, 0.55f, 0.25f, 1f);
            if (defName == "Joy") return new Color(0.55f, 0.30f, 0.65f, 1f);
            if (defName == "Sleep") return new Color(0.25f, 0.35f, 0.70f, 1f);
            if (defName == "Meditate") return new Color(0.25f, 0.65f, 0.35f, 1f);
            if (defName == "Anything") return new Color(0.55f, 0.55f, 0.55f, 1f);
            return new Color(0.45f, 0.45f, 0.50f, 1f);
        }

        private void ShowPresetMenu()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            options.Add(new FloatMenuOption("New schedule", delegate
            {
                detailSchedule = new SchedulePreset { Name = detailScheduleName };
            }));
            foreach (SchedulePreset preset in detailPresets)
            {
                SchedulePreset captured = preset;
                options.Add(new FloatMenuOption(captured.Name, delegate
                {
                    detailSchedule = captured;
                    detailScheduleName = captured.Name;
                }));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private bool SaveDetailSchedule()
        {
            if (detailSchedule == null)
            {
                return false;
            }
            detailSchedule.Name = string.IsNullOrWhiteSpace(detailScheduleName)
                ? detailSchedule.Name
                : detailScheduleName;
            bool saved = SchedulePresetStore.Save(detailSchedule);
            if (saved)
            {
                detailPresets = SchedulePresetStore.LoadAll();
            }
            return saved;
        }
    }
}
