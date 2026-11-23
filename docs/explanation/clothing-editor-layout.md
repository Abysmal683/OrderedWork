# Clothing editor layout — design spec

Status: detailed appendix, no implementation yet.
Canonical v1: `docs/explanation/clothing-equipment-design.md`.
Scope: the neutral clothing preset editor. It does not change the clothing resolver or save contract.
Related: `docs/explanation/clothing-reequip-priority.md` for re-equip order, alternatives, and priority resolution; `docs/explanation/clothing-metadata.md` for the metadata/comparison panel.

## 1. Invariants

1. A slot is a unique cell: `BodyDef + BodyPartGroupDef + ApparelLayerDef`.
2. A cell means one body-part coverage group plus one apparel layer. Cell mode lists only apparel whose declared data can fill that exact cell.
3. RimWorld has no `ApparelSlotDef`; slots are derived from apparel footprints.
3. A piece has a footprint: all cells it can occupy. It has no primary/anchor cell.
4. Tabs, rows, columns, order, and labels are view-only. They never change the slot identity or the candidate set.
5. Occupancy and candidate lookup are keyed by `SlotKey`, never by row/column index.
6. A piece that spans several cells is not a new slot.
7. No dimmed ghost for placed apparel. Coverage is shown with a same-color outline at full tone.
8. Candidate previews may use a dashed outline. A 50% ghost is optional only for empty candidate cells.
9. Changing the view layout must not lose or duplicate cells.

## 2. Data model

```text
SlotKey
  BodyDef body
  BodyPartGroupDef group
  ApparelLayerDef layer

Footprint
  ThingDef def
  List<SlotKey> cells

SlotCatalog
  SlotKey -> possible ThingDefs

Occupancy
  SlotKey -> placed Apparel instances

ViewLayout
  List<ViewTab> tabs
  List<ViewRow> rows
  List<ViewCell> cells
```

`ViewLayout` is persisted separately from clothing presets. It is user display configuration, not gameplay data.

## 3. View model

```text
ViewTab
  id
  label
  order
  type = DefaultFamily | Custom
  rows
  canDelete

ViewRow
  id
  label
  order
  tabs
  blocks

ViewBlock
  id
  label
  row
  cells

ViewCell
  SlotKey key
  tab
  row
  order
```

Rules:

- One `ViewCell` per `SlotKey`.
- Moving a cell between tabs/rows moves it; it never copies it.
- A cell always keeps its raw `group` and `layer` as a tooltip and badge.
- Deleting a tab or row moves its cells to `General`, never deletes them.
- Unknown or new cells are placed by the default heuristic; if that fails they go to `General`.

## 4. Default tabs

Tabs are visual containers, not target layers. A tab can hold layers that are not semantically equal.

Default tabs:

```text
Skin
Middle
Outer
Utility
Head
Accessory
Cape / Overlay
Exosuit
Other / General
```

Default layer mapping:

| Tab | Raw layers |
|---|---|
| Skin | OnSkin, OnSkin_A, Underwear, UnderwearTop |
| Middle | Middle, Middle_A |
| Outer | Shell, Shell_A, DankPyon_LegShell, DankPyon_ShellAttachment |
| Utility | Belt, Belt_A, AM_Pocket |
| Head | Overhead, Overhead_A, DankPyon_Hood, DankPyon_InnerHelmet |
| Accessory | EyeCover, Accessories, DankPyon_NecklaceLayer, DankPyon_Ring1Layer |
| Cape / Overlay | Cape, Armor_Cape, VFEC_OuterShell |
| Exosuit | ExosuitLayer_*, WG_WalkerGear* |
| Other / General | unknown layers |

This is a default seed, not a hardcoded UI. The player can rename, reorder, regroup, or create custom tabs.

## 5. Tab rail behavior

The tab rail must not grow the window when more tabs appear.

- Fixed-height tab rail.
- Horizontal scrolling with left/right chevrons.
- Overflow menu for tabs that do not fit.
- Tab labels truncate with an ellipsis; the tooltip shows the full label.
- Optional tab dropdown for small widths.
- The content viewport keeps its size when tabs are added or renamed.
- The active tab is preserved by `tab.id`, not by index.

## 6. Rows and blocks

Each tab contains rows. A row is a named visual block with a header and a local set of cells.

- Rows are renameable.
- Rows are reorderable.
- Cells and blocks can be assigned to a row/tab.
- Rows scroll vertically inside the fixed content viewport.
- A row can be collapsed and expanded.
- A row never expands the outer panel; it gets an inner scrollbar.
- A cell can belong to exactly one row and one tab.
- Row order is cosmetic; it does not affect `SlotKey`, occupancy, or candidates.

## 7. Scroll and sizing

- Outer editor size is fixed by the window.
- Tab rail: fixed height, horizontal scroll/overflow.
- Body area: fixed viewport, vertical scroll.
- Wide rows: horizontal scroll inside the row; no outer width growth.
- Candidate panel: its own scrollbar.
- Reordering or adding tabs never changes the outer window size.
- Use stable cell sizes so scroll positions and hover hit-tests remain predictable.

## 8. Cell interaction

### No cell selected

- Candidate panel shows all equippable apparel, like Loadout.
- Optional filter by current tab/family.
- Search and category filters are available.

### Cell selected

- Candidate panel shows only apparel whose footprint contains that exact `SlotKey`.
- Filter by body fit and `ApparelPolicy`.
- Results are identical in list and onion views.
- Visual location of the cell is irrelevant to the result.

### Possible options / suggestions

The candidate area should make it obvious what is being shown.

General mode:

```text
Todas las opciones equipables · 128
```

Cell mode:

```text
Opciones para Torso + Shell · 12
```

Empty states:

```text
Selecciona una celda para ver opciones
No hay opciones para esta celda
No hay opciones con este filtro
```

Also show:

- category chips;
- search field;
- source/filter count;
- “posibles” / “disponibles” wording, not a vague empty list.

This is a clarity hint, not an automatic recommendation engine.

### Placed item hover

- Outline every cell in its footprint with the same full-color outline.
- No alpha, no dimming, no duplicated icon requirement.
- If the footprint spans tabs, the outline persists when switching tabs.
- A tab badge may show the number of footprint cells in that tab.

### Candidate hover

- Dashed outline around every cell the candidate would occupy.
- Optional 50% ghost only in empty cells, never for already placed apparel.
- If the candidate spans tabs, show the outline in both tabs.

### Multiple occupants in one cell

- Show a count badge or a compact stack.
- Clicking the cell opens the occupant/candidate list.
- Hovering an item in the list highlights its footprint; it does not redefine the cell.

## 8.1 Vision modes and alternatives

### Two states only

`Vision` has exactly two persistent modes. Hover is transient, not a third mode.

1. **General**
   - No cell is selected.
   - The grid keeps every cell visible.
   - The right column shows all equippable apparel.
   - The left column is inactive or shows `Select a cell`.
   - This is the "show what can be equipped without removing the rest" state.

2. **Cell**
   - One cell is selected.
   - The grid still keeps every cell visible.
   - The left column shows alternatives added for that cell, in priority order.
   - The right column shows only apparel that can fill that exact `SlotKey`.
   - Selecting a cell filters the columns, never the grid.

The grid layout, tab layout, and row layout do not change between modes.

### Two columns

The selected-cell panel is split into:

```text
┌ ADDED — priority order ─────────────┬ AVAILABLE — for this cell ───────────┐
│ 1. Robe                             │ [item] [item] [item] [item]          │
│ 2. Duster                           │ [item] [item] [item] [item]          │
│ 3. Royal siege armor                │                                      │
└─────────────────────────────────────┴──────────────────────────────────────┘
```

- Left: alternatives already saved for the cell.
- Right: candidates that can fill the cell.
- Top of the left list is the highest priority.
- Add from right; remove/reorder on left.
- No duplicate alternative in the same cell.
- An alternative may be unavailable at runtime; the resolver falls back to the next priority.
- Interaction details are defined in `docs/explanation/clothing-reequip-priority.md`.

### Hover behavior

- Hovering an added alternative or an available candidate outlines every cell in its footprint.
- If a target cell has no placed item and no saved alternative, draw the candidate icon at 50% inside that cell.
- If the target cell already has an item or an alternative, draw only the outline; do not overlay a 50% icon.
- Never dim or fade placed items.
- If the footprint extends into another tab, the outline and the 50% empty-cell icon persist there; the tab shows a badge.

### Saved alternatives

The intended model is a priority list per cell, similar to Loadout:

```text
Cell Torso|Shell
  1. Apparel_Robe
  2. Apparel_Duster
  3. VAE_Apparel_RoyalSiegeArmor
```

At runtime, the first available alternative that passes body/policy/conflict checks is used; otherwise the next one. If none passes, the cell stays empty and reports the reason.

Alternatives are stored per cell. A piece has a footprint, but adding it to one cell does **not** copy it into every covered cell. Each cell keeps its own local list and order. Only the active alternative creates coverage; another covered cell may have no local alternative.

### Vision open questions

1. **What is "empty" for the 50% icon?**
   - Recommendation: no placed item and no saved alternative.
2. **General mode left column**: inactive, global alternatives, or hidden?
   - Recommendation: inactive with `Select a cell`.
3. **Selection across tabs**: keep a selected cell that is not visible in the new tab, or clear to General?
   - Recommendation: keep the key, show a small "selected in another tab" badge, and render when that tab is opened.
4. **Priority direction**: top is highest, or bottom is highest?
   - Recommendation: top is highest.
5. **Add position**: append to bottom or insert at top?
   - Recommendation: append to bottom; the player reorders.
6. **Unavailable alternatives**: grey the row, hide it, or show a status badge?
   - Recommendation: keep it visible with a status badge, because availability changes at runtime.

## 9. Head: FullHead vs UpperHead

Do not merge these groups.

According to `Bodies_Humanlike.xml`:

| Group | Covered body parts |
|---|---|
| FullHead | Head, Skull, Brain, Eyes, Ears, Nose, Jaw, Tongue |
| UpperHead | Head, Skull, Brain, Ears |
| Eyes | Skull, Brain, Eyes |
| Teeth | Jaw |
| Mouth | Jaw |
| Neck | Neck |
| Ears | No vanilla human body part; RPG-only compatibility group |

Consequences:

- A `FullHead` item covers the whole head, including eyes, nose, mouth and teeth.
- An `UpperHead` hat covers the top of the head and ears, but not eyes, nose, mouth or teeth.
- `Mouth` and `Teeth` both map to the jaw.
- `FullHead` does not include the neck; Neck is a separate cell.
- `Ears` from RPG Style Inventory is not assigned to vanilla human body parts; it is not a real protection slot without a body patch.

Default labels:

```text
FullHead  -> Cabeza completa
UpperHead -> Parte superior
Eyes      -> Ojos
Teeth     -> Dientes
Mouth     -> Boca
Neck      -> Cuello
```

Tooltips must state the covered parts, not only the defName. Keep both cells visible; this is why Loadout's head area looks unusual but is accurate.

## 10. Layout editor

A small button labeled `Reorder cells` / `Reordenar celdas` opens the layout editor.

Allowed actions:

- Create, rename, reorder, and delete custom tabs.
- Rename and reorder rows.
- Drag cells or blocks between tabs/rows.
- Create a row or block.
- Move unassigned cells to a tab/row.
- Reset to defaults.
- Auto-group unknown layers/body groups.
- Show/hide empty cells.

System behavior:

- The default tabs/rows may be reset but not permanently destroyed.
- Deleting a custom tab/row sends its cells to `General`.
- No layout action changes a preset, a `ThingDef`, or a `SlotKey`.
- `General` always exists as the safe destination.
- New mod content is assigned to a default family when possible; otherwise it appears in `General` with a "new" marker.

## 10.1 WorkType palette reuse

Do not create a new panel for WorkType clothing assignment. Reuse the Work Control work palette.

- Right-click is handled directly by the palette row draw; no separate panel position.
- A natural context/float menu opens from the drawn row.
- Menu options: `Assign clothing`, `Create clothing`, `View/Edit`.
- Selecting an option opens the same clothing editor used by the pawn.
- If a float menu is not available, use a compact save/load-like selector with `Create` and `View`.
- `Create` and `View` open the editor in different contexts.
- The fallback panel shows the current active set in a small label.
- A short help line explains that right-click assigns or creates clothing for that WorkType.
- A pawn can have multiple sets, ordered left-to-right.
- A WorkType has exactly one clothing set.
- The WorkType set is applied while that work is active, like a work schedule.
- It may replace non-locked cells if its item is available.
- Switching work switches the previous work set; it does not leave the old work set blocking the new one.
- Player-locked/forced cells are never overridden.
- If the WorkType set is unavailable, nothing happens; do not loop.

## 11. Persistence

- Store `ViewLayout` in user/mod settings, not in `ClothingPreset`.
- Persist by stable IDs (`defName`), not by row/column index.
- Presets remain shareable; another player's view layout does not need to match.
- Missing layer/group IDs are preserved as orphan entries and shown with a warning.
- Reordering or hiding a cell does not affect save data.

## 12. Performance

- Build `SlotCatalog` once per `BodyDef` and apparel def set.
- Rebuild occupancy per pawn on apparel change or the existing 60-tick cache.
- Reordering tabs/rows only updates view rectangles; it never rebuilds the catalog.
- Do not use `row * columns + column` as identity.
- Cache candidate lists by `SlotKey`.

## 13. Closed decisions

- Dynamic catalog, no hardcoded 28-slot grid.
- Unique cells; no merged slots.
- No primary/anchor slot.
- No dimmed ghost; full-color footprint outline.
- Tabs and rows are visual containers, not target layers.
- Tabs and rows are renameable and reorderable.
- Cells are keyed by `SlotKey`, independent of visual order.
- Candidate list is driven by the exact `SlotKey`.
- `FullHead` and `UpperHead` stay separate and visible.
- Head labels and tooltips must state real coverage.
- Candidate strip shows all equippable when no cell is selected, and exact-cell candidates when selected.
- Vision has exactly two persistent modes: General and Cell. Hover is transient.
- General mode keeps the full grid visible and shows all equippable items.
- Cell mode keeps the full grid visible, filters only the two-column panel, and queries the exact `SlotKey`.
- The two columns are `Added` (priority order) on the left and `Available` on the right.
- Hover outlines the full footprint; a 50% icon is used only in empty cells.
- Alternatives are priority-ordered and fall back at runtime.
- User can open a layout editor for tabs, rows, and cell assignment.
- Layout persistence is separate from presets.

## 14. Open decisions

1. **Empty cells**: show only cells with at least one possible def, or all possible cells?
   - Recommendation: show possible cells; add a “show empty” toggle.
2. **Layout editor scope**: personal settings only, profile-shared, or exportable?
   - Recommendation: personal, with optional export.
3. **General section**: one bucket for undefined cells, or separate `Unassigned` and `Equipment`?
   - Recommendation: separate `Unassigned` and `Equipment`.
4. **Multiple occupants allowed by mods**: conflict by default or expose a stack?
   - Recommendation: conflict; respect `CanWearTogether` and mod behavior.

## 15. Non-goals

- No changes to the clothing resolver.
- No changes to preset save/load semantics.
- No automatic equipping/jobs.
- No live pawn render in the neutral editor; use a mannequin or portrait outside it.
- No merging of `FullHead` and `UpperHead` into one gameplay slot.
