# Mod Settings reference

## Mod Settings — Rules and General (unreleased phase 1b)
- The settings window has two in-memory tabs: **Rules** (WorkType order, Split/Merge/Order editor) and **General** (auto settings, palette stress test, dense-rank spike, reset). Tab selection is not persisted.
- The editor has `Split mode` / `Merge mode` / `Order mode`. Split/Merge use two columns with the left column always **Origin**; the right header is `Split destination` or `Destination`.
- Merge destination = existing target givers marked `(fixed)` at the top, then the incoming origin givers, which are draggable and reorderable. `MergeEntry.targetOrder` stores the final order (existing first, incoming after) and `OrderedWorkStartup.ApplyMerges` applies it after assigning `workType`. Old saves without `targetOrder` keep the old behavior.
- Each giver column has a 6 px scrollbar with a draggable thumb and a hint tooltip. A plain wheel scrolls the outer settings; the column scrolls only while the pointer is over its bar or Ctrl is held over the column. The outer settings scroll is sized from `Listing_Standard.CurHeight`, so it only spans the current tab content.
- Dragged rows are blue, valid drop rows get a blue/green tint, hovered rows highlight, and every movable giver row has a `≡` handle plus a tooltip (`defName` + WorkType). No tooltip is shown while a draft row is being dragged.
- `Add split` is enabled only with a non-empty name, a valid origin, and at least one draft giver that belongs to that origin. `Merge selected` and `Merge all` require a target different from the origin. Disabled actions show an inline warning.
- Drafts are UI-only (not scribed). Saved `SplitEntry` / `MergeEntry` entries remain scribed and apply on restart. Resets only remove the saved entry; no WorkType is created or deleted in the editor.
- `Reset split` stops recreating the split WorkType after restart. `Reset merge` returns foreign givers to their original type after restart. `Reset order` removes the saved giver override and restores the original def order after restart. All three need a restart and use two-step confirmation.

## Profile panel component

`ProfilesPanel` owns the local profile name and initialization state. It receives the active profile, shared status, and persist callback from the settings host so profile selection and pending writes remain coordinated with the other tabs.

## Component ownership

`ProfilesPanel`, `WorkOrderRulesPanel` and `OrderEditorPanel` own their local draft state. `OrderedWorkMod` remains the settings host and supplies shared profile selection, status and drag helpers. `SplitMerge` is the remaining large state block to extract.