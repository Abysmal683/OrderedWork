# Equipment metadata panel — reference

Mounted in the neutral equipment editor and the pawn Equipment tab.

## Sections

- Body: body group · layer, armor S/B/H.
- Global: insulation per active item.
- Equipment: actual primary when pawn context exists, otherwise set primary.
- Inventory: actual carried stacks when pawn context exists, otherwise set targets.
- Detail / Compare: selected item stats, material and quality.
- Source icons: right-aligned with `+N` overflow.

## Behavior

- Neutral editor uses default material and Normal quality.
- Pawn tab uses the real worn item when available.
- `Info card` opens `Dialog_InfoCard`.
- No static game-object caches.
- Draw failures are bounded by `Log.ErrorOnce`.
