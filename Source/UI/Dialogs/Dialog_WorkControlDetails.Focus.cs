using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.Core;
using OrderedWork.Data;
using OrderedWork.Services;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    public partial class Dialog_WorkControlDetails
    {
        private int detailFocusHour;
        private int detailFocusSelected = -1;
        private bool detailFocusInitialized;
        private Vector2 detailFocusVisibleScroll;
        private List<string> detailFocusClipboard = new List<string>();
        private readonly UiDragList detailFocusRows = new UiDragList();
        private PawnWorkOrder detailFocusEntry;

        private void ResetFocusEditor()
        {
            detailFocusHour = 0;
            detailFocusSelected = -1;
            detailFocusInitialized = false;
            detailFocusVisibleScroll = Vector2.zero;
            detailFocusEntry = null;
        }

        private void EnsureFocusEditor()
        {
            if (detailFocusInitialized)
            {
                return;
            }
            detailFocusInitialized = true;
            Map map = pawn != null ? pawn.Map : null;
            detailFocusHour = map != null ? GenLocalDate.HourOfDay(map) : 0;
            detailFocusSelected = -1;
        }

        private void DrawFocusSection(Rect content, float y, float hourWidth)
        {
            EnsureFocusEditor();
            DrawFocusHourRow(content, y, hourWidth);
            y += 38f;
            DrawFocusEditor(content, y);
        }

        private void DrawFocusHourRow(Rect content, float y, float hourWidth)
        {
            Widgets.Label(new Rect(0f, y, 72f, 30f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.Row", "Focus"));
            for (int hour = 0; hour < SchedulePreset.SlotCount; hour++)
            {
                Rect cell = new Rect(76f + hour * hourWidth, y, hourWidth - 1f, 30f);
                List<string> focus = GetFocus(hour);
                Color color = focus.Count > 0 && !string.IsNullOrEmpty(focus[0])
                    ? WorkTypeColorRegistry.HashColor(focus[0])
                    : UiColors.EmptyColumn;
                if (hour == detailFocusHour)
                {
                    color = new Color(color.r * 1.25f, color.g * 1.25f, color.b * 1.25f, 1f);
                }
                Widgets.DrawBoxSolid(cell, color);
                if (hour == detailFocusHour)
                {
                    Widgets.DrawBoxSolid(new Rect(cell.x, cell.y, cell.width, 2f), new Color(1f, 0.85f, 0.35f, 1f));
                    Widgets.DrawBoxSolid(new Rect(cell.x, cell.yMax - 2f, cell.width, 2f), new Color(1f, 0.85f, 0.35f, 1f));
                }

                if (focus.Count > 0)
                {
                    Text.Font = GameFont.Tiny;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    string first = WorkTypePaletteView.DisplayName(DefDatabase<WorkTypeDef>.GetNamedSilentFail(focus[0]));
                    bool tight = hourWidth < 38f;
                    string label = tight
                        ? focus.Count.ToString()
                        : "1" + (string.IsNullOrEmpty(first) ? "" : first.Substring(0, Mathf.Min(4, first.Length)));
                    Widgets.Label(cell, label);
                    if (!tight && focus.Count > 1)
                    {
                        Widgets.Label(new Rect(cell.x, cell.y + 13f, cell.width, 12f), "+" + (focus.Count - 1));
                    }
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.Font = GameFont.Small;
                }

                if (Widgets.ButtonInvisible(cell))
                {
                    detailFocusHour = hour;
                    detailFocusSelected = -1;
                }
                TooltipHandler.TipRegion(cell, FocusTooltip(hour, focus));
            }
        }

        private void DrawFocusEditor(Rect content, float y)
        {
            float width = content.width;
            List<string> focus = GetFocus(detailFocusHour);
            if (detailFocusSelected >= focus.Count)
            {
                detailFocusSelected = -1;
            }

            Rect header = new Rect(0f, y, width, 26f);
            Widgets.Label(new Rect(header.x, header.y, 180f, header.height),
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.Title", "Focus") + " "
                + detailFocusHour.ToString("00") + ":00");
            if (UiButtonBar.Button(new Rect(header.xMax - 278f, header.y, 28f, header.height), "<"))
            {
                StepFocusHour(-1);
            }
            if (UiButtonBar.Button(new Rect(header.xMax - 246f, header.y, 28f, header.height), ">"))
            {
                StepFocusHour(1);
            }
            if (UiButtonBar.Button(new Rect(header.xMax - 210f, header.y, 96f, header.height),
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.CopyHour", "Copy hour")))
            {
                CopyFocusHour();
            }
            if (UiButtonBar.Button(new Rect(header.xMax - 110f, header.y, 96f, header.height),
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.PasteHour", "Paste hour")))
            {
                PasteFocusHour();
            }
            y += 32f;

            if (focus.Count == 0)
            {
                Widgets.Label(new Rect(0f, y, width, 24f),
                    OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.Empty", "No focus for this hour."));
                y += 26f;
            }
            else
            {
                Rect rowsRect = new Rect(0f, y, width, Mathf.Min(5f, focus.Count) * UiDragList.RowHeight);
                detailFocusRows.Draw(
                    rowsRect,
                    focus.Count,
                    delegate(int index, Rect row) { DrawFocusRow(row, index, focus[index]); },
                    delegate(int index, Rect row) { return new Rect(row.x, row.y, row.width - 70f, row.height); },
                    delegate(int index) { detailFocusSelected = index; },
                    delegate(int from, int to) { MoveFocusEntry(from, to); },
                    null);
                y += rowsRect.height + 4f;
            }

            if (focus.Count < HourFocusStack.MaxFocus)
            {
                if (UiButtonBar.Button(new Rect(0f, y, 150f, 24f),
                    OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.Add", "+ Add work")))
                {
                    OpenFocusPicker(-1);
                }
                y += 28f;
            }
            else
            {
                Widgets.Label(new Rect(0f, y, width, 22f),
                    OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.Max", "Maximum 3 focus WorkTypes."));
                y += 26f;
            }

            Widgets.Label(new Rect(0f, y, width, 20f),
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.RightClick", "Right click a row to remove its focus."));
            y += 22f;

            if (UiButtonBar.Button(new Rect(0f, y, 132f, 24f),
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.SavePreset", "Save focus preset")))
            {
                Find.WindowStack.Add(new Dialog_FocusPreset(false, BuildFocusPreset, null,
                    pawn != null ? pawn.LabelShort : null));
            }
            if (UiButtonBar.Button(new Rect(138f, y, 132f, 24f),
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.LoadPresetButton", "Load focus preset")))
            {
                Find.WindowStack.Add(new Dialog_FocusPreset(true, null, ApplyFocusPreset));
            }
            y += 30f;

            Widgets.Label(new Rect(0f, y, width, 20f),
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.Visible", "Visible: how this hour reads"));
            y += 20f;
            DrawVisibleFocusStrip(content, y);
        }

        private void DrawFocusRow(Rect row, int index, string defName)
        {
            if (index == detailFocusSelected)
            {
                Widgets.DrawBoxSolid(row, new Color(0.30f, 0.38f, 0.48f, 0.55f));
            }
            else
            {
                UiZebra.Draw(row, index);
            }

            WorkTypeDef type = DefDatabase<WorkTypeDef>.GetNamedSilentFail(defName);
            Passion passion;
            SkillRecord skill = WorkTypePaletteView.SafeBestSkill(pawn, type, out passion);
            Color color = WorkTypeColorRegistry.GetColor(type);
            Widgets.DrawBoxSolid(new Rect(row.x, row.y, 4f, row.height), color);

            Widgets.Label(new Rect(row.x + 8f, row.y, 18f, row.height), (index + 1).ToString());
            Widgets.LabelEllipses(new Rect(row.x + 26f, row.y, Mathf.Max(40f, row.width - 190f), row.height),
                type != null ? WorkTypePaletteView.DisplayName(type) : defName);
            Rect metaRect = new Rect(row.x + row.width - 162f, row.y, 86f, row.height);
            WorkTypePaletteView.DrawPassionIcon(new Rect(metaRect.x, row.y + 6f, 12f, 12f), passion);
            Widgets.Label(new Rect(metaRect.x + 15f, row.y, 44f, row.height), "N" + (skill != null ? skill.Level.ToString() : "—"));
            TooltipHandler.TipRegion(metaRect,
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.Passion", "Passion")
                + ": " + WorkTypePaletteView.PassionText(passion) + "\n"
                + OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.Level", "Level")
                + ": " + (skill != null ? skill.Level.ToString() : "—"));
            if (Widgets.ButtonText(new Rect(row.xMax - 66f, row.y + 2f, 64f, row.height - 4f),
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.Change", "Change")))
            {
                detailFocusSelected = index;
                OpenFocusPicker(index);
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && Mouse.IsOver(row))
            {
                RemoveFocusEntry(index);
                Event.current.Use();
            }
        }

        private WorkFocusPreset BuildFocusPreset()
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
        private void DrawVisibleFocusStrip(Rect content, float y)
        {
            List<string> baseOrder = WorkOrderService.GetOrder(pawn) ?? new List<string>();
            List<string> focus = GetFocus(detailFocusHour);
            List<string> effective = HourFocusStack.BuildEffectiveOrder(
                baseOrder,
                focus,
                delegate(string id)
                {
                    WorkTypeDef type = DefDatabase<WorkTypeDef>.GetNamedSilentFail(id);
                    return type != null && !WorkOrderService.IsIncapable(pawn, type);
                });

            if (effective.Count == 0)
            {
                Widgets.Label(new Rect(0f, y, content.width, 24f), "—");
                return;
            }

            const float gap = 4f;
            float chipWidth = Mathf.Max(80f, (content.width - gap * 4f) / 5f);
            float total = effective.Count * (chipWidth + gap);
            Rect viewport = new Rect(0f, y, content.width, 26f);
            Rect view = new Rect(0f, 0f, total, 26f);
            Widgets.BeginScrollView(viewport, ref detailFocusVisibleScroll, view);
            try
            {
                for (int i = 0; i < effective.Count; i++)
                {
                    string id = effective[i];
                    WorkTypeDef type = DefDatabase<WorkTypeDef>.GetNamedSilentFail(id);
                    Rect chip = new Rect(i * (chipWidth + gap), 0f, chipWidth, 24f);
                    Color color = type != null ? WorkTypeColorRegistry.GetColor(type) : UiColors.EmptyColumn;
                    Widgets.DrawBoxSolid(chip, new Color(color.r, color.g, color.b, 0.80f));
                    string name = type != null ? WorkTypePaletteView.DisplayName(type) : id;
                    Widgets.LabelEllipses(new Rect(chip.x + 5f, chip.y, chip.width - 10f, chip.height), (i + 1) + " " + name);
                    TooltipHandler.TipRegion(chip, (i + 1) + ". " + name);
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
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
            focus.RemoveAt(index);
            SetFocus(detailFocusHour, focus);
            detailFocusSelected = -1;
        }

        private void CopyFocusHour()
        {
            detailFocusClipboard = new List<string>(GetFocus(detailFocusHour));
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
