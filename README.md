# Ordered Work (Core) 0.3.0-alpha.7

> Status: **0.3.0-alpha.7**. The vanilla Work tab now accepts **manual priorities above 4**: a clean-room Harmony cell override cycles 0..dynamic-max (max(4, visible capable WorkTypes), capped at 99, fallback 99), draws >4 distinctly and keeps the existing capture/stored-order path in sync. The Work Control UI, Draw/Manual/Both modes and all earlier behavior are unchanged. Profiles, schedules, equipment and Overview remain **not implemented**. **Do not use it in a main save.**

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

## Work Control tab (0.3.0-alpha.7)
- Open the new **Work Control** bottom tab (between Work and Schedule). It does **not** replace the vanilla Work tab.
- Layout mirrors the Work tab: one row per player free colonist. The pawn name is a fixed left column; the rest of the row is the colonist's horizontal WorkType lane. Outer/internal margins are ~4 px.
- One **shared horizontal scroll** moves every lane together; one outer vertical scroll moves all pawn rows. The horizontal position is kept across redraws.
- Cards are **flat single-line** rows (~190×30) with a fixed order: **passion icon → best relevant skill level → WorkType name → `×`**. The name is ellipsized and the full name is in the tooltip. If the WorkType has **no relevant skill**, only the name and `×` are drawn (no level/passion placeholders). The vanilla `WidgetsWork` minor/major passion textures are used.
- **Search pawns** (above the matrix) matches flexibly against `LabelShort`, full/short name, colored name, title and first/nick/last name components. A non-empty search **filters the rows** (matching pawns only) and shows `No pawns match` when nothing matches. It never selects pawns, changes orders or changes priorities. A selected pawn that is filtered out stays selected internally and simply is not drawn.
- **Single click** a pawn name/row selects it visually (highlight + border) for Work Control only; it never changes game selection or priorities. **Double-click** selects the pawn in game **unless it is already selected in game**, and then moves the camera to it; an already-selected pawn is left untouched.
- **View mode** (top right, persisted per save): `Draw` → `Manual` → `Both` → `Draw`, default `Both` (semantics unchanged from alpha.5).
- **Open Work tab** (top right) switches to the vanilla Work tab (`Find.MainTabsRoot.ToggleTab`). It does not hide either tab.
- Drag a card to reorder within that pawn's lane (left = highest). The `×` removes a card (off, priority 0). The `+` card opens the candidate picker.
- **Incapable WorkTypes stay out of the lane** and out of default/stored/written orders. The `+` picker lists the remaining visible WorkTypes with levels (`-` when none) and shows incapable ones greyed out as `Incapaz` (not selectable).
- The `R` button next to a pawn recomputes and applies the skill recommendation **for that pawn only**. Recommendations pin `Patient`, `PatientBedRest` and `Firefighter` first (when capable, in that order), then score the rest: no relevant skill `0`; level 0 no/minor/major passion `-3`/`1`/`3`; level ≥1 `level*2 + 2/4`; ties by `naturalPriority` descending.
- **Initialization runs at game start** (`GameComponentTick`), not when the tab opens. Existing colony copies the current Work-tab order without writing; Manual mode always copies; a new/default colony in Draw/Both applies the recommendation once.
- A Harmony postfix on `Pawn_WorkSettings.SetPriority` captures genuine vanilla Work-tab edits and rebuilds the stored order, ignoring our writes, `EnableAndInitialize`, spawning and a 120-tick post-spawn window.
- On spawn/resurrection the stored order is reapplied once; a new colonist with no stored order gets the recommendation applied. Dead-but-not-destroyed pawns keep their entries.
- Edits write dense rank 1..N directly into `Pawn_WorkSettings.priorities` and call `Notify_UseWorkPrioritiesChanged`.
- Each pawn's order is saved per colony in a `GameComponent`; save/load preserves it.
- No result table and no Sync button.

## Vanilla Work tab: manual priorities above 4 (0.3.0-alpha.7)
- The vanilla Work tab's priority cell for a capable **free colonist** is replaced by a clean-room cell:
  - **Left-click** cycles `0 → 1 → … → max → 0`.
  - **Right-click** decrements (stops at `0`).
- `max = max(4, number of visible WorkTypes the pawn is capable of)`, capped at `99`. If the dynamic count fails, it falls back to `99` (logged once).
- Priorities **0–4** keep the vanilla priority colours (the box is tinted and the number uses `WidgetsWork.ColorOfPriority`); `0` is an empty cell like vanilla.
- Priorities **>4** use a distinct blue box, a cyan number and a border, plus a higher-priority tooltip: `Priority N of max (above vanilla 4)`.
- `Pawn_WorkSettings.SetPriority` is normalised to `0..max` (it is not clamped to 4) and notifies once. The existing `SetPriority` capture rebuilds the stored per-pawn order; our own `Apply` is excluded by the `IsApplying` guard, so there is no recursion.
- Incapable cells and non-free-colonist pawns are left to vanilla (0..4 cells), preserving other UI mods' behavior as much as possible.
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

## Limitations (0.3.0-alpha.7)
- No profiles, schedules or equipment yet.
- Mode semantics are unchanged: Manual copies the vanilla order at initialization and for new colonists; resurrection still reapplies the stored order in all modes.
- The >4 vanilla cell applies only to capable free colonists; incapable cells and non-free-colonist pawns keep vanilla 0..4 behavior.
- If another mod also prefixes `WidgetsWork.DrawWorkBoxFor` (e.g. Compact Work Tab), Harmony ordering may let that mod win, leaving the cell capped at 4; the rest of the mod keeps working.
- The dynamic max falls back to 99 when the count fails; the fallback is logged.
- Selection and the search term are UI-only and not persisted; a non-empty search filters the drawn rows.
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
