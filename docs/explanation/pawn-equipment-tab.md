# Pawn Equipment tab — design draft

Status: detailed appendix, no implementation yet.
Canonical v1: `docs/explanation/clothing-equipment-design.md`.
Related: `clothing-editor-layout.md`, `clothing-reequip-priority.md`, `clothing-metadata.md`.

## 1. Placement

Add an `Equipment` tab inside the pawn Detail window.

The tab is the pawn-facing summary and quick-action surface. The full editor opens from a set card.

## 2. Set roles and priority

```text
Forced/Locked item   player override, not a set
Mandatory             always active
Emergency             active on draft/alert
WorkType              one set per assigned work
Principal/Regular     casual/default
```

Priority:

```text
Forced/Locked > Mandatory > Emergency > WorkType > Principal/Regular
```

- A forced/locked item is the only true manual override.
- Force can beat Mandatory; if it does, show an alert in case the player forgot the force.
- Mandatory is “always wear this”, not a player lock.
- Mandatory checks that every cell requested by all mandatory sets was filled; alert if not.
- Emergency falls back to Principal when it has no own set.
- Primary/regular is the default casual fallback.
- Items worn but not explained by any set are `Unmanaged`.
- “Clothing” is renamed to “Equipment” everywhere in this UI.

## 3. Multiple sets per category

Mandatory, Emergency and Principal can each contain **several sets**.

- Sets are complementary; they do not cancel each other.
- A Royal Mechanitor can have two mandatory sets.
- An emergency can have one armor set plus one emergency-inventory set.
- Principal can have several daily sets.
- No hard limit; use internal scroll and collapsible groups.
- Set cards are read-only here.
- One `Open editor` button in the Sets header.
- The editor handles choose/save/view/delete/create; this screen only displays, overrides, and opens it.
- No `+` or per-card edit/delete in this screen.

Resolution within a category: sets fill the remaining cells in order; later sets fill gaps.

## 4. Layout

```text
┌ DETAIL — [Work] [Equipment] ──────────────────────────────────────────────┐
│ [Portrait] Peso 42/75 · Carrying 20 kg  Activo: Emergency (Draft)        │
│ [Gear] [Overrides 3]                                                     │
├ CURRENT GEAR ───────────────────────────┬ SETS ────────────────────────────┤
│ APPAREL (cells/onion)                   │ [Open editor]                     │
│                                         │ ▾ 1 MANDATORY  [2]               │
│  ┌──────────────────────────────────┐   │   • Royal armor                   │
│  │ [celda F] [celda] [celda]         │   │   • Mech belt                     │
│  │ [celda] [celda M] [celda]         │   │   [+ Add mandatory]               │
│  │ [celda] [celda] [celda W]         │   │                                    │
│  └──────────────────────────────────┘   │ ▾ 2 EMERGENCY  [2]               │
│                                         │   • Armor set                     │
│ PRIMARY                                 │   • Emergency inv.                │
│  [Rifle]                     [Equip now]│   [+ Add emergency]               │
│                                         │                                    │
│ INVENTORY                               │ ▾ 3 WORKTYPE SETS                 │
│  [Food x12]        actual/esperado 12/10│   • Cook [ACTIVO]                 │
│  [Medicine x5]     actual/esperado 3/5  │   • Haul (sin set)                │
│  [Steel x40]       override             │   • Research (sin set)            │
│  [Steel x20]       Carrying              │                                    │
│                                         │ ▾ 4 PRINCIPAL/REGULAR [2]         │
│                                         │   • Daily clothes                 │
│                                         │   • Casual tools                  │
│                                         │   [+ Add principal]               │
├ METADATA ───────────────────────────────┴──────────────────────────────────┤
│ 40 SUMMARY (tree)                    │ 60 DETAIL — selected item           │
│ Body / Global / Equipment / Inventory│ material real + calidad real + stats│
│                                      │ [Info card]                         │
└────────────────────────────────────────────────────────────────────────────┘
```

Assumption: Current Gear is left and Sets is right. If both should be left, stack them in that order.

## 5. Apparel cells

The apparel area uses the same cell/onion component as the editor.

- No separate apparel list with text tags.
- Show **one badge** per cell: the highest-priority source that gives it meaning.
- Hovering the badge/cell always shows the tooltip.
- Clicking the badge selects that cell/item for metadata.
- The layer config affects both the editor and this tab.

Badge letters (placeholder until icons):

```text
F  Forced / Locked
M  Mandatory
E  Emergency
W  WorkType
P  Principal / Regular
U  Unmanaged
```

Example:

```text
┌─────────┐
│  Boots  │
│  F      │
└─────────┘
```

Priority for the single badge:

```text
F > M > E > W > P > U
```

Emergency and WorkType should not both be shown; show the winning/active source and explain the rest in the tooltip. If a cell has no set source, show `U`.

## 6. Current Gear order

1. `Apparel` — cells/onion with badges.
2. `Primary` — primary equipment.
3. `Inventory` — carried items and quantities, including a `Carrying`-tagged row when applicable.

Inventory rows show `actual / expected`.

- `actual`: current amount in inventory.
- `expected`: minimum the pawn should keep from the active sets/overrides.
- `0`: temporary/unmanaged; the pawn may use/drop it.
- `1+`: minimum to keep; compatible with Pick Up And Haul.
- Negative/forbidden: explicit “do not keep” override where supported.

Overrides:

- Manual `+/-` changes the target for that pawn.
- The override is an **absolute** per-pawn target, not a relative delta.
- The override does not edit the set.
- The set remains modular; the pawn override is the exception.
- A changed row is visibly marked as `Override`.

`Carrying` is not set inventory and not primary equipment, but it is shown in the inventory list with a `Carrying` tag. It is temporary and usually one item. Its mass is shown as a separate `Carrying: X kg` value next to the main gear/inventory weight; do not merge it into the set inventory weight. Caravan pack capacity (35 kg) is a different metric and is not shown here.

### Overrides

Overrides are per-pawn and are not part of the set.

- They can add an item not declared by any set.
- They can change an inventory target with `+/-`.
- A negative/forbidden target can express “do not keep this” for inventory or unwanted weapons/tools.
- The row is marked `Override`.
- Removing the override returns to the set value.
- Forcing a cell is an override; it can beat Mandatory, with an alert if the player may have forgotten it.

## 6.1 Overrides sub-tab

`Equipment` has a `Gear / Overrides` sub-tab. The `Overrides` view lists every per-pawn override.

```text
┌ OVERRIDES (3) ─────────────────────────────────────────────┐
│ Forced   Torso|Shell: Robe                   [Revert]      │
│ Inventory Food 10                            [Revert]      │
│ Primary   Rifle                              [Revert]      │
│                                                            │
│ [Clear all overrides]                                      │
└────────────────────────────────────────────────────────────┘
```

Rules:

- Overrides are absolute per-pawn values.
- `Revert` clears one override.
- `Clear all overrides` clears every override after confirmation.
- Clearing returns the pawn to the set values.
- Forced cells are overrides too.
- This is the only place to mass-clear overrides.

## 7. Sets panel

Cards/accordion order:

1. Mandatory
2. Emergency
3. WorkType sets
4. Principal/Regular

Each category:

- collapse/expand;
- internal scroll;
- no hard limit on sets;
- cards are draggable to reorder within the group;
- cards can move between Mandatory/Emergency/Principal; WorkType is not movable;
- click/tap highlights the card only;
- `Open editor` opens the highlighted set; if none is highlighted, it opens the most recently edited/opened set;
- right click removes the target set from that group;
- right click on a WorkType set removes the global WorkType set;
- delete only happens in the editor; deleting a set must clear every reference to it;
- `+ Add` is directly under the target group and creates via the editor;
- create/rename/duplicate/delete belong to the editor;
- no per-card edit/delete buttons;
- the tab initially focuses no set.

WorkType sets:

- list all pawn-capable work;
- mark works with no global set;
- no per-pawn WorkType set exists;
- exactly one set per WorkType;
- active work set highlighted;
- click highlights; `Open editor` opens the global WorkType set;
- WorkType sets are not interchangeable with pawn sets.

## 8. Metadata

Use the same summary schema as the editor metadata, but with actual equipped items.

- 40: summary tree (body, global, equipment, inventory).
- 60: selected item detail.
- Material and quality are the **actual** ones, not defaults.
- Access `Info card` for full item info.
- If nothing is selected, show Primary equipment.
- No material/quality pickers here; this is a summary of what is worn/equipped.

## 9. Actions

- `Equip now`: runs the set system in priority order; it does not open the editor.
- It never changes Forced/Locked cells.
- It tries Mandatory first, then Emergency, WorkType, Principal.
- Also expose a compact gizmo for selected pawns, ammunition-mod style.
- Multi-pawn selection: one gizmo, each pawn uses its own active set.
- If no set exists, activate the vanilla apparel-seeking mechanism instead.
- The job is interruptible by the player or by draft/emergency.
- Incapacitated pawns are disabled/no-op.
- Clicking a set card highlights it only.
- `Open editor` opens the highlighted set; if none is highlighted, it selects the most recently edited/opened set.
- The tab opens with no focused set.
- `+ Add` under a group opens the editor in create mode for that group.
- No create/rename/duplicate/delete buttons outside that flow.

Removed:

- `Edit active set`
- `Manage sets`

### Delete safety

- Right click removes only from the group.
- Delete happens only in the editor.
- Deleting a set must clear:
  - every pawn/group reference;
  - every WorkType reference;
  - overrides or links that pointed to it.
- Affected groups fall back to empty/Primary as appropriate.
- Ask for confirmation before deleting a referenced set.

## 10. Open questions

1. **Badge icon source**: colored letters now, or create six small textures/use vanilla glyphs.
2. **Delete confirmation wording** for referenced sets.
3. **Translation text** for the decided `OrderedWork.Equipment.*` keys.

## 11. Vanilla selection note

Vanilla does not simply “pick the best quality”. It filters by the outfit policy and then scores apparel using hit points, armor, insulation, and related factors. Quality affects those stats but is not a standalone first sort.

- No custom quality-first rule.
- Apply outfit filters, then use the vanilla score.
- Mandatory uses the same candidate selection, restricted to its required cells.
- Force/locked remains a manual override.
