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
    /// Save/load list for named Work presets. Saving captures the current WorkType
    /// and WorkGiver order; loading replaces the pawn's Work policy immediately.
    /// </summary>
    public sealed class Dialog_WorkPreset : Window
    {
        private readonly bool loadMode;
        private readonly Func<WorkPreset> capture;
        private readonly Action<WorkPreset> apply;
        private List<WorkPreset> presets;
        private string presetName = "";
        private WorkPreset pendingDelete;
        private WorkPreset pendingLoad;
        private bool closeAfterSave;
        private Vector2 scroll;

        public Dialog_WorkPreset(bool loadMode, Func<WorkPreset> capture, Action<WorkPreset> apply,
            string initialName = null)
        {
            this.loadMode = loadMode;
            this.capture = capture;
            this.apply = apply;
            presetName = initialName ?? "";
            presets = WorkPresetStore.List();
            doCloseX = true;
            doCloseButton = false;
            resizeable = false;
            draggable = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        public override Vector2 InitialSize
        {
            get
            {
                int count = presets != null ? presets.Count : 0;
                return new Vector2(620f, Mathf.Clamp(150f + count * 38f, 260f, 560f));
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 28f),
                loadMode ? "Load Work preset" : "Save Work preset");
            Text.Font = GameFont.Small;

            float listHeight = Mathf.Min(inRect.height - (loadMode ? 90f : 150f),
                Mathf.Max(70f, presets.Count * 38f + 6f));
            Rect listRect = new Rect(0f, 36f, inRect.width, listHeight);
            Rect content = new Rect(0f, 0f, listRect.width - 18f,
                Mathf.Max(listRect.height, presets.Count * 38f));
            Widgets.BeginScrollView(listRect, ref scroll, content);
            for (int i = 0; i < presets.Count; i++)
            {
                WorkPreset preset = presets[i];
                Rect row = new Rect(0f, i * 38f, content.width, 35f);
                UiTable columns = new UiTable(row, 6f);
                columns.Flex().Add(158f).Add(86f).Add(92f);
                if (Widgets.ButtonText(columns.Column(0, 28f), preset.Name))
                {
                    presetName = preset.Name;
                    if (loadMode)
                    {
                        pendingLoad = preset;
                        break;
                    }
                }
                Widgets.Label(columns.Column(1, 28f),
                    string.IsNullOrWhiteSpace(preset.CreatedUtc) ? "unknown" : preset.CreatedUtc);
                if (Widgets.ButtonText(columns.Column(2, 28f), loadMode ? "Load" : "Overwrite"))
                {
                    if (loadMode)
                    {
                        pendingLoad = preset;
                        break;
                    }
                    SaveAs(preset.Name);
                }
                if (Widgets.ButtonText(columns.Column(3, 28f), "Delete"))
                {
                    pendingDelete = preset;
                }
            }
            Widgets.EndScrollView();
            if (pendingLoad != null)
            {
                WorkPreset toLoad = pendingLoad;
                pendingLoad = null;
                Load(toLoad);
                return;
            }
            if (closeAfterSave)
            {
                Close();
                return;
            }

            if (pendingDelete != null)
            {
                DrawDeleteConfirmation(inRect);
                return;
            }

            if (!loadMode)
            {
                Rect nameRect = new Rect(0f, inRect.yMax - 40f, inRect.width - 100f, 28f);
                presetName = Widgets.TextField(nameRect, presetName ?? "");
                bool canSave = !string.IsNullOrWhiteSpace(presetName);
                if (Widgets.ButtonText(new Rect(nameRect.xMax + 8f, nameRect.y, 90f, 28f),
                    "Save", true, true, canSave))
                {
                    SaveAs(presetName);
                }
            }
        }

        private void SaveAs(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || capture == null)
            {
                return;
            }
            WorkPreset preset = capture();
            if (preset == null)
            {
                return;
            }
            preset.Name = name.Trim();
            preset.CreatedUtc = DateTime.UtcNow.ToString("o");
            if (WorkPresetStore.Save(preset))
            {
                Messages.Message("Work preset saved: " + preset.Name, MessageTypeDefOf.TaskCompletion, false);
                presets = WorkPresetStore.List();
                closeAfterSave = true;
            }
        }

        private void Load(WorkPreset preset)
        {
            if (preset == null || apply == null)
            {
                return;
            }
            apply(preset);
            Messages.Message("Work preset applied: " + preset.Name, MessageTypeDefOf.TaskCompletion, false);
            Close();
        }

        private void DrawDeleteConfirmation(Rect inRect)
        {
            Rect footer = new Rect(0f, inRect.yMax - 60f, inRect.width, 60f);
            Widgets.DrawBoxSolid(footer, new Color(0.12f, 0.12f, 0.14f, 1f));
            UiRows rows = new UiRows(UiInset.All(footer, 6f), 18f, 4f);
            Widgets.Label(rows.Next(), "Delete '" + pendingDelete.Name + "'?");
            Rect buttons = rows.Next(26f);
            Rect[] cells = UiFlex.Columns(buttons, 6f, 1f, 1f);
            if (UiButtonBar.Button(cells[0], "Confirm delete"))
            {
                WorkPresetStore.Delete(pendingDelete.Name);
                pendingDelete = null;
                presets = WorkPresetStore.List();
            }
            if (UiButtonBar.Button(cells[1], "Cancel"))
            {
                pendingDelete = null;
            }
        }
    }
}
