# Equipment assignments — reference

Persistence split:

```text
PawnWorkOrder
  mandatoryEquipmentSetIds
  emergencyEquipmentSetIds
  principalEquipmentSetIds
  equipmentOverrides

WorkTypeConfig
  EquipmentSetId
```

## Pawn sets

- Mandatory, Emergency and Principal are ordered lists of set ids.
- Duplicate and empty ids are removed during normalization.
- WorkType owns one `EquipmentSetId` globally.
- Pawn assignment does not edit the saved set.

## Overrides

`PawnEquipmentOverride` is absolute per pawn.

Kinds:

- `forced_apparel`: requires body, group, layer and def.
- `primary_equipment`: requires def.
- `inventory`: requires def; `count` may be negative for forbidden.

Overrides are normalized by kind + body/group/layer + def; the last value wins.

## Save behavior

- Fields are additive to `PawnWorkOrder`; old saves load with empty lists.
- No legacy clothing preset migration.
- No UI yet; T8b mounts this model.

Related: `../explanation/clothing-equipment-design.md`, `../how-to/edit-equipment-sets.md`.
