using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.UI;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork
{
    /// <summary>
    /// Giver-order draft and diff. The host owns profile selection and shared status.
    /// </summary>
    internal sealed class OrderEditorPanel
    {
        private List<string> draft = new List<string>();
        private List<string> initial = new List<string>();
        private string draftType = "";
        private bool initialized;
        private float scroll;

        internal void Reset()
        {
            initialized = false;
            draft.Clear();
            initial.Clear();
            draftType = "";
            scroll = 0f;
        }

        internal void Draw(Listing_Standard list, OrderedWorkMod host, string activeProfile,
            Func<string> getSelected, Action<string> setSelected, Action<string> setStatus)
        {
            string selected = getSelected();
            WorkConfigLoadResult active = WorkConfigStore.LoadCachedActive();
            if (active == null || active.ResolvedProfile != activeProfile)
            {
                active = WorkConfigStore.LoadActiveWithFallback(activeProfile);
            }
            WorkConfigProfile reference = WorkConfigStore.LoadReference();
            if (active == null || active.Profile == null || string.IsNullOrEmpty(selected))
            {
                list.Label(OrderedWorkMod.ConfigText("OrderedWork.Config.Order.SelectPrompt", "Choose a WorkType below to compare its giver order."));
                return;
            }
            WorkTypeConfig current;
            if (!active.Profile.WorkTypes.TryGetValue(selected, out current) || current == null)
            {
                list.Label(OrderedWorkMod.ConfigText("OrderedWork.Config.Order.NotInProfile", "Selected WorkType is not in the active profile."));
                return;
            }
            if (!initialized || draftType != selected)
            {
                draft = (current.Givers ?? new List<GiverConfig>())
                    .Where(g => g != null).OrderBy(g => g.Order).Select(g => g.DefName).ToList();
                initial = new List<string>(draft);
                draftType = selected;
                initialized = true;
            }

            List<string> referenceGivers = initial.Count > 0 ? new List<string>(initial) : draft.ToList();
            string workLabel = DefDatabase<WorkTypeDef>.GetNamedSilentFail(selected)?.labelShort ?? selected;
            list.Label(string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.Order.Header",
                "Giver order — {0}: origin = reference, destination = draft."), workLabel));

            const int VisibleRows = 10;
            int rowCount = Mathf.Max(referenceGivers.Count, draft.Count);
            int visibleRows = Mathf.Min(rowCount, VisibleRows);
            Rect columns = list.GetRect(28f + visibleRows * 24f);
            float barWidth = WorkGridScrollView.Thickness;
            Rect viewport = new Rect(columns.x, columns.y + 24f, columns.width - barWidth - 2f, visibleRows * 24f);
            float columnWidth = (viewport.width - 12f) * 0.5f;
            Rect left = new Rect(viewport.x, viewport.y, columnWidth, viewport.height);
            Rect right = new Rect(left.xMax + 12f, viewport.y, columnWidth, viewport.height);
            Rect track = new Rect(viewport.xMax + 2f, viewport.y, barWidth, viewport.height);
            Rect content = new Rect(0f, 0f, viewport.width, rowCount * 24f);
            float maxScroll = Mathf.Max(0f, content.height - viewport.height);
            scroll = Mathf.Clamp(scroll, 0f, maxScroll);
            OrderedWorkMod.HandleCtrlWheel(viewport, maxScroll, ref scroll, 24f);
            Vector2 scrollVector = new Vector2(0f, scroll);
            WorkGridScrollView.HandleVertical(track, viewport, content, ref scrollVector, "order-editor");

            Widgets.DrawBoxSolid(left, UiColors.ColumnBackground);
            Widgets.DrawBoxSolid(right, UiColors.ColumnBackground);
            Widgets.Label(new Rect(left.x, columns.y, left.width, 22f), OrderedWorkMod.ConfigText("OrderedWork.Config.Order.Origin", "Origin (last applied)"));
            Widgets.Label(new Rect(right.x, columns.y, right.width, 22f), OrderedWorkMod.ConfigText("OrderedWork.Config.Order.Destination", "Destination (current)"));

            GUI.BeginGroup(left);
            for (int i = 0; i < referenceGivers.Count; i++)
            {
                float y = i * 24f - scrollVector.y;
                if (y + 24f <= 0f || y >= viewport.height)
                {
                    continue;
                }
                Rect row = new Rect(0f, y, left.width, 22f);
                UiZebra.Draw(row, i);
                Widgets.DrawBoxSolid(row, new Color(0.20f, 0.25f, 0.30f, 0.45f));
                Widgets.LabelEllipses(new Rect(row.x + 4f, row.y, row.width - 8f, row.height), (i + 1) + ". " + referenceGivers[i]);
            }
            GUI.EndGroup();

            List<Rect> rows = new List<Rect>();
            GUI.BeginGroup(right);
            for (int i = 0; i < draft.Count; i++)
            {
                float y = i * 24f - scrollVector.y;
                if (y + 24f <= 0f || y >= viewport.height)
                {
                    continue;
                }
                Rect row = new Rect(0f, y, right.width, 22f);
                UiZebra.Draw(row, i);
                host.DrawReorderRow(row, (i + 1) + ". " + draft[i], 5, i, null);
                rows.Add(row);
            }
            host.HandleReorderDrag(5, rows, MoveDraft);
            GUI.EndGroup();

            scroll = Mathf.Clamp(scrollVector.y, 0f, maxScroll);
            WorkGridScrollView.DrawVertical(track, viewport, content, ref scrollVector);

            Rect buttons = list.GetRect(30f);
            float half = (buttons.width - 8f) * 0.5f;
            bool changed = !draft.SequenceEqual(initial);
            if (UiButtonBar.Button(new Rect(buttons.x, buttons.y, half, 28f),
                OrderedWorkMod.ConfigText("OrderedWork.Config.Order.Apply", "Apply giver order"), changed))
            {
                Apply(host, active.Profile, selected, setStatus);
            }
            if (UiButtonBar.Button(new Rect(buttons.x + half + 8f, buttons.y, half, 28f),
                OrderedWorkMod.ConfigText("OrderedWork.Config.Order.Cancel", "Cancel giver order"), changed))
            {
                draft = new List<string>(initial);
                initialized = true;
            }
        }

        private void MoveDraft(int from, int to)
        {
            if (from < 0 || from >= draft.Count || to < 0 || to >= draft.Count || from == to)
            {
                return;
            }
            string value = draft[from];
            draft.RemoveAt(from);
            draft.Insert(to, value);
        }

        private void Apply(OrderedWorkMod host, WorkConfigProfile profile, string selected, Action<string> setStatus)
        {
            if (profile == null || string.IsNullOrEmpty(selected)
                || !profile.WorkTypes.TryGetValue(selected, out WorkTypeConfig current)
                || current == null)
            {
                return;
            }
            current.Givers = current.Givers ?? new List<GiverConfig>();
            for (int i = 0; i < draft.Count; i++)
            {
                GiverConfig giver = current.Givers.FirstOrDefault(g => g != null && g.DefName == draft[i]);
                if (giver != null)
                {
                    giver.Order = i;
                }
            }
            current.Givers = current.Givers.OrderBy(g => g.Order).ToList();
            host.CommitProfile(profile);
            setStatus(string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.GiverOrder.Applied",
                "Giver order for {0} applied."), selected));
            Messages.Message(string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.GiverOrder.Saved",
                "Giver order saved for {0}."), selected), MessageTypeDefOf.TaskCompletion, false);
            initialized = false;
        }
    }
}