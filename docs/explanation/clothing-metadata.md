# Clothing metadata panel — design draft

Status: detailed appendix, no implementation yet.
Canonical v1: `docs/explanation/clothing-equipment-design.md`.
Related: `docs/explanation/clothing-editor-layout.md`, `docs/explanation/clothing-reequip-priority.md`.

## 1. Layout

The metadata area sits below the equipment/inventory row. It is a 40/60 split.

```text
┌ EQUIPMENT / INVENTORY ROW ─────────────────────────────────┐
├ SUMMARY 40 ───────────────────────┬ DETAIL 60 ──────────────┤
│ Body coverage                     │ Full item detail        │
│ Armor / insulation                │ or 30/30 comparison     │
│ General stat summary              │                         │
│ Default material/quality note     │                         │
└───────────────────────────────────┴─────────────────────────┘
```

- 40: resumen del plan.
- 60: info del ítem enfocado.
- 30/30: comparación de dos equipos/inventario.
- No hay una sección separada de “Equipment” en metadata; el equipo/inventario enfocado usa el mismo 60.
- El equipo se elige en la fila de equipación; la metadata solo lo inspecciona.

The outer editor size does not change. Each pane has its own scrollbar and footer.

## 2. Summary 40

Purpose: show, per body part, how much protection the active clothing gives.

Definition: the primary item is the first alternative in each cell's local priority list. Inactive alternatives do not contribute to the summary.

### Body parts are individual

Use body **groups**, not left/right extremities. Modded groups such as ears or fingers are included when the body/apparel defs provide them; they are not hardcoded. Their row/group placement is editable in the layout config.

```text
Head
Eyes
Mouth
Neck
Torso
Shoulders
Arms
Hands
Waist
Legs
Feet
```

Arms do not merge with hands. Head does not merge with eyes, mouth or neck. If a full helmet covers head + eyes + mouth, it appears in each group's layer stack. Do not split into left arm/right arm or left hand/right hand in v1.

### Armor is not a sum

Vanilla armor is evaluated from the outermost layer inward. Each layer can deflect or halve the damage. It is not a simple added number.

Summary per body part:

```text
Arms
  Outer:  Sharp 40  Blunt 20  Heat 10
  Middle: Sharp 20  Blunt 10  Heat 0
  Skin:   Sharp 5   Blunt 0   Heat 0
```

Show the layer stack from outer to inner. Do not invent a combined “total armor” value.

Layer order comes from `ApparelLayerDef.drawOrder`, not a hardcoded vanilla list, so modded layers work. Use `ApparelProperties.CoversBodyPart` / `CoversBodyPartGroup` for coverage.

### Compact tree

Summary 40 is a collapsible tree, not a wall of text.

```text
▾ Cuerpo
  ▾ Brazos                  S 40  B 20  H 10   [icons]
      Exterior              S 40  B 20  H 10
      Medio                 S 20  B 10  H 0
      Piel                  S 5   B 0   H 0
  ▸ Manos
  ▸ Cabeza
▾ Global
  ❄ Aislamiento frío        +8°C
  🔥 Aislamiento calor      +2°C
  💬 Negociación            +12%   [icons] +2
▾ Equipo
▾ Inventario
```

- `S` = sharp/corte, `B` = blunt/golpe, `H` = heat/calor.
- Symbols keep the row compact; the tooltip expands to names, layers, sources, and values.
- The tree starts fully expanded: every node is open at first.
- Nodes stay open as their spaces are assigned/filled.
- Body groups are collapsed/expanded individually after that.
- `Equipo` and `Inventario` show an empty-state row when empty.
- Use fixed-width source-icon slots on the right; overflow is `+N`.
- Each contributing item gets its own row with the responsible item icon; do not collapse different sources into one anonymous number.

### Stat order

Use the global vanilla stat order, not “largest contributor first”:

1. `StatCategoryDef.displayOrder`.
2. `StatDef.displayPriorityInCategory`.
3. Stable defName tie-break.

A stat appears in that order if any active item contributes it. Do not reorder rows by contribution size.

### Global values

These are shown once, not per body part:

- Insulation cold/heat.
- General stats from active clothing, such as negotiation.
- Equal general stats are summed only when the stat is additive in vanilla; do not sum armor. If the stat is not additive, show per-item or use the vanilla stat calculation.

### Material and quality

- Show material only if `ThingDef.MadeFromStuff` is true.
- Default material: `GenStuff.DefaultStuffFor(def)`.
- Default quality: `Normal`, only when the item has a quality comp.
- Show material **categories** from `GenStuff.AllowedStuffsFor(def, ...)`. Do not hardcode “leather/cloth/both”.
- Vanilla stuff categories are `Metallic`, `Woody`, `Fabric`, `Leathery`; mods can add more. “Mineral” is not a separate vanilla apparel category; use `Metallic` or whatever the mod declares.
- If only leather or only cloth is allowed, show that one category. If both are allowed, show both. If metal/wood is allowed, show that too.
- Category first; concrete material inside the category when the player opens the picker.

Stats that need a pawn context must be marked or omitted; the neutral editor has no pawn.

### Stat source icons

Each summary row may show the item(s) contributing the value.

- Source icons are right-aligned in a fixed-width slot.
- Text does not move when the number of sources changes.
- Show up to a small fixed limit (for example 3); then show `+N`.
- Hovering the icon/`+N` opens a tooltip with the full source list and each contribution.
- Do not create a new row per source; keep the stat row compact.
- If the value is non-additive or cannot be represented as one number, show `—` or per-item values with the source tooltip.

## 3. Detail 60

The 60 pane has two modes.

### Full mode

- Selected item header.
- Icon, label, defName.
- Full relevant stats, using `StatsReportUtility`.
- Material and quality controls.
- Info button.
- Coverage badges.
- Default material/quality shown explicitly.

### Compare mode

- The 60 pane becomes two 30/30 panes.
- Left: item A. Right: item B.
- Each pane has its own bottom controls.
- The compare view shows deltas or marks where one item is better.
- The compare button toggles the mode.

Compare rules:

- The 30/30 comparison is the shared metadata comparison.
- Equipment/inventory comparison does not require a cell.
- Apparel comparison uses the same 30/30 but requires the same selected cell.
- If no second item is selected, compare the selected item against itself with another material/quality.
- If a different item is selected, add it as the second side.
- Each side can use a different material and quality, or the same.
- Do not compare more than two items in v1.
- Compare does not change the preset; it is preview-only.
- No separate comparison screen or mode.

### Cards vs inline stats

Vanilla has no reusable “card” widget that can be dropped twice into a 30/30 pane. `Dialog_InfoCard` is a separate window, not a card component.

Use the reusable stat API instead:

- `StatsReportUtility.StatsToDraw(thing)` for the stat entries.
- `StatsReportUtility.DrawStatsReport(rect, thing)` for an inline report.
- `Dialog_InfoCard` only for the explicit `Info` action.

Recommended comparison:

- Two lightweight 30/30 headers with icon, label, quality/material.
- Extract the union of `StatsToDraw` from both items.
- Show one row per stat, aligned by `StatDef`.
- If a stat exists on both, color/highlight the row.
- If it exists on only one, show it normally without a “better/worse” claim.
- No fixed winner calculation; show the advantages of both.
- If a full inline report is not feasible, use a per-row tooltip with the full detail.
- Do not open two full info-card windows side by side.

## 4. Footer controls

“Footer” means a fixed bottom bar inside a panel. It holds buttons and stays visible while the panel content scrolls; it is not part of the scrollable stat list.

Suggested controls:

```text
[Material] [Quality] [Compare] [Info]
```

- `Material`: opens a material picker for the focused item. Preview-only in v1.
- `Quality`: opens a quality picker for the focused item. Preview-only in v1.
- `Compare`: toggles the 60 pane between full and 30/30 comparison.
- `Info`: opens `Dialog_InfoCard` for the focused item/variant.

Placement:

- Bottom of each metadata panel.
- 40 Summary can have a minimal footer or none; it uses defaults, so Material/Quality usually belong to detail.
- 60 Full has Material, Quality, Compare, Info.
- Each 30/30 side has its own Material, Quality, Info; Compare is global to exit/enter.
- Do not duplicate controls unnecessarily; keep the footer fixed and the content scrollable.

## 5. Data sources

Use vanilla data and panels:

- `ThingDef.apparel.bodyPartGroups`
- `ThingDef.apparel.layers`
- `ApparelProperties.CoversBodyPart(BodyPartRecord)`
- `ApparelProperties.CoversBodyPartGroup(BodyPartGroupDef)`
- `ApparelLayerDef.drawOrder`
- `Thing.Stuff`
- `ThingDef.MadeFromStuff`
- `GenStuff.DefaultStuffFor(BuildableDef)`
- `GenStuff.AllowedStuffsFor(...)`
- `StuffCategoryDefOf.Metallic`, `Woody`, `Fabric`, `Leathery`
- `QualityUtility.TryGetQuality(...)`
- `CompQuality`
- `StatsReportUtility.StatsToDraw(Thing)`
- `StatsReportUtility.DrawStatsReport(Rect, Thing)`
- `Verse.Dialog_InfoCard`
- `Dialog_InfoCard.InfoCardTab.Stats`
- `ShowMaterialsButton`

Material and quality are item/build properties, not fixed `ThingDef` properties. A default preview needs a synthetic `Thing` or the game's material selection UI.

## 6. Summary versus detail

- Summary 40 is per body part, with outer-to-inner armor layers and global values.
- Detail 60 is one focused item.
- Compare 60 is the shared two-item comparison; equipment/inventory needs no cell, apparel uses the selected-cell rule.
- Inactive alternatives never contribute to Summary.
- Defaults are shown, not silently applied to gameplay.
- Material/quality previews do not alter the preset.

### Tools, weapons and equipment

- Primary equipment is `Pawn.equipment.Primary`: one held tool/weapon.
- There is no body-part cell for a held weapon. The `LeftHand`/`RightHand` body groups are apparel coverage groups, not equipment slots.
- Tools and weapons are equipment, not body-covering apparel.
- Metadata may show primary equipment stats, but it has no armor-by-body-part rows for it.
- If an item is actually apparel, it has cells. If it is in `equipment.Primary`, it does not.
- Weapons do not need a new cell; they need an equipment presentation, not a clothing slot.

## 6.1 Primary-hand equipment — historical note

Superseded by section 6.3.

Earlier options were an equipment strip or a special `Hands/Accessory` cell. The final design is the equipment box in the bottom band, not a body-part cell. Equipment has no `SlotKey`; `LeftHand`/`RightHand` are apparel coverage groups, not equipment slots.

## 6.2 Equip vs inventory vs apparel

These are four different vanilla containers/states.

| State | Vanilla owner | Meaning |
|---|---|---|
| Apparel | `Pawn.apparel.WornApparel` | Worn clothing; has body groups, layers, armor, cells |
| Equipment | `Pawn.equipment.Primary` | One held tool/weapon; has verbs/tools; no body cells |
| Inventory | `Pawn.inventory.innerContainer` | Carried goods; no active verbs; weight/encumbrance |
| Carry tracker | `Pawn.carryTracker` | Temporary hauling/carrying, not personal inventory |

### Equipment

- One primary item in vanilla: `EquipmentType.Primary`.
- Requires `CompEquippable`; validate with `EquipmentUtility.CanEquip`.
- `CompEquippable.AllVerbs`, `PrimaryVerb`, `Tools` become usable only when equipped.
- A weapon/tool lying on the ground or in inventory does not grant its verbs/tools.
- `Pawn_EquipmentTracker.AddEquipment`, `Remove`, `TryDropEquipment`.
- Equipment is not apparel and has no `SlotKey`.
- `LeftHand` / `RightHand` are apparel coverage groups, not equipment slots.

### Inventory

- `Pawn.inventory.innerContainer` is a `ThingOwner<Thing>`.
- Items in inventory are carried, not equipped.
- Inventory mass is tracked by `MassUtility` (`InventoryMass`, `GearMass`, `FreeSpace`, `Capacity`, `EncumbrancePercent`).
- Inventory items generally do not grant combat verbs or armor.
- Drugs/food may be usable from inventory through their own comps; weapons/tools are not.
- The carry tracker is not the inventory; it is temporary carrying/hauling.

### Weapons and tools

- `ThingDef.IsWeapon`, `IsRangedWeapon`, `IsMeleeWeapon`, `Verbs`, `Tools`, and `equipmentType`.
- A tool/weapon is usable only when equipped in `equipment.Primary`.
- A tool can be a melee weapon with `Tools` and no ranged verb.
- `equipmentType == None` is not primary-equippable; do not infer from the label “tool”.
- One primary equipment item in vanilla; no dual-wield/secondary equipment slot.

### Design consequence

The primary-hand strip is an **equipment** presentation:

- no body-part cell;
- no armor-by-body-part rows;
- general/equipment stats only;
- catalog of primary-equippable items;
- inventory is a separate strip in the bottom band, not an equipment cell.

## 6.3 Equipment and inventory in the plan

The plan now has three layers:

```text
Apparel cells
Primary equipment
Inventory
```

No new top-level screen. Reuse the current editor and add a bottom dock between the cell area and metadata.

“Dock” here means a fixed panel attached to the bottom edge, not a new screen. It can be resized/collapsed and keeps its own scroll. Prefer calling it “bottom band” in user-facing text if “dock” is confusing.

```text
┌ VISION / CELLS ─────────────────────────────────────────────┐
│                                                             │
├─────────────────────────────────────────────────────────────┤
│ [PRIMARY: icon + name + clear]  [INVENTORY: chips/counts]   │
│ [WEIGHT: 24.3 kg ─────────────]                             │
├─────────────────────────────────────────────────────────────┤
│ METADATA 40/60                                              │
└─────────────────────────────────────────────────────────────┘
```

### Equipment box

- One primary-hand item.
- Filter: `equipmentType == Primary` and `CompEquippable`.
- Not a body cell.
- Click opens the existing inline picker; no new window.
- Clear button removes it.
- No inventory item becomes equipment until explicitly assigned.
- Weapons and tools share this box; do not create a second weapon system.

### Inventory strip

- Compact list of selected `ThingDef` + quantity.
- Add through the existing suggestions/available area; no new picker window.
- The suggestions area lists all carryable items, with search/categories.
- Items already in inventory are marked in the suggestions area, without quantity.
- Quantity is edited only in the inventory strip.
- Any carryable item (`ThingCategory.Item`) can be selected, not just weapons.
- Quantity controls: `+`, `-`, direct numeric input; zero removes the row.
- Use vanilla bill modifiers exactly: base `±1`, `Ctrl` ×10, `Shift` ×100, `Ctrl+Shift` ×1000. Direct numeric input is always available.
- No stock/ownership filter; this is a neutral plan.
- Avoid instantiating every `Thing`; use defs, lazy icons, search, virtualization.
- Inventory items do not grant verbs or armor.
- Inventory does not occupy apparel cells.
- Quality/material are not edited here in v1.

### Recommendation filters

The inventory/recommendation picker should filter by:

- name search;
- top-level groups: `Consumibles`, `Utilitarios`, `Varios`;
- optional `ThingCategoryDef` subgroups inside those.

Definitions:

- `Consumibles`: the colonist takes/uses directly, such as food and drugs. Use `ThingDef.IsIngestible` / drug data.
- `Utilitarios`: non-consumable use, such as medicine, ammunition, tools and utility gear.
- `Varios`: everything else; effectively no special filter.

This is especially important for consumables versus fixed supplies; do not force the player to scroll the entire catalog.

### Weight bar

- Shows the final active plan only, not alternatives.
- Includes active apparel, primary equipment, and inventory counts.
- Use `MassUtility`/`Thing` mass or the equivalent additive sum for the neutral editor.
- Label the unit (`kg`).
- Non-blocking: it never prevents saving or assigning.
- Recommendation reference: average adult human; the reference value is 75, but use `MassUtility.Capacity` from a neutral adult template, never a hardcoded 75.
  - Option A: total weight only.
  - Option B: total + neutral adult recommendation.
  - Option C: use a selected pawn/WorkType capacity when the editor is opened from a pawn context.
- Do not count inactive alternatives.
- Do not block assignment if above the recommendation.

### Data model

```text
OutfitPreset
  cells
  primaryEquipment: ThingDef
  inventory: List<InventoryEntry>
    def: ThingDef
    count: int
```

- Plan by def/count; reserve actual `Thing` at execution time.
- `equipment.Primary` and inventory are separate.
- No `SlotKey` for equipment or inventory.
- Metadata can show the selected equipment/inventory item; no body coverage rows.

### Metadata and comparison

- Equipment/inventory selected items can be inspected in Detail 60.
- Summary 40 does not invent body armor for equipment.
- Compare is limited to two equipment/inventory info panes for now.
- If apparel comparison remains, it stays a separate later feature.

## 6.4 Empty assignment, stat attribution, localization

### Empty hand or inventory

- Empty means **no assignment**, not “remove everything”.
- Do not remove current carried items just because the preset has no inventory entry.
- Do not treat missing inventory as an explicit empty list.
- If another active set/preset includes inventory, the sets must not silently fight.
- Push/overlap inventory behavior: **union**. Counts for the same def sum; no last-wins.
- Clothing/equipment conflicts are resolved by set order and coverage, not by deleting inventory.

### Stat attribution in Summary

The 40 summary should show **which item(s) contribute** each stat or armor value, similar to Loadout.

- A stat row can list source items.
- Tooltip shows the contribution per item.
- Armor rows show the layer and item that provides the value.
- Do not show an unattributed number.

### Localization

Translate Ordered Work UI strings through RimWorld keyed language files.

Translate:

- tab/family labels;
- row labels;
- buttons;
- statuses and errors;
- metadata labels;
- weight/recommendation labels;
- equipment/inventory labels;
- tooltips.

Do not translate:

- `ThingDef` defNames;
- mod names;
- user-entered preset/row names.

Use the game-localized labels for defs and stats:

- `ThingDef.LabelCap`;
- `BodyPartGroupDef.LabelCap`;
- `ApparelLayerDef.LabelCap`;
- `StatDef.LabelCap`;
- `StuffCategoryDef.LabelCap`;
- `QualityUtility.GetLabel(...)`.

## 6.5 Localization key groups

All user-visible UI strings go through RimWorld keyed language files. Do not translate def names or user text.

| Group | Examples | Why |
|---|---|---|
| Menu | `OrderedWork.Equipment.Menu.Assign`, `.Create`, `.View` | WorkType palette actions |
| Editor | `.Editor.Save`, `.SaveAs`, `.Delete`, `.Close`, `.Unsaved` | Core actions |
| Tabs | `.Tabs.Skin`, `.Middle`, `.Outer`, `.Utility`, `.Head`, `.Accessory`, `.Exosuit`, `.General` | Layout labels |
| Rows | `.Rows.Body`, `.Head`, `.Neck`, `.Torso`, `.Arms`, `.Waist`, `.Legs` | Body-region labels |
| Metadata | `.Metadata.Summary`, `.Detail`, `.Compare`, `.BodyParts`, `.Global` | Panel labels |
| Status | `.Status.Equip`, `.Empty`, `.Missing`, `.Uncovered`, `.KeptCurrent`, `.Blocked`, `.Unavailable` | Cell/item states |
| Equipment | `.Equipment.Primary`, `.Equipment.None`, `.Equipment.Clear` | Primary-hand UI |
| Inventory | `.Inventory.Add`, `.Remove`, `.Count`, `.Search`, `.Consumables`, `.Utilities`, `.Misc` | Inventory strip |
| Weight | `.Weight.Total`, `.Weight.Reference`, `.Weight.Recommended` | Weight bar |
| Config | `.Config.WarnMissing`, `.Config.RestorePrevious`, `.Config.ShowEmpty` | Mod settings |
| Suggestions | `.Suggestions.All`, `.Suggestions.Cell`, `.Suggestions.EmptyCell`, `.Suggestions.NoFilter`, `.Suggestions.Count` | Candidate panel clarity |
| Tooltip | `.Tooltip.Source`, `.Tooltip.Coverage`, `.Tooltip.Blocked` | Tooltip fragments |

Use `LabelCap` for dynamic defs/stats/body groups/stuff categories and `QualityUtility.GetLabel` for quality. Those are already localized by the game and mods.

## 7. Open questions

1. **Pawn set management UI**: deferred; the metadata does not own it.
2. **Exact localization key names**: final naming pass before implementation.

## 8. Non-goals

- No material/quality optimization in v1.
- No combined armor formula invented outside vanilla.
- No three-way comparison.
- No pawn-specific stat simulation in the neutral editor.
- No new top-level screen for equipment or inventory.
- No full inventory management beyond the planned carried items.
- No stock/ownership filter in the neutral editor.
