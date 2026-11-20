# Frame-time and update strategy

The mod must not tie gameplay rules to frame rate. UI draw runs every frame; work rules run on ticks or explicit events.

## Applied

### Cached active profile

`WorkGiverOrderService` no longer reloads profiles from disk for every tooltip and row. `WorkConfigStore.LoadCachedActive` caches the active profile and selected profile name; saves, pending writes and profile selection invalidate it.

Why: profile JSON reads were happening inside `DoWindowContents` through `GetGlobalOrder`.

### Lane cache

`WorkControlRows.BuildLane` caches the ordered `WorkTypeDef` list per pawn by `PawnWorkOrder.orderVersion`. `SetOrder` increments that runtime-only revision. The cache also refreshes at most every 60 ticks so injury, gene or capacity changes are picked up without rebuilding every frame.

Why: Work Control and tooltips rebuild lanes per row per frame. The cache is bypassed when no stored entry exists, so recommendation-only pawns are not held stale.

### Work Control filter snapshot\n\n`GetFilteredColonists` caches the filtered pawn list by search text, Work filter, invert flag, scope and colonist count. It refreshes at most every 60 ticks; filter changes invalidate it immediately through the signature.\n\n### Details Overview snapshot\n\n`DrawOverview` caches top skills and work order by pawn and `orderVersion`, with the same 60-tick ceiling for skill changes.\n\n### Existing tick budgets

`HourFocusService` runs once per 2500 ticks. VSE discovery retries every 300 ticks. Stat/skill tooltips already cache for 60 ticks.

## Not applied yet

- Work Control filter snapshot.
- Details Overview snapshot.
- Configurable UI refresh interval.
- Frame-budget instrumentation.

## Rules

- Game logic: ticks or explicit events.
- UI: revision checks, not FPS.
- Refresh settings may only affect view snapshots.
- A cache without a documented invalidation path is not allowed.