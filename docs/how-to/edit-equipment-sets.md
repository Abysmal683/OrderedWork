# How to edit and save equipment sets

1. Open `Work Control → Clothes`.
2. Open the equipment editor.
3. Select a cell in the apparel grid.
4. Pick a candidate under `Available` to add it to `Added`.
5. Right-click an added alternative to remove it.
6. Use the top actions:
   - `Load...` to open the equipment-set list.
   - `Save` to overwrite the current set by Id.
   - `Save As...` to create a renamed copy with a new Id.
7. New and delete are inside `Load...`, not in the editor header.
8. If the editor has unsaved changes, closing/load/new asks:
   - `Save`
   - `Discard`
   - `Cancel`

Names are edited only in `New` or `Save As...`. The current set name is read-only.

Equipment sets are stored under `Config/OrderedWork/EquipmentSets`. Legacy `ClothingPresets` are ignored.

## General and cell modes

- With no cell selected, `Available` shows all allowed apparel.
- Select a cell to filter candidates to that exact cell.
- Hovering a candidate outlines its footprint.
- Empty target cells show the candidate icon at 50%; occupied cells only get the outline.

## Metadata

- Clear the cell selection to see all allowed apparel in `Available`.
- Select an added alternative to show its detail.
- `Summary` lists the active set's body-part/layer coverage and armor values.
- `Detail` shows material, default quality, and the vanilla stats report.
- `Info card` opens `Dialog_InfoCard` for the selected item.
- The editor uses default material/quality; the pawn Equipment tab will use actual values.
