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





        private WorkTypeDef SelectedWorkType()
        {
            return string.IsNullOrEmpty(selectedWorkType)
                ? null
                : DefDatabase<WorkTypeDef>.GetNamedSilentFail(selectedWorkType);
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