#nullable disable
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
    public sealed partial class WorkPanel
    {        private void DrawWorkTypesColumn(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, UiColors.PanelBody);
            Rect header = new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, 24f);
            Widgets.Label(new Rect(header.x, header.y, header.width - 212f, 22f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Work.Types", "Work types"));
            if (UiButtonBar.Button(new Rect(header.xMax - 204f, header.y, 66f, 22f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Work.Recommend", "Recom."), true,
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Work.RecommendTip", "Apply the existing WorkType recommendation to this pawn immediately. WorkGiver overrides are untouched.")))
            {
                workDraft = WorkOrderService.GetRecommendation(pawn);
                selectedWorkType = workDraft.FirstOrDefault() ?? selectedWorkType;
                LoadSelectedGivers();
                CommitWork();
            }
            if (UiButtonBar.Button(new Rect(header.xMax - 134f, header.y, 62f, 22f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Work.Givers", "Givers"),
                selectedWorkType != null, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Work.GiversTip", "Show or hide the WorkGiver column for the selected WorkType."),
                giversExpanded ? new Color(0.48f, 0.68f, 0.42f, 1f) : (Color?)null))
            {
                giversExpanded = !giversExpanded;
                if (giversExpanded)
                {
                    LoadSelectedGivers();
                }
            }
            if (UiButtonBar.Button(new Rect(header.xMax - 68f, header.y, 62f, 22f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Work.Add", "Add"), true,
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Work.AddTip", "Add a capable WorkType that is not currently in this pawn's list.")))
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
                    if (ActiveWorkService.IsActive(pawn, workType))
                    {
                        Widgets.DrawBoxSolid(new Rect(row.x, row.y, 3f, row.height),
                            new Color(0.55f, 0.88f, 0.42f, 0.95f));
                        Widgets.DrawBox(new Rect(row.x, row.y, row.width, row.height), 1);
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
                        Widgets.Label(new Rect(row.xMax - 126f, row.y + 3f, 58f, 18f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.Changed", "Changed"));
                    }
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && Mouse.IsOver(row))
                    {
                        removeIndex = index;
                        Event.current.Use();
                    }
                    Rect rowHandle = new Rect(row.xMax - 24f, row.y + 1f, 20f, 20f);
                    Widgets.Label(rowHandle, "≡");
                    TooltipHandler.TipRegion(rowHandle,
                        OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Work.RightClickRemove", "Right-click to remove this WorkType."));
                    if (Mouse.IsOver(row)) TooltipHandler.TipRegion(row, WorkTypeTooltip(workType, index + 1));
                },
                delegate(int index, Rect row)
                {
                    return new Rect(row.x, row.y, Mathf.Max(20f, row.width - 26f), row.height);
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
                WorkTypeDef removedType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(removed);
                workDraft.RemoveAt(removeIndex);
                if (selectedWorkType == removed)
                {
                    selectedWorkType = workDraft.FirstOrDefault();
                    LoadSelectedGivers();
                }
                CommitWork();
                Messages.Message(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Work.Removed", "Work type removed: ")
                    + (removedType != null ? removedType.labelShort : removed), MessageTypeDefOf.TaskCompletion, false);
            }
        }

        private void DrawUnableWorkTypes(Rect rect, List<WorkTypeDef> unable)
        {
            Widgets.DrawBoxSolid(rect, UiColors.PanelBody);
            Widgets.Label(new Rect(rect.x + 6f, rect.y + 3f, rect.width - 12f, 20f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Work.Cannot", "Cannot"));
            Rect list = new Rect(rect.x + 6f, rect.y + 24f, rect.width - 12f, Mathf.Max(0f, rect.height - 26f));
            if (unable.Count == 0)
            {
                Widgets.Label(list, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Work.NoLimitations", "No limitations"));
                return;
            }
            UiScrollPane.Draw(list, ref unableScroll, Mathf.Max(list.height, unable.Count * RowHeight), this, delegate(Rect view)
            {
                for (int i = 0; i < unable.Count; i++)
                {
                    Rect row = new Rect(0f, i * RowHeight, view.width, RowHeight - 2f);
                    UiZebra.Draw(row, i);
                    Widgets.LabelEllipses(new Rect(row.x + 4f, row.y + 3f, row.width - 8f, 18f), unable[i].labelShort);
                    if (Mouse.IsOver(row)) TooltipHandler.TipRegion(row, WorkTypeTooltip(unable[i], -1));
                }
            });
        }

        private void ShowAddWorkTypeMenu()
        {
            List<WorkTypeDef> candidates = AddableWorkTypes().ToList();
            if (candidates.Count == 0)
            {
                Messages.Message(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Work.NoAddable", "No capable WorkTypes to add."), MessageTypeDefOf.RejectInput, false);
                return;
            }
            Find.WindowStack.Add(new WorkTypeDropdownWindow(
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Work.AddType", "Add WorkType"),
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
            return workType.labelShort + " · " + (skill != null ? skill.label : OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkTypes.SkillFallback", "skill"))
                + " " + record.Level + " · " + WorkTypePaletteView.PassionText(record.passion);
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
                builder.AppendLine(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkTypes.SkillsLabel", "Skills:"));
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
                builder.AppendLine(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.WorkTypes.GiversLabel", "Givers:"));
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
    }
}