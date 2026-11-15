# Work Control — Milestone 0.3.0-alpha.2 Checklist

Status: in progress — version 0.3.0-alpha.2. Row-lane layout and GameComponent constructor iteration written; in-game checks pending.
Goal: first usable Work Control tab with a horizontal lane that assigns WorkTypes and updates the vanilla Work tab.

## Scope in

- New full-screen bottom tab (`MainTabWindow`) named “Work Control”. It does **not** replace the vanilla Work tab.
- Pawn selector: player free colonists only; NPCs/mechs/animals untouched.
- Horizontal WorkType lane for the selected pawn:
  - left→right = priority;
  - `+` picker;
  - remove = off (priority 0);
  - drag/reorder;
  - dense rank 1..N;
  - grey = incapable.
- Horizontal scroll/slide for the lane; **full names** in this version.
- A second (outer) vertical scroll encloses the pawn list + WorkType lane so the whole block scrolls down; the horizontal lane scroll position is preserved when switching pawns.
- Horizontal scroll/slide for the lane; compact cards; long names do not overflow.
- Per-pawn order persisted in a `GameComponent`; default order from `naturalPriority`; missing defs skipped.
- Direct `Pawn_WorkSettings.priorities` DefMap write; `Notify_UseWorkPrioritiesChanged`; vanilla Work tab updates.
- Result table removed for this iteration (future collapsible toggle).
- Version `0.3.0-alpha.1`, changelog, tests, Git branch/commit.

## Scope out (this milestone)

- Profiles/RoleSlots, schedules, equipment/Outfit Stands, zones, advanced per-pawn tab, multi-pawn jobs, global split/merge.
- Death parking (basic cleanup only; full parking later).
- Mechs, slaves, prisoners, NPCs.
- Replacing the vanilla Work tab.
- Per-giver per-pawn priorities.

## Loading and synchronization semantics

- Fix `OrderedWorkGameComponent` so RimWorld can instantiate it with a `Game` constructor; the current parameterless constructor fails at runtime (`MissingMethodException`).
- Add an `initialized` flag to the GameComponent.
- First initialization in an **existing colony**: copy the current Work-tab priorities into the per-pawn order for existing free colonists; sort active types by priority (1 highest) then `naturalPriority`; do not auto-recommend. Do not write vanilla until the first edit (or a “Sync & apply” button) to avoid silently changing an established colony.
- First initialization in a **new/default colony**: generate the skill recommendation and apply it once.
- Any **new colonist after initialization** (including a recruited enemy): generate the skill recommendation and apply it once.
- After initialization, the player’s lane edits are the source of truth; the stored order is saved.
- Add an isolated **Generate recommendation** button for the selected pawn; it must not affect other pawns.
- Do **not** add a “Sync from Work tab” button. Capture vanilla Work-tab changes automatically:
  - Harmony postfix on `Pawn_WorkSettings.SetPriority`/`Notify_UseWorkPrioritiesChanged` for managed pawns;
  - rebuild the stored order from the current priorities, with an `isApplying` guard to ignore our own writes;
  - future setting: both UIs or only ours.
- On death/resurrection, reapply the stored order after the pawn spawns (Harmony postfix on spawn/resurrect). Work Control already shows the stored order; the vanilla Work tab must be re-synced too.
- UI states: **Active/assigned** (rank), **Off/not assigned** (priority 0, dim), **Incapable** (grey, tooltip reason; not addable).
- No automatic refill with all WorkTypes after the first initialization or after removing entries; an empty stored order means “all off.”

## Main layout: Work-tab rows as lanes

- Keep the Work-tab shell: rows = pawns, one row per pawn.
- Two parallel columns: pawn name/title fixed on the left; the WorkType lane on the right.
- Replace the check-cell row with a **horizontal lane of movable WorkType cards** in that pawn’s priority order.
- Cards show the relevant skill level; a passion marker/icon is prepared (placeholder for now).
- One **shared horizontal scroll** for all rows (so the whole grid moves together and the lane position is preserved); one outer vertical scroll for pawn rows.
- Result table removed for this iteration (future collapsible toggle).
- States: Active/assigned, Off/not assigned, Incapable (grey + reason; not addable).
- Future: a “Sort by WorkType” button aligns/highlights one WorkType across rows without hiding the others, so the player can compare priority/skill for that WorkType.
- Future setting: both Work tabs or only the mod’s Work Control.

## Functional checks

- [ ] New bottom tab appears; full-screen; vanilla Work tab still exists.
- [ ] Pawn list shows only player free colonists; selecting changes the lane.
- [ ] Lane shows current WorkType order; incapable WorkTypes are grey/non-draggable.
- [ ] `+` opens a picker of available WorkTypes; selecting adds it.
- [ ] Remove/off sets that WorkType to 0.
- [ ] Drag reorders the lane; leftmost = highest.
- [ ] Horizontal lane scroll shows full names and does not overflow.
- [ ] Two parallel columns: pawn name left, lane right; one shared HScroll.
- [ ] Cards show the relevant skill level and a passion placeholder/icon hook.
- [ ] Result table is removed/collapsed, not taking main space.
- [ ] Outer vertical scroll moves the pawn list + lane; horizontal lane position is preserved when switching pawns.
- [ ] Dense rank is unique 1..N; numbers hidden in the lane.
- [ ] Applying writes the direct `priorities` map, calls `Notify`, and the vanilla Work tab reflects the values.
- [ ] Values >4 are visible in the vanilla Work tab (as in the spike).
- [ ] Save/load preserves the per-pawn order.
- [ ] Missing/removed defs are skipped without exceptions.
- [ ] No NPC/mech/animals are modified.
- [ ] Result table is removed/collapsed, not taking main space.
- [ ] No IMGUI scroll-pairing or exception errors.

## Technical checks

- [ ] `MainButtonDef` XML for the new tab.
- [ ] `MainTabWindow_WorkControl` with responsive `DoWindowContents`.
- [ ] `OrderedWorkGameComponent` persists per-pawn order.
- [ ] `WorkOrderService` normalizes order, builds ranks, writes direct DefMap.
- [ ] Reuses `DenseRank` and existing tests.
- [ ] Build: 0 warnings / 0 errors.
- [ ] Console tests: all pass.
- [ ] Manual minimal-profile test: no red errors in `Player.log`.

## Manual test script

1. Minimal profile: Core + Harmony + Ordered Work.
2. Load a small test colony.
3. Open the new “Work Control” tab.
4. Select a colonist; add two WorkTypes with `+`.
5. Drag one to the front; remove another.
6. Open the vanilla Work tab: priorities should reflect the new order/values.
7. Save, quit, reload: the order should persist.
8. Check an incapable WorkType stays grey.
9. Check NPCs/animals are untouched.
10. Report `Player.log` errors.

## Risks

- Drag + horizontal scroll in IMGUI (click-through, scroll pairing).
- `MainTabWindow`/`MainButtonDef` boilerplate and tab order.
- Persistence references to dead/removed pawns.
- Direct DefMap write + `Notify` timing.
- Vanilla UI display >4 with Compact Work Tab; test minimal first.
- No death parking yet; document the limitation.

## Done criteria

- All functional and technical checks pass.
- The user can reorder a colonist’s WorkTypes through the lane and see the vanilla Work tab change.
- No regressions for unmanaged pawns.
- Version/tag/changelog/Git commit recorded.
