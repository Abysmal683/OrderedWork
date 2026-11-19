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
    {        private void DrawGiversColumn(Rect rect)
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
    }
}
