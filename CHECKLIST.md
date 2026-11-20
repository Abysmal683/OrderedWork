# Ordered Work — Current Checklist (0.3.0-alpha.276)

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
## Split/Merge safety (alpha.256)

- [x] Save Split/Merge actions are wrapped in a try/catch.
- [x] Failure logs and shows a reject message.
- [x] User is told to save and restart when apply fails.
- [x] Build 0/0 and tests pass.
- [x] SplitMergePanel component extraction completed.
## SplitMergePanel state (alpha.257)

- [x] Split/merge drafts, scrolls, drag state and metadata moved to `SplitMergePanel`.
- [x] Old state fields removed from `OrderedWorkMod`.
- [x] Split/Merge save actions guarded by `SafeSplitMergeAction`.
- [x] Move SplitMerge top-level editor into the panel.\n- [x] Move Split/Merge editors and giver column into the panel.\n- [x] Move destination column and split/merge validation.\n- [x] Move persistence and metadata.\n- [x] Split tab lists existing splits with edit/delete/metadata/revert draft.
- [ ] Runtime panel check.
## Config panel components (alpha.255)

- [x] `WorkOrderRulesPanel` owns its draft and baseline.
- [x] `OrderEditorPanel` owns its giver-order draft.
- [x] Existing drag helpers remain shared temporarily.
- [x] Build 0/0 and tests pass.
- [x] `SplitMergePanel` extraction.
- [ ] Runtime panel checks.
## Active work indicator (alpha.254)

- [x] Current WorkType service with a 5-tick cache.
- [x] Detail Work row highlight.
- [x] WorkControl card highlight and tooltip.
- [x] No global palette marker.
- [x] Build 0/0 and tests pass.
- [ ] Runtime check for sleep/food/joy/null work givers.
## Frame optimization (alpha.251)

- [x] Active profile cached with explicit invalidation.
- [x] WorkGiver order uses the cached profile.
- [x] WorkControl lanes cached by order revision.
- [x] Frame/tick boundaries documented.
- [x] Filter and Overview snapshots.
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
## Hidden WorkTypes (alpha.276)

- [x] `Hidden` flag and Config-only add/right-click removal.
- [x] Hidden entries remain in config order and Split/Merge/Order.
- [x] Fixed priority overlay and hidden-first WorkGiver cache.
- [x] Skip incapable pawns; refresh on `Notify_DisabledWorkTypesChanged`.
- [ ] Runtime check with a hidden native WorkType.
- [ ] Runtime check with a hidden mod WorkType and a pawn gaining/losing incapability.
## Work tab private columns (alpha.275)

- [x] Patch `PawnTable.RecacheColumns` to reorder the private `columns` copy.
- [x] Document verified `SetPriority`/`GetPriority`, disabled-type and `workGiversDirty` behavior.
- [ ] Runtime check with Work tab opened before profile apply.
- [ ] Runtime check with Compact Work Tab enabled.
## Work tab draw-time order (alpha.274)

- [x] Reapply `PawnTableDefOf.Work.columns` order in the Work tab draw prefix.
- [x] Preserve the WorkTab `allColumns` cache refresh.
- [ ] Runtime check with the Work tab open before and after profile changes.
## Work tab column reorder (alpha.273)

- [x] Reorder `PawnTableDefOf.Work.columns` work-column slots from the active profile.
- [x] Preserve non-work column positions.
- [x] Refresh WorkTab `allColumns` cache when present.
- [x] Reapply column order at Work tab draw time.
- [ ] Runtime check that the vanilla Work tab follows the profile after restart.
- [ ] Runtime check with WorkTab mod and without it.
## Work tab order and passion layout (alpha.272)

- [x] Work tab order first attempt (superseded by .273 column reorder).
- [x] Passion weights render all rows without internal cap or scrollbar.
- [x] Rules sections default collapsed and remember expanded state.
- [x] Hidden/always-active WorkType design note saved.
- [ ] Runtime check of Work tab order after restart.
- [ ] Runtime check of full passion table scrolling in the outer settings.
## Profile reconciliation and mod WorkTypes (alpha.271)

- [x] Missing visible live WorkTypes and Givers are appended to the profile on load.
- [x] Hidden WorkTypes are not added; stale visible flags are cleared on reconciliation.
- [x] Split delete and Merge revert restore origin visibility when Givers return.
- [x] Passion tooltip handles local and screen mouse coordinates.
- [x] Project health audit saved under `docs/explanation/project-health-audit.md`.
- [ ] Runtime check with Hospitality enabled and Order selecting `Diplomat`.
- [ ] Runtime check that a hidden mod WorkType stays hidden.
- [ ] Runtime check of passion tooltips after profile reconciliation.
## Passion tooltip coordinate fix (alpha.270)

- [x] Hover test runs inside the clipped group.
- [x] Tooltip rect is read from the pointer after `GUI.EndGroup`.
- [x] Passion tooltip registration matches `TooltipHandler` coordinates.
- [x] Runtime confirmation of passion tooltips.
## Tooltip and button polish (alpha.269)

- [x] Passion tooltip hit test uses the hovered row and pointer-sized tooltip rect.
- [x] Split/Merge action buttons expose tooltips.
- [x] Disabled action labels use native centered text.
- [x] User documentation updated for the profile-backed editor, Action word, Revert and 10-row caps.
- [x] Runtime check of passion and Split/Merge tooltips.
## Controls polish (alpha.267)

- [x] Merge/Order rows support Revert with confirmation.
- [x] Merge Revert returns extra Givers to their reference owner.
- [x] Order Revert restores the reference Giver sequence.
- [x] Candidate lists and Order editor columns show 10 rows with scrollbar and Ctrl+wheel.
- [x] Ctrl+wheel direction fixed; passion weights support it.
- [x] Split Action word field added to create and metadata editors.
- [x] Passion-weight tooltips no longer render below the table.
- [x] Runtime check of Merge/Order Revert and split Action word.
## UI consolidation (alpha.266)

- [x] Block disabling `Allow empty origin` when any WorkType has no Givers.
- [x] Merge excludes split WorkTypes from its candidate list.
- [x] Split/Merge/Order results moved below warnings/actions.
- [x] Removed Revert Split/Merge/Order buttons; Cancel remains.
- [x] Removed Order's old reference-reset block.
- [x] Added Order warning text.
- [x] Fixed Spanish split delete fallback text.
- [x] Runtime check of the three tabs.
## Order candidates (alpha.265)

- [x] Order tab lists WorkTypes whose Giver order differs from `reference.json`.
- [x] Selecting a row loads the existing Order editor.
- [x] Revert Order clears the Order selection/draft (superseded by alpha.267 candidate-row Revert).
- [x] Build 0/0 and tests pass.
- [x] Runtime check of order selection/revert.
## Merge candidates (alpha.264)

- [x] Merge tab lists WorkTypes with missing/extra Givers against `reference.json`.
- [x] Selecting a row loads the existing Merge editor.
- [x] Revert Merge clears the merge draft/selection (superseded by alpha.267 candidate-row Revert).
- [x] Build 0/0 and tests pass.
- [x] Runtime check of merge selection and revert.
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