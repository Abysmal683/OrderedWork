# UiDragList external scrollbar

Goal: remove the native scrollbar from reusable draggable lists.

## Tasks

- [x] `UiDragList` reserves the external track before drawing rows.
- [x] It calls `WorkGridScrollView.HandleVertical` with a per-list owner.
- [x] It draws the track outside the content viewport.
- [x] Existing vertical callers pass an owner.
- [x] Build 0/0 and tests pass.
- [ ] Runtime check with two visible lists.

## Log

- L1. Native `Widgets.BeginScrollView` bar no longer covers the last row.
- L2. Per-list owner prevents two visible lists from sharing one drag state.