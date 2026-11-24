# How to use the pawn Equipment tab

1. Select a pawn.
2. Open `Details`.
3. Open the `Equipment` tab.

Current Gear:

- Apparel with resolver-derived badges `F/M/E/W/P/U`
- Primary
- Inventory
- Carrying
- Weight

Sets:

- Mandatory
- Emergency
- WorkType
- Principal

Actions:

- Click a set to select it.
- `Open editor` opens it.
- Right-click a Mandatory/Emergency/Principal set to assign/remove.
- Drag assigned sets in the same role to reorder.
- `Overrides` opens the equipment override list.
- Right-click an override to remove it.
- `Clear all overrides`.

Metadata now uses the real worn item for material and quality when available. The neutral editor still uses defaults.

Debug category: `Equipment` in the logging bitmask. It logs resolver summaries and assignment changes, never per-frame.

## Metadata sections

- Body: group · layer and armor.
- Global: insulation per active item.
- Equipment: actual pawn primary, or set primary in the editor.
- Inventory: actual pawn stacks, or set targets in the editor.
- Source icons show contributors; `+N` overflows with a tooltip.

## Equip now

`Equip now` resolves the assigned sets and queues vanilla `Wear`/`Equip` jobs for missing items. It does not force locked apparel, does not create a custom reservation ledger, and skips unavailable items.
