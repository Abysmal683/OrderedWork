using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Schedule preset picker/save dialog. The layout follows the vanilla save/load
    /// pattern: search, list, overwrite/delete actions, name field and one primary button.
    /// </summary>
    public class Dialog_ScheduleSave : Window
    {
        private readonly SchedulePreset draft;
        private readonly Action<SchedulePreset> onSaved;
        private List<SchedulePreset> allPresets;
        private List<SchedulePreset> visiblePresets;
        private string presetName;
        private string search = "";
        private int selectedIndex = -1;
        private SchedulePreset pendingDelete;
        private Vector2 scroll;

        public Dialog_ScheduleSave(SchedulePreset draft, string initialName, Action<SchedulePreset> onSaved)
        {
            this.draft = draft;
            this.onSaved = onSaved;
            presetName = initialName ?? "";
            allPresets = SchedulePresetStore.LoadAll();
            visiblePresets = Filter(allPresets, search);
            doCloseX = true;
            doCloseButton = false;
            resizeable = false;
            draggable = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        public override Vector2 InitialSize => new Vector2(620f, 520f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 28f), "Save schedule preset");
            Text.Font = GameFont.Small;

            Rect searchRect = new Rect(0f, 34f, inRect.width, 24f);
            search = Widgets.TextField(searchRect, search ?? "");
            if (search != null && search.Length > 0)
            {
                if (Widgets.ButtonText(new Rect(searchRect.xMax - 54f, searchRect.y, 54f, searchRect.height), "Clear"))
                {
                    search = "";
                }
            }
            visiblePresets = Filter(allPresets, search);
            if (selectedIndex >= visiblePresets.Count)
            {
                selectedIndex = -1;
            }

            Rect listRect = new Rect(0f, 64f, inRect.width, inRect.height - 154f);
            float rowHeight = 38f;
            Rect content = new Rect(0f, 0f, listRect.width - 18f,
                Mathf.Max(listRect.height, visiblePresets.Count * rowHeight));
            Widgets.BeginScrollView(listRect, ref scroll, content);
            for (int i = 0; i < visiblePresets.Count; i++)
            {
                SchedulePreset preset = visiblePresets[i];
                Rect row = new Rect(0f, i * rowHeight, content.width, rowHeight - 3f);
                if (i == selectedIndex)
                {
                    Widgets.DrawBoxSolid(row, new Color(0.22f, 0.30f, 0.40f, 1f));
                }
                Rect rowNameRect = new Rect(row.x + 4f, row.y + 3f, row.width - 338f, 28f);
                if (Widgets.ButtonText(rowNameRect, preset.Name))
                {
                    selectedIndex = i;
                    presetName = preset.Name;
                }
                Widgets.Label(new Rect(row.xMax - 330f, row.y + 4f, 128f, 28f),
                    string.IsNullOrWhiteSpace(preset.ModifiedUtc) ? "v" + preset.SchemaVersion : preset.ModifiedUtc);
                if (Widgets.ButtonText(new Rect(row.xMax - 196f, row.y + 3f, 84f, 28f), "Overwrite"))
                {
                    OrderedWorkMod.DebugLog("Schedule overwrite clicked: " + preset.Name);
                    Replace(preset);
                }
                if (Widgets.ButtonText(new Rect(row.xMax - 108f, row.y + 3f, 100f, 28f), "Delete"))
                {
                    OrderedWorkMod.DebugLog("Schedule delete clicked: " + preset.Name);
                    pendingDelete = preset;
                }
            }
            Widgets.EndScrollView();

            if (pendingDelete != null)
            {
                Rect footer = new Rect(0f, inRect.yMax - 82f, inRect.width, 54f);
                Widgets.DrawBoxSolid(footer, new Color(0.12f, 0.12f, 0.14f, 1f));
                Widgets.Label(new Rect(footer.x + 4f, footer.y + 4f, footer.width - 8f, 20f),
                    "Delete '" + pendingDelete.Name + "'?");
                if (Widgets.ButtonText(new Rect(footer.x + 4f, footer.y + 24f, 110f, 26f), "Confirm delete"))
                {
                    DeletePending();
                }
                if (Widgets.ButtonText(new Rect(footer.x + 120f, footer.y + 24f, 70f, 26f), "Cancel"))
                {
                    pendingDelete = null;
                }
                return;
            }

            Rect nameRect = new Rect(0f, inRect.yMax - 76f, inRect.width - 100f, 28f);
            presetName = Widgets.TextField(nameRect, presetName ?? "");
            bool canSave = !string.IsNullOrWhiteSpace(presetName);
            if (Widgets.ButtonText(new Rect(nameRect.xMax + 8f, nameRect.y, 90f, 28f), "Save", true, true, canSave))
            {
                if (selectedIndex >= 0 && selectedIndex < visiblePresets.Count)
                {
                    Replace(visiblePresets[selectedIndex]);
                }
                else
                {
                    SaveAsNew();
                }
            }
        }

        private static List<SchedulePreset> Filter(List<SchedulePreset> presets, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return presets;
            }
            string needle = query.Trim();
            return presets
                .Where(p => p != null && p.Name != null
                    && p.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        private void SaveAsNew()
        {
            if (draft == null || string.IsNullOrWhiteSpace(presetName))
            {
                return;
            }
            SchedulePreset existing = allPresets.FirstOrDefault(
                p => p != null && string.Equals(p.Name, presetName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                Replace(existing);
                return;
            }
            SchedulePreset saved = new SchedulePreset
            {
                Name = presetName.Trim(),
                Slots = draft.Slots != null ? new List<string>(draft.Slots) : SchedulePreset.NewSlots()
            };
            if (SchedulePresetStore.Save(saved))
            {
                OrderedWorkMod.DebugLog("Schedule preset created: " + saved.Name + " [" + saved.Id + "]");
                Messages.Message("Schedule preset saved: " + saved.Name, MessageTypeDefOf.TaskCompletion, false);
                onSaved?.Invoke(saved);
                Close(true);
            }
            else
            {
                Messages.Message("Could not save the schedule preset.", MessageTypeDefOf.RejectInput, false);
            }
        }

        private void Replace(SchedulePreset target)
        {
            if (target == null || draft == null)
            {
                return;
            }
            target.Name = string.IsNullOrWhiteSpace(presetName) ? target.Name : presetName.Trim();
            target.Slots = draft.Slots != null ? new List<string>(draft.Slots) : SchedulePreset.NewSlots();
            if (SchedulePresetStore.Save(target))
            {
                OrderedWorkMod.DebugLog("Schedule preset replaced: " + target.Name + " [" + target.Id + "]");
                Messages.Message("Schedule preset overwritten: " + target.Name, MessageTypeDefOf.TaskCompletion, false);
                onSaved?.Invoke(target);
                Close(true);
            }
            else
            {
                Messages.Message("Could not replace the schedule preset.", MessageTypeDefOf.RejectInput, false);
            }
        }

        private void DeletePending()
        {
            if (pendingDelete == null)
            {
                return;
            }
            SchedulePreset target = pendingDelete;
            pendingDelete = null;
            if (SchedulePresetStore.Delete(target.Id))
            {
                OrderedWorkMod.DebugLog("Schedule preset deleted: " + target.Name + " [" + target.Id + "]");
                allPresets = SchedulePresetStore.LoadAll();
                Messages.Message("Schedule preset deleted: " + target.Name, MessageTypeDefOf.TaskCompletion, false);
                visiblePresets = Filter(allPresets, search);
                selectedIndex = -1;
            }
        }

        private void ConfirmDelete(SchedulePreset target)
        {
            Dialog_MessageBox.CreateConfirmation(
                "Delete schedule preset '" + target.Name + "'?",
                delegate
                {
                    if (SchedulePresetStore.Delete(target.Id))
                    {
                        OrderedWorkMod.DebugLog("Schedule preset deleted: " + target.Name + " [" + target.Id + "]");
                        allPresets = SchedulePresetStore.LoadAll();
                        visiblePresets = Filter(allPresets, search);
                        selectedIndex = -1;
                    }
                },
                true,
                "Delete schedule",
                WindowLayer.Dialog);
        }
    }
}