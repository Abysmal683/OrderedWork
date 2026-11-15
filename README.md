# Ordered Work (Core) 0.3.0-alpha.2

> Status: **0.3.0-alpha.2**. The **Work Control** tab now uses the vanilla Work-tab shell: one row per free colonist, a fixed pawn column on the left, a horizontal lane of WorkType cards on the right, one shared horizontal scroll for every row, and skill level + passion marker on each card. The per-pawn result table is removed. It still writes directly to `Pawn_WorkSettings` so the vanilla Work tab updates. Profiles, schedules, equipment, Overview, first-run recommendation/copy, vanilla-edit synchronization, death/resurrection reapply and compatibility detection are **not implemented yet**. **Do not use it in a main save.**

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

## Work Control tab (0.3.0-alpha.2)
- Open the new **Work Control** bottom tab (between Work and Schedule). It does **not** replace the vanilla Work tab.
- Layout mirrors the Work tab: one row per player free colonist. The pawn name is a fixed left column; the rest of the row is the colonist's horizontal WorkType lane.
- One **shared horizontal scroll** moves every lane together; one outer vertical scroll moves all pawn rows. The horizontal position is kept across redraws.
- Cards show the best relevant skill level (`WorkTypeDef.relevantSkills`, highest level; `—` when none) and a temporary passion marker (`**` major, `*` minor). The marker is isolated in one method so a future icon can replace it.
- Drag a card to reorder within that pawn's lane (left = highest). The `×` on a card removes it (off, priority 0). The `+` card opens a picker of WorkTypes not in that lane.
- Incapable WorkTypes are grey, labelled `Incapaz`, cannot be added or dragged, and can still be removed.
- The first change writes dense rank 1..N directly into `Pawn_WorkSettings.priorities` and calls `Notify_UseWorkPrioritiesChanged`, so the vanilla Work tab reflects the order. Merely opening the tab never writes.
- Each pawn's order is saved per colony in a `GameComponent`; save/load preserves it.
- The per-pawn result table from 0.3.0-alpha.1 is removed from the main view.

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

## Limitations (0.3.0-alpha.2)
- No profiles, schedules or equipment yet.
- No first-run copy or skill recommendation: a pawn without a stored order shows the default `naturalPriority` order until you edit it, and editing writes the full lane at once.
- No vanilla Work-tab synchronization hook; edits made in the vanilla Work tab are not pulled back into the lane.
- No death/resurrection reapply yet; only basic cleanup of null/dead pawns in the saved order list.
- The "Sort by WorkType" header button is not implemented yet.
- Incapable WorkTypes are shown grey with a generic `Incapaz` label; a precise disability reason is not derived yet.
- If another mod replaces the Work tab, this mod does not replace it; it writes the same vanilla priorities.
- Not safe for a main save.

## Build and test
```
dotnet build Source/OrderedWork.Core/OrderedWork.Core.csproj -c Release
dotnet build Source/OrderedWork.csproj -c Release
dotnet run --project Tests/OrderedWork.Tests/OrderedWork.Tests.csproj -c Release
```
