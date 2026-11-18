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
            Overview,
            Schedule,
            Zone
        }

        private readonly Pawn pawn;
        private DetailTab detailTab;
        private Vector2 overviewScroll;
        private Vector2 detailTabScroll;

        public Dialog_WorkControlDetails(Pawn pawn)
        {
            this.pawn = pawn;
            doCloseX = true;
            doCloseButton = false;
            resizeable = true;
            draggable = false;
            absorbInputAroundWindow = true;
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
            EnforceMinimumWindowSize();
            draggable = detailTab != DetailTab.Schedule;
            if (pawn == null || pawn.Destroyed)
            {
                Widgets.Label(inRect, "Pawn unavailable.");
                return;
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width - 40f, 30f), PawnLabel(pawn));
            Text.Font = GameFont.Small;

            Rect tabRow = new Rect(0f, 64f, inRect.width, 28f);
            OrderedTabStrip.Draw(tabRow, new[] { "Overview", "Horario", "Zona" }, (int)detailTab,
                delegate(int index)
                {
                    detailTab = (DetailTab)index;
                    detailTabScroll = Vector2.zero;
                });

            Rect content = new Rect(12f, tabRow.y + 8f, inRect.width - 24f, inRect.height - tabRow.y - 48f);

            switch (detailTab)
            {
                case DetailTab.Schedule:
                    DrawScrollableTab(content, DrawScheduleTab, 860f, 340f);
                    break;
                case DetailTab.Zone:
                    DrawScrollableTab(content, DrawZoneTab, 500f, 300f);
                    break;
                default:
                    DrawOverview(content);
                    break;
            }
        }

        private void DrawDetailTab(Rect rect, DetailTab tab, string label)
        {
            if (Widgets.ButtonText(rect, label) && detailTab != tab)
            {
                detailTab = tab;
                detailTabScroll = Vector2.zero;
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