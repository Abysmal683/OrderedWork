# Ordered Work — Current Status

Version: `0.3.0-alpha.338` (runtime checkpoint). Build 0/0; tests pass.

## Latest checkpoint (.338)

- Profiles append visible live WorkTypes and missing Givers on load; Hospitality `Diplomat` can be selected in Order, Split and Merge.
- The Work tab reorders the list returned by `PawnTable.get_Columns`, which rebuilds from the table def on every call.
- Hidden layer implemented: right-click toggle, fixed GetPriority overlay, hidden-first WorkGiver cache, incapable-pawn skip and game-load revalidation.
- The `GetPriority` patch parameter was corrected to `w`; this restores all Harmony patches, manual priorities above 4 and the hidden layer.
- Work tab order is applied once at `PawnColumnDefGenerator` generation time; per-frame FPS hooks were removed.
- Project health P0 complete: `CommitProfile`, dead-code removal, SplitMergePanel partials, profile tests, build paths and Core nullable/analyzers.
- Restore order has two-step confirmation, reference-alignment blocking and red danger tint; all confirmation buttons use the same tint.
- Removed the debug WorkGiver spike and added `scripts/verify.ps1`; docs corrected for per-pawn WorkGiver order.
- Hardened UI/profile commit paths and removed the WorkTypePaletteView static-asset warning.
- `Reset config mod all` and preset deletes are guarded; reset never deletes profile/pending/reference files.
- Verification, build-path, nullable and documentation-index references are current.
- Global reset clears settings/passions and rebuilds only the active profile from reference; other files remain.
- Nullable is complete and warnings-as-errors are enabled.
- Clothing editor layout was corrected; touch cards add immediately and grid right-click removes rules. UX review and execution remain pending.
- WorkType order has `Restore reference order` beside Apply/Cancel; right-click toggles hidden with a visible hint.
- Passion weights render in full and Rules sections start collapsed.
- Hidden mod WorkTypes are not added and stale visible flags are cleared during reconciliation.
- Split delete and Merge revert restore origin visibility when Givers return.
- Passion tooltips resolve local-group and screen-space mouse coordinates before registering at the pointer.
- The project health audit is saved at `docs/explanation/project-health-audit.md`.
- Runtime confirmed through `.270`: tooltips, disabled Merge label, Merge/Order Revert, split Action word, the three tabs and Ctrl+wheel. Runtime confirmed: Work tab order (including Compact Work Tab), hidden WorkTypes and the new passion tooltip path.
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
- Schedule presets are named, global, 24-slot `TimeAssignmentDef` files in `Config/OrderedWork/schedules/<id>.json` (one file per preset).
- Apply a chosen preset to selected pawns; no per-WorkType hour windows in v1.
- Override contract comes before policy UI; manual lock > emergency > schedule/zone > base lane.
- Full override UI and clothing policy are later; clothing is by WorkType, per-pawn override in detail.

## Schedule focus (alpha.224)

- Hourly focus stack is implemented as a temporary overlay over `PawnWorkOrder.order`.
- Up to 3 ordered WorkTypes per hour, sparse per pawn; the base order is never rewritten.
- Runtime applies focus only at hour boundaries; it never stops jobs or interrupts sleep, food or recreation.
- The Schedule tab has an inline focus editor with a horizontal visible effective order, drag reorder, right-click removal, copy/paste hour and independent focus presets.
- Focus presets are separate atomic JSON files under `Config/OrderedWork/focus`; scheduler and focus work independently.
- Legacy `scheduleWorkOverrides` entries migrate into `hourFocusOverrides`.
## Pending verification

- Runtime smoke test after the refactor: settings, pins, weights, split/merge, save/load.
- `test_1`/pending/reference alignment after a full create→restart→delete→restart cycle.
- No startup/profile fallback anomalies.
- No in-game exceptions; logs remain clean.
- Pawn detail now exposes schedule, zone, override and read-only clothing status; clothing application is still pending.

## Next scope

1. `PawnPolicy` + sparse override contract and persistence.
2. Zone assignment for existing areas, per map, with missing-area alert.
3. Schedule two-lane timetable editor and named presets.
4. Pawn detail UI, including zone/schedule/override status.
5. Clothing policy by WorkType.
6. Optional per-WorkType hour windows.
