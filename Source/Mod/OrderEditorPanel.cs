using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
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

        internal void Reset()
        {
            initialized = false;
            draft.Clear();
            initial.Clear();
            draftType = "";
        }

        internal void Draw(Listing_Standard list, OrderedWorkMod host, string activeProfile,
            Func<string> getSelected, Action<string> setSelected, Action<string> setStatus)
        {
            string selected = getSelected();
            WorkConfigLoadResult active = WorkConfigStore.LoadActiveWithFallback(activeProfile);
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

            int rowCount = Mathf.Max(referenceGivers.Count, draft.Count);
            Rect columns = list.GetRect(Mathf.Max(120f, 28f + rowCount * 24f));
            float columnWidth = (columns.width - 12f) * 0.5f;
            Rect left = new Rect(columns.x, columns.y, columnWidth, columns.height);
            Rect right = new Rect(columns.x + columnWidth + 12f, columns.y, columnWidth, columns.height);
            Widgets.DrawBoxSolid(left, UiColors.ColumnBackground);
            Widgets.DrawBoxSolid(right, UiColors.ColumnBackground);
            Widgets.Label(new Rect(left.x, left.y, left.width, 22f), OrderedWorkMod.ConfigText("OrderedWork.Config.Order.Origin", "Origin (last applied)"));
            Widgets.Label(new Rect(right.x, right.y, right.width, 22f), OrderedWorkMod.ConfigText("OrderedWork.Config.Order.Destination", "Destination (current)"));

            for (int i = 0; i < referenceGivers.Count; i++)
            {
                Rect row = new Rect(left.x, left.y + 24f + i * 24f, left.width, 22f);
                UiZebra.Draw(row, i);
                Widgets.DrawBoxSolid(row, new Color(0.20f, 0.25f, 0.30f, 0.45f));
                Widgets.LabelEllipses(new Rect(row.x + 4f, row.y, row.width - 8f, row.height), (i + 1) + ". " + referenceGivers[i]);
            }

            List<Rect> rows = new List<Rect>();
            for (int i = 0; i < draft.Count; i++)
            {
                Rect row = new Rect(right.x, right.y + 24f + i * 24f, right.width, 22f);
                UiZebra.Draw(row, i);
                host.DrawReorderRow(row, (i + 1) + ". " + draft[i], 5, i, null);
                rows.Add(row);
            }
            host.HandleReorderDrag(5, rows, MoveDraft);

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
            WorkConfigStore.SaveProfile(profile, true);
            WorkConfigStore.SavePending(profile);
            OrderedWorkStartup.ApplyProfileNow(profile, OrderedWorkMod.Settings, host);
            setStatus(string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.GiverOrder.Applied",
                "Giver order for {0} applied."), selected));
            Messages.Message(string.Format(OrderedWorkMod.ConfigText("OrderedWork.Config.GiverOrder.Saved",
                "Giver order saved for {0}."), selected), MessageTypeDefOf.TaskCompletion, false);
            initialized = false;
        }
    }
}