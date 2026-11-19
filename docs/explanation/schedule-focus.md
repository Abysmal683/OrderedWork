# Why hourly focus is an overlay

The pawn's stored work order is the source of truth. Hourly focus exists to change priorities for a short window without editing that order. Schedule and focus are independent: either component can be used without the other.

## Design decisions

- **Base order is immutable during focus.** `PawnWorkOrder.order` is never rewritten by hourly focus.
- **Focus is sparse.** Only changed hours are stored, with at most three ordered WorkTypes per hour.
- **The effective order is derived.** Valid focus entries are prepended; base entries follow without duplicates.
- **Invalid focus degrades gracefully.** An unavailable or removed WorkType is skipped, then the next focus is tried, then base #1.
- **Application is delayed.** Focus applies at the next hour boundary, never mid-hour and never per tick.
- **Jobs are not interrupted.** Priorities change; sleep, food and recreation continue. No `StopJob` call is made.
- **Presets are templates.** A focus preset stores 24 × 3 defNames and has no live link to a pawn.
- **Copy/paste is local.** It copies one hour's assignable focus in memory and validates against the target pawn at apply time.

## Consequences

- The UI can show a preview without changing runtime state.
- Missing content never silently damages the base order.
- Focus cannot force work during sleep, food or recreation.
- A broken or missing schedule preset cannot break focus, and vice versa.

## Boundaries

- No schedule-per-WorkType model.
- No direct apparel or item changes.
- No automatic application on load; the applied timetable already persists.
- Extended UI mechanics live in `docs/reference/ui-layout-rules.md`.
- User workflow lives in `docs/how-to/schedule-focus.md`.
