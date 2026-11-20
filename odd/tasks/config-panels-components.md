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
- [ ] Runtime check of profile, work order and giver-order panels.