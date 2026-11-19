using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// <summary>
    /// Self-contained Work editor. It can be hosted by the current detail dialog now and
    /// by a future grid cell without changing the editor itself.
    /// </summary>
    public sealed partial class WorkPanel
    {
        private const float RowHeight = 24f;
        private const float HeaderHeight = 32f;
        private const float Gap = 8f;

        private readonly Pawn pawn;
        private bool initialized;
        private List<string> workDraft = new List<string>();
        private string selectedWorkType;
        private bool giversExpanded = true;
        private readonly Dictionary<string, List<string>> giverDrafts =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> giverBaselines =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);

        private Vector2 unableScroll;
        private Vector2 skillsScroll;
        private Vector2 genericStatsScroll;
        private Vector2 specializedStatsScroll;
        private bool filterLevel = true;
        private bool filterPassion = true;
        private bool filterExpertise;
        private bool filterMods = true;
        private readonly UiDragList workList = new UiDragList();
        private readonly UiDragList giverList = new UiDragList();
        private readonly Dictionary<string, string> workTypeTooltipCache =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> giverTooltipCache =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> skillTooltipCache =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private int skillTooltipCacheTick = -1;
        private readonly Dictionary<string, string> statTooltipCache =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private int statTooltipCacheTick = -1;
        private bool resetAllArmed;
        private float resetAllArmedUntil;

        public WorkPanel(Pawn pawn)
        {
            this.pawn = pawn;
        }

        public void Draw(Rect area)
        {
            if (pawn == null || pawn.Destroyed)
            {
                Widgets.Label(area, "Pawn unavailable.");
                return;
            }
            EnsureInitialized();

            WorkPanelRects layout = WorkPanelLayout.Build(area, giversExpanded);
            DrawHeader(layout.Header);
            if (layout.Stacked)
            {
                DrawCompact(layout);
                return;
            }
            DrawWorkTypesColumn(layout.WorkTypes);
            DrawGiversColumn(layout.Givers);
            DrawMetadataColumn(layout.Metadata);
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;
            if (OrderedWorkMod.Settings != null)
            {
                filterLevel = OrderedWorkMod.Settings.skillFilterLevel;
                filterPassion = OrderedWorkMod.Settings.skillFilterPassion;
                filterExpertise = OrderedWorkMod.Settings.skillFilterExpertise;
                filterMods = OrderedWorkMod.Settings.skillFilterMods;
            }
            workDraft = WorkOrderService.GetOrder(pawn) ?? new List<string>();
            selectedWorkType = workDraft.FirstOrDefault() ?? AddableWorkTypes().FirstOrDefault()?.defName;
            giverDrafts.Clear();
            giverBaselines.Clear();
            LoadAllGiverDrafts();
        }

        private void Reload()
        {
            initialized = false;
            EnsureInitialized();
        }

        private void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y, 150f, 26f), "Work");
            Text.Font = GameFont.Small;

            Rect load = new Rect(rect.xMax - 136f, rect.y + 2f, 64f, 24f);
            Rect save = new Rect(rect.xMax - 68f, rect.y + 2f, 64f, 24f);
            if (UiButtonBar.Button(load, "Load", true, "Load a named Work preset and replace this pawn's Work policy."))
            {
                Find.WindowStack.Add(new Dialog_WorkPreset(true, null, ApplyPreset));
            }
            if (UiButtonBar.Button(save, "Save", true, "Save this pawn's WorkType and WorkGiver order as a named preset."))
            {
                Find.WindowStack.Add(new Dialog_WorkPreset(false, CapturePreset, null, pawn.LabelShort));
            }
        }

        private WorkPreset CapturePreset()
        {
            WorkPreset preset = new WorkPreset
            {
                WorkTypes = new List<string>(workDraft)
            };
            foreach (string workTypeName in workDraft)
            {
                WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(workTypeName);
                if (workType == null)
                {
                    continue;
                }
                preset.WorkTypeLabels[workTypeName] = workType.labelShort;
                int workTypeMod = ModIndex(preset, workType);
                if (workTypeMod >= 0)
                {
                    preset.WorkTypeMods[workTypeName] = workTypeMod;
                }
                List<string> order;
                if (!giverDrafts.TryGetValue(workTypeName, out order))
                {
                    order = WorkGiverOrderService.GetEffectiveOrder(pawn, workType);
                }
                preset.Givers[workTypeName] = new List<string>(order ?? new List<string>());
                foreach (string giverName in preset.Givers[workTypeName])
                {
                    WorkGiverDef giver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(giverName);
                    int giverMod = ModIndex(preset, giver);
                    if (giverMod >= 0)
                    {
                        preset.GiverMods[giverName] = giverMod;
                    }
                }
            }
            return preset;
        }

        private static int ModIndex(WorkPreset preset, Def def)
        {
            if (def == null || def.modContentPack == null)
            {
                return -1;
            }
            string packageId = def.modContentPack.PackageId;
            if (string.IsNullOrEmpty(packageId))
            {
                return -1;
            }
            int index = preset.Mods.IndexOf(packageId);
            if (index < 0)
            {
                index = preset.Mods.Count;
                preset.Mods.Add(packageId);
            }
            return index;
        }

        private void ApplyPreset(WorkPreset preset)
        {
            if (preset == null)
            {
                return;
            }
            List<string> order = (preset.WorkTypes ?? new List<string>())
                .Where(name =>
                {
                    WorkTypeDef def = DefDatabase<WorkTypeDef>.GetNamedSilentFail(name);
                    return def != null && !WorkOrderService.IsIncapable(pawn, def);
                })
                .Distinct(StringComparer.Ordinal)
                .ToList();
            WorkOrderService.SaveAndApply(pawn, order);
            WorkGiverOrderService.ResetAll(pawn);
            foreach (KeyValuePair<string, List<string>> pair in preset.Givers ?? new Dictionary<string, List<string>>())
            {
                WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(pair.Key);
                if (workType == null || pair.Value == null || pair.Value.Count == 0)
                {
                    continue;
                }
                List<string> valid = pair.Value
                    .Where(name =>
                    {
                        WorkGiverDef giver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(name);
                        return giver != null && giver.workType == workType;
                    })
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                if (valid.Count > 0)
                {
                    WorkGiverOrderService.SaveOrder(pawn, workType, valid);
                }
            }
            workDraft = WorkOrderService.GetOrder(pawn) ?? order;
            selectedWorkType = workDraft.FirstOrDefault();
            giverDrafts.Clear();
            giverBaselines.Clear();
            LoadAllGiverDrafts();
            InvalidateTooltips();
        }

        private void DrawWorkTypesColumn(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, UiColors.PanelBody);
            Rect header = new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, 24f);
            Widgets.Label(new Rect(header.x, header.y, header.width - 212f, 22f), "Work types");
            if (UiButtonBar.Button(new Rect(header.xMax - 204f, header.y, 66f, 22f), "Recom.", true,
                "Apply the existing WorkType recommendation to this pawn immediately. WorkGiver overrides are untouched."))
            {
                workDraft = WorkOrderService.GetRecommendation(pawn);
                selectedWorkType = workDraft.FirstOrDefault() ?? selectedWorkType;
                LoadSelectedGivers();
                CommitWork();
            }
            if (UiButtonBar.Button(new Rect(header.xMax - 134f, header.y, 62f, 22f), "Givers",
                selectedWorkType != null, "Show or hide the WorkGiver column for the selected WorkType.",
                giversExpanded ? new Color(0.48f, 0.68f, 0.42f, 1f) : (Color?)null))
            {
                giversExpanded = !giversExpanded;
                if (giversExpanded)
                {
                    LoadSelectedGivers();
                }
            }
            if (UiButtonBar.Button(new Rect(header.xMax - 68f, header.y, 62f, 22f), "Add", true,
                "Add a capable WorkType that is not currently in this pawn's list."))
            {
                ShowAddWorkTypeMenu();
            }

            Rect body = new Rect(rect.x + 6f, header.yMax + 4f, rect.width - 12f, Mathf.Max(0f, rect.height - header.height - 12f));
            List<WorkTypeDef> unable = UnableWorkTypes();
            float unableHeight = unable.Count > 0 ? body.height * 0.30f : 0f;
            Rect active = new Rect(body.x, body.y, body.width, body.height - unableHeight - (unableHeight > 0f ? Gap : 0f));
            DrawActiveWorkTypes(active);

            if (unableHeight > 0f)
            {
                Rect unableRect = new Rect(body.x, active.yMax + Gap, body.width, unableHeight);
                Widgets.DrawBoxSolid(new Rect(body.x, active.yMax + 3f, body.width, 2f), UiColors.SectionSeparator);
                DrawUnableWorkTypes(unableRect, unable);
            }
        }

        private void DrawActiveWorkTypes(Rect rect)
        {
            int removeIndex = -1;
            workList.Draw(
                rect,
                workDraft.Count,
                delegate(int index, Rect row)
                {
                    if (index < 0 || index >= workDraft.Count)
                    {
                        return;
                    }
                    WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(workDraft[index]);
                    if (workType == null)
                    {
                        return;
                    }
                    if (workList.DraggingIndex == index)
                    {
                        Widgets.DrawBoxSolid(row, new Color(0.22f, 0.42f, 0.62f, 0.65f));
                    }
                    else if (workList.IsDragMoved && Mouse.IsOver(row))
                    {
                        Widgets.DrawBoxSolid(row, new Color(0.55f, 0.72f, 0.35f, 0.40f));
                    }
                    else
                    {
                        UiZebra.Draw(row, index);
                    }
                    if (selectedWorkType == workType.defName && workList.DraggingIndex != index)
                    {
                        Widgets.DrawBoxSolid(row, new Color(0.30f, 0.52f, 0.28f, 0.48f));
                    }

                    Widgets.Label(new Rect(row.x + 4f, row.y + 3f, 22f, 18f), (index + 1).ToString());
                    Widgets.LabelEllipses(new Rect(row.x + 26f, row.y + 3f, Mathf.Max(50f, row.width - 216f), 18f), workType.labelShort);
                    SkillDef bestSkill = BestSkillDef(workType);
                    SkillRecord bestRecord = bestSkill != null && pawn.skills != null ? pawn.skills.GetSkill(bestSkill) : null;
                    WorkTypePaletteView.DrawPassionIcon(
                        new Rect(row.xMax - 190f, row.y + 3f, 16f, 16f),
                        bestRecord != null ? bestRecord.passion : Passion.None);
                    Widgets.Label(new Rect(row.xMax - 168f, row.y + 3f, 24f, 18f),
                        bestRecord != null ? bestRecord.Level.ToString() : "—");
                    if (WorkTypeGiversChanged(workType.defName))
                    {
                        Widgets.Label(new Rect(row.xMax - 126f, row.y + 3f, 58f, 18f), "Changed");
                    }
                    Rect remove = new Rect(row.xMax - 46f, row.y + 1f, 18f, 20f);
                    if (Widgets.ButtonText(remove, "x"))
                    {
                        removeIndex = index;
                    }
                    Widgets.Label(new Rect(row.xMax - 24f, row.y + 1f, 20f, 20f), "≡");
                    if (Mouse.IsOver(row)) TooltipHandler.TipRegion(row, WorkTypeTooltip(workType, index + 1));
                },
                delegate(int index, Rect row)
                {
                    return new Rect(row.x, row.y, Mathf.Max(20f, row.width - 52f), row.height);
                },
                delegate(int index)
                {
                    if (index < 0 || index >= workDraft.Count)
                    {
                        return;
                    }
                    selectedWorkType = workDraft[index];
                    giversExpanded = true;
                    LoadSelectedGivers();
                },
                MoveWorkType,
                CommitWork);

            if (removeIndex >= 0 && removeIndex < workDraft.Count)
            {
                string removed = workDraft[removeIndex];
                workDraft.RemoveAt(removeIndex);
                if (selectedWorkType == removed)
                {
                    selectedWorkType = workDraft.FirstOrDefault();
                    LoadSelectedGivers();
                }
                CommitWork();
            }
        }

        private void DrawUnableWorkTypes(Rect rect, List<WorkTypeDef> unable)
        {
            Widgets.DrawBoxSolid(rect, UiColors.PanelBody);
            Widgets.Label(new Rect(rect.x + 6f, rect.y + 3f, rect.width - 12f, 20f), "Cannot");
            Rect list = new Rect(rect.x + 6f, rect.y + 24f, rect.width - 12f, Mathf.Max(0f, rect.height - 26f));
            if (unable.Count == 0)
            {
                Widgets.Label(list, "No limitations");
                return;
            }
            Rect view = new Rect(0f, 0f, list.width - 16f, Mathf.Max(list.height, unable.Count * RowHeight));
            Widgets.BeginScrollView(list, ref unableScroll, view);
            try
            {
                for (int i = 0; i < unable.Count; i++)
                {
                    Rect row = new Rect(0f, i * RowHeight, view.width, RowHeight - 2f);
                    UiZebra.Draw(row, i);
                    Widgets.LabelEllipses(new Rect(row.x + 4f, row.y + 3f, row.width - 8f, 18f), unable[i].labelShort);
                    if (Mouse.IsOver(row)) TooltipHandler.TipRegion(row, WorkTypeTooltip(unable[i], -1));
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        private void DrawGiversColumn(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, UiColors.PanelBody);
            if (!giversExpanded)
            {
                DrawStats(new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, rect.height - 12f),
                    ref specializedStatsScroll);
                return;
            }
            WorkTypeDef workType = SelectedWorkType();
            Rect header = new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, 24f);
            string title = workType != null ? "Givers · " + workType.labelShort : "Givers";
            Widgets.Label(new Rect(header.x, header.y, header.width - 218f, 22f), title);
            List<string> draft = CurrentGiverDraft();
            List<string> baseline = CurrentGiverBaseline();
            bool changed = draft != null && baseline != null && !SameOrder(draft, baseline);
            if (UiButtonBar.Button(new Rect(header.xMax - 212f, header.y, 70f, 22f), "Reset", changed && workType != null,
                "Reset this WorkType's giver order to the saved global JSON order."))
            {
                if (workType != null)
                {
                    giverDrafts[workType.defName] = new List<string>(baseline ?? new List<string>());
                    CommitGivers();
                }
            }
            if (resetAllArmed && Time.realtimeSinceStartup >= resetAllArmedUntil)
            {
                resetAllArmed = false;
            }
            bool hasOverrides = WorkGiverOrderService.HasAnyOverride(pawn);
            string resetAllLabel = resetAllArmed ? "Confirm reset" : "Reset all";
            Color? resetTint = resetAllArmed ? new Color(0.86f, 0.30f, 0.26f, 1f) : (Color?)null;
            if (UiButtonBar.Button(new Rect(header.xMax - 138f, header.y, 132f, 22f), resetAllLabel,
                hasOverrides || resetAllArmed,
                resetAllArmed ? "Click again within 5 seconds to reset every WorkGiver override."
                    : "Reset every WorkGiver override for this pawn. Requires confirmation.",
                resetTint))
            {
                if (resetAllArmed)
                {
                    WorkGiverOrderService.ResetAll(pawn);
                    giverDrafts.Clear();
                    giverBaselines.Clear();
                    resetAllArmed = false;
                    LoadSelectedGivers();
                }
                else
                {
                    resetAllArmed = true;
                    resetAllArmedUntil = Time.realtimeSinceStartup + 5f;
                }
            }

            Rect body = new Rect(rect.x + 6f, header.yMax + 4f, rect.width - 12f, Mathf.Max(0f, rect.height - header.height - 12f));
            if (workType == null || draft == null)
            {
                Widgets.Label(body, "Select a WorkType.");
                return;
            }

            float statsHeight = Mathf.Clamp(body.height * 0.30f, 96f, 180f);
            Rect listRect = new Rect(body.x, body.y, body.width, Mathf.Max(0f, body.height - statsHeight - Gap));
            Rect statsRect = new Rect(body.x, listRect.yMax + Gap, body.width, statsHeight);
            Widgets.DrawBoxSolid(new Rect(body.x, listRect.yMax + 3f, body.width, 2f), UiColors.SectionSeparator);
            giverList.Draw(
                listRect,
                draft.Count,
                delegate(int index, Rect row)
                {
                    if (index < 0 || index >= draft.Count)
                    {
                        return;
                    }
                    string giverName = draft[index];
                    if (giverList.DraggingIndex == index)
                    {
                        Widgets.DrawBoxSolid(row, new Color(0.22f, 0.42f, 0.62f, 0.65f));
                    }
                    else if (giverList.IsDragMoved && Mouse.IsOver(row))
                    {
                        Widgets.DrawBoxSolid(row, new Color(0.55f, 0.72f, 0.35f, 0.40f));
                    }
                    else
                    {
                        UiZebra.Draw(row, index);
                    }
                    Widgets.Label(new Rect(row.x + 4f, row.y + 3f, 22f, 18f), (index + 1).ToString());
                    Widgets.LabelEllipses(new Rect(row.x + 26f, row.y + 3f, Mathf.Max(50f, row.width - 104f), 18f), giverName);
                    if (GiverChanged(giverName, index))
                    {
                        Widgets.Label(new Rect(row.xMax - 82f, row.y + 3f, 58f, 18f), "Changed");
                    }
                    Widgets.Label(new Rect(row.xMax - 20f, row.y + 3f, 18f, 18f), "≡");
                    if (Mouse.IsOver(row)) TooltipHandler.TipRegion(row, GiverTooltip(giverName));
                },
                null,
                null,
                MoveGiver,
                CommitGivers);
            DrawStats(statsRect, ref specializedStatsScroll);
        }

        private void DrawMetadataColumn(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, UiColors.PanelBody);
            float skillsHeight = Mathf.Clamp(rect.height * 0.58f, 150f, 420f);
            Rect skills = new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, skillsHeight);
            Rect stats = new Rect(rect.x + 6f, skills.yMax + 6f, rect.width - 12f,
                Mathf.Max(0f, rect.height - skillsHeight - 18f));

            bool hasVse = VseExpertiseAdapter.IsAvailable;
            Widgets.Label(new Rect(skills.x, skills.y, skills.width - (hasVse ? 156f : 82f), 20f), "Skills");
            if (hasVse)
            {
                Rect expertiseButton = new Rect(skills.xMax - 146f, skills.y, 70f, 20f);
                if (Widgets.ButtonText(expertiseButton, "Expertise"))
                {
                    VseExpertiseAdapter.OpenPanel(pawn);
                }
                TooltipHandler.TipRegion(expertiseButton, "Open the VSE expertise interface for this pawn.");
            }
            Rect filter = new Rect(skills.xMax - 74f, skills.y, 74f, 20f);
            if (Widgets.ButtonText(filter, "Filter"))
            {
                Find.WindowStack.Add(new Dialog_SkillFilter(
                    filterLevel, filterPassion, filterExpertise, filterMods,
                    hasVse, ApplySkillFilter));
            }
            TooltipHandler.TipRegion(filter, "Open skill filters.");
            DrawTopSkills(skills);
            Widgets.DrawBoxSolid(new Rect(rect.x + 6f, skills.yMax + 2f, rect.width - 12f, 2f),
                UiColors.SectionSeparator);
            DrawGenericStats(stats);
        }
















        private void DrawCompact(WorkPanelRects layout)
        {
            DrawWorkTypesColumn(layout.WorkTypes);
            DrawGiversColumn(layout.Givers);
            DrawMetadataColumn(layout.Metadata);
        }

        private void ShowAddWorkTypeMenu()
        {
            List<WorkTypeDef> candidates = AddableWorkTypes().ToList();
            if (candidates.Count == 0)
            {
                Messages.Message("No capable WorkTypes to add.", MessageTypeDefOf.RejectInput, false);
                return;
            }
            Find.WindowStack.Add(new WorkTypeDropdownWindow(
                "Add WorkType",
                candidates,
                null,
                delegate(WorkTypeDef workType)
                {
                    if (workType == null)
                    {
                        return;
                    }
                    if (!workDraft.Contains(workType.defName))
                    {
                        workDraft.Add(workType.defName);
                    }
                    selectedWorkType = workType.defName;
                    LoadSelectedGivers();
                    CommitWork();
                },
                AddWorkTypeLabel));
        }


        private string AddWorkTypeLabel(WorkTypeDef workType)
        {
            SkillRecord record = BestSkill(workType);
            if (record == null)
            {
                return workType.labelShort;
            }
            SkillDef skill = BestSkillDef(workType);
            return workType.labelShort + " · " + (skill != null ? skill.label : "skill")
                + " " + record.Level + " · " + WorkTypePaletteView.PassionText(record.passion);
        }


        private List<SkillRow> RelevantSkills(WorkTypeDef workType)
        {
            List<SkillRow> result = new List<SkillRow>();
            if (workType == null || workType.relevantSkills == null)
            {
                return result;
            }
            foreach (SkillDef skill in workType.relevantSkills)
            {
                if (skill == null)
                {
                    continue;
                }
                SkillRecord record = pawn.skills != null ? pawn.skills.GetSkill(skill) : null;
                result.Add(new SkillRow
                {
                    Label = skill.label,
                    Level = record != null ? record.Level : 0,
                    Passion = record != null ? record.passion : Passion.None
                });
            }
            return result;
        }

        private SkillRecord BestSkill(WorkTypeDef workType)
        {
            SkillRecord best = null;
            if (workType == null || workType.relevantSkills == null || pawn.skills == null)
            {
                return null;
            }
            foreach (SkillDef skill in workType.relevantSkills)
            {
                SkillRecord record = skill != null ? pawn.skills.GetSkill(skill) : null;
                if (record != null && (best == null || record.Level > best.Level
                    || (record.Level == best.Level && PassionRank(record.passion) > PassionRank(best.passion))))
                {
                    best = record;
                }
            }
            return best;
        }

        private SkillDef BestSkillDef(WorkTypeDef workType)
        {
            SkillDef best = null;
            int bestLevel = -1;
            if (workType == null || workType.relevantSkills == null || pawn.skills == null)
            {
                return null;
            }
            foreach (SkillDef skill in workType.relevantSkills)
            {
                SkillRecord record = skill != null ? pawn.skills.GetSkill(skill) : null;
                if (record != null && record.Level > bestLevel)
                {
                    best = skill;
                    bestLevel = record.Level;
                }
            }
            return best;
        }

        private string WorkTypeTooltip(WorkTypeDef workType, int rank)
        {
            string cached;
            if (workType != null && workTypeTooltipCache.TryGetValue(workType.defName, out cached))
            {
                return cached;
            }
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(workType.labelShort);
            if (!string.IsNullOrEmpty(workType.description))
            {
                builder.AppendLine(workType.description);
            }
            if (workType.relevantSkills != null && workType.relevantSkills.Count > 0)
            {
                builder.AppendLine("Skills:");
                foreach (SkillRow row in RelevantSkills(workType))
                {
                    builder.AppendLine("- " + row.Label + " " + row.Level + " (" + WorkTypePaletteView.PassionText(row.Passion) + ")");
                }
            }

            List<string> current;
            List<string> baseline;
            if (!giverDrafts.TryGetValue(workType.defName, out current))
            {
                current = WorkGiverOrderService.GetEffectiveOrder(pawn, workType);
            }
            baseline = giverBaselines.ContainsKey(workType.defName)
                ? giverBaselines[workType.defName]
                : WorkGiverOrderService.GetGlobalOrder(pawn, workType);
            if (current != null && current.Count > 0)
            {
                builder.AppendLine("Givers:");
                for (int i = 0; i < current.Count; i++)
                {
                    bool overridden = baseline == null
                        || baseline.IndexOf(current[i]) != i;
                    builder.AppendLine((i + 1) + ". " + current[i] + (overridden ? " *" : ""));
                }
            }
            string result = builder.ToString();
            if (workType != null)
            {
                workTypeTooltipCache[workType.defName] = result;
            }
            return result;
        }

        private string GiverTooltip(string giverDefName)
        {
            string cached;
            if (giverTooltipCache.TryGetValue(giverDefName, out cached))
            {
                return cached;
            }
            WorkGiverDef def = DefDatabase<WorkGiverDef>.GetNamedSilentFail(giverDefName);
            if (def == null)
            {
                return giverDefName;
            }
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(def.LabelCap);
            if (!string.IsNullOrEmpty(def.description))
            {
                builder.AppendLine(def.description);
            }
            if (!string.IsNullOrEmpty(def.verb))
            {
                builder.AppendLine("Verb: " + def.verb);
            }
            if (def.emergency)
            {
                builder.AppendLine("Emergency giver");
            }
            string result = builder.ToString();
            giverTooltipCache[giverDefName] = result;
            return result;
        }

        private List<WorkTypeDef> UnableWorkTypes()
        {
            return WorkOrderService.GetAvailableWorkTypes()
                .Where(w => w != null && WorkOrderService.IsIncapable(pawn, w))
                .ToList();
        }

        private IEnumerable<WorkTypeDef> AddableWorkTypes()
        {
            return WorkOrderService.GetAvailableWorkTypes()
                .Where(w => w != null && !WorkOrderService.IsIncapable(pawn, w) && !workDraft.Contains(w.defName));
        }

        private WorkTypeDef SelectedWorkType()
        {
            return string.IsNullOrEmpty(selectedWorkType)
                ? null
                : DefDatabase<WorkTypeDef>.GetNamedSilentFail(selectedWorkType);
        }


        private void MoveWorkType(int from, int to)
        {
            if (from < 0 || from >= workDraft.Count || to < 0 || to >= workDraft.Count || from == to)
            {
                return;
            }
            string value = workDraft[from];
            workDraft.RemoveAt(from);
            workDraft.Insert(to, value);
        }

        private void MoveGiver(int from, int to)
        {
            List<string> draft = CurrentGiverDraft();
            if (draft == null || from < 0 || from >= draft.Count || to < 0 || to >= draft.Count || from == to)
            {
                return;
            }
            string value = draft[from];
            draft.RemoveAt(from);
            draft.Insert(to, value);
        }

        private void LoadSelectedGivers()
        {
            WorkTypeDef workType = SelectedWorkType();
            if (workType != null)
            {
                LoadGiversFor(workType);
            }
        }

        private void LoadAllGiverDrafts()
        {
            foreach (string workTypeName in workDraft)
            {
                WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(workTypeName);
                if (workType != null)
                {
                    LoadGiversFor(workType);
                }
            }
        }

        private void LoadGiversFor(WorkTypeDef workType)
        {
            List<string> baseline = WorkGiverOrderService.GetGlobalOrder(pawn, workType);
            giverBaselines[workType.defName] = baseline;
            workTypeTooltipCache.Clear();
            if (!giverDrafts.ContainsKey(workType.defName))
            {
                List<string> stored = WorkGiverOrderService.GetStoredOrder(pawn, workType);
                List<string> initial = stored != null ? MergeWithBaseline(stored, baseline) : new List<string>(baseline);
                giverDrafts[workType.defName] = initial;
            }
        }

        private List<string> MergeWithBaseline(List<string> stored, List<string> baseline)
        {
            List<string> result = new List<string>(stored ?? new List<string>());
            foreach (string name in baseline)
            {
                if (!result.Contains(name))
                {
                    result.Add(name);
                }
            }
            return result;
        }

        private List<string> CurrentGiverDraft()
        {
            WorkTypeDef workType = SelectedWorkType();
            if (workType == null)
            {
                return null;
            }
            List<string> draft;
            return giverDrafts.TryGetValue(workType.defName, out draft) ? draft : null;
        }

        private List<string> CurrentGiverBaseline()
        {
            WorkTypeDef workType = SelectedWorkType();
            if (workType == null)
            {
                return null;
            }
            List<string> baseline;
            return giverBaselines.TryGetValue(workType.defName, out baseline) ? baseline : null;
        }


        private bool WorkTypeGiversChanged(string defName)
        {
            List<string> draft;
            List<string> baseline;
            return giverDrafts.TryGetValue(defName, out draft)
                && giverBaselines.TryGetValue(defName, out baseline)
                && !SameOrder(draft, baseline);
        }

        private bool GiverChanged(string name, int index)
        {
            List<string> baseline = CurrentGiverBaseline();
            if (baseline == null)
            {
                return false;
            }
            int baselineIndex = baseline.IndexOf(name);
            return baselineIndex != index;
        }

        private void InvalidateTooltips()
        {
            workTypeTooltipCache.Clear();
            giverTooltipCache.Clear();
            skillTooltipCache.Clear();
            skillTooltipCacheTick = -1;
        }

        private void CommitWork()
        {
            InvalidateTooltips();
            WorkOrderService.SaveAndApply(pawn, workDraft);
        }

        private void CommitGivers()
        {
            InvalidateTooltips();
            WorkTypeDef workType = SelectedWorkType();
            if (workType == null)
            {
                return;
            }
            List<string> draft = CurrentGiverDraft();
            List<string> baseline = CurrentGiverBaseline();
            if (draft == null || baseline == null)
            {
                return;
            }
            if (SameOrder(draft, baseline))
            {
                WorkGiverOrderService.ResetOrder(pawn, workType);
            }
            else
            {
                WorkGiverOrderService.SaveOrder(pawn, workType, draft);
            }
        }


        private static bool SameOrder(List<string> left, List<string> right)
        {
            if (left == null || right == null || left.Count != right.Count)
            {
                return false;
            }
            for (int i = 0; i < left.Count; i++)
            {
                if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }
            return true;
        }

        private static int PassionRank(Passion passion)
        {
            if (passion == Passion.Major) return 2;
            if (passion == Passion.Minor) return 1;
            return 0;
        }


        private struct SkillRow
        {
            public string Label;
            public int Level;
            public Passion Passion;
            public float XpCurrent;
            public float XpRequired;
            public float LearnFactor;
            public bool IsExpertise;
            public string Description;
        }
    }
}