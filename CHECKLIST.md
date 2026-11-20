# Ordered Work — Current Checklist (0.3.0-alpha.251)

Status: build 0/0; tests pass. Runtime checks remain for schedule focus and detail input.

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
- [x] `docs/reference/credits.md` documents provenance and licensing posture.
- [x] Project documentation follows the Diátaxis map in `docs/README.md`.

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

## Scroll unification (alpha.249)

- [x] Shared `UiScrollPane` introduced.
- [x] Local dialogs migrated.
- [x] WorkPanel Skills, Stats and Unable lists migrated.
- [x] Details Overview migrated.
- [x] Schedule and Zone keep an external horizontal track when compressed.
- [x] Native/custom exceptions documented.
- [x] Build 0/0 and tests pass.
- [ ] Runtime wheel/shift-wheel check.
## Focus preset error toast (alpha.248)

- [x] Invalid preset load uses a reject toast only.
- [x] No duplicate "preset loaded" success message.
- [x] Dialog closes without opening another panel.
- [x] Build 0/0 and tests pass.
- [ ] Confirm with BakTest runtime.
## Frame optimization (alpha.251)

- [x] Active profile cached with explicit invalidation.
- [x] WorkGiver order uses the cached profile.
- [x] WorkControl lanes cached by order revision.
- [x] Frame/tick boundaries documented.
- [ ] Filter and Overview snapshots.
- [ ] Runtime large-colony check.
## Dead code cleanup (alpha.246)

- [x] Seven unused localisation keys removed.
- [x] `Clothing.Worn` retained for the future clothing module.
- [x] Unused `ScheduleApplyController.Save` removed.
- [x] Legacy save fields retained pending the old-save migration test.
- [x] Build 0/0 and tests pass.
- [ ] Remove legacy fields after the old-save migration test.
## Store fallback and migration tests (alpha.245)

- [x] Atomic store recovers from a corrupt main file through `.bak` and repairs the main file.\n- [x] Save does not overwrite a valid backup with an invalid main file.
- [x] Roundtrip, backup and schema tests pass.
- [ ] Runtime profile pending/promotion check.
- [ ] Runtime legacy focus migration check on an old save.
## ProfilesPanel component (alpha.244)

- [x] Profile list/save/pending UI moved into a component.
- [x] Local name/initialization state owned by the panel.
- [x] Old profile partial removed.
- [x] Build 0/0 and tests pass.
- [ ] Runtime check of select/save/write-pending.
## Log categories (alpha.243)

- [x] Diagnostic categories added.
- [x] Legacy verbose setting migrates to All.
- [x] Diagnostics UI has one checkbox per category.
- [x] Work Control, Schedule, Passion and Config calls tagged.
- [x] Build 0/0 and tests pass.
- [ ] Runtime check with one category at a time.
## Schedule editor split (alpha.242)

- [x] State, brush, apply controller, view and visuals separated.
- [x] Schedule tab reduced to wiring.
- [x] Paint, erase, load, save and apply preserve their behavior.
- [x] Build 0/0 and tests pass.
- [ ] Runtime check of paint, erase, load, save and apply.
## WorkControl extraction (alpha.241)

- [x] Metrics, row renderer, grid and interaction separated.
- [x] Main tab reduced to orchestration and top controls.
- [x] Old row/interaction partials removed.
- [x] Build 0/0 and tests pass.
- [ ] Runtime check of selection, palette drag and card reorder.
## PassionSource split (alpha.239)

- [x] Registry, metadata and icon resolver separated.
- [x] `PassionSource` retained as compatibility facade.
- [x] Existing UI and policy callers unchanged.
- [x] Build 0/0 and tests pass.
- [ ] Runtime check with and without VSE/Alpha Skills.
## UiDragList external scroll (alpha.238)

- [x] `UiDragList` uses the shared external track.
- [x] Track is drawn outside the content viewport.
- [x] Per-list owner prevents cross-list drag interference.
- [x] Existing vertical callers pass an owner.
- [x] Build 0/0 and tests pass.
- [ ] Runtime check with two visible lists.
## WorkConfigStore split (alpha.237)

- [x] Paths, serializer, profile service and resolver isolated.
- [x] `WorkConfigStore` reduced to compatibility facade.
- [x] Profile schema, backups and active/pending fallback preserved.
- [x] Build 0/0 and tests pass.
- [ ] Runtime profile save/load and pending promotion check.
## JSON store unification (alpha.236)

- [x] Shared `AtomicJsonRecordStore<T>` in Core.
- [x] Schedule, focus and Work presets use the shared atomic store.
- [x] Per-store schema, normalization, id policy and sorting preserved.
- [x] Roundtrip, backup and schema tests added.
- [x] Build 0/0 and tests pass.
- [ ] Runtime save/load check in game.
## Config localization (alpha.235)

- [x] Config mod strings moved to `OrderedWork.Config.xml`.
- [x] English, Spanish and SpanishLatin each have 163 config keys.
- [x] General, Rules, Profiles, Order, SplitMetadata and SplitMerge UI use keyed text.
- [x] Build 0/0 and tests pass.
- [ ] Verify the Config mod in game at both UI scales.
## Schedule focus (alpha.224)

- [x] `HourFocusStack` pure policy and tests.
- [x] Sparse `hourFocusOverrides` with max three and legacy migration.
- [x] Hour-boundary runtime overlay; no stop-job and no per-tick work.
- [x] Inline focus editor: visible horizontal order, three assignable rows, metadata, right-click removal, copy/paste hour.
- [x] Separate atomic `WorkFocusPresetStore` under `Config/OrderedWork/focus`.
- [x] English, Spanish and SpanishLatin focus key files.
- [ ] Runtime smoke test in RimWorld.
## Future scope

See `docs/reference/policy-data.md` for the relational policy model.

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