# Clothing re-equip and priority — design draft

Status: detailed appendix, no implementation yet.
Canonical v1: `docs/explanation/clothing-equipment-design.md`.
Related: `docs/explanation/clothing-editor-layout.md`.

## 1. Terms

- **Cell**: a unique `SlotKey` = body + body part group + apparel layer.
- **Alternative**: one apparel definition that can fill a cell.
- **Footprint**: all cells an apparel occupies.
- **Cell order**: the global order in which cells are considered for equipping. It is visible and editable through the cell layout.
- **Local priority**: the numbered order of alternatives inside one cell. `1` is first.
- **Plan**: the final set of apparel selected for a pawn/preset after resolving all claims.
- **Claim**: one `(cell, alternative, local priority)` tuple. One multi-cell item can have several claims.

There is no global item priority in v1. The global ordering is **cell order**; local priority is used inside each cell.

## 2. Priority model

### Cell order

Cells are ordered by their visible layout order by default. The player can reorder cells/tabs/rows, and that order becomes the equip order.

Recommended default semantic order when no custom order exists:

```text
Torso
Head
Legs
Arms / Hands
Waist
Feet
Accessories
Other
```

The visible order is authoritative. There is no hidden priority number per cell beyond the order the player sees.

### Local priority

Each cell has its own ordered list:

```text
Torso|Shell
  1. Robe
  2. Duster
  3. Royal siege armor
```

- Smaller number = tried first.
- Top = highest priority.
- New alternatives append to the bottom.
- The list is local to the cell. It does not create a global item rank.

## 3. V1 resolver

V1 is a deterministic, bounded, claim-based resolver.

### Build claims

For every cell, in cell order, for every alternative, in local priority:

```text
claim = {
  cell,
  alternative,
  cellOrder,
  localPriority,
  footprint = Footprint(alternative.def)
}
```

### Select

```text
selected = empty
coveredCells = empty
failed = empty

for each claim ordered by (cellOrder, localPriority, defName):
    if claim.cell is covered:
        continue

    if claim.alternative is not available:
        failed.add(claim)
        continue

    if claim.alternative conflicts with selected:
        failed.add(claim)
        continue

    selected.add(claim.alternative)
    coveredCells.addAll(claim.footprint)
```

Consequences for **base pawn sets**:

- The earliest cell in the visible order wins a conflict.
- An item selected for one cell fills all cells in its footprint.
- Later cells see those cells as covered and do not select another item for them.
- A later cell can only win by being moved earlier in the layout.
- If the first alternative is unavailable or conflicts, the next local alternative is tried.

The active WorkType set may replace Principal/Regular cells when available, but not Mandatory or Emergency cells; see section 4.1.

### Repair pass

After the first pass, run one bounded repair pass for uncovered cells:

- Try the remaining alternatives.
- Never remove an already selected base item.
- Never revisit a cell that became covered.
- Stop when no progress is made.

The WorkType overlay is resolved separately and is not part of this repair pass.

This is not a general backtracking solver. It is one forward pass plus one repair pass.

## 4. No-loop rules

### Plan once, then execute

1. Build the plan from the current preset and availability.
2. Compare plan with worn apparel.
3. Remove only items that must be replaced.
4. Add missing items.
5. Do not rebuild the plan during an active equip job.

Recompute only on:

- WorkType change.
- Load.
- Inventory/stock change.
- Manual apply.
- A plan invalidated by an unavailable item, locked apparel, or a body/policy change.

### No per-tick re-equip

- No re-evaluation every tick.
- No repeated attempts for the same unavailable item in the same cycle.
- Failed items get a cooldown or an `attempted` mark until relevant state changes.

### Deterministic ties

Tie-break with:

1. `cellOrder`
2. `localPriority`
3. `defName`

Never use random selection.

### Locked and forced apparel

- Do not force a replacement over locked apparel.
- Record `blocked_by_locked`.
- Try the next alternative.
- Do not loop trying to remove a locked item.

### Duplicate pieces

- Select one `ThingDef` alternative once.
- If it appears in several cells, it is one selection with one footprint.
- Do not plan the same piece for multiple independent cells.

### Failure states

Each uncovered cell records one reason:

```text
no_alternative
unavailable
policy_denied
body_incompatible
conflict
locked
unreachable
```

The UI must show the reason; the resolver must not silently retry forever.

## 4.1 Multiple active sets and priority

Priority:

```text
Forced/Locked item > Mandatory > Emergency > WorkType > Principal/Regular
```

- Forced/Locked is a per-cell player override, not a set.
- Mandatory, Emergency and Principal can each contain several complementary sets.
- WorkType has one set per work.
- Principal is the casual/default fallback.

Resolution:

```text
apply forced/locked cells
apply mandatory sets in order, filling gaps
check that every mandatory-requested cell is filled
apply emergency sets in order, or Principal if Emergency has none
apply the active WorkType set to non-mandatory/non-emergency cells
apply Principal sets in order, filling remaining gaps
```

Rules:

- Sets within a category complement; later sets fill gaps.
- Mandatory notifications are checked after all mandatory sets.
- Emergency falls back to Principal when it has no own set.
- WorkType can replace Principal cells when its item is available, but not Mandatory or Emergency.
- Forced/locked cells are never overridden.
- If an item is unavailable, do nothing; no loop.
- Inventory is a union across all active categories.
- Primary equipment: the highest-priority category with a valid item wins; later equipment goes to inventory for Sidearms-style secondary use.
- If it cannot be stored, report `cannot_store_secondary`.
- Do not use inventory occupancy to filter clothing cells.
- Weight includes the union of inventory counts.
- On reservation failure, use vanilla behavior: try the target if it exists and is available; otherwise nothing happens until a later trigger. No search loop.

## 4.2 Removal and coverage repair

When a garment is removed, repair coverage simply:

1. Treat the pawn as if it had no clothing for this calculation.
2. Resolve the active desired plan from the preset, respecting `ApparelPolicy`.
3. Preserve locked/forced clothing and policy-protected items.
4. Compare the empty-state plan with what remains after removal.
5. Fill the cells that would be left uncovered and whose item is available.
6. Change/replace non-work clothing only when it blocks a desired cell.
7. If no available item fills a cell, leave it empty and report `uncovered`.

Rules:

- One pass only; no loop.
- Locked/forced and policy are the only exceptions.
- Do not search aggressively if the item is unavailable.
- The plan is authoritative over loose non-work items when they conflict.
- This is a repair of the active plan, not a full inventory rebuild.

### Current area

Automatic restoration/search is limited to the current workbench area:

- dynamic per workbench;
- default radius: 30 cells;
- configurable per workbench or globally;
- if the pawn is far outside, do not chase the clothing;
- a direct player order can still trigger the action manually.

The default is a starting value, not a hardcoded rule.

## 5. WorkControl search

The planner does not define what is wearable. It follows the item data and the cell:

1. the item's `ApparelProperties.bodyPartGroups` and `layers` contain the cell's group/layer;
2. the item is allowed by `ApparelPolicy` when the policy is relevant;
3. the pawn has the required body parts (`HasPartsToWear`);
4. the item is compatible with already selected apparel (`CanWearTogether`, forced/locked rules);
5. no locked apparel blocks replacement;
6. the item's footprint does not intersect an already occupied cell.

Availability is not a planning filter. The planner says what should be worn; execution finds an actual item. If the item is missing, report `missing` (future craft/request flow) rather than silently changing the preset.

If no alternative passes coverage/compatibility, the cell stays unfilled and reports the reason. It does not fall back to an unrelated item outside the cell alternatives.

A mod-added cape, hood, or layer works automatically if its `ThingDef` declares the appropriate body groups and layers; the catalog is dynamic and the cell model has no hardcoded wearability list.

## 6. UI: rows, not icon columns

The added/available panels are vertical rows. Each row contains:

```text
[priority] [item icon] [label] [coverage badge] [status badge]
```

- Priority is local; `1` is first.
- Top = highest.
- Added alternatives append to the bottom.
- Left panel is always `Added`; right panel is always `Available`.
- In General mode, the left panel shows the planned equip order for the main garments.
- In Cell mode, the left panel shows the alternatives for that cell.
- The right panel in General shows all apparel in the game.
- The right panel in Cell shows all apparel that can fill that cell, regardless of stock/ownership; unavailable items are marked, not hidden.
- Hovering a row outlines its footprint; empty target cells show a 50% icon.

Rows do **not** show quality or material. This is a neutral environment. Quality, material, stats, and special stats belong to the separate metadata space, not to the priority row.

The info button (`i`) is removed from the row. The item icon remains as the visual identifier.

### Mouse behavior

- Left click on an available row: select the row for info/preview; do not equip.
- Right click on an available row: equip it into the selected cell.
- Left click on an equipped row: select the row for info/preview.
- Right click on an equipped row: unequip it from the selected cell.
- Drag available to equipped: equip.
- Drag equipped out: unequip.
- The word `equip` is the only persistent status label.
- There is no `select` text label. The touched row gets a persistent highlight/color.
- Only one row is highlighted for info at a time. Multi-select comparison is deferred.
- A piece that occupies another cell is shown spatially through grid occupancy and footprint highlight; it is not shown as a `linked` row.

### Metadata behavior

- Hover a row: transient metadata preview.
- Left click a row: keep the metadata preview visible.
- If nothing is pinned, hover updates the metadata panel.
- If something is pinned, hover does not replace it.
- A pin button inside the metadata space pins/unpins the current item.
- `Esc` releases the pin.
- Right click on a row keeps its equip/unequip behavior; it does not release metadata.
- The exact pin-button placement and visual state belong to the metadata design pass.

### Coverage, not visible links

Each cell keeps its own local alternative list and order. A piece can cover several cells without being added to every covered cell's local list.

The coverage relation is internal. The UI does **not** show a `link` row or a `linked` label. Coverage is shown spatially:

- the grid cell becomes occupied or planned;
- the footprint is outlined when the item is hovered/selected;
- an empty target cell shows the 50% icon during preview.

There is no cell or piece “primary”. Evaluation is per occupied cell. A piece simply claims all cells in its footprint when selected.

- An active alternative with footprint A+B occupies A and B.
- An alternative with footprint A+C occupies A and C when active.
- Inactive alternatives do not occupy anything.
- A cell can be occupied by a piece selected in another cell; it does not show a link label.
- A cell can have no local alternative and still be occupied by a piece selected elsewhere.
- Local lists keep their own order; coverage never reorders them.
- Unequipping a piece removes it atomically from every cell it occupies; never a partial piece.
- Losing coverage can uncover a cell; a bounded repair pass resolves it if that cell has a local alternative; otherwise the cell reports `uncovered`.
- Changing the active alternative recomputes coverage once: the old footprint is released and the new footprint is claimed.

### Example: cells 1–4

```text
S1 = { cell 1 }
S2 = { cell 1, cell 2 }
S3 = { cell 3, cell 4 }
S4 = { cell 2 }
```

Local lists:

```text
Cell 1: 1. S1, 2. S2
Cell 2: 1. S4
Cell 3: 1. S3
Cell 4: (empty)
```

Cell order `1 → 2 → 3 → 4`:

```text
Cell 1: S1 selected, occupies cell 1. S2 is skipped.
Cell 2: S4 selected, occupies cell 2.
Cell 3: S3 selected, occupies cells 3 and 4.
Cell 4: occupied by S3; no local alternative is required.
Plan: S1, S4, S3
```

If Cell 1 prioritizes S2:

```text
Cell 1: S2 selected, occupies cells 1 and 2. S1 is skipped.
Cell 2: S4 cannot be selected because cell 2 is occupied; no link label is shown.
Cell 3: S3 selected, occupies cells 3 and 4.
Cell 4: occupied by S3.
Plan: S2, S3
```

If cell order is `2 → 1 → 3 → 4`:

```text
Cell 2: S4 selected, occupies cell 2.
Cell 1: S2 is blocked because cell 2 is already occupied; S1 is selected instead.
Plan: S4, S1, S3
```

That is the deterministic conflict rule. To make S2 win, move Cell 1 before Cell 2 or remove S4. The UI should show `S2 blocked: cell 2 already occupied` or simply leave the cell visually occupied; it should not show a link.

### Your 1–2–3 example

```text
Cell 1: A
Cell 2: BC
Cell 3: AB
Global order: Cell 1 → Cell 2 → Cell 3
```

If `BC` occupies Cell 2 and Cell 3, and `AB` occupies Cell 1 and Cell 2:

```text
Cell 1: A is equipped first; Cell 1 is occupied.
Cell 2: BC is equipped next; Cell 2 and Cell 3 are occupied.
Cell 3: AB cannot be equipped because Cell 1 and Cell 2 are already occupied.
       Skip AB and try the next alternative in Cell 3.
```

The `AB` row can still exist in Cell 3. It simply cannot be selected while its footprint is already occupied. Hovering it outlines its intended cells; the grid shows why it cannot take them. No link row is needed.

## 7. Info panel research

Use RimWorld's existing info card instead of reimplementing stats.

- `Verse.Dialog_InfoCard`
  - `new Dialog_InfoCard(Thing)` shows an item with its actual stuff, quality, hit points, and stats.
  - `new Dialog_InfoCard(ThingDef, ThingDef, Precept_ThingStyle)` shows a definition/material variant.
  - `SetTab(Dialog_InfoCard.InfoCardTab.Stats)` opens the Stats tab.
  - `ShowMaterialsButton` is the existing material-variant control.
- `RimWorld.StatsReportUtility`
  - `DrawStatsReport(Rect, Thing)` draws the full stats report inline.
  - `StatsToDraw(Thing)` returns the stat entries for a custom compact view.
- Material:
  - `Thing.Stuff` is the actual material.
  - `ThingDef.MadeFromStuff` tells whether the def can have a material.
  - `GenStuff.AllowedStuffsFor(BuildableDef, TechLevel, ...)` lists allowed materials.
- Quality:
  - `QualityUtility.TryGetQuality(Thing, out QualityCategory)` reads an item's quality.
  - `CompQuality` exposes and sets quality for a synthetic item.
- Special stats:
  - The Stats tab from `Dialog_InfoCard` / `StatsReportUtility` already includes special stats and explanations.

Implementation consequence:

- A `ThingDef` row does not have a fixed material or quality. Material and quality are item-instance/build properties.
- The metadata space can show a selected material/quality variant, or create a synthetic `Thing` with the chosen stuff and quality.
- Do not reimplement the stat list; use `StatsReportUtility` and `Dialog_InfoCard`.
- Ordered Work does not control material/quality constraints; vanilla handles those.

## 8. Decisions made

- Local priority per cell; top = highest; `1` is first.
- Global order = visible cell order, not a hidden global item rank.
- A multi-cell item claims all cells in its footprint.
- First cell in order wins a conflict.
- Added alternatives append to the bottom.
- Left = added; right = available; top/left always.
- All apparel is shown; availability does not hide rows.
- The planner does not filter by stock/availability; it declares the desired outfit.
- Missing items are an execution concern (`missing`), not a preset/UI filter.
- Wearability comes from the item's declared body groups/layers plus the cell; no hardcoded allow-list.
- Cell mode lists only apparel whose data can fill that exact cell.
- A cell is body-part group + layer.
- Rows show priority, item icon, label, coverage, and `equip` only.
- Highlight/color is the only selection indicator; no `select` text label.
- Quality, material, stats, and special stats live in the metadata space, not in rows.
- The row info icon is removed.
- Left click selects for info; right click equips/unequips; drag equips/unequips.
- Left click available does not equip.
- A covered cell may have no local alternative; it is simply occupied in the grid.
- Coverage belongs to the active alternative, not to the cell as a whole.
- Equipping/unequipping is atomic across every covered cell.
- Local lists keep their own order when an item covers another cell.
- No `link` row or `linked` label is displayed.
- Coverage is spatial: occupied cell, 50% ghost on empty preview, footprint outline.
- Info details hover when not pinned; left click keeps; pin button/Esc releases.
- Priority: Forced/Locked > Mandatory > Emergency > WorkType > Principal/Regular.
- Mandatory, Emergency and Principal can each contain several complementary sets.
- WorkType is one set and may replace Principal, but not Mandatory/Emergency.
- Previous clothing is restored only if it is still inside the current area.
- Missing work clothing: equip what is available; alert only when the config checkbox is enabled.
- Coverage repair is one bounded automatic pass; no loop.
- Kept-current is shown only in detail/tooltip, not as a noisy badge.
- No per-tick re-equip and no unbounded retry.

## 9. Open questions

1. **Current area definition**: exact workbench/room/distance rule for automatic restore/search.
2. **Pawn sets management UI**: deferred to a later design pass.
3. **Performance coordinator**: planning queue/budget details for 50 pawns; analyze vanilla and the ammunition mod before implementing.

## 10. Limits

- No NP-hard outfit solver in v1.
- No unbounded backtracking.
- No per-tick re-equip.
- No global item priority number.
- No automatic material/quality optimization in v1.
- No custom cross-pawn reservation model; use vanilla `ReservationManager`.
- No forced removal of locked apparel.

## 11. Performance and contention

Use vanilla mechanisms where they already exist.

### Reuse

- `ApparelPolicy` is the authority for allowed apparel.
- `ApparelUtility.CanWearTogether`, `HasPartsToWear`, and `Pawn_ApparelTracker` handle conflict/body/locked checks.
- `OutfitForcedHandler` and `Pawn_ApparelTracker` handle forced/locked apparel.
- `IApparelSource` / `Building_OutfitStand` handle outfit-stand sources.
- `ListerThings` / `ThingOwner` provide map stock without per-pawn full-map scans.
- `ReservationManager` reserves the actual item target for an equip job. This is the whole reservation model; do not add a custom TTL ledger or pre-reserve the plan.
- If reservation fails or the target is absent, the job does nothing; it retries only on a later trigger. No fallback search loop.
- `JobGiver_OptimizeApparel` already exists for vanilla apparel scoring and check intervals. Do not build a second autonomous stat optimizer. Reuse its scoring only as a fallback/tie-break if needed; local preset priority remains authoritative.

Ammunition mods that solve shared-item contention are useful as a reference for reservation scope and failure handling. Analyze them alongside vanilla `ReservationManager`; do not copy their storage or job model.

### Central coordinator

- One map-level `OutfitCoordinator`, not one planner per pawn.
- Pawns mark themselves dirty on WorkType change, load, manual apply, or relevant inventory/policy change.
- Process a bounded number of dirty pawns per tick, not all 50 at once.
- Cache the resolved plan per pawn; do not recompute while it is being applied.
- Recompute only on invalidation.
- Warm the workbench area/catalog before frequent activation; first use may build the index, later uses reuse it.
- Coalesce dirty pawns and process a small budget per tick; never scan all 50 on activation.
- Reuse collections and avoid allocations in the hot path; no LINQ in per-cell loops.
- Do not rebuild footprints, stat rows, or candidate lists when only the active tab changes.

### Stock index and reservations

- Build a map-level apparel stock index once and invalidate it on apparel spawn/destroy/forbid/quality changes.
- Plan by `ThingDef` + footprint because quality/material are not controlled.
- Do not pre-reserve counts for the plan; reserve only the actual `Thing` at equip-job creation.
- Vanilla `ReservationManager` owns the reservation lifetime; no custom soft TTL.
- If only one item exists and one pawn reserves it, the other job does nothing and retries on a later trigger; no fallback search loop.

### Avoid overengineering

- No NP-hard solver.
- No per-cell reservation.
- No per-pawn full stock scan.
- No per-tick planning.
- No parallel planning.
- No quality/material optimization.
- No multi-select comparison in v1.
- No duplicate vanilla apparel optimizer.
