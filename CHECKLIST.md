# Ordered Work — Current Checklist (0.3.0-alpha.170)

Status: architecture cleanup complete; build 0/0 and 7/7 tests pass. Runtime checks remain for the final profile-only path.

## Architecture

- [x] Global JSON profiles with `active`, `pending` and `reference`.
- [x] Atomic profile writes and timestamped backups.
- [x] Canonical `WorkTypes` map keyed by `defName`; one owner per giver.
- [x] `WorkTypeConfig.Order` is the only WorkType order source.
- [x] `reference.json` is the reset baseline; `WorkConfigDiff` drives modified-state detection.
- [x] Legacy merge model removed (`MergeEntry`, `GiverOrigin`, `merges`).
- [x] Legacy giver-order model removed (`GiverOrderEntry`, `giverOrder`, `originalGiverOrder`).
- [x] Legacy split persistence removed (`Settings.splits`, persisted `SplitEntry`).
- [x] Legacy `workTypeOrder` removed.
- [x] Legacy Draw/Manual/Both `WorkViewMode` removed.
- [x] Startup promotes pending and applies the JSON profile.
- [x] Split creation is profile-backed; `EnsureSplitWorkTypes` creates defs from `WorkTypeConfig`.
- [x] Merge is profile-backed; giver movement is final state, not history.
- [x] `MainTabWindow_WorkControl` split into partial files: core, search, rows, interaction.
- [x] Mod Settings split into partial files: core, settings, order, split/merge, helpers.
- [x] `CREDITS.md` documents provenance and licensing posture.

## Confirmed in game

- [x] Active profile persists across restart.
- [x] Pending promotion and profile application.
- [x] Version shown once in General.
- [x] Order Apply/Cancel disabled until a draft change; reset works.
- [x] Split create/restart/delete.
- [x] Merge and restore.
- [x] Delete split does not create ghost types.
- [x] Reset changes unified in Order.
- [x] `Allow empty origin` is a single Rules-wide checkbox.
- [x] Work Control colonist-bar order, selection, cards, filters and scrollbars.
- [x] Passion icons and labels with VSE/Alpha Skills.

'## Automatic recommendation rules (alpha.81)

- [x] Global checkbox `Prioritize low-level passion over work without skill`.
- [x] Configurable recommendation pins with drag order and remove.
- [x] Add-pin dropdown covers every visible WorkType; no per-worktype weight yet.
- [x] Recommendation service reads the configured pins and policy.
- [x] Verbose diagnostics setting; normal logs are silent.
- [ ] Runtime recommendation test with different pawn skills/passions.
- [x] Each registered passion exposes Level 0, per-level and minimum-level weights; negative weights supported.
- [x] Default pins tooltip documents Patient/PatientBedRest/Firefighter.
- [ ] Runtime test of modded passion weights (VSE/Alpha).
- [ ] Decide whether each WorkType should also expose an individual weight or exclude toggle.

## Pending deep test after alpha.80'

- [ ] Create a split, restart, delete it, restart again.
- [ ] Merge Patient→Firefighter, restore Firefighter first, then Patient.
- [ ] Verify `test_1.json`, `test_1.pending.json` and `reference.json` stay aligned.
- [ ] Reorder WorkTypes in Rules, restart, verify palette order.
- [ ] Confirm no `Collection was modified` or profile fallback log.
- [ ] Confirm no legacy lists are read after load.

## Refactor checkpoint (alpha.96)

- [x] `PassionSource` owns VSE/Alpha discovery and metadata.
- [x] `PassionWeightPolicy` owns defaults, migration and reset.
- [x] `PassionWeightsView` owns the weight table, sprites, tooltips, scrollbar and reset.
- [x] `RecommendationScorer` owns skill selection and score composition.
- [x] Pure `RecommendationScorePolicy` has automated tests.
- [x] `SplitMerge` split into orchestration, columns, drag and persistence partials.
- [x] `WorkOrderService` split into core, apply and lifecycle partials.
- [x] `WorkTypePaletteView` split into core, tooltip and passion partials.
- [ ] Runtime smoke test after the refactor: settings, pins, weights, split/merge, save/load.

## Future scope

See `POLICY_DATA_DESIGN.md` for the relational policy model.

- [ ] Define `PawnPolicy` and sparse override contract before policy UI.
- [x] Zone policy: assign existing `Area_Allowed` per pawn and map; no zone creation.
- [x] Zone section in pawn detail: map selector, colored area selector, vanilla area manager link.
- [ ] Zone alert: semi-persistent alert when the assigned area is missing on the current map.
- [ ] Zone bulk apply to selected pawns.
- [ ] Zone UI: bulk/emergency assignment; no block-WorkType warning.
- [x] Schedule presets: named 24-slot `TimeAssignmentDef` presets in `Config/OrderedWork/schedules/<id>.json` (one file per preset).
- [x] Detail Horario section: Origen read-only, Modif editable, compact assignment brush, preset save/apply.
- [ ] Priority-by-hour row editing (storage exists).
- [ ] Apply-on-load policy.
- [ ] Full scheduler polish and selected-pawn apply.
- [x] Schedule apply to the current pawn from the detail section.
- [ ] Clothing policy: clothing by WorkType; per-pawn clothing override in pawn detail.
- [x] Pawn detail UI foundation: Overview, Horario, Zona, Overrides and Ropa.
- [x] Pawn detail override status: work, schedule, hourly-work and clothing overrides are visible read-only.
- [ ] Clothing assignment editing and automatic application from the detail panel.
- [ ] Per-WorkType hour windows: optional later layer, not in first schedule implementation.
- [ ] WorkGiverDef clones: rejected as a design path; do not add runtime Def clones.
- [ ] Compact per-save pawn policy format and migration.

## Non-goals

- [ ] No full compatibility while replacing the vanilla Work tab.
- [ ] No runtime `WorkGiverDef` cloning.
- [ ] No zone creation; only existing allowed areas.
- [ ] No per-WorkType hour windows in the first schedule implementation.
- [ ] No parallel schedule/apparel/area system.
- [ ] No per-pawn per-giver numeric priorities in this milestone.
- [ ] No global shared core with Medical Hub/Security Suite yet.

- No full compatibility while replacing the vanilla Work tab.
- No runtime `WorkGiverDef` cloning. Schedules, apparel and areas are planned policy tabs, not runtime Def clones.
- No per-pawn per-giver numeric priorities in this milestone.
- No global shared core with Medical Hub/Security Suite yet.