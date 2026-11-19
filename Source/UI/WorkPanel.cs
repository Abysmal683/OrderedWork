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
    public sealed class WorkPanel
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
        private Vector2 metadataScroll;
        private int relevantMode;
        private readonly UiDragList workList = new UiDragList();
        private readonly UiDragList giverList = new UiDragList();
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
            if (giversExpanded)
            {
                DrawGiversColumn(layout.Givers);
                DrawMetadataColumn(layout.Metadata);
            }
            else
            {
                DrawMetadataColumn(layout.Metadata);
            }
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;
            workDraft = WorkOrderService.GetOrder(pawn) ?? new List<string>();
            selectedWorkType = workDraft.FirstOrDefault() ?? AddableWorkTypes().FirstOrDefault()?.defName;
            giverDrafts.Clear();
            giverBaselines.Clear();
            LoadSelectedGivers();
        }

        private void Reload()
        {
            initialized = false;
            EnsureInitialized();
        }

        private void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y, 190f, 26f), "Trabajo");
            Text.Font = GameFont.Small;
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
                selectedWorkType != null, "Show or hide the WorkGiver column for the selected WorkType."))
            {
                giversExpanded = !giversExpanded;
                if (giversExpanded)
                {
                    LoadSelectedGivers();
                }
            }
            if (UiButtonBar.Button(new Rect(header.xMax - 68f, header.y, 62f, 22f), "Agregar", true,
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
                    Widgets.LabelEllipses(new Rect(row.x + 26f, row.y + 3f, Mathf.Max(50f, row.width - 156f), 18f), workType.labelShort);
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
                    TooltipHandler.TipRegion(row, WorkTypeTooltip(workType, index + 1));
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
            Widgets.Label(new Rect(rect.x + 6f, rect.y + 3f, rect.width - 12f, 20f), "No puede");
            Rect list = new Rect(rect.x + 6f, rect.y + 24f, rect.width - 12f, Mathf.Max(0f, rect.height - 26f));
            if (unable.Count == 0)
            {
                Widgets.Label(list, "Sin limitaciones");
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
                    TooltipHandler.TipRegion(row, WorkTypeTooltip(unable[i], -1));
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
                Widgets.Label(body, "Seleccioná un WorkType.");
                return;
            }

            float statsHeight = Mathf.Clamp(body.height * 0.30f, 96f, 180f);
            Rect listRect = new Rect(body.x, body.y, body.width, Mathf.Max(0f, body.height - statsHeight - Gap));
            Rect statsRect = new Rect(body.x, listRect.yMax + Gap, body.width, statsHeight);
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
                    TooltipHandler.TipRegion(row, GiverTooltip(giverName));
                },
                null,
                null,
                MoveGiver,
                CommitGivers);
            DrawStats(statsRect);
        }

        private void DrawMetadataColumn(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, UiColors.PanelBody);
            Rect skills = new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, Mathf.Max(0f, rect.height - 12f));

            Widgets.Label(new Rect(skills.x, skills.y, skills.width - 58f, 20f), "Top 5 habilidades");
            string modeLabel = relevantMode == 0 ? "Lv" : relevantMode == 1 ? "P" : "Lv.P";
            if (UiButtonBar.Button(new Rect(skills.xMax - 48f, skills.y, 48f, 20f), modeLabel, true,
                "Filter: Lv = level, P = passion, Lv.P = both."))
            {
                relevantMode = (relevantMode + 1) % 3;
            }
            DrawTopSkills(skills);
        }

        private void DrawTopSkills(Rect rect)
        {
            float y = rect.y + 24f;
            List<SkillRow> skills = RankedSkills();
            if (skills.Count == 0)
            {
                Widgets.Label(new Rect(rect.x, y, rect.width, 40f), "Sin coincidencias para este filtro.");
                return;
            }
            for (int i = 0; i < skills.Count; i++)
            {
                SkillRow entry = skills[i];
                Rect row = new Rect(rect.x, y, rect.width, 22f);
                UiZebra.Draw(row, i);
                Widgets.LabelEllipses(new Rect(row.x + 2f, row.y + 2f, row.width - 132f, 18f), entry.Label);
                Rect icon = new Rect(row.xMax - 110f, row.y + 3f, 16f, 16f);
                WorkTypePaletteView.DrawPassionIcon(icon, entry.Passion);
                Widgets.Label(new Rect(row.xMax - 90f, row.y + 2f, 24f, 18f), entry.Level.ToString());
                DrawSkillBar(new Rect(row.xMax - 62f, row.y + 7f, 58f, 8f), entry);
                TooltipHandler.TipRegion(row, SkillTooltip(entry));
                y += 22f;
            }
        }

        private static void DrawSkillBar(Rect rect, SkillRow row)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.12f, 0.14f, 0.16f, 0.9f));
            float percent = row.XpRequired > 0.01f
                ? Mathf.Clamp01(row.XpCurrent / row.XpRequired)
                : 1f;
            if (percent > 0f)
            {
                Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width * percent, rect.height),
                    new Color(0.42f, 0.68f, 0.34f, 0.9f));
            }
        }

        private static string SkillTooltip(SkillRow row)
        {
            return row.Label
                + "\nPassion: " + WorkTypePaletteView.PassionText(row.Passion)
                + "\nLearning multiplier: x" + row.LearnFactor.ToString("0.00")
                + "\nXP: " + row.XpCurrent.ToString("0") + " / " + row.XpRequired.ToString("0");
        }

        private List<SkillRow> RankedSkills()
        {
            List<SkillRow> candidates = new List<SkillRow>();
            if (pawn.skills == null)
            {
                return candidates;
            }
            foreach (SkillDef skill in DefDatabase<SkillDef>.AllDefsListForReading)
            {
                SkillRecord record = skill != null ? pawn.skills.GetSkill(skill) : null;
                if (record == null)
                {
                    continue;
                }
                bool level = record.Level > 0;
                bool passion = record.passion != Passion.None;
                if (relevantMode == 0 && !level) continue;
                if (relevantMode == 1 && !passion) continue;
                if (relevantMode == 2 && (!level || !passion)) continue;
                candidates.Add(new SkillRow
                {
                    Label = skill.label,
                    Level = record.Level,
                    Passion = record.passion,
                    XpCurrent = record.xpSinceLastLevel,
                    XpRequired = record.XpRequiredForLevelUp,
                    LearnFactor = record.passion == Passion.Major ? SkillRecord.LearnFactorPassionMajor
                        : record.passion == Passion.Minor ? SkillRecord.LearnFactorPassionMinor
                        : SkillRecord.LearnFactorPassionNone
                });
            }

            if (relevantMode == 0)
            {
                return candidates.OrderByDescending(r => r.Level)
                    .ThenByDescending(r => PassionRank(r.Passion))
                    .Take(5)
                    .ToList();
            }
            return candidates.OrderByDescending(SkillScore)
                .ThenByDescending(r => r.Level)
                .Take(5)
                .ToList();
        }

        private int SkillScore(SkillRow row)
        {
            PassionWeightEntry entry = PassionWeightPolicy.Find(OrderedWorkMod.Settings, row.Passion);
            bool prioritizeLowSkill = OrderedWorkMod.Settings == null
                || OrderedWorkMod.Settings.prioritizeLowSkillPassion;
            return RecommendationScorePolicy.Score(
                row.Level,
                entry != null,
                entry != null ? entry.level0Score : 0,
                entry != null ? entry.perLevelBonus : 0,
                entry != null ? entry.minLevel : 0,
                prioritizeLowSkill);
        }

        private void DrawStats(Rect rect)
        {
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 20f), "Stats del oficio");
            Rect body = new Rect(rect.x, rect.y + 24f, rect.width, Mathf.Max(0f, rect.height - 24f));
            Rect view = new Rect(0f, 0f, body.width - 16f, Mathf.Max(body.height, 170f));
            Widgets.BeginScrollView(body, ref metadataScroll, view);
            try
            {
                float y = 0f;
                DrawStatLine(view.width, ref y, "Work Speed", SafeStat(StatDefOf.WorkSpeedGlobal));
                DrawStatLine(view.width, ref y, "Move Speed", SafeStat(StatDefOf.MoveSpeed));
                StatDef relevant = RelevantWorkStat(SelectedWorkType());
                DrawStatLine(view.width, ref y, relevant.label, SafeStat(relevant));
                Widgets.Label(new Rect(0f, y + 8f, view.width, 40f),
                    "Stats específicos y desglose quedan para la siguiente versión.");
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        private float SafeStat(StatDef stat)
        {
            if (stat == null)
            {
                return 0f;
            }
            try
            {
                return pawn.GetStatValue(stat);
            }
            catch (Exception)
            {
                return 0f;
            }
        }

        private static void DrawStatLine(float width, ref float y, string label, float value)
        {
            Rect row = new Rect(0f, y, width, 20f);
            Widgets.Label(row, label + ": " + value.ToString("0.##"));
            y += 20f;
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
                Messages.Message("No hay WorkTypes capaces para agregar.", MessageTypeDefOf.RejectInput, false);
                return;
            }
            Find.WindowStack.Add(new WorkTypeDropdownWindow(
                "Agregar WorkType",
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

        private bool MatchesRelevantFilter(WorkTypeDef workType)
        {
            SkillRecord record = BestSkill(workType);
            if (record == null)
            {
                return false;
            }
            bool level = record.Level > 0;
            bool passion = record.passion != Passion.None;
            if (relevantMode == 0) return level;
            if (relevantMode == 1) return passion;
            return level && passion;
        }

        private static StatDef RelevantWorkStat(WorkTypeDef workType)
        {
            if (workType != null)
            {
                switch (workType.defName)
                {
                    case "Construction": return StatDefOf.ConstructionSpeed;
                    case "Mining": return StatDefOf.MiningSpeed;
                    case "Growing": return StatDefOf.PlantWorkSpeed;
                    case "Cooking": return StatDefOf.GeneralLaborSpeed;
                    case "Research": return StatDefOf.ResearchSpeed;
                    case "Doctor": return StatDefOf.MedicalTendSpeed;
                    case "Cleaning": return StatDefOf.CleaningSpeed;
                    case "Fishing": return StatDefOf.FishingSpeed;
                }
            }
            return StatDefOf.GeneralLaborSpeed;
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
            return builder.ToString();
        }

        private static string GiverTooltip(string giverDefName)
        {
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
            return builder.ToString();
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
            if (workType == null)
            {
                return;
            }
            List<string> baseline = WorkGiverOrderService.GetGlobalOrder(pawn, workType);
            giverBaselines[workType.defName] = baseline;
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

        private void CommitWork()
        {
            WorkOrderService.SaveAndApply(pawn, workDraft);
        }

        private void CommitGivers()
        {
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
        }
    }
}