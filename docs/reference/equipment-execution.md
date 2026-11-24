# Equipment execution — reference

`Equip now` builds the same resolver plan used by badges and queues vanilla jobs.

## Flow

1. Resolve active sets from the pawn assignment.
2. Add active WorkType set, emergency state and overrides.
3. Resolve the plan.
4. Skip apparel already worn.
5. Find an available apparel by def on the map:
   - spawned;
   - not forbidden;
   - reservable by the pawn;
   - reachable.
6. Queue `JobDefOf.Wear`.
7. If the plan has a different primary, find an available equipment item and queue `JobDefOf.Equip`.
8. Log a summary under `OrderedWorkLogCategory.Equipment`.

## Current limits

- Reservations are owned by the vanilla job/driver, not preclaimed.
- No coverage repair after removal yet.
- No vanilla apparel-optimizer fallback when there is no set yet.
- No compact pawn gizmo yet.
- Missing items are skipped; no craft/request loop.
## Approved execution decisions

- Coverage repair: automatic after a removal.
  - one bounded pass;
  - preserve locked/forced and policy;
  - no loop;
  - missing item -> `uncovered` + warning.
- No active set: use the vanilla apparel optimizer/fallback.
- Gizmo: compact, multi-pawn, disabled when incapacitated or the equipment system is off.
  - Float menu can temporarily widen the search:
    - current radius;
    - wider radius;
    - whole map;
    - reset to setting;
    - open Equipment.
- Player/direct/authorized interruptions cancel the clothing process:
  - do not fight the interrupting job;
  - cancel untaken dress jobs;
  - leave the work plan valid for a later retry.
- Execution order:
  1. apparel;
  2. primary equipment;
  3. inventory last.
- Inventory is not part of the first execution slice; it waits for apparel/primary to settle.
## Gizmo

One compact Equipment gizmo is added to the first selected colonist. It applies the action to all selected colonists and offers current radius, 100, 200, whole map, and Open Equipment.

## Coverage repair and no-set fallback

`Equip now` builds the plan with an availability callback. For each cell it tries the local alternatives in priority order and skips an alternative whose item is not available, reserving the first available one.

- One bounded pass only; no retry loop.
- Forced/locked cells are never replaced by the repair pass.
- Apparel policy still filters candidates before availability.
- If no alternative is available, the cell stays uncovered and `MandatoryUncovered` is reported; the execution log names the uncovered cells.
- If the pawn has no active set, no equipment job is created and vanilla behavior remains in control.

## Inventory execution

Inventory targets are queued after apparel and primary equipment.

- Existing carried count is subtracted from the expected count.
- Only the remaining amount is taken.
- The item must be spawned, unforbidden, reachable, reservable, inside the search area and accepted by `Pawn_InventoryTracker`.
- Vanilla `JobDefOf.TakeInventory` is used; no custom hold capacity or reservation model.
- Missing items are skipped; inventory failure does not stop the earlier apparel/equipment jobs.
