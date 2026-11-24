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
