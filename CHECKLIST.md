# Work Control — Milestone 0.3.0-alpha.33 Checklist

Status: in progress — version 0.3.0-alpha.33. Bug-fix and polish iteration written: half-height tab with top resize grip (minimum four pawn rows), centered Work combo with `Filter Work` title (selected WorkType excluded, `Any` only while filtering, 10 rows + thin bar), 3-state `Filter: Line/Palette/Both` scope (default Line, disabled until filtering) with exact WorkType matching and a `Clear` button, 135 px lane cards with 14 px passion icons, shared natural + modded passion icon resolver (VSE/Alpha Skills soft reflection, vanilla fallback, `Def.LabelCap` text), scrollbar input split before the scroll view from drawing after it plus separate palette/matrix horizontal drag owners, palette column-major fill in saved `workTypeOrder` with Shift+wheel and a cached debug 60-type stress setting, vertical drag-and-drop order for work types and givers with drag/drop visuals and hover tooltips, WorkType dropdown selector, shared layout/passion constants, Shift+wheel snap-to-end, and `CREDITS.md` provenance/licensing documentation. Build 0/0 and existing tests pass; runtime checks pending.

## Phase 1 — Mod Settings Rules/General + unified Split/Merge editor (unreleased)

Written in `Source/OrderedWorkMod.cs` only:

- [x] Two in-memory top tabs: `Rules` (WorkType order, giver order, Split/Merge editor) and `General` (auto recommend, auto-hide scrollbars, palette stress test, dense-rank spike, `Reset config mod all`). Tab selection is not scribed.
- [x] One shared two-column editor with `Split mode` / `Merge mode`; left column is always Origin, right column header is `Split destination` / `Merge destination`.
- [x] Split: origin dropdown, editable name with generated `defName` preview, Origin → Split adds, Split → Origin removes, origin row stays in place and turns gray while in the draft.
- [x] Split validation: `Add split` only with non-empty name, valid origin, and at least one origin-owned draft giver; inline warning when disabled.
- [x] Merge: origin + different-target selectors (origin excluded), `Merge selected` (draft only) and `Merge all` (every origin giver), inline warnings when disabled.
- [x] Drafts are UI-only; `Cancel draft` clears only the draft; saved entries are read-only and never deleted; saved entries remain scribed and apply on restart.
- [x] Drag visuals: blue dragged row, blue/green valid-drop tint, hover highlight, `≡` handle, giver tooltips, no tooltip while dragging.
- [x] `Reset config mod all` clears drafts and settings but not pawn 1..N priorities.
- [x] Build `0 errors / 0 warnings`; existing test suite prints `All tests passed.`

### Runtime checks (pending)

- [ ] Opening Mod Settings shows `Rules` and `General` tabs; switching tabs does not save anything.
- [ ] WorkType and giver order drag rows still reorder as before.
- [ ] Split: dragging origin rows into the destination adds them; dragging back removes them; origin rows turn gray in place.
- [ ] Split: `Add split` stays disabled with a warning until name + valid origin + one origin giver are present; saving adds a read-only saved entry.
- [ ] Merge: target list omits the origin; `Merge selected` saves only dragged givers; `Merge all` saves every origin giver.
- [ ] `Cancel draft` leaves saved split/merge entries untouched.
- [ ] Restarting RimWorld applies saved splits/merges; drafts never persist.
- [ ] `Reset config mod all` clears settings and drafts, and pawn priorities are unchanged.

## Phase 1b — vision fixes (unreleased)

Written in `Source/OrderedWorkMod.cs`, `Source/OrderedWorkSettings.cs`, `Source/OrderedWorkStartup.cs`:

- [x] Outer Mod Settings scroll height comes from `Listing_Standard.CurHeight` (used next frame) instead of a fixed 4000 px; it resets on tab/mode change.
- [x] Each giver column has a 6 px scrollbar track with a draggable thumb and a hint tooltip.
- [x] Plain wheel scrolls the outer settings unless the pointer is over the column bar or Ctrl is held over the column.
- [x] Merge destination shows existing target givers fixed at the top (marked `(fixed)`) and incoming origin givers below, reorderable with position numbers and a drop preview.
- [x] `MergeEntry.targetOrder` added; `SaveMerge` writes existing target givers first then incoming; `ApplyMerges` assigns `workType` then applies `targetOrder` to `priorityInType`. Empty `targetOrder` = old save behavior.
- [x] Third `Order mode` with origin selector and one reorderable giver list; old separate `Giver order` block removed from `Rules`.
- [x] `Reset split` / `Reset merge` per saved row and `Reset order` in Order mode, each with two-step confirmation; `Cancel draft` stays separate.
- [x] No WorkType creation/deletion in the editor; all resets take effect on restart.
- [x] Build `0 errors / 0 warnings`; existing test suite prints `All tests passed.`

### Phase 1b runtime checks (pending)

- [ ] The Mod Settings outer scroll stops at the current tab content instead of 4000 px.
- [ ] A giver column with overflow shows a thin bar; dragging it scrolls that column; a plain wheel over the column scrolls the settings; Ctrl+wheel scrolls the column.
- [ ] Merge destination shows `(fixed)` target givers first and movable incoming givers after; the position numbers and drop preview match the saved insertion order.
- [ ] `Merge selected` saves only draft givers with a `targetOrder` that places them after the fixed givers; `Merge all` does the same for every origin giver.
- [ ] After restart, the merged givers appear in the saved `targetOrder` within the target work type.
- [ ] Order mode reorders givers; `Reset order` asks for confirmation, removes the override, and the original order returns after restart.
- [ ] `Reset split` / `Reset merge` ask for confirmation, remove the saved row, and the change appears after restart; `Cancel draft` does not touch saved rows.


## Alpha.14 fixed items

- Full-screen `RequestedTabSize` (`UI.screenWidth`, `UI.screenHeight - 35`).
- Combo anchored under the Work button; bottom overflow flips it above; selected WorkType excluded; `Any` first; max 10 visible rows + thin integrated scrollbar.
- Work filter scope `Line` (default) / `Palette` / `Both`; `Not` inverts where it applies; Pawn search filters only pawn rows; exact `WorkTypeDef` identity (`Patient` ≠ `PatientBedRest`); `N match(es)` for Pawn search or Line scope.
- Lane `CardWidth` 135; ghost/placeholder, `+`, remove, ellipsis and lane content follow.
- Passion visual resolver shared by lane, palette drag card and Details; VSE/Alpha Skills `Icon` then `WorkBoxIcon` + `color` tint; vanilla colored then gray fallback; no icon for `None`; cached reflection with silent fallback; `PassionText` uses `LabelCap`; custom passions ranked by `color`.
- Scrollbars: `HandleVertical`/`HandleHorizontal` run before `Widgets.BeginScrollView`; `DrawVertical`/`DrawHorizontal` after content; horizontal bar stays interactive with the vertical bar; one temporary `MouseDown` diagnostic per bar.

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

## Alpha.14 runtime checks

- [ ] Work Control fills the screen above the main-tab bar.
- [ ] Work combo opens under the button; near the bottom edge it flips above; it excludes the current selection and keeps `Any` first.
- [ ] `Filter: Line` (default) filters pawn rows only and leaves the palette complete.
- [ ] `Filter: Palette` filters chips only; `Filter: Both` filters both; `Not` inverts the Work filter where it applies.
- [ ] Pawn search never hides palette chips; `N match(es)` appears for Pawn search or Line scope.
- [ ] `Patient` does not match `PatientBedRest` in either direction.
- [ ] Lane cards are 135 px and the drag ghost/remove/ellipsis still line up.
- [ ] With VSE/Alpha Skills active, lane/drag/Details show the modded passion icons and `LabelCap` text.
- [ ] Without VSE/Alpha Skills, the vanilla colored icons appear, with gray fallback if missing.
- [ ] Clicking/dragging the horizontal bar works while the vertical bar is visible; one `[OrderedWork][Scroll] H/V down ...` line appears per MouseDown.

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
