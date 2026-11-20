# Mod Settings reference

## Mod Settings — Rules and General

- The settings window has two in-memory tabs: **Rules** (WorkType order and Split/Merge/Order editor) and **General** (auto settings, palette stress test, dense-rank spike, reset). Tab selection is not persisted.
- The editor has `Split` / `Merge` / `Order` modes. Split and Merge use two columns with the left column always **Origin**; the right header is `Split destination` or `Destination`.
- Split and Merge action buttons expose tooltips. `Add split`, `Merge selected`, `Merge all` and `Cancel draft` explain their effect on hover.
- Candidate lists and the Order editor columns show at most **10 rows**. Use the scrollbar or `Ctrl+wheel` over the list/column; a plain wheel scrolls the outer settings.
- Collapsible Rules sections start collapsed. Passion weight rows render in full; the outer settings scroll handles the full table, with no internal passion cap or scrollbar.
- Merge destination = existing target Givers marked `(fixed)` at the top, then incoming origin Givers, which are draggable and reorderable.
- Each Giver column has a 6 px scrollbar with a draggable thumb and a hint tooltip.
- Drafts are UI-only and are not persisted. Saved profile entries are applied by the profile pipeline and are safe across restart.
- Split metadata: `Label`, `Action word` and `Description` are editable. `defName` and `Base` are immutable. `Action word` maps to `gerundLabel`; blank falls back to the name.
- Split rows provide `Edit` and two-step `Delete`. Delete returns Givers to `Base` and is blocked when `Base` is missing.
- Merge and Order candidate rows provide `Revert` with two-step confirmation. Merge returns extra Givers to their reference owner and restores the selected WorkType; Order restores the reference sequence.
- There are no persistent `Reset split`, `Reset merge` or `Reset order` buttons. Use split `Delete` or candidate-row `Revert`.
- The outer settings scroll is sized from `Listing_Standard.CurHeight`, so it only spans the current tab content.

## Component ownership

`ProfilesPanel`, `WorkOrderRulesPanel`, `OrderEditorPanel` and `SplitMergePanel` own their local draft state. `SplitMergePanel` owns the split/merge editor state, metadata, candidate list state and persistence actions; the host provides profile selection and shared status callbacks.