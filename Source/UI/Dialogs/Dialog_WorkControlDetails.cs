using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using OrderedWork.Services;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Advanced per-pawn panel. Sections share the pawn header and expose schedule,
    /// zone and (later) clothing/override editing without replacing vanilla tabs.
    /// </summary>
    public partial class Dialog_WorkControlDetails : Window
    {
        private enum DetailTab
        {
            Work,
            Overview,
            Schedule,
            Zone,
            Overrides,
            Clothing
        }

        private Pawn pawn;
        private List<Pawn> detailPawns = new List<Pawn>();
        private int detailPawnIndex;
        private DetailTab detailTab;
        private Vector2 overviewScroll;
        private Vector2 detailTabScroll;
        private WorkPanel workPanel;

        public Dialog_WorkControlDetails(Pawn pawn)
        {
            this.pawn = pawn;
            detailPawns = BuildDetailPawnList(pawn);
            detailPawnIndex = Mathf.Max(0, detailPawns.IndexOf(pawn));
            doCloseX = true;
            doCloseButton = false;
            resizeable = true;
            draggable = false;
            absorbInputAroundWindow = false;
            closeOnClickedOutside = false;
            forcePause = false;
        }


        public override Vector2 InitialSize => new Vector2(1100f, 720f);

        private bool resizerConfigured;

        private void ConfigureResizer()
        {
            if (resizerConfigured)
            {
                return;
            }
            FieldInfo field = typeof(Window).GetField("resizer", BindingFlags.Instance | BindingFlags.NonPublic);
            WindowResizer resizer = field != null ? field.GetValue(this) as WindowResizer : null;
            if (resizer != null)
            {
                resizer.minWindowSize = new Vector2(500f, 700f);
                resizerConfigured = true;
            }
        }

        private Vector2 lastWindowSize;

        private void EnforceMinimumWindowSize()
        {
            ConfigureResizer();
            if (!resizerConfigured)
            {
                if (windowRect.width < 500f) windowRect.width = 500f;
                if (windowRect.height < 700f) windowRect.height = 700f;
            }
            if (lastWindowSize != windowRect.size)
            {
                Widgets.mouseOverScrollViewStack.Clear();
                lastWindowSize = windowRect.size;
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            ConfigureResizer();
        }

        public override void DoWindowContents(Rect inRect)
        {
            HandleTimeHotkeys();
            EnforceMinimumWindowSize();
            draggable = detailTab != DetailTab.Schedule && detailTab != DetailTab.Work;
            if (workPanel == null && pawn != null)
            {
                workPanel = new WorkPanel(pawn);
            }
            if (pawn == null || pawn.Destroyed)
            {
                Widgets.Label(inRect, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Main.PawnUnavailable", "Pawn unavailable."));
                return;
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width - 160f, 30f), PawnLabel(pawn));
            DrawPawnNavigation(inRect);
            DrawPanelLock(inRect);
            if (workPanel == null && pawn != null)
            {
                workPanel = new WorkPanel(pawn);
            }
            Text.Font = GameFont.Small;

            Rect tabRow = new Rect(0f, 64f, inRect.width, 28f);
            OrderedTabStrip.Draw(tabRow, new[] { OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Tab.Work", "Work"), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Tab.Overview", "Overview"), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Schedule.Title", "Schedule"), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Zone.Title", "Zone"), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Tab.Overrides", "Overrides"), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Tab.Clothing", "Clothing") }, (int)detailTab,
                delegate(int index)
                {
                    detailTab = (DetailTab)index;
                    detailTabScroll = Vector2.zero;
                });

            Rect content = new Rect(12f, tabRow.y + 8f, inRect.width - 24f, inRect.height - tabRow.y - 48f);

            switch (detailTab)
            {
                case DetailTab.Work:
                    workPanel.Draw(content, true);
                    break;
                case DetailTab.Schedule:
                    DrawScrollableTab(content, DrawScheduleTab, 860f, 500f);
                    break;
                case DetailTab.Zone:
                    DrawScrollableTab(content, DrawZoneTab, 500f, 300f);
                    break;
                case DetailTab.Overrides:
                    DrawOverridesTab(content);
                    break;
                case DetailTab.Clothing:
                    DrawClothingTab(content);
                    break;
                default:
                    DrawOverview(content);
                    break;
            }
        }

        private static List<Pawn> BuildDetailPawnList(Pawn current)
        {
            List<Pawn> result = new List<Pawn>();
            try
            {
                if (Find.ColonistBar != null)
                {
                    foreach (Pawn pawn in Find.ColonistBar.GetColonistsInOrder())
                    {
                        if (pawn != null && !pawn.Dead && pawn.IsFreeColonist && pawn.Spawned)
                        {
                            result.Add(pawn);
                        }
                    }
                }
            }
            catch (Exception)
            {
                result.Clear();
            }
            if (result.Count == 0 && Find.CurrentMap != null)
            {
                result.AddRange(Find.CurrentMap.mapPawns.FreeColonists.Where(p => p != null && !p.Dead));
            }
            if (current != null && !result.Contains(current))
            {
                result.Add(current);
            }
            return result;
        }

        private void DrawPanelLock(Rect inRect)
        {
            if (detailTab != DetailTab.Work && detailTab != DetailTab.Schedule)
            {
                return;
            }
            Rect lockRect = new Rect(inRect.xMax - 22f, 2f, 18f, 18f);
            Color gold = new Color(0.82f, 0.64f, 0.24f);
            Widgets.DrawBoxSolid(new Rect(lockRect.x + 3f, lockRect.y + 8f, 12f, 8f), gold);
            Widgets.DrawBoxSolid(new Rect(lockRect.x + 5f, lockRect.y + 3f, 2f, 7f), gold);
            Widgets.DrawBoxSolid(new Rect(lockRect.x + 11f, lockRect.y + 3f, 2f, 7f), gold);
            Widgets.DrawBoxSolid(new Rect(lockRect.x + 5f, lockRect.y + 3f, 8f, 2f), gold);
            Widgets.DrawBoxSolid(new Rect(lockRect.x + 8f, lockRect.y + 11f, 2f, 3f), new Color(0.16f, 0.14f, 0.10f));
            TooltipHandler.TipRegion(lockRect, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Main.PanelLocked", "Panel movement is locked in this tab."));
        }

        private void DrawPawnNavigation(Rect inRect)
        {
            Rect search = new Rect(inRect.xMax - 152f, 0f, 68f, 24f);
            Rect left = new Rect(inRect.xMax - 82f, 0f, 26f, 24f);
            Rect right = new Rect(inRect.xMax - 52f, 0f, 26f, 24f);
            if (Widgets.ButtonText(search, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Main.Find", "Find")))
            {
                Find.WindowStack.Add(new Dialog_PawnPicker(detailPawns, SetPawn));
            }
            if (Widgets.ButtonText(left, "<"))
            {
                StepPawn(-1);
            }
            if (Widgets.ButtonText(right, ">"))
            {
                StepPawn(1);
            }
            TooltipHandler.TipRegion(left, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Main.Previous", "Previous colonist"));
            TooltipHandler.TipRegion(right, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Main.Next", "Next colonist"));
            TooltipHandler.TipRegion(search, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Main.SearchTip", "Search another colonist without closing details"));
        }

        private void StepPawn(int delta)
        {
            if (detailPawns == null || detailPawns.Count == 0)
            {
                return;
            }
            int next = (detailPawnIndex + delta + detailPawns.Count) % detailPawns.Count;
            SetPawn(detailPawns[next]);
        }

        private void SetPawn(Pawn selected)
        {
            if (selected == null || selected == pawn || selected.Dead)
            {
                return;
            }
            pawn = selected;
            detailPawns = BuildDetailPawnList(pawn);
            detailPawnIndex = Mathf.Max(0, detailPawns.IndexOf(pawn));
            workPanel = null;
            detailSchedule = null;
            detailPresets = null;
            detailScheduleName = "";
            ResetFocusEditor();
            detailBrush = null;
            detailZoneMap = null;
            detailZoneArea = null;
            detailZoneInitialized = false;
            detailZoneMissing = false;
            detailTabScroll = Vector2.zero;
            overviewScroll = Vector2.zero;
            Find.Selector.ClearSelection();
            Find.Selector.Select(pawn, true, false);
        }
        private static void HandleTimeHotkeys()
        {
            Event current = Event.current;
            if (current == null || current.type != EventType.KeyDown
                || current.control || current.alt || current.shift
                || Find.TickManager == null)
            {
                return;
            }

            switch (current.keyCode)
            {
                case KeyCode.Alpha1:
                case KeyCode.Keypad1:
                    Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                    PlayTimeSound(TimeSpeed.Normal);
                    current.Use();
                    break;
                case KeyCode.Alpha2:
                case KeyCode.Keypad2:
                    Find.TickManager.CurTimeSpeed = TimeSpeed.Fast;
                    PlayTimeSound(TimeSpeed.Fast);
                    current.Use();
                    break;
                case KeyCode.Alpha3:
                case KeyCode.Keypad3:
                    Find.TickManager.CurTimeSpeed = TimeSpeed.Superfast;
                    PlayTimeSound(TimeSpeed.Superfast);
                    current.Use();
                    break;
                case KeyCode.Alpha4:
                case KeyCode.Keypad4:
                    Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
                    PlayTimeSound(TimeSpeed.Ultrafast);
                    current.Use();
                    break;
                case KeyCode.Space:
                    Find.TickManager.TogglePaused();
                    PlayTimeSound(Find.TickManager.Paused ? TimeSpeed.Paused : TimeSpeed.Normal);
                    current.Use();
                    break;
            }
        }

        private static void PlayTimeSound(TimeSpeed speed)
        {
            try
            {
                MethodInfo method = typeof(TimeControls).GetMethod("PlaySoundOf",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    method.Invoke(null, new object[] { speed });
                }
            }
            catch (Exception)
            {
            }
        }

        private void DrawScrollableTab(Rect content, System.Action<Rect> draw, float minWidth, float minHeight)
        {
            Rect view = new Rect(0f, 0f,
                Mathf.Max(content.width, minWidth),
                Mathf.Max(content.height, minHeight));
            Widgets.BeginScrollView(content, ref detailTabScroll, view);
            draw(view);
            Widgets.EndScrollView();
        }


        private static string PawnLabel(Pawn pawn)
        {
            string label = pawn.NameFullColored;
            return string.IsNullOrEmpty(label) ? pawn.LabelShort : label;
        }
    }
}