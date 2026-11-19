using System.Collections.Generic;
using System.Linq;
using OrderedWork.Data;
using OrderedWork.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    public partial class Dialog_WorkControlDetails
    {
        private void DrawOverview(Rect content)
        {
            List<SkillRecord> topSkills = pawn.skills != null && pawn.skills.skills != null
                ? pawn.skills.skills.Where(s => s != null).OrderByDescending(s => s.Level).Take(8).ToList()
                : new List<SkillRecord>();
            List<string> order = WorkOrderService.GetOrder(pawn);
            PawnWorkOrder entry = OrderedWorkGameComponent.GetOrCreate()?.GetEntry(pawn);

            float contentHeight = 32f + 22f + 22f + (topSkills.Count * 20f) + 10f + 22f
                + (Mathf.Max(1, order.Count) * 20f) + 132f;
            Rect scrollContent = new Rect(0f, 0f, content.width - 20f, Mathf.Max(content.height, contentHeight));

            Widgets.BeginScrollView(content, ref overviewScroll, scrollContent);
            try
            {
                float y = 0f;
                if (pawn.story != null && !string.IsNullOrEmpty(pawn.story.Title))
                {
                    Widgets.Label(new Rect(0f, y, scrollContent.width, 20f), pawn.story.Title);
                }
                y += 22f;

                Widgets.Label(new Rect(0f, y, scrollContent.width, 20f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overview.TopSkills", "Top skills"));
                y += 22f;
                if (topSkills.Count == 0)
                {
                    Widgets.Label(new Rect(6f, y, scrollContent.width - 6f, 20f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overview.NoSkills", "No skills."));
                    y += 20f;
                }
                else
                {
                    foreach (SkillRecord skill in topSkills)
                    {
                        WorkTypePaletteView.DrawPassionIcon(
                            new Rect(4f, y + 4f, WorkTypePaletteView.PassionIconSize, WorkTypePaletteView.PassionIconSize),
                            skill.passion);
                        Widgets.Label(new Rect(20f, y, scrollContent.width - 20f, 20f),
                            skill.def.label + ": " + skill.Level);
                        y += 20f;
                    }
                }

                y += 10f;
                Widgets.Label(new Rect(0f, y, scrollContent.width, 20f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overview.WorkOrder", "Work order"));
                y += 22f;
                if (order.Count == 0)
                {
                    Widgets.Label(new Rect(6f, y, scrollContent.width - 6f, 20f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Profile.NoWorkTypes", "No active WorkTypes."));
                }
                else
                {
                    float rankWidth = 44f;
                    float statusWidth = 110f;
                    float skillWidth = 90f;
                    float nameWidth = scrollContent.width - rankWidth - statusWidth - skillWidth - 12f;
                    Widgets.Label(new Rect(0f, y, rankWidth, 18f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Schedule.Priority", "Priority"));
                    Widgets.Label(new Rect(rankWidth, y, nameWidth, 18f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overview.WorkType", "Work type"));
                    Widgets.Label(new Rect(rankWidth + nameWidth, y, skillWidth, 18f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overview.Skill", "Skill"));
                    Widgets.Label(new Rect(rankWidth + nameWidth + skillWidth, y, statusWidth, 18f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overview.Status", "Status"));
                    y += 20f;

                    for (int i = 0; i < order.Count; i++)
                    {
                        WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(order[i]);
                        if (workType == null)
                        {
                            continue;
                        }
                        Passion passion;
                        SkillRecord bestSkill = WorkTypePaletteView.SafeBestSkill(pawn, workType, out passion);
                        string levelText = bestSkill != null ? bestSkill.Level.ToString() : "—";
                        int priority = pawn.workSettings != null ? pawn.workSettings.GetPriority(workType) : 0;
                        string status = WorkOrderService.IsIncapable(pawn, workType)
                            ? OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.Incapable", "Incapable")
                            : priority > 0 ? OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.Active", "Active") : OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.Off", "Off");
                        Widgets.Label(new Rect(0f, y, rankWidth, 18f), (i + 1).ToString());
                        Widgets.Label(new Rect(rankWidth, y, nameWidth, 18f), WorkTypePaletteView.DisplayName(workType));
                        WorkTypePaletteView.DrawPassionIcon(
                            new Rect(rankWidth + nameWidth, y + 3f, WorkTypePaletteView.PassionIconSize, WorkTypePaletteView.PassionIconSize),
                            passion);
                        Widgets.Label(new Rect(rankWidth + nameWidth + 16f, y, skillWidth - 16f, 18f), levelText);
                        Widgets.Label(new Rect(rankWidth + nameWidth + skillWidth, y, statusWidth, 18f), status);
                        y += 20f;
                    }
                }

                y += 10f;
                Widgets.Label(new Rect(0f, y, scrollContent.width, 20f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overview.Policy", "Policy"));
                y += 22f;
                string workPreset = entry == null || string.IsNullOrEmpty(entry.workPresetId)
                    ? OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.LegacySnapshot", "legacy snapshot")
                    : entry.workPresetId;
                string schedulePreset = entry == null || string.IsNullOrEmpty(entry.schedulePresetId)
                    ? OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.NotConfigured", "not configured")
                    : entry.schedulePresetId;
                string clothingPreset = entry == null || string.IsNullOrEmpty(entry.clothingPresetId)
                    ? OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.NotConfigured", "not configured")
                    : entry.clothingPresetId;
                int workOverrides = entry?.workOverrides?.Count ?? 0;
                int scheduleOverrides = entry?.scheduleOverrides?.Count ?? 0;
                int hourlyOverrides = entry?.scheduleWorkOverrides?.Count ?? 0;
                int clothingOverrides = entry?.clothingOverrides?.Count ?? 0;
                int zones = entry?.zones?.Count ?? 0;
                Widgets.Label(new Rect(0f, y, scrollContent.width, 20f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overview.WorkPreset", "Work preset: ") + workPreset);
                y += 20f;
                Widgets.Label(new Rect(0f, y, scrollContent.width, 20f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overview.SchedulePreset", "Schedule preset: ") + schedulePreset);
                y += 20f;
                Widgets.Label(new Rect(0f, y, scrollContent.width, 20f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overview.ClothingPreset", "Clothing preset: ") + clothingPreset);
                y += 20f;
                Widgets.Label(new Rect(0f, y, scrollContent.width, 20f),
                    OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overview.Overrides", "Overrides: ") + workOverrides + OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overview.WorkSuffix", " work, ") + scheduleOverrides + OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overview.ScheduleSuffix", " schedule, ")
                    + hourlyOverrides + OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overview.HourlySuffix", " hourly, ") + clothingOverrides + OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overview.ClothingSuffix", " clothing"));
                y += 20f;
                Widgets.Label(new Rect(0f, y, scrollContent.width, 20f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overview.Zones", "Zones: ") + zones);
            }
            finally
            {
                Widgets.EndScrollView();
                Text.Font = GameFont.Small;
            }
        }
    }
}