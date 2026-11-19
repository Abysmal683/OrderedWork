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
        private Vector2 stackedScroll;
        private Vector2 specializedStatsScroll;
        private readonly WorkSkillFilter skillFilter = new WorkSkillFilter();
        private readonly UiDragList workList = new UiDragList();
        private readonly UiDragList giverList = new UiDragList();
        private readonly Dictionary<string, string> workTypeTooltipCache =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> giverTooltipCache =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> skillTooltipCache =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private int skillTooltipCacheTick = -1;
        private readonly StatTooltipService statTooltips;
        private bool resetAllArmed;
        private float resetAllArmedUntil;

        public WorkPanel(Pawn pawn)
        {
            this.pawn = pawn;
            statTooltips = new StatTooltipService(pawn);
        }

        public void Draw(Rect area, bool forceStacked = false)
        {
            if (pawn == null || pawn.Destroyed)
            {
                Widgets.Label(area, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Main.PawnUnavailable", "Pawn unavailable."));
                return;
            }
            EnsureInitialized();

            WorkPanelRects layout = WorkPanelLayout.Build(area, giversExpanded, forceStacked);
            DrawHeader(layout.Header);
            if (layout.Stacked)
            {
                DrawStacked(layout);
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
            skillFilter.Load(OrderedWorkMod.Settings);
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
            Widgets.Label(new Rect(skills.x, skills.y, skills.width - (hasVse ? 156f : 82f), 20f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Skills.Title", "Skills"));
            if (hasVse)
            {
                Rect expertiseButton = new Rect(skills.xMax - 146f, skills.y, 70f, 20f);
                if (Widgets.ButtonText(expertiseButton, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Skills.Expertise", "Expertise")))
                {
                    VseExpertiseAdapter.OpenPanel(pawn);
                }
                TooltipHandler.TipRegion(expertiseButton, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Skills.OpenExpertise", "Open the VSE expertise interface for this pawn."));
            }
            Rect filter = new Rect(skills.xMax - 74f, skills.y, 74f, 20f);
            if (Widgets.ButtonText(filter, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Skills.Filter", "Filter")))
            {
                Find.WindowStack.Add(new Dialog_SkillFilter(
                    skillFilter.Level, skillFilter.Passion, skillFilter.Expertise, skillFilter.Mods,
                    hasVse, ApplySkillFilter));
            }
            TooltipHandler.TipRegion(filter, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Skills.OpenFilters", "Open skill filters."));
            DrawTopSkills(skills);
            Widgets.DrawBoxSolid(new Rect(rect.x + 6f, skills.yMax + 2f, rect.width - 12f, 2f),
                UiColors.SectionSeparator);
            DrawGenericStats(stats);
        }
















        private void DrawStacked(WorkPanelRects layout)
        {
            const float workHeight = 330f;
            const float giversHeight = 260f;
            const float metadataHeight = 300f;
            const float gap = 8f;
            Rect viewport = layout.Body;
            Rect view = new Rect(0f, 0f, viewport.width,
                workHeight + gap + giversHeight + gap + metadataHeight);
            Widgets.BeginScrollView(viewport, ref stackedScroll, view);
            try
            {
                DrawWorkTypesColumn(new Rect(0f, 0f, view.width, workHeight));
                Widgets.DrawBoxSolid(new Rect(0f, workHeight + 3f, view.width, 2f), UiColors.SectionSeparator);
                DrawGiversColumn(new Rect(0f, workHeight + gap, view.width, giversHeight));
                Widgets.DrawBoxSolid(new Rect(0f, workHeight + gap + giversHeight + 3f, view.width, 2f), UiColors.SectionSeparator);
                DrawMetadataColumn(new Rect(0f, workHeight + gap + giversHeight + gap, view.width, metadataHeight));
            }
            finally
            {
                Widgets.EndScrollView();
            }
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