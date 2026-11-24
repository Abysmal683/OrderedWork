# Equipment system settings — reference

## Enable/disable

`OrderedWorkSettings.equipmentEnabled` (default `true`).

When off:

- the Work Control `Equipment` button is hidden;
- the pawn `Equipment` tab is hidden;
- `Equip now` does not queue jobs;
- no equipment execution or residue runs.

Use this to avoid conflicts with other clothing/loadout mods.

## Search area

`equipmentSearchAreaEnabled` (default `true`) and `equipmentSearchRadius` (default `30`).

When enabled, `Equip now` filters available apparel/equipment by distance from:

- the current job worktable, when the pawn's current job targets one;
- otherwise the pawn position.

When disabled, search uses the whole map.

The editor catalog is not radius-filtered; the radius applies to execution searches only.

## Debug

`OrderedWorkLogCategory.Equipment` logs plan and assignment summaries. It is off unless enabled in the logging bitmask.
