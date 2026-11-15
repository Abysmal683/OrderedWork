# Ordered Work (Core) 0.3.0-alpha.4

> Status: **0.3.0-alpha.4**. The **Work Control** tab keeps the vanilla Work-tab shell (one row per free colonist, fixed pawn column, shared horizontal lane scroll, outer vertical scroll). Cards are compact two-line rows (~190×64), the `+` picker lists skill levels and greys incapable types, recommendations pin Patient/PatientBedRest/Firefighter and use a scored skill/passion model, and colony initialization now runs at game start instead of tab-open. The vanilla capture hook ignores our writes, `EnableAndInitialize`, spawning and a 120-tick post-spawn window to stop partial orders from overwriting stored ones. Profiles, schedules, equipment, Overview and the both-tabs setting remain **not implemented**. **Do not use it in a main save.**

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

## Work Control tab (0.3.0-alpha.4)
- Open the new **Work Control** bottom tab (between Work and Schedule). It does **not** replace the vanilla Work tab.
- Layout mirrors the Work tab: one row per player free colonist. The pawn name is a fixed left column; the rest of the row is the colonist's horizontal WorkType lane.
- One **shared horizontal scroll** moves every lane together; one outer vertical scroll moves all pawn rows. The horizontal position is kept across redraws.
- Cards are compact two-line rows (~190×64): the WorkType name on one line (ellipsized; full name in the tooltip) and the best relevant skill level (`—` when none) plus a temporary passion marker (`**` major, `*` minor). No extra blank line.
- **Incapable WorkTypes stay out of the lane** and out of default/stored/written orders. The `+` picker still lists the remaining visible WorkTypes with their best relevant skill level in brackets (`-` when none) and shows incapable ones greyed out as `Incapaz`; those are not selectable.
- Drag a card to reorder within that pawn's lane (left = highest). The `×` on a card removes it (off, priority 0). The `+` card opens the candidate picker.
- The `R` button next to a pawn recomputes and applies the skill recommendation **for that pawn only**. Recommendations pin `Patient`, `PatientBedRest` and `Firefighter` first (when capable, in that order), then score the rest: no relevant skill `0`; level 0 no/minor/major passion `-3`/`1`/`3`; level ≥1 `level*2 + 2/4`; ties by `naturalPriority` descending.
- **Initialization runs at game start** (`GameComponentTick`), not when the tab opens. An existing colony (any colonist with a vanilla priority other than 0/3) copies its current Work-tab order without writing. A new/default colony applies the recommendation once. The tab's initialization call is idempotent.
- A Harmony postfix on `Pawn_WorkSettings.SetPriority` captures genuine vanilla Work-tab edits and rebuilds the stored order. It ignores our own writes (`IsApplying`), `EnableAndInitialize` (`IsInitializing`), spawning (`IsSpawning`) and a 120-tick window after spawn/resurrection.
- On spawn/resurrection the stored order is reapplied once; a new colonist (including a recruited pawn via the faction hook) with no stored order gets the recommendation applied. Dead-but-not-destroyed pawns keep their entries.
- Edits write dense rank 1..N directly into `Pawn_WorkSettings.priorities` and call `Notify_UseWorkPrioritiesChanged`.
- Each pawn's order is saved per colony in a `GameComponent`; save/load preserves it.
- No result table, no Sync button, and no selected-row/search/mode selector yet.

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

## Limitations (0.3.0-alpha.4)
- No profiles, schedules or equipment yet.
- Initialization only scans the current map's free colonists; a colony initialized on one map does not backfill colonists on another map until they spawn.
- The vanilla capture hook covers `SetPriority`; a mod that changes priorities by writing the private map directly will not be captured.
- Work Control is the source of truth for the lane; the vanilla Work tab may not visibly reflect every lane edit yet (known limitation, not regressed further).
- The "Sort by WorkType" header button, selected-row highlight, double-click and mode selector are not implemented yet.
- If another mod replaces the Work tab, this mod does not replace it; it writes the same vanilla priorities.
- Not safe for a main save.

## Build and test
```
dotnet build Source/OrderedWork.Core/OrderedWork.Core.csproj -c Release
dotnet build Source/OrderedWork.csproj -c Release
dotnet run --project Tests/OrderedWork.Tests/OrderedWork.Tests.csproj -c Release
```
