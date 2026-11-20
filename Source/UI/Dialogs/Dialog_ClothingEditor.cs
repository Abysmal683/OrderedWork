using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.Core;
using OrderedWork.Services;
using OrderedWork.UI.Clothing;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Minimal shared clothing preset editor. It edits the preset and assigns it to
    /// one WorkType; the visual grid composer comes in the next UI pass.
    /// </summary>
    public sealed class Dialog_ClothingEditor : Window
    {
        private readonly OrderedWorkMod host;
        private string workTypeName;
        private ClothingPreset? preset;
        private int selectedRule = -1;
        private string candidateText = "";
        private Vector2 scroll;
        private bool deletePending;
        private float gridBottom;
        private float ruleEditorTop;

        public Dialog_ClothingEditor(OrderedWorkMod host, string? initialWorkType = null)
        {
            this.host = host;
            doCloseX = true;
            doCloseButton = false;
            resizeable = true;
            draggable = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            workTypeName = initialWorkType ?? "";
            SelectFirstWorkType();
            SelectAssignedPreset();
        }

        public override Vector2 InitialSize => new Vector2(900f, 860f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 28f), "Clothing preset editor");
            Text.Font = GameFont.Small;

            WorkConfigProfile? profile = LoadProfile();
            if (profile == null)
            {
                Widgets.Label(new Rect(0f, 40f, inRect.width, 24f), "No active profile.");
                return;
            }

            DrawTop(profile, inRect);
            DrawGrid(inRect);
            DrawRuleList(inRect);
            DrawRuleEditor();
            DrawFooter(profile, inRect);
        }

        private void DrawTop(WorkConfigProfile profile, Rect inRect)
        {
            Rect row = new Rect(0f, 36f, inRect.width, 28f);
            Rect[] cells = UiFlex.Columns(row, 6f, 1.6f, 1.6f, 0.8f, 0.8f);
            if (Widgets.ButtonText(cells[0], WorkTypeLabel(workTypeName) + " ▼"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (KeyValuePair<string, WorkTypeConfig> entry in profile.WorkTypes
                    .Where(entry => entry.Value != null)
                    .OrderBy(entry => entry.Value.Order))
                {
                    string local = entry.Key;
                    options.Add(new FloatMenuOption(WorkTypeLabel(local), delegate
                    {
                        workTypeName = local;
                        SelectAssignedPreset();
                    }));
                }
                if (options.Count > 0)
                {
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }

            if (Widgets.ButtonText(cells[1], (preset?.Name ?? "no preset") + " ▼"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (ClothingPreset item in ClothingPresetStore.LoadAll())
                {
                    ClothingPreset local = item;
                    options.Add(new FloatMenuOption(item.Name, delegate { preset = local; selectedRule = -1; SyncCandidateText(); }));
                }
                if (options.Count > 0)
                {
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }

            if (Widgets.ButtonText(cells[2], "New"))
            {
                CreatePreset();
            }
            if (Widgets.ButtonText(cells[3], "Delete preset", true, true, preset != null))
            {
                deletePending = true;
            }
            if (deletePending)
            {
                Widgets.DrawBoxSolid(new Rect(0f, 68f, inRect.width, 28f), new Color(0.25f, 0.10f, 0.10f, 0.8f));
                Widgets.Label(new Rect(6f, 72f, inRect.width - 180f, 20f), "Delete this preset and clear assignments?");
                if (Widgets.ButtonText(new Rect(inRect.width - 166f, 70f, 74f, 24f), "Confirm"))
                {
                    DeletePreset(profile);
                    deletePending = false;
                }
                if (Widgets.ButtonText(new Rect(inRect.width - 86f, 70f, 74f, 24f), "Cancel"))
                {
                    deletePending = false;
                }
            }
        }

        private void DrawGrid(Rect inRect)
        {
            if (preset == null)
            {
                return;
            }
            ClothingContext context = ClothingContextBuilder.Build(null, preset);
            ClothingResolution resolution = ClothingResolver.Resolve(preset, context);
            float gridTop = deletePending ? 104f : 76f;
            Rect gridRect = new Rect(0f, gridTop, inRect.width, ClothingSlotGrid.Height(context.KnownSlots.Count));
            gridBottom = gridRect.yMax;
            Widgets.Label(new Rect(gridRect.x, gridRect.y - 20f, gridRect.width, 18f), "Slots · click to select/create a rule · right-click to remove");
            ClothingSlot? selectedSlot = null;
            if (preset.Rules != null && selectedRule >= 0 && selectedRule < preset.Rules.Count)
            {
                ClothingRule selectedRuleData = preset.Rules[selectedRule];
                selectedSlot = new ClothingSlot(selectedRuleData.Slot, selectedRuleData.BodyLayer ?? "", selectedRuleData.Target);
            }
            ClothingSlotGrid.Draw(gridRect, context.KnownSlots, resolution, SelectOrCreateRule, RemoveRuleAtSlot, selectedSlot);
        }

        private void DrawRuleList(Rect inRect)
        {
            if (preset == null)
            {
                Widgets.Label(new Rect(0f, 80f, inRect.width, 24f), "Select or create a preset.");
                return;
            }

            Rect listRect = new Rect(0f, gridBottom + 30f, inRect.width, 100f);
            Widgets.Label(new Rect(listRect.x, listRect.y - 20f, listRect.width, 18f), "Rules");
            int count = preset.Rules?.Count ?? 0;
            UiScrollPane.Draw(listRect, ref scroll, Mathf.Max(listRect.height, count * 30f + 4f), this, delegate(Rect view)
            {
                int index = 0;
                foreach (ClothingRule rule in preset.Rules ?? new List<ClothingRule>())
                {
                    Rect row = new Rect(0f, index * 30f, view.width, 27f);
                    string label = rule.Phase + " · " + rule.Target + " · " + rule.Slot
                        + (string.IsNullOrEmpty(rule.BodyLayer) ? "" : " / " + rule.BodyLayer);
                    if (Widgets.ButtonText(row, label))
                    {
                        selectedRule = index;
                        SyncCandidateText();
                    }
                    index++;
                }
            });

            Rect buttons = new Rect(listRect.x, listRect.yMax + 4f, listRect.width, 26f);
            ruleEditorTop = buttons.yMax + 10f;
            Rect[] cells = UiFlex.Columns(buttons, 4f, 1f, 1f, 1f);
            if (Widgets.ButtonText(cells[0], "Add"))
            {
                preset.Rules = preset.Rules ?? new List<ClothingRule>();
                preset.Rules.Add(DefaultRule());
                selectedRule = preset.Rules.Count - 1;
                SyncCandidateText();
            }
            if (Widgets.ButtonText(cells[1], "Delete", true, true, selectedRule >= 0))
            {
                if (preset.Rules != null && selectedRule >= 0 && selectedRule < preset.Rules.Count)
                {
                    preset.Rules.RemoveAt(selectedRule);
                    selectedRule = Mathf.Min(selectedRule, (preset.Rules.Count - 1));
                    SyncCandidateText();
                }
            }
            if (Widgets.ButtonText(cells[2], "Up", true, true, selectedRule > 0))
            {
                MoveRule(-1);
            }
        }

        private void SelectOrCreateRule(ClothingSlot slot)
        {
            if (preset == null)
            {
                return;
            }
            preset.Rules = preset.Rules ?? new List<ClothingRule>();
            int index = preset.Rules.FindIndex(rule => rule != null
                && rule.Target == slot.Target
                && rule.Slot == slot.Slot
                && (rule.BodyLayer ?? "") == (slot.BodyLayer ?? ""));
            if (index < 0)
            {
                ClothingRule created = DefaultRule();
                created.Target = slot.Target;
                created.Slot = slot.Slot;
                created.BodyLayer = slot.BodyLayer;
                preset.Rules.Add(created);
                index = preset.Rules.Count - 1;
            }
            selectedRule = index;
            SyncCandidateText();
        }

        private void RemoveRuleAtSlot(ClothingSlot slot)
        {
            if (preset?.Rules == null)
            {
                return;
            }
            int index = preset.Rules.FindIndex(rule => rule != null
                && rule.Target == slot.Target
                && rule.Slot == slot.Slot
                && (rule.BodyLayer ?? "") == (slot.BodyLayer ?? ""));
            if (index < 0)
            {
                return;
            }
            preset.Rules.RemoveAt(index);
            selectedRule = preset.Rules.Count == 0 ? -1 : Mathf.Min(index, preset.Rules.Count - 1);
            SyncCandidateText();
        }

        private void DrawRuleEditor()
        {
            if (preset == null || preset.Rules == null || selectedRule < 0 || selectedRule >= preset.Rules.Count)
            {
                return;
            }
            ClothingRule rule = preset.Rules[selectedRule];

            Rect area = new Rect(0f, ruleEditorTop, 880f, 200f);
            UiRows rows = new UiRows(area, 24f, 4f);
            Widgets.Label(rows.Next(), "Selected rule " + (selectedRule + 1));
            Rect phaseRow = rows.Next();
            if (Widgets.ButtonText(new Rect(phaseRow.x, phaseRow.y, 150f, 24f), "Phase: " + rule.Phase))
            {
                rule.Phase = rule.Phase == ClothingRulePhase.Locked ? ClothingRulePhase.Emergency
                    : rule.Phase == ClothingRulePhase.Emergency ? ClothingRulePhase.Work
                    : ClothingRulePhase.Locked;
            }
            if (Widgets.ButtonText(new Rect(phaseRow.x + 156f, phaseRow.y, 120f, 24f), "Target: " + rule.Target))
            {
                rule.Target = rule.Target == ClothingTarget.Apparel ? ClothingTarget.Tool : ClothingTarget.Apparel;
                if (rule.Target == ClothingTarget.Tool)
                {
                    rule.BodyLayer = "";
                }
            }
            Rect slotRow = rows.Next();
            Widgets.Label(new Rect(slotRow.x, slotRow.y, 110f, 24f), "Slot");
            rule.Slot = Widgets.TextField(new Rect(slotRow.x + 114f, slotRow.y, 180f, 24f), rule.Slot ?? "");
            Widgets.Label(new Rect(slotRow.x + 300f, slotRow.y, 60f, 24f), "Layer");
            rule.BodyLayer = Widgets.TextField(new Rect(slotRow.x + 364f, slotRow.y, 160f, 24f), rule.BodyLayer ?? "");
            Rect priorityRow = rows.Next();
            Widgets.Label(new Rect(priorityRow.x, priorityRow.y, 110f, 24f), "Priority");
            int priority = rule.Priority;
            Widgets.TextFieldNumeric(new Rect(priorityRow.x + 114f, priorityRow.y, 80f, 24f), ref priority, ref priorityBuffer, 0, 999);
            rule.Priority = priority;
            bool mandatory = rule.Mandatory;
            Widgets.CheckboxLabeled(new Rect(priorityRow.x + 210f, priorityRow.y, 150f, 24f), "Mandatory", ref mandatory);
            rule.Mandatory = mandatory;
            Rect candidates = rows.Next();
            Widgets.Label(new Rect(candidates.x, candidates.y, 110f, 24f), "Candidates");
            candidateText = Widgets.TextField(new Rect(candidates.x + 114f, candidates.y, 430f, 24f), candidateText ?? "");
            if (Widgets.ButtonText(new Rect(candidates.x + 550f, candidates.y, 90f, 24f), "Pick..."))
            {
                Find.WindowStack.Add(new Dialog_ThingDefPicker(rule.Target, rule.Candidates, delegate(IReadOnlyList<string> defNames)
                {
                    AddCandidates(defNames);
                    rule.Candidates = ParseCandidates(candidateText);
                }));
            }
            rule.Candidates = ParseCandidates(candidateText);
            Widgets.Label(new Rect(candidates.x + 114f, candidates.y + 24f, 650f, 18f),
                "Comma-separated ThingDef defNames. A policy-denied candidate is reported, never forced.");
            if (selectedRule >= 0)
            {
                Rect move = rows.Next(26f);
                if (Widgets.ButtonText(new Rect(move.x + 114f, move.y, 80f, 24f), "Up"))
                {
                    MoveRule(-1);
                }
                if (Widgets.ButtonText(new Rect(move.x + 200f, move.y, 80f, 24f), "Down"))
                {
                    MoveRule(1);
                }
            }
        }

        private void DrawFooter(WorkConfigProfile profile, Rect inRect)
        {
            Rect footer = new Rect(0f, inRect.height - 34f, inRect.width, 28f);
            Rect[] cells = UiFlex.Columns(footer, 6f, 1f, 1f);
            if (Widgets.ButtonText(cells[0], "Save preset", true, true, preset != null))
            {
                SavePreset();
            }
            if (Widgets.ButtonText(cells[1], "Assign to WorkType", true, true, preset != null && !string.IsNullOrEmpty(workTypeName)))
            {
                Assign(profile);
            }
        }

        private WorkConfigProfile? LoadProfile()
        {
            return WorkConfigStore.LoadEditingProfile(WorkConfigStore.LoadSelectedProfileName());
        }

        private void SelectFirstWorkType()
        {
            if (!string.IsNullOrEmpty(workTypeName))
            {
                return;
            }
            WorkConfigProfile? profile = LoadProfile();
            workTypeName = profile?.WorkTypes.Keys.FirstOrDefault() ?? "";
        }

        private void SelectAssignedPreset()
        {
            WorkConfigProfile? profile = LoadProfile();
            WorkTypeConfig? config = null;
            if (profile != null && workTypeName != null
                && profile.WorkTypes.TryGetValue(workTypeName, out WorkTypeConfig? found))
            {
                config = found;
            }
            preset = config != null && !string.IsNullOrEmpty(config.ClothingPresetId)
                ? ClothingPresetStore.LoadAll().FirstOrDefault(item => item.Id == config.ClothingPresetId)
                : ClothingPresetStore.LoadAll().FirstOrDefault();
            selectedRule = preset?.Rules != null && preset.Rules.Count > 0 ? 0 : -1;
            SyncCandidateText();
        }

        private void CreatePreset()
        {
            ClothingPreset created = new ClothingPreset { Name = "New clothing" };
            created.Normalize();
            ClothingPresetStore.Save(created);
            preset = created;
            selectedRule = -1;
            candidateText = "";
        }

        private void DeletePreset(WorkConfigProfile profile)
        {
            if (preset == null)
            {
                return;
            }
            foreach (WorkTypeConfig config in profile.WorkTypes.Values)
            {
                if (config != null && config.ClothingPresetId == preset.Id)
                {
                    config.ClothingPresetId = "";
                }
            }
            host.CommitProfile(profile);
            ClothingPresetStore.Delete(preset.Id);
            preset = ClothingPresetStore.LoadAll().FirstOrDefault();
            selectedRule = -1;
            SyncCandidateText();
        }

        private void SavePreset()
        {
            if (preset == null)
            {
                return;
            }
            preset.Normalize();
            if (ClothingPresetStore.Save(preset))
            {
                Messages.Message("Clothing preset saved: " + preset.Name, MessageTypeDefOf.TaskCompletion, false);
            }
        }

        private void Assign(WorkConfigProfile profile)
        {
            if (preset == null || string.IsNullOrEmpty(workTypeName))
            {
                return;
            }
            if (!profile.WorkTypes.TryGetValue(workTypeName, out WorkTypeConfig? config) || config == null)
            {
                return;
            }
            config.ClothingPresetId = preset.Id;
            host.CommitProfile(profile);
            Messages.Message("Clothing preset assigned: " + preset.Name, MessageTypeDefOf.TaskCompletion, false);
        }

        private void MoveRule(int delta)
        {
            if (preset?.Rules == null)
            {
                return;
            }
            int target = selectedRule + delta;
            if (selectedRule < 0 || target < 0 || target >= preset.Rules.Count)
            {
                return;
            }
            ClothingRule rule = preset.Rules[selectedRule];
            preset.Rules.RemoveAt(selectedRule);
            preset.Rules.Insert(target, rule);
            selectedRule = target;
        }

        private void SyncCandidateText()
        {
            if (preset?.Rules != null && selectedRule >= 0 && selectedRule < preset.Rules.Count)
            {
                ClothingRule rule = preset.Rules[selectedRule];
                candidateText = string.Join(", ", rule.Candidates ?? new List<string>());
                priorityBuffer = rule.Priority.ToString();
            }
            else
            {
                candidateText = "";
                priorityBuffer = "";
            }
        }

        private string priorityBuffer = "";

        private static ClothingRule DefaultRule()
        {
            return new ClothingRule
            {
                Phase = ClothingRulePhase.Work,
                Target = ClothingTarget.Apparel,
                Slot = "Torso",
                BodyLayer = "Shell",
                Priority = 0
            };
        }

        private void AddCandidates(IReadOnlyList<string>? defNames)
        {
            List<string> values = ParseCandidates(candidateText);
            if (defNames != null)
            {
                foreach (string defName in defNames)
                {
                    if (!string.IsNullOrWhiteSpace(defName) && !values.Contains(defName, StringComparer.Ordinal))
                    {
                        values.Add(defName);
                    }
                }
            }
            candidateText = string.Join(", ", values);
        }

        private static List<string> ParseCandidates(string? text)
        {
            return (text ?? "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim())
                .Where(value => !string.IsNullOrEmpty(value))
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private static string WorkTypeLabel(string? defName)
        {
            WorkTypeDef? def = string.IsNullOrEmpty(defName) ? null : DefDatabase<WorkTypeDef>.GetNamedSilentFail(defName);
            return def?.labelShort ?? defName ?? "no WorkType";
        }
    }
}