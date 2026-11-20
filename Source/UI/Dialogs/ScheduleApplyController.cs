#nullable disable
using System;
using System.Collections.Generic;
using OrderedWork.Config;
using OrderedWork.Services;
using RimWorld;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Owns schedule draft transitions and persistence. The view never writes files.
    /// </summary>
    internal sealed class ScheduleApplyController
    {
        private readonly ScheduleEditorState state;
        private readonly Pawn pawn;

        internal ScheduleApplyController(ScheduleEditorState state, Pawn pawn)
        {
            this.state = state;
            this.pawn = pawn;
        }

        internal bool HasChanges
        {
            get { return state.HasChanges(pawn); }
        }

        internal void New()
        {
            state.SetDraft(new SchedulePreset { Name = "" });
            state.Name = "";
        }

        internal void CopyCurrent()
        {
            state.SetDraft(ScheduleAdapter.CaptureCurrent(pawn, ""));
            state.Name = "";
        }

        internal void OpenLoad()
        {
            Find.WindowStack.Add(new Dialog_SchedulePreset(true, null, delegate(SchedulePreset loaded)
            {
                state.SetDraft(loaded);
            }));
        }

        internal void OpenSave()
        {
            if (state.Draft == null)
            {
                return;
            }
            string initialName = string.IsNullOrWhiteSpace(state.Name) ? pawn.LabelShort : state.Name;
            Find.WindowStack.Add(new Dialog_SchedulePreset(false,
                delegate { return state.Draft; }, null, initialName));
        }

        internal bool Apply()
        {
            if (state.Draft == null)
            {
                return false;
            }
            ScheduleAdapter.ApplyDraft(pawn, state.Draft);
            Messages.Message(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Schedule.AppliedPrefix", "Schedule applied to ")
                + pawn.LabelShort + OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Schedule.AppliedSuffix", "."),
                MessageTypeDefOf.TaskCompletion, false);
            return true;
        }


    }
}