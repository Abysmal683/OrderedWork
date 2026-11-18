using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Small save-as/replace/delete dialog for schedule presets. It avoids writing
    /// a preset just because the user pressed Save in the detail panel.
    /// </summary>
    public class Dialog_ScheduleSave : Window
    {
        private readonly SchedulePreset draft;
        private readonly Action<SchedulePreset> onSaved;
        private List<SchedulePreset> presets;
        private string presetName;
        private int selectedIndex = -1;
        private Vector2 scroll;

        public Dialog_ScheduleSave(SchedulePreset draft, Action<SchedulePreset> onSaved)
        {
            this.draft = draft;
            this.onSaved = onSaved;
            presetName = draft != null ? draft.Name : "";
            presets = SchedulePresetStore.LoadAll();
            doCloseX = true;
            doCloseButton = false;
            resizeable = false;
            draggable = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        public override Vector2 InitialSize => new Vector2(460f, 430f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 28f), "Save schedule");
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, 30f, inRect.width, 20f),
                "Save as a new preset, replace an existing one, or delete presets.");
            Rect nameRect = new Rect(0f, 58f, inRect.width, 24f);
            presetName = Widgets.TextField(nameRect, presetName ?? "");

            Rect listRect = new Rect(0f, 90f, inRect.width, inRect.height - 170f);
            float rowHeight = 26f;
            Rect content = new Rect(0f, 0f, listRect.width - 18f, Mathf.Max(listRect.height, presets.Count * rowHeight));
            Widgets.BeginScrollView(listRect, ref scroll, content);
            for (int i = 0; i < presets.Count; i++)
            {
                SchedulePreset preset = presets[i];
                Rect row = new Rect(0f, i * rowHeight, content.width, rowHeight - 2f);
                if (i == selectedIndex)
                {
                    Widgets.DrawBoxSolid(row, new Color(0.24f, 0.32f, 0.42f, 1f));
                }
                if (Widgets.ButtonText(row, preset.Name))
                {
                    selectedIndex = i;
                    presetName = preset.Name;
                }
            }
            Widgets.EndScrollView();

            bool hasName = !string.IsNullOrWhiteSpace(presetName);
            bool hasSelection = selectedIndex >= 0 && selectedIndex < presets.Count;
            if (Widgets.ButtonText(new Rect(0f, inRect.yMax - 66f, 120f, 28f), "Save as new", true, true, hasName))
            {
                SaveAsNew();
            }
            if (Widgets.ButtonText(new Rect(126f, inRect.yMax - 66f, 120f, 28f), "Replace", true, true, hasName && hasSelection))
            {
                ReplaceSelected();
            }
            if (Widgets.ButtonText(new Rect(252f, inRect.yMax - 66f, 90f, 28f), "Delete", true, true, hasSelection))
            {
                DeleteSelected();
            }
            if (Widgets.ButtonText(new Rect(348f, inRect.yMax - 66f, 100f, 28f), "Cancel"))
            {
                Close(true);
            }
        }

        private void SaveAsNew()
        {
            SchedulePreset saved = new SchedulePreset
            {
                Name = presetName.Trim(),
                Slots = new List<string>(draft.Slots)
            };
            if (SchedulePresetStore.Save(saved))
            {
                OrderedWorkMod.DebugLog("Schedule preset created: " + saved.Name + " [" + saved.Id + "]");
                onSaved?.Invoke(saved);
                Close(true);
            }
            else
            {
                Messages.Message("Could not save the schedule preset.", MessageTypeDefOf.RejectInput, false);
            }
        }

        private void ReplaceSelected()
        {
            if (selectedIndex < 0 || selectedIndex >= presets.Count)
            {
                return;
            }
            SchedulePreset target = presets[selectedIndex];
            target.Name = presetName.Trim();
            target.Slots = new List<string>(draft.Slots);
            if (SchedulePresetStore.Save(target))
            {
                OrderedWorkMod.DebugLog("Schedule preset replaced: " + target.Name + " [" + target.Id + "]");
                onSaved?.Invoke(target);
                Close(true);
            }
            else
            {
                Messages.Message("Could not replace the schedule preset.", MessageTypeDefOf.RejectInput, false);
            }
        }

        private void DeleteSelected()
        {
            if (selectedIndex < 0 || selectedIndex >= presets.Count)
            {
                return;
            }
            SchedulePreset target = presets[selectedIndex];
            if (SchedulePresetStore.Delete(target.Id))
            {
                OrderedWorkMod.DebugLog("Schedule preset deleted: " + target.Name + " [" + target.Id + "]");
                presets.RemoveAt(selectedIndex);
                selectedIndex = -1;
            }
        }
    }
}
