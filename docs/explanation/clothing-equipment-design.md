# Clothing / Equipment — canonical v1 design

Status: source of truth for implementation.
Detailed appendices: `clothing-editor-layout.md`, `clothing-reequip-priority.md`, `clothing-metadata.md`, `pawn-equipment-tab.md`, `clothing-design-draft.md` (historical).

## 1. Scope

V1 includes:

- dynamic apparel cell catalog;
- neutral clothing editor;
- pawn Equipment tab;
- Main/Primary, Mandatory, Emergency and WorkType sets;
- multiple complementary sets in Mandatory/Emergency/Principal;
- inventory and primary equipment;
- per-pawn overrides;
- metadata summary/detail;
- vanilla reservations and vanilla apparel scoring.

Deferred:

- advanced material/quality editing;
- source-icon art;
- multi-select comparison;
- per-entry inventory quality/material;
- advanced layout presets/editor;
- custom cross-pawn reservation ledger;
- full inventory management.

## 2. Core model

```text
SlotKey = BodyDef + BodyPartGroupDef + ApparelLayerDef
Footprint = all SlotKeys for a ThingDef
```

- A slot is one unique cell.
- A piece may cover many cells.
- Alternatives are stored per cell.
- Adding a piece in one cell does not clone it into every covered cell.
- Only the active alternative claims coverage.
- Tabs, rows, families and order are view-only; `SlotKey` is the identity.
- Selection/occupancy/candidates are always keyed by `SlotKey`, never row/column index.

The catalog is built dynamically from apparel defs. No hardcoded vanilla slot list.

## 3. Sets and priority

```text
Forced/Locked item > Mandatory > Emergency > WorkType > Principal/Regular
```

- Forced/Locked is a per-cell player override, not a set.
- Mandatory, Emergency and Principal can contain several complementary sets.
- WorkType has one set per work.
- Principal is the casual/default fallback.
- Emergency falls back to Principal if it has no own set.
- WorkType can replace Principal, but not Mandatory or Emergency.
- Sets within a category fill gaps in order; later sets do not cancel earlier ones.
- Mandatory checks that all its requested cells were filled; alert if not.
- Forced can beat Mandatory; alert if it may be forgotten.
- Inventory is a union across active sets.
- Primary equipment: one; later equipment goes to inventory for Sidearms-style secondary use.
- Items not explained by any set are `Unmanaged`.

## 4. Resolution

Simple bounded resolution:

1. Apply forced/locked cells.
2. Apply Mandatory sets in order; fill gaps.
3. Check mandatory coverage.
4. Apply Emergency sets in order; fallback to Principal if none.
5. Apply the active WorkType set to Principal-level cells.
6. Apply Principal sets in order; fill remaining gaps.
7. Union inventory counts.
8. Fill primary equipment if free; otherwise add to inventory.

Rules:

- No NP-hard solver.
- No unbounded backtracking.
- No per-tick re-equip.
- One plan, then diff against worn.
- Recompute only on work change, load, inventory/policy change, manual action or invalidation.
- Locked/forced always win.
- Missing item: do nothing/retry later; no search loop.
- Vanilla `ReservationManager` only; no custom TTL or pre-reservation.
- Coverage repair: treat pawn as empty, resolve plan, preserve locked/forced/policy, fill uncovered cells, one pass.

## 5. Editor UI

- Dynamic cells/onion.
- Two modes: General (all equippable) and Cell (exact cell).
- Two columns: Added (priority) / Available.
- Click left = info/select; right click or drag = equip/unequip.
- One `equip` label; highlight is the only selection indicator.
- Hover outlines the full footprint; 50% icon only in empty cells.
- No visible link rows.
- No primary/anchor slot.
- FullHead and UpperHead remain separate.
- Layout config affects editor and pawn Equipment tab.
- V1 layout customization: auto tabs/rows + rename/reorder; advanced family/preset editor deferred.
- Tab rail has overflow; outer layout never grows.

## 6. Metadata

- 40 Summary: collapsible body/global/equipment/inventory tree.
- One row per contributing item with its icon.
- Armor is outer-to-inner, per body group, never summed.
- General stat order: vanilla category/display priority, not contribution size.
- Non-additive stats show per item or `—`.
- Material/quality: actual values in pawn view; defaults in neutral editor.
- 60 Detail: selected item.
- Shared 30/30 comparison for two items; union of stats, common rows highlighted, no winner.
- No two info-card windows; use `StatsReportUtility`.
- `Info card` opens `Dialog_InfoCard`.
- Material/quality pickers deferred.

## 7. Pawn Equipment tab

```text
Header: portrait | weight/capacity | carrying weight | active source
Current Gear (left): Apparel (cells) -> Primary -> Inventory
Sets (right): Mandatory -> Emergency -> WorkType -> Principal
Metadata (bottom): 40 summary / 60 detail
```

- Apparel cells show one source badge: `F/M/E/W/P/U`.
- Tooltip shows all sources.
- Inventory rows show `actual / expected`.
  - `0` = temporary/unmanaged.
  - `1+` = minimum to keep.
  - negative/forbidden = do not keep.
  - absolute per-pawn override.
- Carrying item appears in inventory with `Carrying` tag, but its mass is separate.
- Sets: click highlights only; `Open editor` opens highlighted set or the most recently edited/opened set; drag reorders/moves between Mandatory/Emergency/Principal only; right click removes target from group; delete only in editor and clears all references; `+ Add` under group.
- Badge: one source badge per cell; hover always shows tooltip; click selects the item/cell for metadata.
- Equip now runs set priority; never changes forced cells; no set = vanilla apparel search; compact multi-pawn gizmo.
- Overrides: absolute, `Revert` + `Clear all`.
- New name: `Equipment`, replacing `Clothing`.

## 8. Performance

- Cache catalog/footprints; no rebuild per pawn.
- Map-level stock lookup via `ListerThings`/`ThingOwner`.
- Dirty pawn queue; bounded plans per tick.
- Cache plans; invalidate on relevant changes.
- Reuse collections; no LINQ in hot loops.
- Use vanilla `ReservationManager`; no custom ledger.
- No custom quality/material optimizer; use vanilla apparel scoring.
- No per-tick scans.
- 50-pawn target: no all-at-once planning, no hitches on activation.

## 9. Overengineering cuts

- No custom armor aggregation formula.
- No custom quality-first rule.
- No custom reservation/TTL system.
- No full inventory management.
- No per-entry inventory quality/material.
- No comparison cards.
- No two info-card windows.
- No deep layout editor in v1.
- No custom source-icon art in v1.
- No multi-select info in v1.
- No separate WorkType editor panel.
- No visible link rows or primary/anchor.

## 10. Implementation order

1. SlotKey/catalog/footprint + tests.
2. Alternative/set model.
3. Resolver and overrides.
4. Editor UI.
5. Pawn Equipment tab.
6. Metadata.
7. WorkType palette integration.
8. Localization keys: prefix `OrderedWork.Equipment.*`; group names are fixed in `clothing-metadata.md`.

Then refactor: responsibilities, namespaces, file layout, language/style, allocation tuning.

## 11. Open decisions

- Badge icon source.
- Translation text for the decided keys.
- Exact right-click confirmation wording when removing a referenced set.
