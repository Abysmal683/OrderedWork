# Equipment system settings — reference

## Enable/disable

`OrderedWorkSettings.equipmentEnabled` (default `true`).

When off:

- the Work Control `Equipment` button is hidden;
- the pawn `Equipment` tab is hidden;
- `Equip now` does not queue jobs;
- WorkType `EquipmentSetId` is inert;
- no equipment search or resolver execution runs.

This is the real fallback to vanilla apparel policy/other mods.

## Search area

`equipmentSearchAreaEnabled` requires `equipmentEnabled`.

When enabled, `Equip now` filters available apparel/equipment by distance from:

- the current job worktable, if the current job targets one;
- otherwise the pawn position.

When disabled, search uses the whole map. It still finds stockpiles, outfit stands and ground items.

The editor catalog is not radius-filtered.

## Pre-work dressing

Automatic dressing before a work job is not implemented yet. `Equip now` is manual.

Before a work job exists there is no current worktable target, so the search center would be the pawn. A true pre-work worktable area needs an explicit WorkType/workstation anchor; vanilla has no reliable WorkType-to-worktable mapping.

Options:

1. Search around the pawn before work; switch to the worktable anchor when a worktable job exists.
2. Add an explicit configurable workstation anchor per WorkType.
3. Trigger dressing only after a worktable job is assigned.

Do not invent a worktable mapping.

## Debug

`OrderedWorkLogCategory.Equipment` logs plan and assignment summaries.
## Gizmo search widening

Selected pawns get one compact Equipment gizmo on the first selected pawn.

Float menu:

- `Equip now · radius current`;
- `Equip now · radius 100`;
- `Equip now · radius 200`;
- `Equip now · whole map`;
- `Open Equipment`.

Search radius setting:

- min 25;
- max 200;
- default 30;
- configurable range starts at 25 and allows up to 200.
- The 100 and 200 values are quick gizmo overrides, not settings writes.

The gizmo applies to all selected colonists; downed pawns are ignored.
