using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.Services;
using OrderedWork.UI.Layout;
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
            Widgets.Label(new Rect(0f, y, 100f, 24f), "Schedule");
            Text.Font = GameFont.Small;
            Rect infoRect = new Rect(104f, y + 2f, 18f, 20f);
            Widgets.Label(infoRect, "?");
            TooltipHandler.TipRegion(infoRect,
                "Origin: current vanilla timetable, read-only. Edit: editable preset; the brush paints hours. Priority: reserved for WorkType overrides.");
            y += 28f;

            float line = 24f;
            if (UiButtonBar.Button(new Rect(0f, y, 64f, line), "Load"))
            {
                Find.WindowStack.Add(new Dialog_SchedulePreset(true, null, delegate(SchedulePreset loaded)
                {
                    detailSchedule = loaded;
                    detailScheduleName = loaded.Name;
                }));
            }
            if (UiButtonBar.Button(new Rect(68f, y, 56f, line), "New"))
            {
                detailSchedule = new SchedulePreset { Name = "" };
                detailScheduleName = "";
            }
            if (UiButtonBar.Button(new Rect(128f, y, 100f, line), "Copy current"))
            {
                detailSchedule = ScheduleAdapter.CaptureCurrent(pawn, "");
                detailScheduleName = "";
            }
            if (UiButtonBar.Button(new Rect(232f, y, 64f, line), "Save"))
            {
                OpenSaveDialog();
            }
            bool hasChanges = HasScheduleChanges();
            if (UiButtonBar.Button(new Rect(300f, y, 116f, line), "Apply to pawn", hasChanges))
            {
                ScheduleAdapter.ApplyDraft(pawn, detailSchedule);
                Messages.Message("Schedule applied to " + pawn.LabelShort + ".",
                    MessageTypeDefOf.TaskCompletion, false);
            }
            if (Mouse.IsOver(new Rect(300f, y, 116f, line)) && !hasChanges)
            {
                TooltipHandler.TipRegion(new Rect(300f, y, 116f, line),
                    "Apply is enabled only when a non-empty hour differs from the current timetable.");
            }
            y += line + 8f;

            Rect brushRect = new Rect(0f, y, 210f, line);
            DrawBrush(brushRect);
            Widgets.LabelEllipses(new Rect(brushRect.xMax + 8f, y, content.width - brushRect.width - 8f, line),
                "Left click: paint · Right click: clear");
            y += line + 8f;

            float hourWidth = Mathf.Max(22f, (content.width - 76f) / SchedulePreset.SlotCount);
            Widgets.Label(new Rect(0f, y, 72f, 18f), "Hour");
            for (int hour = 0; hour < SchedulePreset.SlotCount; hour++)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(new Rect(76f + hour * hourWidth, y, hourWidth, 18f), hour.ToString("00"));
                Text.Anchor = TextAnchor.UpperLeft;
            }
            y += 20f;
            DrawScheduleRow(content, y, hourWidth, "Origin", false, true);
            y += 42f;
            DrawScheduleRow(content, y, hourWidth, "Edit", true, false);
            y += 42f;
            DrawScheduleRow(content, y, hourWidth, "Priority", false, false);
            y += 42f;

            Widgets.Label(new Rect(76f, y, content.width - 76f, 20f),
                "Hourly priority: reserved for WorkType overrides; it is not stored in the preset.");
        }

        private void OpenSaveDialog()
        {
            if (detailSchedule == null)
            {
                return;
            }
            string initialName = string.IsNullOrWhiteSpace(detailScheduleName) ? pawn.LabelShort : detailScheduleName;
            Find.WindowStack.Add(new Dialog_SchedulePreset(false,
                delegate { return detailSchedule; }, null, initialName));
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
                    detailSchedule = new SchedulePreset { Name = "" };
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
            Color color = AssignmentColor(detailBrush);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y + 4f, 16f, 16f), color);
            if (Widgets.ButtonText(new Rect(rect.x + 20f, rect.y, rect.width - 20f, rect.height), ShortLabel(detailBrush)))
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
            TooltipHandler.TipRegion(rect, "Active schedule brush. Click or drag over Modif to paint this assignment.");
        }

        private void ShowBrushMenu()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (TimeAssignmentDef def in DefDatabase<TimeAssignmentDef>.AllDefsListForReading
                .OrderBy(d => d.LabelCap.ToString(), StringComparer.OrdinalIgnoreCase))
            {
                TimeAssignmentDef captured = def;
                options.Add(new FloatMenuOption(captured.LabelCap.ToString(), delegate { detailBrush = captured; }));
            }
            Find.WindowStack.Add(new FloatMenu(options));
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
                    if (Event.current.button == 1)
                    {
                        detailSchedule.Slots[hour] = "";
                    }
                    else if (Event.current.button == 0)
                    {
                        detailSchedule.Slots[hour] = detailBrush != null ? detailBrush.defName : "";
                    }
                    Event.current.Use();
                }
                Color color = def != null ? AssignmentColor(def) : new Color(0.18f, 0.18f, 0.20f, 1f);
                if ((hour & 1) == 1)
                {
                    color = new Color(color.r * 0.82f, color.g * 0.82f, color.b * 0.82f, color.a);
                }
                if (origin)
                {
                    color = new Color(color.r, color.g, color.b, 0.55f);
                }
                Widgets.DrawBoxSolid(cell, color);
                if (def != null)
                {
                    string text = ShortLabel(def, hourWidth);
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(cell, text);
                    Text.Anchor = TextAnchor.UpperLeft;
                }
                TooltipHandler.TipRegion(cell, def != null
                    ? def.LabelCap + "\n" + def.defName
                    : (editable ? "Click to paint " + (detailBrush != null ? detailBrush.LabelCap.ToString() : "—") : "—"));
            }
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

        private TimeAssignmentDef ResolveDraft(int hour)
        {
            if (detailSchedule == null || detailSchedule.Slots == null || hour >= detailSchedule.Slots.Count)
            {
                return null;
            }
            return SchedulePolicyResolver.ResolveDef(detailSchedule.Slots[hour]);
        }

        private static string ShortLabel(TimeAssignmentDef def)
        {
            return ShortLabel(def, 999f);
        }

        private static string ShortLabel(TimeAssignmentDef def, float width)
        {
            if (def == null)
            {
                return "—";
            }
            string name = def.defName;
            if (width < 36f)
            {
                if (name == "Work") return "W";
                if (name == "Joy") return "J";
                if (name == "Sleep") return "S";
                if (name == "Meditate") return "M";
                if (name == "Anything") return "A";
                return "·";
            }
            if (name == "Anything") return "Any";
            if (name == "Meditate") return "Med";
            if (name == "Work") return "Work";
            if (name == "Joy") return "Joy";
            if (name == "Sleep") return width < 54f ? "Slp" : "Sleep";
            string label = def.LabelCap.ToString();
            return label.Length <= 5 ? label : label.Substring(0, 5);
        }

        private static Color AssignmentColor(TimeAssignmentDef def)
        {
            if (def == null)
            {
                return new Color(0.18f, 0.18f, 0.20f, 1f);
            }
            Color color = def.color;
            if (color.a <= 0.01f || (color.r <= 0.01f && color.g <= 0.01f && color.b <= 0.01f))
            {
                int hash = 0;
                string name = def.defName ?? def.label ?? "unknown";
                for (int i = 0; i < name.Length; i++)
                {
                    hash = (hash * 31 + name[i]) & 0x7fffffff;
                }
                color = Color.HSVToRGB((hash % 360) / 360f, 0.55f, 0.75f);
            }
            color.a = 1f;
            return color;
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