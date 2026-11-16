# Ordered Work (Core) 0.3.0-alpha.9

> Status: **0.3.0-alpha.9**. Interaction/view iteration: vanilla Work-tab right-click now jumps from 0 to max; the top bar button is **Recommend** and acts on all selected pawns; rows show the pawn **portrait** plus name/title; selection is driven by `Find.Selector` (single click selects, shift-click toggles, double-click selects/pans/closes); the Draw/Manual/Both cycle is replaced by **View: Numeric** in Work Control and a **View: Draw** return button in the vanilla Work tab; and a Mod Settings checkbox **Auto recommend new colony** controls automatic recommendations. Everything else, including the above-4 vanilla priority patch, is kept. Profiles, schedules, equipment, drag palette and multi-pawn remain **not implemented**. **Do not use it in a main save.**

Basic RimWorld 1.7 work-management framework.

## What it does
- Orders work types and work givers. The top of each list runs first.
- Splits a work type into a new work type from a base type plus selected givers.
- Merges givers into an existing work type.
- Never deletes a work type or a work giver.

## Trait and incapacity safety
- Split work types copy `workTags`, `relevantSkills`, `requireCapableColonist`, `alwaysStartActive`,
  `disabledForSlaves`, `visible`, `visibleOnlyWithChildrenInColony`, life-stage work settings and
  `mechEnabledWorkTypes`.
- Traits with `disabledWorkTypes` / `requiredWorkTypes` also receive the new split work type.
- Backstory and trait tag-based work disables apply automatically through the copied `workTags`.
- Merging a giver changes which work type (and therefore which tags) govern it. Pick the target with
  the tags you want. Do not merge a giver into a type whose tags would let an incapable pawn do it.

## Install
1. Copy this folder to `RimWorld/Mods/OrderedWork`.
2. Enable it after work mods (Job Splitter, Some More Jobs, Compact Work Tab, etc.).
3. Back up the save before enabling.

## Work Control tab (0.3.0-alpha.9)
- Open the new **Work Control** bottom tab (between Work and Schedule). It does **not** replace the vanilla Work tab.
- Layout mirrors the Work tab: one row per player free colonist. The fixed left column shows the pawn **portrait** (about 32×32, scaled to row height) plus name and title; the rest of the row is the colonist's horizontal WorkType lane. Outer/internal margins are ~4 px.
- One **shared horizontal scroll** moves every lane together; one outer vertical scroll moves all pawn rows. The horizontal position is kept across redraws.
- Cards are **flat single-line** rows (~190×30) with a fixed order: **passion icon → best relevant skill level → WorkType name → `×`**. The name is ellipsized and the full name is in the tooltip. If the WorkType has **no relevant skill**, only the name and `×` are drawn (no level/passion placeholders). The vanilla `WidgetsWork` minor/major passion textures are used.
- **Search pawns** (above the matrix) matches flexibly against `LabelShort`, full/short name, colored name, title and first/nick/last name components. A non-empty search **filters the rows** (matching pawns only) and shows `No pawns match` when nothing matches. It never selects pawns, changes orders or changes priorities.
- **Selection is `Find.Selector`-driven.** Rows highlight the pawns currently selected in game (none selected → no highlight). **Single click** selects the pawn (no camera move, no close); **shift-click** toggles it in/out; **double-click** selects the pawn, moves the camera and closes Work Control. Selection is shared with the vanilla tab, so opening either tab in either order keeps the same selection.
- The top-bar **Recommend** button recomputes and applies the skill recommendation to **every selected pawn** (disabled when nothing is selected). The top-bar **Details** button opens the read-only `Dialog_WorkControlDetails` for the **first selected pawn**: name/title, top skills with levels/passions, and the WorkType order with rank, best skill/passion and status. Recommendations pin `Patient`, `PatientBedRest` and `Firefighter` first (when capable, in that order), then score the rest: no relevant skill `0`; level 0 no/minor/major passion `-3`/`1`/`3`; level ≥1 `level*2 + 2/4`; ties by `naturalPriority` descending.
- **View: Numeric** (top right) switches to the vanilla Work tab; the vanilla Work tab gets a **View: Draw** button to return. Both use `Find.MainTabsRoot`; neither tab is hidden or replaced.
- **Auto recommend new colony** (Mod Settings, default on): on = new/default colonies and new pawns get the scored recommendation applied; off = they copy the current vanilla order instead (no automatic recommendation). The top Recommend button still works when off.
- Drag a card to reorder within that pawn's lane (left = highest). The `×` removes a card (off, priority 0). The `+` card opens the candidate picker.
- **Incapable WorkTypes stay out of the lane** and out of default/stored/written orders. The `+` picker lists the remaining visible WorkTypes with levels (`-` when none) and shows incapable ones greyed out as `Incapaz` (not selectable).
- **Initialization runs at game start** (`GameComponentTick`), not when the tab opens. Existing colony copies the current Work-tab order without writing; with auto-recommend off, new/default colonies also copy; with it on, a new/default colony applies the recommendation once.
- A Harmony postfix on `Pawn_WorkSettings.SetPriority` captures genuine vanilla Work-tab edits and rebuilds the stored order, ignoring our writes, `EnableAndInitialize`, spawning and a 120-tick post-spawn window.
- On spawn/resurrection the stored order is reapplied once; with auto-recommend on, a new colonist with no stored order gets the recommendation applied; with it off, the vanilla order is copied. Dead-but-not-destroyed pawns keep their entries.
- Edits write dense rank 1..N directly into `Pawn_WorkSettings.priorities` and call `Notify_UseWorkPrioritiesChanged`.
- Each pawn's order is saved per colony in a `GameComponent`; save/load preserves it.
- No result table and no Sync button.

## Vanilla Work tab: manual priorities above 4 (0.3.0-alpha.9)
- The vanilla Work tab's priority cell for a capable **free colonist** is replaced by a clean-room cell:
  - **Left-click** cycles `0 → 1 → … → max → 0`.
  - **Right-click** jumps from `0` to `max`; otherwise decrements by 1.
- `max = max(4, number of visible WorkTypes the pawn is capable of)`, capped at `99`. If the dynamic count fails, it falls back to `99` (logged once).
- Priorities **0–4** keep the vanilla priority colours (the box is tinted and the number uses `WidgetsWork.ColorOfPriority`); `0` is an empty cell like vanilla.
- Priorities **>4** use a distinct blue box, a cyan number and a border, plus a higher-priority tooltip: `Priority N of max (above vanilla 4)`.
- `Pawn_WorkSettings.SetPriority` is normalised to `0..max` (it is not clamped to 4) and notifies once. The existing `SetPriority` capture rebuilds the stored per-pawn order; our own `Apply` is excluded by the `IsApplying` guard, so there is no recursion.
- Incapable cells and non-free-colonist pawns are left to vanilla (0..4 cells), preserving other UI mods' behavior as much as possible.
- The vanilla Work tab now also draws a **View: Draw** return button (top-right) that opens Work Control.
- If the expected `WidgetsWork` API or patch path is unavailable, the prefix logs once and returns to vanilla drawing.
- **Known limitation:** other mods that also prefix `WidgetsWork.DrawWorkBoxFor` (e.g. Compact Work Tab) may win the Harmony order; in that case the vanilla cell may stay capped at 4. The rest of the mod keeps working.

## Use
1. Open `Options -> Mod Settings -> Ordered Work`.
2. Reorder work types and givers with Up/Down.
3. Split: fill a new label, the base work type defName, and a comma-separated list of giver defNames.
4. Merge: fill a comma-separated list of giver defNames and the target work type defName.
5. Restart RimWorld. Changes are applied at load time.

## Technical spike: dense rank test (0.2.0-alpha.2)
- Load a colony first. In `Options -> Mod Settings -> Ordered Work`, click **Test dense rank on colonists (log)**. With no map loaded the window shows `Load a colony to run the dense-rank test.` instead of the button.
- The button writes dense rank `1..N` (where `N` is the number of visible work types, minimum 4) into each free colonist's private `Pawn_WorkSettings.priorities` map, bypassing the `SetPriority` clamp, then calls `Notify_UseWorkPrioritiesChanged()`. If the private field is unavailable it falls back to `SetPriority`.
- Inside a `try/catch`, any failure is logged as `[Ordered Work] Dense rank test failed: ...` so the Mod Settings scroll view always closes cleanly.
- It only logs; the Work Control tab above now provides the visual editor.
- Expected log shape: `[Ordered Work] Dense rank test <pawn>: max=<max>, count=<count>, first=<defName>=<priority>, last=<defName>=<priority>`. Compare `last` with its assigned rank (e.g. 23): if it survives, direct-map mode bypasses the vanilla 4 cap; if it reads 4, the value was clamped downstream.
- **Do not use this build in a main save**; test in a throwaway colony.

## Limitations (0.3.0-alpha.9)
- No profiles, schedules, equipment, drag palette or multi-pawn yet.
- Selection is `Find.Selector`-driven; shift-drag/marquee multi-select is not implemented (shift-click toggles). Row selection is via the pawn name/portrait column; lane cards remain editing controls.
- The vanilla **View: Draw** button is an overlay at the top-right of the vanilla Work tab and may cover part of the last column header.
- **Auto recommend new colony** is a global Mod Settings toggle. The old per-save `viewMode` field is kept only so old saves load harmlessly; it no longer drives behavior.
- `Dialog_WorkControlDetails` is read-only and basic: name/title, top 8 skills, and the current WorkType order. Editing, profiles, schedules and equipment are future work.
- The >4 vanilla cell applies only to capable free colonists; incapable cells and non-free-colonist pawns keep vanilla 0..4 behavior.
- If another mod also prefixes `WidgetsWork.DrawWorkBoxFor` (e.g. Compact Work Tab), Harmony ordering may let that mod win, leaving the cell capped at 4; the rest of the mod keeps working.
- The dynamic max falls back to 99 when the count fails; the fallback is logged.
- The search term is UI-only and not persisted; a non-empty search filters the drawn rows.
- Initialization only scans the current map's free colonists; a colony initialized on one map does not backfill colonists on another map until they spawn.
- The vanilla capture hook covers `SetPriority`; a mod that changes priorities by writing the private map directly will not be captured.
- The "Sort by WorkType" header button is not implemented yet.
- If another mod replaces the Work tab, this mod does not replace it; it writes the same vanilla priorities.
- Not safe for a main save.

## Build and test
```
dotnet build Source/OrderedWork.Core/OrderedWork.Core.csproj -c Release
dotnet build Source/OrderedWork.csproj -c Release
dotnet run --project Tests/OrderedWork.Tests/OrderedWork.Tests.csproj -c Release
```
