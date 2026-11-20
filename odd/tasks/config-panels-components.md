# Config panel components

Goal: move config-tab state and drawing out of `OrderedWorkMod`.

## Done

- [x] `ProfilesPanel`.
- [x] `WorkOrderRulesPanel` owns work-order draft/baseline and pin rendering state.
- [x] `OrderEditorPanel` owns giver-order draft/initial state.
- [x] Existing drag helpers remain shared for now.
- [x] Build 0/0 and tests pass.

## Remaining

- [ ] `SplitMergePanel` owns split/merge/drag/scroll/metadata state.
- [ ] Remove the remaining config fields from `OrderedWorkMod`.
- [x] SplitMerge state/drawing/validation/persistence/metadata extracted.\n- [x] Split tab lists existing splits with edit/delete/metadata/revert draft.\n- [x] Merge tab lists WorkTypes with giver differences against reference.json.\n- [x] Order tab lists WorkTypes with out-of-order Givers against reference.json.\n- [ ] Runtime check of profile, work order, giver-order and SplitMerge panels.