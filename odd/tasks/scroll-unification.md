# Scroll unification

Goal: one external-track scroll pane for local lists and dialogs.

## Migrated

- [x] `UiScrollPane` wrapper.
- [x] Focus preset dialog.
- [x] Schedule preset dialog.
- [x] Work preset dialog.
- [x] Pawn picker.
- [x] WorkType dropdown.
- [x] WorkPanel Skills.
- [x] WorkPanel Stats (specialized and generic).
- [x] WorkPanel Unable WorkTypes.
- [x] Details Overview.

## Deliberate native/custom exceptions

- [ ] Settings global scroll (`Listing_Standard`; not a local list).
- [ ] SplitMerge per-column scrollbars (special fixed columns).
- [ ] Details Overrides/Clothing (single text panel, no nested local list).
- [ ] Focus visible horizontal strip (horizontal behavior).
- [ ] WorkType combo custom thin bar (keyboard/Any row behavior).

## Verification

- [x] Build 0/0.
- [x] Core tests pass.
- [ ] Runtime scroll/wheel/shift-wheel check.