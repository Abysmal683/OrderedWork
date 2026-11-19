# Schedule editor split

Goal: isolate Schedule draft state, brush, persistence and drawing.

## Tasks

- [x] `ScheduleEditorState` owns draft, name, change detection and draft resolution.
- [x] `ScheduleBrush` owns brush selection and paint/erase.
- [x] `ScheduleApplyController` owns load/new/copy/save/apply.
- [x] `ScheduleEditorView` owns drawing and the current-hour highlight hook.
- [x] `ScheduleVisuals` owns labels and assignment colors.
- [x] `Dialog_WorkControlDetails.Schedule` reduced to wiring.
- [x] Build 0/0 and tests pass.
- [ ] Runtime check of paint, erase, load, save and apply.

## Log

- L1. Existing shift/click behavior and preset dialogs preserved.
- L2. SetPawn resets schedule state through one method.