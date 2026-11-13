# Ordered Work (Core) 0.2.0-alpha.2

> Status: **0.2.0-alpha.2 technical spike**. The Work Control UI, per-pawn dense-rank UI, colors/stats, Overview, death parking and compatibility detection are **not implemented yet**. This build adds a pure `OrderedWork.Core` dense-rank library, a console test harness, and a Mod Settings test button. **Do not use it in a main save.**

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
- It only logs; there is no visual Work Control UI yet.
- Expected log shape: `[Ordered Work] Dense rank test <pawn>: max=<max>, count=<count>, first=<defName>=<priority>, last=<defName>=<priority>`. Compare `last` with its assigned rank (e.g. 23): if it survives, direct-map mode bypasses the vanilla 4 cap; if it reads 4, the value was clamped downstream.
- **Do not use this build in a main save**; test in a throwaway colony.

## Limitations (0.2.0-alpha.2)
- Technical spike: the dense-rank test only logs; no Work Control UI.
- Text fields for split/merge, no drag-and-drop yet.
- No removal of work types or givers.
- Global order, not per-pawn UI.
- No custom UI; uses the Mod Settings window.
- If another mod replaces the Work tab, this mod does not replace it; it only changes the def data.
- Not safe for a main save.

## Build and test
```
dotnet build Source/OrderedWork.Core/OrderedWork.Core.csproj -c Release
dotnet build Source/OrderedWork.csproj -c Release
dotnet run --project Tests/OrderedWork.Tests/OrderedWork.Tests.csproj -c Release
```
