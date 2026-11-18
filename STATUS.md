# Ordered Work — Current Status

Version: `0.3.0-alpha.96` (refactor checkpoint). Build 0/0; tests 15/15.

## Completed architecture

- JSON profiles are the global source of truth: `WorkTypes` map keyed by `defName`, one owner per giver, `WorkTypeConfig.Order`.
- `active`, `pending` and `reference` JSON; atomic writes and backups.
- Startup promotes pending and applies the profile.
- `WorkConfigDiff` computes modified WorkTypes against `reference`.
- Legacy persisted lists are gone: `splits`, `merges`, `giverOrder`, `originalGiverOrder`, `workTypeOrder`.
- Split and merge are final profile state, not operation history.
- Work Control and Mod Settings are split into partial classes by responsibility.

## User-confirmed behavior

- Profile persistence and pending application.
- Version displayed once.
- Order Apply/Cancel dirty guards and reset.
- Split create/restart/delete.
- Merge/restore.
- Unified reset, no duplicated reset button.
- Palette, filters, cards, scrollbars, passion resolver.
- Settings, weights, Work Control, split/merge and save/load tested by the user.

## Refactor checkpoint (alpha.96)

- Passion pipeline split into `PassionSource`, `PassionWeightPolicy`, `PassionWeightsView` and `RecommendationScorer`.
- Pure scoring formula lives in `OrderedWork.Core.RecommendationScorePolicy` and is covered by tests (15/15).
- `SplitMerge` split into orchestration, columns, drag and persistence partials.
- `WorkOrderService` split into core, apply and lifecycle partials.
- Work Control remains split into core, rows, search and interaction.
- `WorkTypePaletteView` split into core (307), tooltip (142) and passion (415) partials.
- `WorkGiverDef` cloning is rejected as a design path.

## Approved policy decisions

- Work Control keeps one integrated tab. No separate global `Original` view.
- Top-level views: `Work`, `Policies` (`Scheduler` + `Zones`), and later `Clothing`.
- Returning to vanilla is an integrated per-view control.
- Zone assigns existing `Area_Allowed` areas only; no zone creation.
- Zone is per pawn and per map; store by `Map.uniqueID` plus area identity.
- Missing assigned area produces a semi-persistent alert, not a temporary card.
- Do not warn that an area blocks WorkTypes.
- Schedule source of truth is the vanilla 24-slot `Pawn.timetable`.
- Schedule view uses two parallel lanes: current timetable and selected preset/draft ghost.
- Schedule presets are named, global, 24-slot `TimeAssignmentDef` files in `Config/OrderedWork/schedules.json`.
- Apply a chosen preset to selected pawns; no per-WorkType hour windows in v1.
- Override contract comes before policy UI; manual lock > emergency > schedule/zone > base lane.
- Full override UI and clothing policy are later; clothing is by WorkType, per-pawn override in detail.

## Pending verification

- Runtime smoke test after the refactor: settings, pins, weights, split/merge, save/load.
- `test_1`/pending/reference alignment after a full create→restart→delete→restart cycle.
- No startup/profile fallback anomalies.
- No in-game exceptions; logs remain clean.
- Zone/schedule UI not implemented yet.

## Next scope

1. `PawnPolicy` + sparse override contract and persistence.
2. Zone assignment for existing areas, per map, with missing-area alert.
3. Schedule two-lane timetable editor and named presets.
4. Pawn detail UI, including zone/schedule/override status.
5. Clothing policy by WorkType.
6. Optional per-WorkType hour windows.
