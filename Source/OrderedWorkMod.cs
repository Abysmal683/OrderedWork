using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OrderedWork.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork
{
    public class OrderedWorkMod : Mod
    {
        public static OrderedWorkSettings Settings;

        private Vector2 scroll;
        private string splitLabel = "";
        private string splitBase = "";
        private string splitGivers = "";
        private string mergeGivers = "";
        private string mergeTarget = "";
        private string selectedGiverType = "";

        private int dragKind;
        private int dragIndex = -1;
        private float dragStartMouseY;
        private bool dragMoved;

        public OrderedWorkMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<OrderedWorkSettings>();
        }

        public override string SettingsCategory()
        {
            return "Ordered Work";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            EnsureWorkTypeOrder();
            Rect viewRect = new Rect(0f, 0f, inRect.width - 24f, 4000f);
            Widgets.BeginScrollView(inRect, ref scroll, viewRect);
            Listing_Standard list = new Listing_Standard();
            list.Begin(viewRect);

            list.Label("Ordered Work (Core) - changes apply on restart.");
            list.Label("Split copies work tags, skills, life stages and mech availability.");
            list.Gap(6f);

            list.CheckboxLabeled(
                "Auto recommend new colony",
                ref Settings.autoRecommendNewColony,
                "On: new/default colonies and new pawns get a skill recommendation applied. Off: copy the current vanilla order instead; the top Recommend button still works.");
            list.CheckboxLabeled(
                "Auto-hide scrollbars",
                ref Settings.autoHideScrollbars,
                "Show Work Control scrollbars only while hovering or scrolling, so the matrix keeps its space.");
            list.CheckboxLabeled(
                "Palette stress test (debug)",
                ref Settings.paletteStressTest,
                "Adds 60 visual-only WorkTypes to test the palette's own horizontal scrollbar. They cannot be dragged or assigned.");
            list.Gap(6f);

            list.Label("Work type order (top = first). Drag rows to reorder:");
            List<Rect> workRows = new List<Rect>();
            for (int i = 0; i < Settings.workTypeOrder.Count; i++)
            {
                string defName = Settings.workTypeOrder[i];
                WorkTypeDef wt = DefDatabase<WorkTypeDef>.GetNamedSilentFail(defName);
                string label = wt != null ? wt.labelShort : defName;
                Rect row = list.GetRect(24f);
                DrawReorderRow(row, i + 1 + ". " + label, 1, i);
                workRows.Add(row);
            }
            HandleReorderDrag(1, workRows, MoveWorkType);

            list.Gap(10f);
            list.Label("Giver order (top = first) for one work type:");
            if (string.IsNullOrEmpty(selectedGiverType) || DefDatabase<WorkTypeDef>.GetNamedSilentFail(selectedGiverType) == null)
            {
                selectedGiverType = Settings.workTypeOrder.FirstOrDefault() ?? "";
            }
            string selectedLabel = DefDatabase<WorkTypeDef>.GetNamedSilentFail(selectedGiverType)?.labelShort ?? selectedGiverType;
            if (Widgets.ButtonText(list.GetRect(24f), "Selected: " + selectedLabel + "  (click to change)"))
            {
                int idx = Settings.workTypeOrder.IndexOf(selectedGiverType);
                if (Settings.workTypeOrder.Count > 0)
                {
                    selectedGiverType = Settings.workTypeOrder[(idx + 1 + Settings.workTypeOrder.Count) % Settings.workTypeOrder.Count];
                }
            }
            if (!string.IsNullOrEmpty(selectedGiverType))
            {
                List<WorkGiverDef> givers = DefDatabase<WorkGiverDef>.AllDefsListForReading
                    .Where(g => g.workType != null && g.workType.defName == selectedGiverType)
                    .OrderByDescending(g => g.priorityInType)
                    .ToList();
                List<Rect> giverRows = new List<Rect>();
                for (int i = 0; i < givers.Count; i++)
                {
                    WorkGiverDef giver = givers[i];
                    Rect row = list.GetRect(24f);
                    DrawReorderRow(row, i + 1 + ". " + giver.defName, 2, i);
                    giverRows.Add(row);
                }
                HandleReorderDrag(2, giverRows, (from, to) =>
                {
                    WorkGiverDef item = givers[from];
                    givers.RemoveAt(from);
                    givers.Insert(Mathf.Clamp(to, 0, givers.Count), item);
                    CommitGiverOrder(selectedGiverType, givers);
                });
            }

            list.Gap(10f);
            list.Label("Split: create a new work type from a base type and giver defNames (comma separated).");
            Rect labelRect = list.GetRect(24f);
            splitLabel = Widgets.TextField(labelRect, splitLabel);
            list.Label("New label: " + splitLabel);
            Rect baseRect = list.GetRect(24f);
            splitBase = Widgets.TextField(baseRect, splitBase);
            list.Label("Base work type defName: " + splitBase);
            Rect giversRect = list.GetRect(24f);
            splitGivers = Widgets.TextField(giversRect, splitGivers);
            list.Label("Givers: " + splitGivers);
            if (list.ButtonText("Add split"))
            {
                AddSplit();
            }
            foreach (SplitEntry split in Settings.splits)
            {
                list.Label("Split: " + split.newDefName + " <- " + split.baseWorkType + " [" + string.Join(", ", split.givers) + "]");
            }

            list.Gap(10f);
            list.Label("Merge: move giver defNames into an existing work type.");
            Rect mergeGiversRect = list.GetRect(24f);
            mergeGivers = Widgets.TextField(mergeGiversRect, mergeGivers);
            list.Label("Givers: " + mergeGivers);
            Rect mergeTargetRect = list.GetRect(24f);
            mergeTarget = Widgets.TextField(mergeTargetRect, mergeTarget);
            list.Label("Target work type defName: " + mergeTarget);
            if (list.ButtonText("Add merge"))
            {
                AddMerge();
            }
            foreach (MergeEntry merge in Settings.merges)
            {
                list.Label("Merge: [" + string.Join(", ", merge.givers) + "] -> " + merge.targetWorkType);
            }

            list.Gap(10f);
            list.Label("Technical spike: writes dense rank 1..N to free colonists and logs the result.");
            if (Find.CurrentMap == null)
            {
                list.Label("Load a colony to run the dense-rank test.");
            }
            else if (list.ButtonText("Test dense rank on colonists (log)"))
            {
                ApplyTestDenseRank();
            }

            list.Gap(10f);
            list.Label("No work type or giver is ever deleted. Back up your save before applying.");
            list.End();
            Widgets.EndScrollView();
        }

        private void DrawReorderRow(Rect row, string label, int kind, int index)
        {
            if ((dragKind == kind && dragIndex == index) || Mouse.IsOver(row))
            {
                Widgets.DrawHighlight(row);
            }
            Widgets.LabelEllipses(new Rect(row.x + 4f, row.y, row.width - 30f, row.height), label);
            Widgets.Label(new Rect(row.xMax - 24f, row.y, 20f, row.height), "≡");
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

        private void MoveWorkType(int from, int to)
        {
            List<string> order = Settings.workTypeOrder;
            if (from < 0 || from >= order.Count || to < 0 || to >= order.Count || from == to)
            {
                return;
            }
            string item = order[from];
            order.RemoveAt(from);
            order.Insert(to, item);
        }

        private void CommitGiverOrder(string workType, List<WorkGiverDef> givers)
        {
            for (int i = 0; i < givers.Count; i++)
            {
                givers[i].priorityInType = givers.Count - i;
            }

            Settings.giverOrder.RemoveAll(e => e.workType == workType);
            for (int i = 0; i < givers.Count; i++)
            {
                Settings.giverOrder.Add(new GiverOrderEntry
                {
                    workType = workType,
                    giver = givers[i].defName,
                    order = i
                });
            }
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

                List<string> order = DenseRank.NormalizeOrder(Settings.workTypeOrder, available);
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

        private void EnsureWorkTypeOrder()
        {
            if (Settings.workTypeOrder == null)
            {
                Settings.workTypeOrder = new List<string>();
            }
            if (Settings.workTypeOrder.Count == 0)
            {
                Settings.workTypeOrder = DefDatabase<WorkTypeDef>.AllDefsListForReading
                    .OrderByDescending(w => w.naturalPriority)
                    .Select(w => w.defName)
                    .ToList();
                return;
            }
            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading.OrderByDescending(w => w.naturalPriority))
            {
                if (!Settings.workTypeOrder.Contains(workType.defName))
                {
                    Settings.workTypeOrder.Add(workType.defName);
                }
            }
        }

        private void MoveGiver(string workTypeDefName, List<WorkGiverDef> givers, int index, int direction)
        {
            int other = index + direction;
            if (other < 0 || other >= givers.Count) return;
            WorkGiverDef first = givers[index];
            WorkGiverDef second = givers[other];
            int tmp = first.priorityInType;
            first.priorityInType = second.priorityInType;
            second.priorityInType = tmp;

            Settings.giverOrder.RemoveAll(e => e.workType == workTypeDefName);
            for (int i = 0; i < givers.Count; i++)
            {
                Settings.giverOrder.Add(new GiverOrderEntry
                {
                    workType = workTypeDefName,
                    giver = givers[i].defName,
                    order = i
                });
            }
        }

        private void AddSplit()
        {
            if (string.IsNullOrWhiteSpace(splitLabel) || string.IsNullOrWhiteSpace(splitBase) || string.IsNullOrWhiteSpace(splitGivers))
            {
                return;
            }
            string defName = "OrderedWork_" + Sanitize(splitLabel) + "_" + (Settings.splits.Count + 1);
            if (Settings.splits.Any(s => s.newDefName == defName))
            {
                return;
            }
            Settings.splits.Add(new SplitEntry
            {
                newDefName = defName,
                label = splitLabel.Trim(),
                baseWorkType = splitBase.Trim(),
                givers = ParseList(splitGivers)
            });
            splitLabel = "";
            splitBase = "";
            splitGivers = "";
        }

        private void AddMerge()
        {
            if (string.IsNullOrWhiteSpace(mergeGivers) || string.IsNullOrWhiteSpace(mergeTarget))
            {
                return;
            }
            Settings.merges.Add(new MergeEntry
            {
                givers = ParseList(mergeGivers),
                targetWorkType = mergeTarget.Trim()
            });
            mergeGivers = "";
            mergeTarget = "";
        }

        private static List<string> ParseList(string text)
        {
            return text.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).Distinct().ToList();
        }

        private static string Sanitize(string text)
        {
            char[] chars = text.Trim().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
            return new string(chars);
        }
    }
}
