# Ordered Work (Core) 0.3.0-alpha.1

> Status: **0.3.0-alpha.1**. This build adds the first full-screen **Work Control** tab: a per-pawn horizontal WorkType lane with add/remove/drag, dense rank and a read-only result table, writing directly to `Pawn_WorkSettings` so the vanilla Work tab updates. Profiles, schedules, equipment, Overview, lane colors/stats, death parking and compatibility detection are **not implemented yet**. **Do not use it in a main save.**

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

## Work Control tab (0.3.0-alpha.1)
- Open the new **Work Control** bottom tab (between Work and Schedule). It does **not** replace the vanilla Work tab.
- Choose a colonist from the list at the top (player free colonists only).
- The horizontal lane shows that colonist's WorkTypes with full names, left = highest. Drag a card to reorder; the `×` on a card removes it (off, priority 0); the `+` card opens a picker of WorkTypes not already in the lane.
- Incapable WorkTypes are grey and cannot start a drag; they can still be removed.
- The horizontal lane scroll position is kept when you switch pawns. The pawn list, lane and result table are all inside one outer vertical scroll.
- The first change you make writes dense rank 1..N directly into `Pawn_WorkSettings.priorities` and calls `Notify_UseWorkPrioritiesChanged`, so the vanilla Work tab reflects the order. Merely selecting a pawn never writes.
- Each pawn's order is saved per colony in a `GameComponent`; save/load preserves it.
- The table below the lane is read-only: rank, WorkType and status (Active/Incapable). Removed types are off.

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

## Limitations (0.3.0-alpha.1)
- No profiles, schedules or equipment yet.
- No death parking yet; only basic cleanup of null/dead pawns in the saved order list.
- Incapable WorkTypes are shown grey and are never assigned.
- One selected pawn at a time; no multi-pawn editing and no Overview comparison yet.
- Reading the lane uses the stored order or the default `naturalPriority` order; it never applies priorities until you edit.
- If another mod replaces the Work tab, this mod does not replace it; it writes the same vanilla priorities.
- Not safe for a main save.

## Build and test
```
dotnet build Source/OrderedWork.Core/OrderedWork.Core.csproj -c Release
dotnet build Source/OrderedWork.csproj -c Release
dotnet run --project Tests/OrderedWork.Tests/OrderedWork.Tests.csproj -c Release
```
