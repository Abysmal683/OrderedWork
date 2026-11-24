# Clothing / Equipment — v1 implementation batches

Branch: `feat/clothing-stack` (current; create `feat/equipment-v1` when starting)
Source of truth: `docs/explanation/clothing-equipment-design.md`
Test runner: `scripts/verify.ps1` and `Tests/OrderedWork.Tests`
Baseline: build 0/0; 39 tests passing; no code changes yet.

## Specs

- S1: Implementation follows the canonical v1 spec; appendices are reference only.
- S2: Slots are stable `SlotKey` values; UI order never changes identity.
- S3: Catalog and footprints are derived dynamically from apparel defs.
- S4: Alternatives are stored per cell; active coverage is projected by footprint.
- S5: Sets use `Forced > Mandatory > Emergency > WorkType > Principal`; multiple sets are complementary within a category.
- S6: Equipment/Inventory/Carrying use the pawn Equipment tab; no body cells for equipment.
- S7: Overrides are absolute per-pawn values and can be reverted.
- S8: Editor and Equipment tab share the same cells, layers, metadata, and localization keys.
- S9: Reservations use vanilla `ReservationManager`; no custom ledger/TTL.
- S10: No advanced layout editor, material/quality pickers, icon art, comparison cards, or multi-select info in v1.
- S11: Existing `OrderedWork.Clothing.*` keys remain stable; new UI uses `OrderedWork.Equipment.*` where appropriate.
- S12: Every batch ends with tests/build evidence and a work-unit commit on the feature branch.

## Tasks

- T1: Localization guardrails. Done.
  - Fixed XML encoding declarations to UTF-8.
  - Added `scripts/verify-localization.ps1` for missing, duplicate, orphaned, empty, encoding and code-key checks; hardcoded UI strings are warnings.
  - Integrated it into `scripts/verify.ps1`.
  - Kept existing Clothing keys; documented the Equipment namespace.
  - Acceptance verified with a temporary missing-key fixture: exit 1 with `Missing 'Test.Key' in Spanish`.

- T2: Domain models. Done.
  - `ApparelSlotKey`, `ApparelFootprint`, `ApparelSlotCatalog`, `CellRule`, `CellAlternative`, `EquipmentSetRole`, `InventoryTarget`, `EquipmentOverrides`.
  - Pure/core tests for identity, footprint deduplication, catalog mapping, local priorities, set normalization and merging.
  - Result: 5 new core tests; 44 total passing; no UI/RimWorld dependency.

- T3: Resolver. Done.
  - Priority order, complementary sets, coverage, mandatory check, emergency fallback, equipment overflow, inventory union.
  - Forced/locked preservation and mandatory coverage reporting.
  - Pure tests for priority, WorkType replacement, multi-cell claims, inventory union, forced cells and fallback.
  - Coverage repair remains T9 execution work; no loop.

- T4: Persistence without legacy migration. Done.
  - New `EquipmentSetJsonStore` and `EquipmentSetStore`; schema v1.
  - Legacy `ClothingPresets` are intentionally ignored.
  - Round-trip and delete tests.
  - Result: 52 total tests passing; build 0 warnings.

- T5: View layout model. Done.
  - Auto tabs/rows from cells with injected tab/row selectors.
  - Basic rename/reorder only; no advanced layout editor.
  - Slot identity remains `ApparelSlotKey`; placement is view-only.
  - Acceptance: 1 layout test; reorder/rename leaves cell identity stable.

- T6: Editor UI. Done for the neutral editor.
  - Dynamic square cells, grouped tabs, rows, local alternatives, Added/Available columns, search, add/remove.
  - General/Cell behavior: no cell shows all allowed apparel; selected cell filters by exact cell.
  - Hover footprint preview with 50% icon only on empty cells.
  - Save/load panel with New/Delete/Load; Save As and dirty Save/Discard/Cancel guard.
  - Name editing only in New/Save As; header uses Load/Save/Save As.
  - UI gadgets added: `UiCard`, `UiGrid`, `UiSearchField`.
  - `equip` label added to added alternatives.
  - Pawn source badges remain for T8 Equipment tab.
  - Old `Dialog_ClothingEditor` and `Dialog_ThingDefPicker` removed; overview opens the new editor.

- T7: Metadata. In progress — second metadata slice implemented.
  - 40 body/layer and per-item global insulation summary.
  - 60 detail with compare mode, Pick, StatsReportUtility and Info card per pane.
  - Default material/quality shown.
  - Pending: equipment/inventory summary sections and actual pawn values (depends T8a), source icon overflow.

- T8a: Equipment assignment/overrides persistence. BLOCKING T8b and T7 pawn values.
  - Add pawn assignment profile: Mandatory/Emergency/Principal set ids, WorkType set id, forced cells, primary/inventory overrides.
  - Save/load normalization and tests.
  - No UI yet.
- T8b: Pawn Equipment tab.
  - Current Gear left (Apparel -> Primary -> Inventory with Carrying tag), Sets right, click-highlight and Open editor, drag/reorder/remove, Overrides sub-tab, metadata.
  - Acceptance: set identity survives reorder/move; delete clears references.
  - Depends on T8a.

- T9: Equip now and execution.
  - Active-set job, forced/locked preservation, vanilla fallback when no set, compact multi-pawn gizmo, interruptible behavior.
  - Acceptance: no forced/mandatory override; no per-tick loop.

- T10: WorkType palette integration.
  - Right-click drawn palette row; Assign/Create/View/Edit.
  - Acceptance: no separate panel; global WorkType set cleanly opened.

- T11: Integration and performance.
  - Cache catalog/footprints, dirty queue, bounded plans, `ListerThings`, vanilla reservations.
  - Acceptance: no all-pawn scan on activation; no per-tick re-equip.

- T12: Refactor phase.
  - Responsibilities, namespaces, file layout, language/style, allocation tuning.
  - Light ports/adapters (hexagonal) only where a real seam exists: catalog, stock, reservations, sources, persistence, localization.
  - No interface ceremony for every class.
  - Acceptance: behavior unchanged; full suite passes.

## Deferred

- Advanced layout presets/editor.
- Material/quality pickers and comparison cards.
- Custom badge icon art.
- Multi-select info comparison.
- Per-entry inventory quality/material.
- Custom reservation ledger/TTL.
- Custom quality-first optimizer.
- NP-hard solver/backtracking.

## Log

- L1: User asked to organize the implementation plan into batches after the design freeze.
- L2: Canonical v1 created; appendices marked; overengineering cuts recorded.
- L3: First batch will be localization guardrails, then domain models and resolver before UI.

- L4: T1 complete: build 0/0, 39 tests pass; localization guardrail 0 errors / 68 hardcoded-UI warnings; missing-key fixture correctly fails.

- L5: T2 complete: pure equipment domain models added with 5 tests; 44 total passing; duplicate cell rules merge alternatives by local priority.

- L6: T3 complete: pure equipment resolver added; 7 resolver tests; 51 total passing; build 0 warnings.

- L7: T4 complete: new EquipmentSets JSON store; legacy ClothingPresets intentionally ignored; alpha data-loss accepted; 52 tests; build 0 warnings.

- L8: T5 complete: pure view layout with tabs/rows, rename/reorder; 53 tests; build 0 warnings.

- L9: T6 first visual slice: new Dialog_EquipmentEditor with dynamic cells/tabs/rows, alternatives, search, save; old clothing editor/picker removed; localization warnings reduced 24→13; visual test pending.

- L10: T6 second slice: square cells, grouped tabs, save/load manager, Save As, dirty guard, UI gadgets (`UiCard`, `UiGrid`, `UiSearchField`); Diátaxis docs added.

- L11: T6 neutral editor complete: General/Cell behavior, hover footprint and 50% empty preview, equip label, save/load contract, UI gadgets. Pawn source badges deferred to T8.

- L12: T7 first slice: reusable EquipmentMetadataPanel mounted below candidates; 40 body/layer summary, 60 detail, default material/quality, StatsReportUtility, Info card; comparison/global sections pending.

- L13: T7 second slice: global insulation rows, 30/30 compare mode with Pick and per-pane Info card; 53 tests; build 0 warnings.

- L14: Priority correction: assignment/overrides persistence must precede Equipment-tab UI; removed static metadata Thing cache; metadata panel guarded with Log.ErrorOnce fallback; nullable suppressions removed from new UI.
