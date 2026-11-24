using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.Core;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Work Control clothing overview: every WorkType and its assigned preset.
    /// The shared editor is opened from each row.
    /// </summary>
    public sealed class Dialog_ClothingOverview : Window
    {
        private readonly OrderedWorkMod host;
        private Vector2 scroll;

        public Dialog_ClothingOverview(OrderedWorkMod host)
        {
            this.host = host;
            doCloseX = true;
            doCloseButton = false;
            resizeable = true;
            draggable = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        public override Vector2 InitialSize => new Vector2(720f, 560f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 28f), "WorkType clothing");
            Text.Font = GameFont.Small;

            WorkConfigProfile? profile = WorkConfigStore.LoadEditingProfile(WorkConfigStore.LoadSelectedProfileName());
            if (profile == null)
            {
                Widgets.Label(new Rect(0f, 40f, inRect.width, 24f), "No active profile.");
                return;
            }

            List<KeyValuePair<string, WorkTypeConfig>> rows = profile.WorkTypes
                .Where(pair => pair.Value != null)
                .OrderBy(pair => pair.Value.Order)
                .ToList();
            Rect listRect = new Rect(0f, 36f, inRect.width, inRect.height - 80f);
            UiScrollPane.Draw(listRect, ref scroll, Mathf.Max(listRect.height, rows.Count * 30f + 4f), this, delegate(Rect view)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    string workType = rows[i].Key;
                    WorkTypeConfig config = rows[i].Value;
                    ClothingPreset? preset = ClothingPresetStore.LoadAll()
                        .FirstOrDefault(item => item.Id == config.ClothingPresetId);
                    Rect row = new Rect(0f, i * 30f, view.width, 27f);
                    UiZebra.Draw(row, i);
                    Widgets.LabelEllipses(new Rect(row.x + 4f, row.y, row.width - 96f, 24f),
                        WorkTypeLabel(workType) + " · " + (preset?.Name ?? "no preset"));
                    if (Widgets.ButtonText(new Rect(row.xMax - 86f, row.y, 80f, 24f), "Edit"))
                    {
                        Find.WindowStack.Add(new Dialog_EquipmentEditor(host));
                    }
                }
            });

            if (Widgets.ButtonText(new Rect(0f, inRect.height - 34f, 180f, 28f), "Open editor"))
            {
                Find.WindowStack.Add(new Dialog_EquipmentEditor(host));
            }
        }

        private static string WorkTypeLabel(string defName)
        {
            WorkTypeDef? def = DefDatabase<WorkTypeDef>.GetNamedSilentFail(defName);
            return def?.labelShort ?? defName;
        }
    }
}