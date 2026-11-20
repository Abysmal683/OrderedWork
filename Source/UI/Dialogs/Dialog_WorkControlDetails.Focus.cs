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
        private PawnWorkOrder? detailFocusEntry;

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
            Map? map = pawn != null ? pawn.Map : null;
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
                    Widgets.Label(cell, focus.Count.ToString());
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.Font = GameFont.Small;
                }

                if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && Mouse.IsOver(cell))
                {
                    ClearFocusHour(hour);
                    Event.current.Use();
                    continue;
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
                    delegate
                    {
                        Messages.Message(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.OrderChanged", "Focus order updated: ")
                            + detailFocusHour.ToString("00") + ":00", MessageTypeDefOf.TaskCompletion, false);
                    });
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
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.SavePreset", "Save preset")))
            {
                Find.WindowStack.Add(new Dialog_FocusPreset(false, BuildFocusPreset, null,
                    pawn != null ? pawn.LabelShort : null));
            }
            if (UiButtonBar.Button(new Rect(138f, y, 132f, 24f),
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.LoadPresetButton", "Load preset")))
            {
                Find.WindowStack.Add(new Dialog_FocusPreset(true, null, ApplyFocusPreset));
            }
            PawnWorkOrder? focusEntry = DetailEntry();
            bool canClearAll = focusEntry != null && focusEntry.hourFocusOverrides.Count > 0;
            if (UiButtonBar.Button(new Rect(276f, y, 132f, 24f),
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.ScheduleFocus.ClearAll", "Clear all"), canClearAll))
            {
                ClearAllFocus();
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
            SkillRecord? skill = WorkTypePaletteView.SafeBestSkill(pawn, type, out passion);
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
            Rect viewport = new Rect(0f, y, content.width, 42f);
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

    }
}
