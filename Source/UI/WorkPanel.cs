using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OrderedWork.Config;
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
        private List<string> workSaved = new List<string>();
        private List<string> workBaseline = new List<string>();
        private string selectedWorkType;
        private bool giversExpanded = true;
        private readonly Dictionary<string, List<string>> giverDrafts =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> giverBaselines =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> giverSaved =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);

        private Vector2 workScroll;
        private Vector2 unableScroll;
        private Vector2 giverScroll;
        private Vector2 metadataScroll;
        private int relevantMode;
        private int dragKind;
        private int dragIndex = -1;
        private float dragStartY;
        private bool dragMoved;

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

            Rect header = new Rect(area.x, area.y, area.width, HeaderHeight);
            DrawHeader(header);

            Rect body = new Rect(area.x, header.yMax + 4f, area.width, Mathf.Max(0f, area.height - HeaderHeight - 30f));
            Rect footer = new Rect(area.x, area.yMax - 24f, area.width, 24f);
            DrawFooter(footer);
            if (body.width < 940f)
            {
                DrawCompact(body);
                return;
            }

            float leftWidth = Mathf.Clamp(body.width * 0.31f, 280f, 390f);
            float middleWidth = giversExpanded ? Mathf.Clamp(body.width * 0.34f, 300f, 430f) : 0f;
            float metadataWidth = Mathf.Max(260f, body.width - leftWidth - middleWidth - (giversExpanded ? Gap * 2f : Gap));
            Rect left = new Rect(body.x, body.y, leftWidth, body.height);
            DrawWorkTypesColumn(left);

            if (giversExpanded)
            {
                Rect middle = new Rect(left.xMax + Gap, body.y, middleWidth, body.height);
                DrawGiversColumn(middle);
                Rect metadata = new Rect(middle.xMax + Gap, body.y, metadataWidth, body.height);
                DrawMetadataColumn(metadata);
            }
            else
            {
                Rect metadata = new Rect(left.xMax + Gap, body.y, metadataWidth, body.height);
                DrawMetadataColumn(metadata);
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
            workSaved = new List<string>(workDraft);
            workBaseline = BuildGlobalWorkTypeOrder();
            selectedWorkType = workDraft.FirstOrDefault() ?? AddableWorkTypes().FirstOrDefault()?.defName;
            giverDrafts.Clear();
            giverBaselines.Clear();
            giverSaved.Clear();
            LoadSelectedGivers();
        }

        private void Reload()
        {
            initialized = false;
            EnsureInitialized();
        }

        private void DrawHeader(Rect rect)
        {
            bool workChanged = !SameOrder(workDraft, workSaved);
            bool giverChanged = giverDrafts.Any(pair =>
            {
                List<string> saved;
                return !giverSaved.TryGetValue(pair.Key, out saved) || !SameOrder(pair.Value, saved);
            });
            bool changed = workChanged || giverChanged;
            float buttonWidth = 86f;
            Rect apply = new Rect(rect.xMax - buttonWidth * 3f - 12f, rect.y + 2f, buttonWidth, 24f);
            if (UiButtonBar.Button(apply, "Apply", changed, changed
                ? "Apply this pawn's Work order and WorkGiver overrides."
                : "No unpublished Work changes."))
            {
                ApplyDrafts();
            }
            if (UiButtonBar.Button(new Rect(apply.xMax + 4f, apply.y, buttonWidth, 24f), "Cancel", changed,
                "Discard the Work draft and reload the saved per-pawn state."))
            {
                Reload();
            }
            if (UiButtonBar.Button(new Rect(apply.xMax + buttonWidth + 8f, apply.y, buttonWidth, 24f), "Recommend", true,
                "Fill the WorkType draft with the existing recommendation. WorkGiver overrides are untouched."))
            {
                workDraft = WorkOrderService.GetRecommendation(pawn);
                selectedWorkType = workDraft.FirstOrDefault() ?? selectedWorkType;
                LoadSelectedGivers();
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y, 190f, 26f), "Trabajo");
            Text.Font = GameFont.Small;
        }

        private void DrawWorkTypesColumn(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, UiColors.PanelBody);
            Rect header = new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, 24f);
            Widgets.Label(new Rect(header.x, header.y, header.width - 168f, 22f), "Work types");
            if (UiButtonBar.Button(new Rect(header.xMax - 164f, header.y, 78f, 22f), "Givers",
                selectedWorkType != null, "Show or hide the WorkGiver column for the selected WorkType."))
            {
                giversExpanded = !giversExpanded;
                if (giversExpanded)
                {
                    LoadSelectedGivers();
                }
            }
            if (UiButtonBar.Button(new Rect(header.xMax - 82f, header.y, 82f, 22f), "+ Agregar", true,
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
            List<Rect> rows = new List<Rect>();
            Rect view = new Rect(0f, 0f, rect.width - 16f, Mathf.Max(rect.height, workDraft.Count * RowHeight));
            Widgets.BeginScrollView(rect, ref workScroll, view);
            try
            {
                for (int i = 0; i < workDraft.Count; i++)
                {
                    WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(workDraft[i]);
                    if (workType == null)
                    {
                        continue;
                    }
                    Rect row = new Rect(0f, i * RowHeight, view.width, RowHeight - 2f);
                    Rect hit = new Rect(rect.x + row.x, rect.y + row.y - workScroll.y, row.width, row.height);
                    if (dragKind == 1 && dragIndex == i)
                    {
                        Widgets.DrawBoxSolid(row, new Color(0.22f, 0.42f, 0.62f, 0.65f));
                    }
                    else if (dragKind == 1 && dragMoved && dragIndex >= 0 && hit.Contains(Event.current.mousePosition))
                    {
                        Widgets.DrawBoxSolid(row, new Color(0.55f, 0.72f, 0.35f, 0.40f));
                    }
                    else
                    {
                        UiZebra.Draw(row, i);
                    }
                    if (selectedWorkType == workType.defName)
                    {
                        Widgets.DrawHighlight(row);
                    }

                    Widgets.Label(new Rect(row.x + 4f, row.y + 3f, 22f, 18f), (i + 1).ToString());
                    Widgets.LabelEllipses(new Rect(row.x + 26f, row.y + 3f, Mathf.Max(50f, row.width - 156f), 18f), workType.labelShort);
                    if (WorkTypeGiversChanged(workType.defName))
                    {
                        Widgets.Label(new Rect(row.xMax - 126f, row.y + 3f, 58f, 18f), "Changed");
                    }
                    Rect remove = new Rect(row.xMax - 46f, row.y + 1f, 18f, 20f);
                    if (Widgets.ButtonText(remove, "x"))
                    {
                        workDraft.RemoveAt(i);
                        if (selectedWorkType == workType.defName)
                        {
                            selectedWorkType = workDraft.FirstOrDefault();
                            LoadSelectedGivers();
                        }
                        break;
                    }
                    Rect handle = new Rect(row.xMax - 24f, row.y + 1f, 20f, 20f);
                    Widgets.Label(handle, "≡");
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && hit.Contains(Event.current.mousePosition)
                        && !remove.Contains(Event.current.mousePosition))
                    {
                        selectedWorkType = workType.defName;
                        giversExpanded = true;
                        LoadSelectedGivers();
                    }
                    TooltipHandler.TipRegion(hit, WorkTypeTooltip(workType, i + 1));
                    rows.Add(hit);
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
            HandleDrag(1, rows, MoveWorkType);
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
            Widgets.Label(new Rect(header.x, header.y, header.width - 86f, 22f), title);
            List<string> draft = CurrentGiverDraft();
            List<string> baseline = CurrentGiverBaseline();
            bool changed = draft != null && baseline != null && !SameOrder(draft, baseline);
            if (UiButtonBar.Button(new Rect(header.xMax - 82f, header.y, 82f, 22f), "Reset", changed && workType != null,
                "Reset this WorkType's giver order to the saved global JSON order."))
            {
                if (workType != null)
                {
                    giverDrafts[workType.defName] = new List<string>(baseline ?? new List<string>());
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
            List<Rect> rows = new List<Rect>();
            Rect view = new Rect(0f, 0f, listRect.width - 16f, Mathf.Max(listRect.height, draft.Count * RowHeight));
            Widgets.BeginScrollView(listRect, ref giverScroll, view);
            try
            {
                for (int i = 0; i < draft.Count; i++)
                {
                    string giverName = draft[i];
                    Rect row = new Rect(0f, i * RowHeight, view.width, RowHeight - 2f);
                    Rect hit = new Rect(listRect.x + row.x, listRect.y + row.y - giverScroll.y, row.width, row.height);
                    if (dragKind == 2 && dragIndex == i)
                    {
                        Widgets.DrawBoxSolid(row, new Color(0.22f, 0.42f, 0.62f, 0.65f));
                    }
                    else if (dragKind == 2 && dragMoved && dragIndex >= 0 && hit.Contains(Event.current.mousePosition))
                    {
                        Widgets.DrawBoxSolid(row, new Color(0.55f, 0.72f, 0.35f, 0.40f));
                    }
                    else
                    {
                        UiZebra.Draw(row, i);
                    }
                    Widgets.Label(new Rect(row.x + 4f, row.y + 3f, 22f, 18f), (i + 1).ToString());
                    Widgets.LabelEllipses(new Rect(row.x + 26f, row.y + 3f, Mathf.Max(50f, row.width - 46f), 18f), giverName);
                    Widgets.Label(new Rect(row.xMax - 20f, row.y + 3f, 18f, 18f), "≡");
                    rows.Add(hit);
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
            HandleDrag(2, rows, MoveGiver);
            DrawStats(statsRect);
        }

        private void DrawMetadataColumn(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, UiColors.PanelBody);
            Rect skills = new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, Mathf.Max(0f, rect.height - 12f));

            Widgets.Label(new Rect(skills.x, skills.y, skills.width - 58f, 20f), "Top 5 trabajos");
            string modeLabel = relevantMode == 0 ? "Lv" : relevantMode == 1 ? "P" : "Lv.P";
            if (UiButtonBar.Button(new Rect(skills.xMax - 48f, skills.y, 48f, 20f), modeLabel, true,
                "Filter: Lv = level, P = passion, Lv.P = both."))
            {
                relevantMode = (relevantMode + 1) % 3;
            }
            DrawTopWorkTypes(skills);
        }

        private void DrawTopWorkTypes(Rect rect)
        {
            float y = rect.y + 24f;
            List<WorkTypeDef> workTypes = RankedRelevantWorkTypes();
            if (workTypes.Count == 0)
            {
                Widgets.Label(new Rect(rect.x, y, rect.width, 40f), "Sin coincidencias para este filtro.");
                return;
            }
            for (int i = 0; i < workTypes.Count; i++)
            {
                WorkTypeDef workType = workTypes[i];
                Rect row = new Rect(rect.x, y, rect.width, 22f);
                UiZebra.Draw(row, i);
                if (workType.defName == selectedWorkType)
                {
                    Widgets.DrawHighlight(row);
                }
                Widgets.LabelEllipses(new Rect(row.x + 2f, row.y + 2f, row.width - 112f, 18f), workType.labelShort);
                SkillRecord record = BestSkill(workType);
                SkillDef skill = BestSkillDef(workType);
                Rect icon = new Rect(row.xMax - 102f, row.y + 3f, 16f, 16f);
                WorkTypePaletteView.DrawPassionIcon(icon, record != null ? record.passion : Passion.None);
                Widgets.Label(new Rect(row.xMax - 82f, row.y + 2f, 24f, 18f),
                    record != null ? record.Level.ToString() : "—");
                Widgets.LabelEllipses(new Rect(row.xMax - 56f, row.y + 2f, 56f, 18f),
                    skill != null ? skill.label : "—");
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && row.Contains(Event.current.mousePosition))
                {
                    selectedWorkType = workType.defName;
                    giversExpanded = true;
                    LoadSelectedGivers();
                }
                TooltipHandler.TipRegion(row, WorkTypeTooltip(workType, -1));
                y += 22f;
            }
        }

        private List<WorkTypeDef> RankedRelevantWorkTypes()
        {
            List<WorkTypeDef> candidates = WorkOrderService.GetAvailableWorkTypes()
                .Where(w => w != null && !WorkOrderService.IsIncapable(pawn, w))
                .Where(w =>
                {
                    int level = RecommendationScorer.BestSkillLevel(pawn, w, out Passion passion);
                    if (relevantMode == 0) return level > 0;
                    if (relevantMode == 1) return passion != Passion.None;
                    return level > 0 && passion != Passion.None;
                })
                .ToList();

            if (relevantMode == 0)
            {
                return candidates
                    .OrderByDescending(w => RecommendationScorer.BestSkillLevel(pawn, w, out _))
                    .ThenByDescending(w => RecommendationScorer.Score(pawn, w, OrderedWorkMod.Settings))
                    .Take(5)
                    .ToList();
            }
            return candidates
                .OrderByDescending(w => RecommendationScorer.Score(pawn, w, OrderedWorkMod.Settings))
                .ThenByDescending(w => RecommendationScorer.BestSkillLevel(pawn, w, out _))
                .Take(5)
                .ToList();
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

        private void DrawFooter(Rect rect)
        {
            bool hasOverrides = WorkGiverOrderService.HasAnyOverride(pawn);
            if (UiButtonBar.Button(new Rect(rect.x, rect.y, 176f, 22f), "Reset all WorkGivers",
                hasOverrides, hasOverrides
                    ? "Reset every WorkGiver override for this pawn. WorkType order is not changed."
                    : "This pawn has no WorkGiver overrides."))
            {
                Dialog_MessageBox.CreateConfirmation(
                    "Reset all WorkGiver overrides for " + pawn.LabelShort + "? WorkType order will not change.",
                    delegate
                    {
                        WorkGiverOrderService.ResetAll(pawn);
                        giverDrafts.Clear();
                        giverBaselines.Clear();
                        LoadSelectedGivers();
                    },
                    true);
            }
        }

        private void DrawCompact(Rect area)
        {
            Rect view = new Rect(0f, 0f, area.width, Mathf.Max(area.height, 980f));
            Widgets.BeginScrollView(area, ref workScroll, view);
            try
            {
                float width = view.width;
                DrawWorkTypesColumn(new Rect(0f, 0f, width, 420f));
                DrawGiversColumn(new Rect(0f, 428f, width, 300f));
                DrawMetadataColumn(new Rect(0f, 736f, width, 240f));
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        private void DrawHeaderButtons(Rect rect)
        {
            // Reserved for future panel-level actions; keeps header geometry in one place.
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
                },
                AddWorkTypeLabel));
        }

        private void ShowRelevantSearch()
        {
            List<WorkTypeDef> candidates = WorkOrderService.GetAvailableWorkTypes()
                .Where(w => w != null && !WorkOrderService.IsIncapable(pawn, w))
                .Where(MatchesRelevantFilter)
                .ToList();
            if (candidates.Count == 0)
            {
                Messages.Message("No hay WorkTypes que coincidan con el filtro.", MessageTypeDefOf.RejectInput, false);
                return;
            }
            Find.WindowStack.Add(new WorkTypeDropdownWindow(
                "Buscar relevante",
                candidates,
                null,
                delegate(WorkTypeDef workType)
                {
                    if (workType != null)
                    {
                        selectedWorkType = workType.defName;
                        giversExpanded = true;
                        LoadSelectedGivers();
                    }
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
            if (rank > 0)
            {
                builder.AppendLine("Rank: " + rank);
            }
            if (workType.relevantSkills != null && workType.relevantSkills.Count > 0)
            {
                builder.AppendLine("Skills:");
                foreach (SkillRow row in RelevantSkills(workType))
                {
                    builder.AppendLine("- " + row.Label + " " + row.Level + " (" + WorkTypePaletteView.PassionText(row.Passion) + ")");
                }
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

        private List<string> BuildGlobalWorkTypeOrder()
        {
            List<WorkTypeDef> all = WorkOrderService.GetAvailableWorkTypes();
            List<string> result = new List<string>();
            HashSet<string> added = new HashSet<string>(StringComparer.Ordinal);
            WorkConfigProfile profile = null;
            try
            {
                WorkConfigLoadResult loaded = WorkConfigStore.LoadActiveWithFallback(
                    WorkConfigStore.LoadSelectedProfileName());
                profile = loaded != null ? loaded.Profile : null;
            }
            catch (Exception)
            {
                profile = null;
            }
            if (profile != null && profile.WorkTypes != null)
            {
                foreach (KeyValuePair<string, WorkTypeConfig> pair in profile.WorkTypes
                    .Where(p => p.Value != null)
                    .OrderBy(p => p.Value.Order))
                {
                    WorkTypeDef workType = all.FirstOrDefault(w => w.defName == pair.Key);
                    if (workType != null && !WorkOrderService.IsIncapable(pawn, workType) && added.Add(workType.defName))
                    {
                        result.Add(workType.defName);
                    }
                }
            }
            foreach (WorkTypeDef workType in all)
            {
                if (workType != null && !WorkOrderService.IsIncapable(pawn, workType) && added.Add(workType.defName))
                {
                    result.Add(workType.defName);
                }
            }
            return result;
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
                giverSaved[workType.defName] = new List<string>(initial);
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

        private bool WorkTypeChanged(int index)
        {
            if (index < 0 || index >= workDraft.Count)
            {
                return false;
            }
            int baselineIndex = workBaseline.IndexOf(workDraft[index]);
            return baselineIndex != index;
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

        private void ApplyDrafts()
        {
            WorkOrderService.SaveAndApply(pawn, workDraft);
            foreach (KeyValuePair<string, List<string>> pair in giverDrafts)
            {
                WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(pair.Key);
                if (workType == null)
                {
                    continue;
                }
                List<string> baseline;
                if (!giverBaselines.TryGetValue(pair.Key, out baseline) || SameOrder(pair.Value, baseline))
                {
                    WorkGiverOrderService.ResetOrder(pawn, workType);
                }
                else
                {
                    WorkGiverOrderService.SaveOrder(pawn, workType, pair.Value);
                }
            }
            workBaseline = BuildGlobalWorkTypeOrder();
            workDraft = WorkOrderService.GetOrder(pawn) ?? workDraft;
            workSaved = new List<string>(workDraft);
            foreach (KeyValuePair<string, List<string>> pair in giverDrafts)
            {
                giverSaved[pair.Key] = new List<string>(pair.Value);
            }
            Messages.Message("Work aplicado para " + pawn.LabelShort + ".", MessageTypeDefOf.TaskCompletion, false);
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

        private void HandleDrag(int kind, List<Rect> rows, Action<int, int> move)
        {
            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    Rect handle = new Rect(rows[i].xMax - 24f, rows[i].y, 20f, rows[i].height);
                    bool rowDrag = kind == 2 ? rows[i].Contains(current.mousePosition)
                        : rows[i].Contains(current.mousePosition) && current.mousePosition.x < rows[i].xMax - 30f;
                    if (handle.Contains(current.mousePosition) || rowDrag)
                    {
                        dragKind = kind;
                        dragIndex = i;
                        dragStartY = current.mousePosition.y;
                        dragMoved = false;
                        current.Use();
                        return;
                    }
                }
            }
            if (dragKind != kind || dragIndex < 0)
            {
                return;
            }
            if (current.type == EventType.MouseDrag)
            {
                if (!dragMoved && Mathf.Abs(current.mousePosition.y - dragStartY) > 4f)
                {
                    dragMoved = true;
                }
                if (dragMoved)
                {
                    for (int i = 0; i < rows.Count; i++)
                    {
                        if (i != dragIndex && rows[i].Contains(current.mousePosition))
                        {
                            move(dragIndex, i);
                            dragIndex = i;
                            break;
                        }
                    }
                    current.Use();
                }
            }
            else if (current.type == EventType.MouseUp)
            {
                dragKind = 0;
                dragIndex = -1;
                dragMoved = false;
            }
        }

        private struct SkillRow
        {
            public string Label;
            public int Level;
            public Passion Passion;
        }
    }
}
