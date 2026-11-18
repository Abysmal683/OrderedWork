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
        private string detailScheduleName = "";

        private void DrawScheduleTab(Rect content)
        {
            EnsureDetailSchedule();
            float y = 0f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, 100f, 24f), "Horario");
            Text.Font = GameFont.Small;
            Rect infoRect = new Rect(104f, y + 2f, 18f, 20f);
            Widgets.Label(infoRect, "?");
            TooltipHandler.TipRegion(infoRect,
                "Origen: timetable vanilla actual, solo lectura. Modif: preset editable; el brush pinta horas. Prioridad: reservado para override por WorkType.");
            y += 28f;

            float line = 24f;
            Widgets.Label(new Rect(0f, y, 42f, line), "Load:");
            string loadedName = string.IsNullOrWhiteSpace(detailScheduleName)
                ? "(none)"
                : detailScheduleName;
            if (Widgets.ButtonText(new Rect(44f, y, 150f, line), loadedName))
            {
                ShowPresetMenu();
            }
            if (Widgets.ButtonText(new Rect(198f, y, 56f, line), "New"))
            {
                detailSchedule = new SchedulePreset { Name = "" };
                detailScheduleName = "";
            }
            if (Widgets.ButtonText(new Rect(258f, y, 100f, line), "Copy current"))
            {
                detailSchedule = ScheduleAdapter.CaptureCurrent(pawn, detailScheduleName);
                if (detailSchedule != null)
                {
                    detailSchedule.Name = "";
                }
                detailScheduleName = "";
            }
            y += line + 4f;

            Widgets.Label(new Rect(0f, y, 48f, line), "Name:");
            detailScheduleName = Widgets.TextField(new Rect(50f, y, 220f, line), detailScheduleName ?? "");
            bool hasValidName = !string.IsNullOrWhiteSpace(detailScheduleName);
            bool hasChanges = HasScheduleChanges();
            if (Widgets.ButtonText(new Rect(276f, y, 64f, line), "Save"))
            {
                OpenSaveDialog();
            }
            if (DrawActionButton(new Rect(344f, y, 116f, line), "Apply to pawn", hasChanges))
            {
                if (hasValidName && SaveDetailSchedule())
                {
                    ScheduleAdapter.ApplyPreset(pawn, detailSchedule);
                }
                else
                {
                    ScheduleAdapter.ApplyDraft(pawn, detailSchedule);
                }
            }
            if (Mouse.IsOver(new Rect(344f, y, 116f, line)) && !hasChanges)
            {
                TooltipHandler.TipRegion(new Rect(344f, y, 116f, line),
                    "Apply is enabled only when the edited timetable differs from the pawn's current timetable.");
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
                "Prioridad por hora: reservada para el override de WorkType; no se guarda en el preset.");
        }

        private void OpenSaveDialog()
        {
            if (detailSchedule == null)
            {
                return;
            }
            Find.WindowStack.Add(new Dialog_ScheduleSave(detailSchedule, delegate(SchedulePreset saved)
            {
                detailPresets = SchedulePresetStore.LoadAll();
                detailSchedule = saved;
                detailScheduleName = saved.Name;
            }));
        }

        private static bool DrawActionButton(Rect rect, string label, bool active)
        {
            Color previous = GUI.color;
            if (!active)
            {
                GUI.color = new Color(0.55f, 0.55f, 0.55f, 0.7f);
            }
            bool clicked = Widgets.ButtonText(rect, label, true, true, active);
            GUI.color = previous;
            return clicked;
        }
        private bool HasScheduleChanges()
        {
            if (detailSchedule == null || detailSchedule.Slots == null || pawn == null || pawn.timetable == null)
            {
                return false;
            }
            for (int hour = 0; hour < SchedulePreset.SlotCount; hour++)
            {
                string draft = detailSchedule.Slots[hour];
                TimeAssignmentDef current = pawn.timetable.GetAssignment(hour);
                string currentName = current != null ? current.defName : "";
                if (!string.IsNullOrEmpty(draft) && draft != currentName)
                {
                    return true;
                }
            }
            return false;
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
            Color color = detailBrush.color;
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y + 4f, 16f, 16f), color);
            if (Widgets.ButtonText(new Rect(rect.x + 20f, rect.y, rect.width - 20f, rect.height),
                ShortLabel(detailBrush)))
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
                Color color = def != null ? def.color : new Color(0.18f, 0.18f, 0.20f, 1f);
                if (origin)
                {
                    color = new Color(color.r, color.g, color.b, 0.55f);
                }
                Widgets.DrawBoxSolid(cell, color);
                if (def != null)
                {
                    string text = ShortLabel(def);
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(cell, text);
                    Text.Anchor = TextAnchor.UpperLeft;
                }
                TooltipHandler.TipRegion(cell, def != null
                    ? def.LabelCap + "\n" + def.defName
                    : (editable ? "Click to paint " + (detailBrush != null ? detailBrush.LabelCap.ToString() : "—") : "—"));
            }
        }

        private static string ShortLabel(TimeAssignmentDef def)
        {
            if (def == null)
            {
                return "—";
            }
            string name = def.defName;
            if (name == "Anything") return "Any";
            if (name == "Meditate") return "Med";
            if (name == "Work") return "Work";
            if (name == "Joy") return "Joy";
            if (name == "Sleep") return "Sleep";
            string label = def.LabelCap.ToString();
            return label.Length <= 5 ? label : label.Substring(0, 5);
        }
        private TimeAssignmentDef ResolveDraft(int hour)
        {
            if (detailSchedule == null || detailSchedule.Slots == null || hour >= detailSchedule.Slots.Count)
            {
                return null;
            }
            return SchedulePolicyResolver.ResolveDef(detailSchedule.Slots[hour]);
        }

        private void ShowPresetMenu()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
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
            if (detailSchedule == null || string.IsNullOrWhiteSpace(detailScheduleName))
            {
                return false;
            }
            detailSchedule.Name = detailScheduleName.Trim();
            bool saved = SchedulePresetStore.Save(detailSchedule);
            if (saved)
            {
                detailPresets = SchedulePresetStore.LoadAll();
            }
            return saved;
        }
    }
}
