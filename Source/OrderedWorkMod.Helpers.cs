using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OrderedWork.Config;
using OrderedWork.Core;
using OrderedWork.Services;
using OrderedWork.UI;
using RimWorld;
using UnityEngine;
using Verse;
using System.Text;

namespace OrderedWork
{
    public partial class OrderedWorkMod : Mod
    {
        private void DrawReorderRow(Rect row, string label, int kind, int index, string tooltip)
        {
            bool dragging = dragKind == kind;
            bool dragged = dragging && dragIndex == index;
            bool dropTarget = dragging && dragIndex >= 0 && dragIndex != index && Mouse.IsOver(row);

            if (dragged)
            {
                Widgets.DrawBoxSolid(row, new Color(0.22f, 0.42f, 0.62f, 0.55f));
            }
            else if (dropTarget)
            {
                Widgets.DrawBoxSolid(row, new Color(0.55f, 0.72f, 0.35f, 0.35f));
            }
            else if (Mouse.IsOver(row))
            {
                Widgets.DrawHighlight(row);
            }

            Widgets.LabelEllipses(new Rect(row.x + 4f, row.y, row.width - 30f, row.height), label);
            Widgets.Label(new Rect(row.xMax - 24f, row.y, 20f, row.height), "≡");

            if (!dragging && !dragMoved && !string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(row, tooltip);
            }
        }

        private static string WorkTypeTooltip(string defName)
        {
            WorkTypeDef wt = DefDatabase<WorkTypeDef>.GetNamedSilentFail(defName);
            if (wt == null)
            {
                return defName;
            }
            string text = wt.labelShort + "\n\n" + (wt.description ?? "");
            List<WorkGiverDef> givers = DefDatabase<WorkGiverDef>.AllDefsListForReading
                .Where(g => g.workType == wt)
                .OrderByDescending(g => g.priorityInType)
                .ToList();
            if (givers.Count > 0)
            {
                text += "\n\nWorkGivers:";
                foreach (WorkGiverDef giver in givers)
                {
                    text += "\n- " + giver.defName;
                }
            }
            return text;
        }

        private static string GiverTooltip(WorkGiverDef giver)
        {
            if (giver == null)
            {
                return "";
            }
            string text = giver.defName;
            if (giver.workType != null)
            {
                text += "\nWork type: " + giver.workType.labelShort;
            }
            return text;
        }

        private void HandleReorderDrag(int kind, List<Rect> rows, Action<int, int> move)
        {
            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    if (rows[i].Contains(current.mousePosition))
                    {
                        dragKind = kind;
                        dragIndex = i;
                        dragStartMouseY = current.mousePosition.y;
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
                if (!dragMoved && Mathf.Abs(current.mousePosition.y - dragStartMouseY) > 4f)
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
                current.Use();
            }
        }

        private void MoveWorkTypeProfile(List<WorkTypeDef> ordered, int from, int to)
        {
            if (ordered == null || from < 0 || from >= ordered.Count || to < 0 || to >= ordered.Count || from == to)
            {
                return;
            }
            WorkTypeDef item = ordered[from];
            ordered.RemoveAt(from);
            ordered.Insert(to, item);
            WorkConfigProfile profile = WorkConfigStore.LoadEditingProfile(activeProfileName);
            if (profile == null)
            {
                return;
            }
            for (int i = 0; i < ordered.Count; i++)
            {
                if (profile.WorkTypes.TryGetValue(ordered[i].defName, out WorkTypeConfig config) && config != null)
                {
                    config.Order = i;
                }
            }
            WorkConfigStore.SaveProfile(profile, true);
            WorkConfigStore.SavePending(profile);
            OrderedWorkStartup.ApplyProfileNow(profile, Settings, this);
        }

        public static void DebugLog(string message)
        {
            if (Settings != null && Settings.verboseLogging)
            {
                Log.Message("[OrderedWork] " + message);
            }
        }

        private void MoveRecommendationPin(int from, int to)
        {
            List<string> pins = Settings.recommendationPins;
            if (pins == null || from < 0 || from >= pins.Count || to < 0 || to >= pins.Count || from == to)
            {
                return;
            }
            string item = pins[from];
            pins.RemoveAt(from);
            pins.Insert(to, item);
        }

        private void ApplyTestDenseRank()
        {
            try
            {
                if (Find.CurrentMap == null)
                {
                    Log.Warning("[Ordered Work] No map loaded.");
                    return;
                }

                List<string> available = DefDatabase<WorkTypeDef>.AllDefsListForReading
                    .Where(w => w.visible)
                    .Select(w => w.defName)
                    .ToList();

                List<string> order = WorkConfigStore.GetOrderedVisibleWorkTypes(
                    WorkConfigStore.LoadEditingProfile(activeProfileName)).Select(w => w.defName).ToList();
                order = DenseRank.NormalizeOrder(order, available);
                if (order.Count == 0)
                {
                    Log.Warning("[Ordered Work] No visible work types available for the dense rank test.");
                    return;
                }

                Dictionary<string, int> ranks = DenseRank.BuildRanks(order, available);
                int max = Math.Max(4, order.Count);

                FieldInfo prioritiesField = typeof(Pawn_WorkSettings).GetField(
                    "priorities",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                bool loggedDirectMode = false;

                int colonists = 0;
                foreach (Pawn pawn in Find.CurrentMap.mapPawns.FreeColonists)
                {
                    Pawn_WorkSettings ws = pawn.workSettings;
                    if (ws == null)
                    {
                        continue;
                    }

                    DefMap<WorkTypeDef, int> priorities = prioritiesField != null
                        ? prioritiesField.GetValue(ws) as DefMap<WorkTypeDef, int>
                        : null;

                    foreach (WorkTypeDef wt in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                    {
                        int rank;
                        int value = ranks.TryGetValue(wt.defName, out rank) ? rank : 0;
                        if (priorities != null)
                        {
                            priorities[wt] = value;
                        }
                        else
                        {
                            ws.SetPriority(wt, value);
                        }
                    }

                    if (priorities != null && !loggedDirectMode)
                    {
                        Log.Message("[Ordered Work] Dense rank test using direct priorities map (bypasses the SetPriority clamp).");
                        loggedDirectMode = true;
                    }

                    ws.Notify_UseWorkPrioritiesChanged();

                    WorkTypeDef firstWorkType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(order[0]);
                    WorkTypeDef lastWorkType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(order[order.Count - 1]);
                    string firstPriority = firstWorkType != null ? ws.GetPriority(firstWorkType).ToString() : "n/a";
                    string lastPriority = lastWorkType != null ? ws.GetPriority(lastWorkType).ToString() : "n/a";
                    Log.Message("[Ordered Work] Dense rank test " + pawn.LabelShort + ": max=" + max + ", count=" + order.Count + ", first=" + order[0] + "=" + firstPriority + ", last=" + order[order.Count - 1] + "=" + lastPriority);
                    colonists++;
                }

                Log.Message("[Ordered Work] Dense rank test applied to " + colonists + " colonist(s).");
            }
            catch (Exception ex)
            {
                Log.Error("[Ordered Work] Dense rank test failed: " + ex);
            }
        }

        private static string Sanitize(string text)
        {
            char[] chars = text.Trim().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
            return new string(chars);
        }

    }
}
