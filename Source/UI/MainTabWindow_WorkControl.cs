using System.Collections.Generic;
using System.Linq;
using OrderedWork.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Full-screen "Work Control" tab. The shell mirrors the vanilla Work tab: one row per
    /// free colonist, a fixed pawn column on the left and a horizontal lane of WorkType
    /// cards on the right. A single scroll view supplies the outer vertical scroll and the
    /// shared horizontal scroll, so moving it moves every lane together.
    /// </summary>
    public class MainTabWindow_WorkControl : MainTabWindow
    {
        private const float Pad = 12f;
        private const float TitleHeight = 30f;
        private const float HeaderHeight = 24f;
        private const float RowHeight = 100f;
        private const float CardWidth = 190f;
        private const float CardHeight = 80f;
        private const float CardGap = 6f;
        private const float LeftColumnMin = 180f;
        private const float LeftColumnMax = 280f;

        private Vector2 gridScroll;

        private bool dragging;
        private Pawn dragPawn;
        private int dragSourceIndex = -1;
        private int dragTargetIndex = -1;
        private int dragCount;

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.DrawBoxSolid(inRect, new Color(0.11f, 0.12f, 0.13f, 1f));

            if (Find.CurrentMap == null)
            {
                DrawCentered(inRect, "Load a colony");
                return;
            }

            List<Pawn> colonists = Find.CurrentMap.mapPawns.FreeColonists.ToList();
            if (colonists.Count == 0)
            {
                DrawCentered(inRect, "No free colonists");
                return;
            }

            List<WorkTypeDef> available = WorkOrderService.GetAvailableWorkTypes();

            float leftWidth = Mathf.Clamp(inRect.width * 0.22f, LeftColumnMin, LeftColumnMax);
            float titleY = Pad;
            float headerY = titleY + TitleHeight + 2f;
            float bodyTop = headerY + HeaderHeight + 6f;
            float bodyHeight = Mathf.Max(60f, inRect.yMax - Pad - bodyTop);
            Rect bodyRect = new Rect(Pad, bodyTop, inRect.width - Pad * 2f, bodyHeight);
            Rect laneViewport = new Rect(bodyRect.x + leftWidth, bodyRect.y, bodyRect.width - leftWidth, bodyRect.height);

            int maxLane = 0;
            foreach (Pawn pawn in colonists)
            {
                maxLane = Mathf.Max(maxLane, BuildLane(pawn).types.Count);
            }

            float laneContentWidth = Mathf.Max(laneViewport.width, (maxLane + 1) * (CardWidth + CardGap) + CardGap);
            float rowsHeight = colonists.Count * RowHeight;
            float contentHeight = Mathf.Max(laneViewport.height, rowsHeight);
            Rect laneContent = new Rect(0f, 0f, laneContentWidth, contentHeight);

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(Pad, titleY, inRect.width - Pad * 2f, TitleHeight), "Work Control");
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(bodyRect.x, headerY, leftWidth - 8f, HeaderHeight), "Pawn");
            Widgets.Label(new Rect(laneViewport.x, headerY, laneViewport.width, HeaderHeight),
                "Work order (left = highest). Drag to reorder, × remove, + add.");

            Widgets.BeginScrollView(laneViewport, ref gridScroll, laneContent);
            try
            {
                HandleDrop();

                for (int i = 0; i < colonists.Count; i++)
                {
                    Pawn pawn = colonists[i];
                    float rowY = i * RowHeight;

                    Color rowColor = (i % 2 == 0)
                        ? new Color(0.14f, 0.15f, 0.17f, 0.6f)
                        : new Color(0.16f, 0.17f, 0.19f, 0.6f);
                    Widgets.DrawBoxSolid(new Rect(0f, rowY, laneContentWidth, RowHeight), rowColor);

                    DrawRowLane(pawn, rowY, available);
                }
            }
            finally
            {
                Widgets.EndScrollView();
                Text.Font = GameFont.Small;
            }

            DrawPawnColumn(colonists, bodyRect, leftWidth);
        }

        private void DrawCentered(Rect inRect, string message)
        {
            TextAnchor previous = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(inRect, message);
            Text.Anchor = previous;
        }

        private void DrawPawnColumn(List<Pawn> colonists, Rect bodyRect, float leftWidth)
        {
            GUI.BeginGroup(bodyRect);
            try
            {
                for (int i = 0; i < colonists.Count; i++)
                {
                    float y = i * RowHeight - gridScroll.y;
                    if (y + RowHeight < 0f || y > bodyRect.height)
                    {
                        continue;
                    }

                    TextAnchor previous = Text.Anchor;
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(new Rect(6f, y, leftWidth - 14f, RowHeight), PawnLabel(colonists[i]));
                    Text.Anchor = previous;
                }
            }
            finally
            {
                GUI.EndGroup();
            }
        }

        private void DrawRowLane(Pawn pawn, float rowY, List<WorkTypeDef> available)
        {
            LaneData lane = BuildLane(pawn);
            float cardY = rowY + (RowHeight - CardHeight) / 2f;
            int removeIndex = -1;

            for (int i = 0; i < lane.types.Count; i++)
            {
                Rect cardRect = new Rect(CardGap + i * (CardWidth + CardGap), cardY, CardWidth, CardHeight);
                DrawCard(pawn, cardRect, lane.types[i], lane.ids, i, ref removeIndex);
            }

            Rect addRect = new Rect(CardGap + lane.types.Count * (CardWidth + CardGap), cardY, CardWidth, CardHeight);
            if (Widgets.ButtonText(addRect, "+"))
            {
                OpenAddMenu(pawn, lane.ids, available);
            }

            if (removeIndex >= 0 && removeIndex < lane.ids.Count)
            {
                List<string> reduced = new List<string>(lane.ids);
                reduced.RemoveAt(removeIndex);
                WorkOrderService.SaveAndApply(pawn, reduced);
            }
        }

        private void DrawCard(Pawn pawn, Rect cardRect, WorkTypeDef workType, List<string> laneIds, int index, ref int removeIndex)
        {
            bool incapable = WorkOrderService.IsIncapable(pawn, workType);

            Widgets.DrawBoxSolid(cardRect, new Color(0.19f, 0.21f, 0.24f, 0.95f));

            if (dragging && pawn == dragPawn && index == dragTargetIndex)
            {
                Widgets.DrawHighlight(cardRect);
                Widgets.DrawBox(cardRect, 2);
            }

            Rect removeRect = new Rect(cardRect.xMax - 22f, cardRect.y + 3f, 18f, 18f);
            Rect bodyRect = new Rect(cardRect.x, cardRect.y + 22f, cardRect.width, cardRect.height - 26f);

            Color previousColor = GUI.color;
            if (incapable)
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            }

            Rect labelRect = new Rect(cardRect.x + 6f, cardRect.y + 4f, cardRect.width - 30f, 44f);
            bool previousWrap = Text.WordWrap;
            Text.WordWrap = true;
            Widgets.Label(labelRect, workType.labelShort);
            Text.WordWrap = previousWrap;

            GameFont previousFont = Text.Font;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(cardRect.x + 6f, cardRect.yMax - 24f, cardRect.width - 12f, 20f),
                CardInfo(pawn, workType, incapable));
            Text.Font = previousFont;
            GUI.color = previousColor;

            if (Mouse.IsOver(cardRect))
            {
                TooltipHandler.TipRegion(cardRect, CardTooltip(pawn, workType, incapable));
            }

            if (Widgets.ButtonText(removeRect, "×"))
            {
                removeIndex = index;
            }

            // The remove button must never start a drag, and incapable cards are not draggable.
            bool overRemove = removeRect.Contains(Event.current.mousePosition);
            if (!incapable
                && !overRemove
                && Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && bodyRect.Contains(Event.current.mousePosition))
            {
                dragging = true;
                dragPawn = pawn;
                dragSourceIndex = index;
                dragTargetIndex = index;
                dragCount = laneIds.Count;
                Event.current.Use();
            }

            if (Widgets.ButtonInvisible(bodyRect))
            {
                // Click only; reordering is driven by the drag events above.
            }
        }

        private void HandleDrop()
        {
            if (!dragging)
            {
                return;
            }

            if (Event.current.type != EventType.MouseUp)
            {
                dragTargetIndex = IndexAtX(Event.current.mousePosition.x, dragCount);
                return;
            }

            int from = dragSourceIndex;
            int to = IndexAtX(Event.current.mousePosition.x, dragCount);
            Pawn pawn = dragPawn;

            dragging = false;
            dragPawn = null;
            dragSourceIndex = -1;
            dragTargetIndex = -1;

            if (pawn != null && from >= 0 && to >= 0 && from != to)
            {
                List<string> ids = BuildLane(pawn).ids;
                if (from < ids.Count && to < ids.Count)
                {
                    string moved = ids[from];
                    ids.RemoveAt(from);
                    ids.Insert(Mathf.Clamp(to, 0, ids.Count), moved);
                    WorkOrderService.SaveAndApply(pawn, ids);
                }
            }
            Event.current.Use();
        }

        private void OpenAddMenu(Pawn pawn, List<string> currentOrder, List<WorkTypeDef> available)
        {
            HashSet<string> inOrder = new HashSet<string>(currentOrder);
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            foreach (WorkTypeDef workType in available)
            {
                if (inOrder.Contains(workType.defName))
                {
                    continue;
                }
                if (WorkOrderService.IsIncapable(pawn, workType))
                {
                    continue;
                }

                WorkTypeDef local = workType;
                options.Add(new FloatMenuOption(local.labelShort, delegate
                {
                    List<string> updated = WorkOrderService.GetOrder(pawn);
                    if (!updated.Contains(local.defName))
                    {
                        updated.Add(local.defName);
                        WorkOrderService.SaveAndApply(pawn, updated);
                    }
                }));
            }

            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption("Nothing available to add", null));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static string CardInfo(Pawn pawn, WorkTypeDef workType, bool incapable)
        {
            if (incapable)
            {
                return "Incapaz";
            }
            Passion passion;
            int level = BestSkillLevel(pawn, workType, out passion);
            string levelText = level >= 0 ? "Lv " + level : "—";
            return levelText + PassionMarker(passion);
        }

        private static string CardTooltip(Pawn pawn, WorkTypeDef workType, bool incapable)
        {
            string text = workType.labelShort;
            if (workType.relevantSkills != null && workType.relevantSkills.Count > 0)
            {
                foreach (SkillDef skill in workType.relevantSkills)
                {
                    if (skill == null)
                    {
                        continue;
                    }
                    SkillRecord record = pawn.skills != null ? pawn.skills.GetSkill(skill) : null;
                    string level = record != null ? record.Level.ToString() : "—";
                    text += "\n" + skill.label + ": " + level + PassionMarker(record != null ? record.passion : Passion.None);
                }
            }
            else
            {
                text += "\nNo relevant skill";
            }

            if (incapable)
            {
                text += "\nIncapaz: this colonist cannot do this work.";
            }
            return text;
        }

        private static int BestSkillLevel(Pawn pawn, WorkTypeDef workType, out Passion passion)
        {
            passion = Passion.None;
            if (pawn == null || pawn.skills == null || workType == null || workType.relevantSkills == null)
            {
                return -1;
            }

            int best = -1;
            foreach (SkillDef skill in workType.relevantSkills)
            {
                if (skill == null)
                {
                    continue;
                }
                SkillRecord record = pawn.skills.GetSkill(skill);
                if (record == null)
                {
                    continue;
                }
                if (record.Level > best)
                {
                    best = record.Level;
                    passion = record.passion;
                }
            }
            return best;
        }

        /// <summary>
        /// Temporary text marker for a future passion icon. Major = "**", minor = "*".
        /// Replace this method with an icon draw when art is available.
        /// </summary>
        private static string PassionMarker(Passion passion)
        {
            if (passion == Passion.Major)
            {
                return " **";
            }
            if (passion == Passion.Minor)
            {
                return " *";
            }
            return "";
        }

        private static string PawnLabel(Pawn pawn)
        {
            string label = pawn.NameFullColored;
            return string.IsNullOrEmpty(label) ? pawn.LabelShort : label;
        }

        private static LaneData BuildLane(Pawn pawn)
        {
            LaneData lane = new LaneData();
            foreach (string id in WorkOrderService.GetOrder(pawn))
            {
                WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(id);
                if (workType != null)
                {
                    lane.types.Add(workType);
                    lane.ids.Add(id);
                }
            }
            return lane;
        }

        private static int IndexAtX(float x, int count)
        {
            if (count <= 0)
            {
                return 0;
            }
            int index = Mathf.FloorToInt((x - CardGap) / (CardWidth + CardGap));
            return Mathf.Clamp(index, 0, count - 1);
        }

        private sealed class LaneData
        {
            public readonly List<WorkTypeDef> types = new List<WorkTypeDef>();
            public readonly List<string> ids = new List<string>();
        }
    }
}
