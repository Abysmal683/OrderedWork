# Work Control — Milestone 0.3.0-alpha.1 Checklist

Status: planned, no code yet.
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
- Horizontal scroll/slide for the lane; compact cards; long names do not overflow.
- Per-pawn order persisted in a `GameComponent`; default order from `naturalPriority`; missing defs skipped.
- Direct `Pawn_WorkSettings.priorities` DefMap write; `Notify_UseWorkPrioritiesChanged`; vanilla Work tab updates.
- Read-only result table below the lane for verification.
- Version `0.3.0-alpha.1`, changelog, tests, Git branch/commit.

## Scope out (this milestone)

- Profiles/RoleSlots, schedules, equipment/Outfit Stands, zones, advanced per-pawn tab, multi-pawn jobs, global split/merge.
- Death parking (basic cleanup only; full parking later).
- Mechs, slaves, prisoners, NPCs.
- Replacing the vanilla Work tab.
- Per-giver per-pawn priorities.

## Functional checks

- [ ] New bottom tab appears; full-screen; vanilla Work tab still exists.
- [ ] Pawn list shows only player free colonists; selecting changes the lane.
- [ ] Lane shows current WorkType order; incapable WorkTypes are grey/non-draggable.
- [ ] `+` opens a picker of available WorkTypes; selecting adds it.
- [ ] Remove/off sets that WorkType to 0.
- [ ] Drag reorders the lane; leftmost = highest.
- [ ] Dense rank is unique 1..N; numbers hidden in the lane.
- [ ] Applying writes the direct `priorities` map, calls `Notify`, and the vanilla Work tab reflects the values.
- [ ] Values >4 are visible in the vanilla Work tab (as in the spike).
- [ ] Save/load preserves the per-pawn order.
- [ ] Missing/removed defs are skipped without exceptions.
- [ ] No NPC/mech/animals are modified.
- [ ] Result table is read-only.
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
