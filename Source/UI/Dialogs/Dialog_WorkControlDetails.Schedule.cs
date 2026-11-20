using System;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    public partial class Dialog_WorkControlDetails
    {
        private readonly ScheduleEditorState scheduleState = new ScheduleEditorState();
        private readonly ScheduleBrush scheduleBrush = new ScheduleBrush();
        private ScheduleApplyController? scheduleApply;
        private ScheduleEditorView? scheduleView;

        private void DrawScheduleTab(Rect content)
        {
            scheduleApply = scheduleApply ?? new ScheduleApplyController(scheduleState, pawn);
            scheduleView = scheduleView ?? new ScheduleEditorView(scheduleState, scheduleBrush, scheduleApply);
            scheduleView?.Draw(content, pawn, delegate(Rect area, float y, float hourWidth)
            {
                DrawFocusSection(area, y, hourWidth);
            });
        }

        private void ResetScheduleEditor()
        {
            scheduleState.Reset();
            scheduleBrush.Reset();
            scheduleApply = null;
            scheduleView = null;
        }
    }
}