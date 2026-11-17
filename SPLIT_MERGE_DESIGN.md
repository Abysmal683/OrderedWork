# Split / Merge / Order editor — scope

Status: **Phase 1b implemented in Mod Settings (unreleased)**. The settings window has `Rules` and `General` tabs. `Rules` holds the WorkType order and a three-mode `Split / Merge / Order` editor. Setup still applies on restart; hot apply remains out of scope.

## Shared layout

One editor is used for Split, Merge and Order:

- Mode switch: `Split mode` / `Merge mode` / `Order mode`.
- Split and Merge use two columns, top-to-bottom:
  - Left column: **Origin** WorkType selector + vertical list of its WorkGivers.
  - Right column: the draft destination list.
    - Split: the draft split list; the split name/defName fields sit above it.
    - Merge: the target WorkType selector (excludes the selected origin), the existing target givers (fixed at the top), then the incoming origin givers (movable).
- Order mode shows one origin WorkType selector and one reorderable giver list.
- Every giver column reserves a **6 px scrollbar track** on its right edge. A plain wheel scrolls the outer settings; the column scrolls only when the pointer is over its scrollbar or Ctrl is held over the column. The track shows a thumb, supports dragging, and has a scroll hint tooltip.
- The outer Mod Settings scroll is measured from `Listing_Standard.CurHeight` after drawing, so it is only as tall as the current tab content. The measurement resets when the tab or editor mode changes.
- Bottom controls: `Cancel draft` and the mode action (`Add split` / `Merge selected` / `Merge all`). Cancel clears the draft only.

## Split

- Required before `Add split` enables:
  - non-empty, editable name;
  - a valid base/origin WorkType;
  - at least one WorkGiver that belongs to the selected origin WorkType.
- Drag from Origin to the split list. The origin row stays in place but turns gray, preserving its original position; the split row is drawn in the normal color.
- Drag from Split back to Origin to remove it.
- `Add split` creates a new WorkType from the base WorkType and the selected givers, matching the existing `SplitEntry` data model.
- An empty split cannot be saved. The UI warns instead of silently creating an empty WorkType.
- Cancel clears the draft, not saved split entries.

## Merge

- Origin selector is always present; the target selector excludes the selected origin.
- The destination column shows, top to bottom:
  1. **Existing target givers**, fixed at the top and not draggable (they keep their current order).
  2. **Incoming origin givers**, added from the origin column, draggable and reorderable inside the destination.
- Each destination row shows its final position number; fixed rows are marked `(fixed)`. While dragging, the UI previews the insertion position.
- Drag individual givers from Origin to the destination and use `Merge selected`; the draft order is preserved. `Merge all` moves every origin giver. Both save a `MergeEntry.targetOrder` = existing target givers first (fixed order) followed by the incoming givers in their chosen order.
- A target WorkType cannot be the same as origin and cannot be empty.
- Merge does not delete either WorkType; it moves giver ownership. The origin may become a WorkType with no givers.
- `OrderedWorkStartup.ApplyMerges` assigns `workType` first and then applies `targetOrder` to `priorityInType`. Old saves without `targetOrder` keep the previous behavior.

## Order

- One origin WorkType selector plus one reorderable giver list; the old separate `Giver order` block is removed from `Rules`.
- Reordering writes `priorityInType` and the saved `giverOrder` entries for that WorkType.
- `Reset order` clears the saved override. The original def order returns after a restart because fresh defs are loaded before `ApplyOrder` runs; the current session keeps its priority values, which the UI states explicitly.

## Reset actions

- Split mode, per saved row: `Reset split` removes the `SplitEntry` (two-step confirmation). No WorkType is created or deleted in the editor; on restart the split WorkType is no longer recreated, so the split reverts.
- Merge mode, per saved row: `Reset merge` removes the `MergeEntry` (two-step confirmation). Foreign givers return to their original WorkType on restart.
- Order mode: `Reset order` (two-step confirmation) as described above.
- `Cancel draft` stays separate from every saved reset and never removes a saved entry.

## Validation and safety

- All actions are disabled until their required inputs are valid.
- Invalid/empty drafts show an inline warning and do not write to `OrderedWorkSettings`.
- `Cancel draft` never touches saved entries.
- Saved resets (`Reset split`, `Reset merge`, `Reset order`) and `Reset config mod all` are separate destructive controls and require two-step confirmation.
- Split/Merge/Order data is applied by `OrderedWorkStartup`; the editor states that changes require a restart. Hot-apply can be added later only with an explicit rebuild/reload path.
- No WorkType or WorkGiver is deleted; the editor never writes to a pawn.

## Confirmed merge model

Merge means “move giver ownership into the target WorkType.” `Merge selected` moves only the givers dragged to the destination; `Merge all` moves every giver of the origin. Neither WorkType is deleted, and the origin may end up with no givers. Existing target givers stay fixed at the top; incoming givers are inserted after them in the chosen order via `MergeEntry.targetOrder`. Drafts are UI-only; saved entries are applied by `OrderedWorkStartup` on restart.
