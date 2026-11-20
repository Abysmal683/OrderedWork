# Ordered Work — Policy Data Design

Status: work/schedule/zone/clothing storage foundations are implemented as described below. Clothing application, equipment, bulk zone and automatic policy application remain pending.

This document defines the logical data model for per-pawn policy:
work order, schedule, zone and the later clothing layer. It is a relational
model in the logical sense; RimWorld does not run SQLite for this. Global
presets are JSON files; per-save state is serialized through Scribe in the save.

## 1. Source of truth

There are three different things that must not be confused:

| Layer | Owned by | Persisted in | Purpose |
| --- | --- | --- | --- |
| Global presets | Ordered Work | JSON under `Config/OrderedWork/` | Reusable intent: work profile, schedule preset, clothing rules |
| Per-save bindings and overrides | Ordered Work | `OrderedWorkGameComponent` in the save | Which preset a pawn links to and which sparse exceptions apply |
| Applied effect | Vanilla RimWorld | Pawn fields and save | What the game actually executes now |

The vanilla field is the source of truth for the **current effect**. Ordered Work
is the source of truth for the **intent**. We never treat a mirrored copy as
authoritative.

Examples:

- `Pawn_WorkSettings.priorities` is the applied work effect.
- `Pawn.timetable` is the applied schedule effect.
- `Pawn.playerSettings.allowedAreas` is the applied zone effect.
- `Pawn.outfits.CurrentApparelPolicy` is the applied clothing effect.
- `PawnPolicy` says which preset should be applied and which exceptions exist.

A manual change in a vanilla UI is not automatically the same thing as an
Ordered Work override. It is either:

- a deliberate manual override, if the user edits through Ordered Work; or
- drift, if the user edits vanilla directly while a link is active.

The first implementation records drift and exposes it; it does not silently
rewrite the pawn.

## 2. What vanilla can and cannot store

### Work

Can store:

- one numeric priority per `WorkTypeDef` per pawn;
- the current set of enabled/disabled WorkTypes;
- the vanilla priority map, saved by the game.

Cannot store:

- per-WorkType time windows;
- per-WorkGiver per-pawn order;
- a separate priority for normal/emergency work;
- two independent work presets on one pawn.

`WorkGiverDef` is global. Per-pawn giver behavior requires Ordered Work data and
an application layer; it cannot be represented as a vanilla priority.

### Schedule

`Pawn_TimetableTracker.times` is a list of 24 `TimeAssignmentDef` values.

Can store:

- the vanilla Work/Joy/Sleep/Meditate/anything assignment for each hour;
- modded `TimeAssignmentDef` values;
- one timetable per pawn, independent of map.

Cannot store:

- WorkType priority windows;
- a second timetable;
- per-map timetables;
- per-WorkType schedule overrides.

WorkType windows need their own table and a runtime compiler. They are not
stored in vanilla.

### Zone

`Pawn_PlayerSettings.allowedAreas` is a `Dictionary<Map, Area>`.

Can store:

- one allowed area per pawn, per map;
- `null`/unrestricted;
- references to existing `Area_Allowed` objects;
- map removal notifications.

Cannot store:

- zone creation;
- a zone per WorkType;
- zone data independent of a map;
- a stable area name across maps.

Area identity must use the map and the area identity. Labels can be renamed and
can collide.

### Clothing

`Pawn_OutfitTracker.CurrentApparelPolicy` is one `ApparelPolicy`.

Can store:

- one active apparel policy/filter per pawn;
- policy identity (`id`, `LoadKey`, label);
- apparel that is currently worn.

Cannot store:

- a different outfit per WorkType;
- automatic per-job switching;
- instant equip guarantees;
- a parallel apparel resolver.

Per-WorkType clothing is Ordered Work intent plus a swap/equip layer. It is
deferred until the policy model is stable.

## 3. Logical tables

### Global tables (JSON)

These are reusable and are not tied to one save.

```text
work_preset
  id              stable UUID/string, primary key
  name            display name
  schema_version  integer
  created_utc
  work_types      normalized work-order rows
  givers          normalized giver rows
```

The existing `WorkConfigProfile` is the first implementation of this table.

```text
schedule_preset
  id              stable UUID/string, primary key
  name            display name
  schema_version  integer
  slot_0..slot_23 TimeAssignmentDef defName
```

Only the 24 vanilla timetable slots are stored. WorkType windows are a separate
future table.

```text
clothing_preset
  id              stable UUID/string, primary key
  name            display name
  schema_version  integer
  rules           WorkTypeDef -> apparel policy/outfit reference
```

Clothing is not implemented in the first policy slice.

### Per-save tables (Scribe XML)

These are scoped to one save.

```text
pawn_policy
  pawn_ref        stable pawn identity/reference
  work_preset_id  FK -> work_preset, nullable
  schedule_preset_id FK -> schedule_preset, nullable
  clothing_preset_id FK -> clothing_preset, nullable
  schema_version
```

The current `PawnWorkOrder` is the ancestor of this row. Its full `order` list is
a **snapshot override**, not a sparse link.

```text
pawn_work_override
  pawn_ref
  work_type_def
  rank/priority
  enabled
  locked
  source          Manual | Preset | Schedule | Emergency
  PRIMARY KEY (pawn_ref, work_type_def)
```

Only changed WorkTypes are stored. A pawn with no changes has no rows and simply
uses the base order.

```text
pawn_schedule_override
  pawn_ref
  hour            0..23
  assignment_def  TimeAssignmentDef defName
  source
  PRIMARY KEY (pawn_ref, hour)
```

Sparse hourly exceptions. The base schedule comes from
`schedule_preset_id`.

```text
pawn_zone
  pawn_ref
  map_unique_id   Map.uniqueID
  area_id         Area.ID / stable area identity
  source
  PRIMARY KEY (pawn_ref, map_unique_id)
```

A pawn can have one row per map. Missing area is a referential-integrity alert,
not a deletion.

```text
pawn_clothing_override
  pawn_ref
  work_type_def
  apparel_policy_id
  source
  PRIMARY KEY (pawn_ref, work_type_def)
```

Not implemented yet; reserved so clothing does not force a second migration.

## 4. Link vs override

### Link

A link is a reference to reusable intent or to an existing vanilla object:

- pawn -> `work_preset`;
- pawn -> `schedule_preset`;
- pawn -> `clothing_preset`;
- pawn/map -> existing `Area`;
- clothing rule -> existing `ApparelPolicy` or external loadout provider.

A link does **not** copy the target. If the preset changes, linked pawns see the
new resolved value on the next apply.

### Override

An override is a local exception that wins over the linked base:

- pawn + WorkType rank/enabled;
- pawn + hour timetable;
- pawn + map zone;
- pawn + WorkType clothing policy.

Overrides are sparse. We do not copy a full schedule or full WorkType list into
every pawn.

### Precedence

```text
Manual lock
  > Emergency
  > Schedule policy
  > Zone policy
  > linked preset/base
```

The exact ordering between schedule and zone is domain-specific:

- schedule changes **when** work is attempted;
- zone changes **where** it can happen;
- clothing changes **what** is worn.

They are constraints/boosts, not competing sources of the same value.

## 5. What the current code has

### Already per-save

- `PawnWorkOrder` in `OrderedWorkGameComponent`;
- one full `List<string> order` per edited pawn;
- a raw `Pawn` Scribe reference;
- cleanup for destroyed pawns;
- resurrection reapply.

This is a **full snapshot override**. It is not yet the normalized model above.

### Already global

- `OrderedWorkSettings` ModSettings XML: pins, passion weights, UI flags;
- `WorkConfigProfile` JSON: WorkTypes, giver ownership/order, split/merge;
- `reference.json`: baseline comparison.

### Not implemented

- `PawnPolicy`;
- sparse work overrides;
- schedule presets;
- zone bindings;
- clothing rules;
- map-aware zone storage;
- drift detection;
- policy migration to link + override.

## 6. Migration from the current work order

Do not break existing saves.

Phase 1 keeps `PawnWorkOrder.order` readable and adds policy fields:

```text
PawnWorkOrder
  pawn
  legacyOrder
  workPresetId?
  workOverrides?
```

Resolution:

1. If no policy rows exist, use `legacyOrder` exactly as today.
2. If a policy exists, resolve base preset + sparse overrides.
3. On first policy edit, snapshot the current resolved order as a legacy
   compatibility value, then switch to link + overrides.
4. Keep the old field until a schema migration proves it is no longer needed.

This gives backward compatibility without making every pawn carry a full copy
forever.

## 7. Identity rules

- Presets: generated stable ID, never reuse.
- WorkTypes/Givers: `defName` string, never numeric enum.
- Time assignments: `TimeAssignmentDef.defName`.
- Pawns: stable save identity plus Scribe reference; do not use display name.
- Maps: `Map.uniqueID`.
- Areas: map-scoped `Area.ID`/unique load identity, not label.
- Apparel policies: policy ID/load key, not a copied ThingFilter.

## 8. Referential integrity and alerts

Missing references must not crash and must not silently delete player data.

- Missing schedule assignment: skip that slot, preserve the override, alert.
- Missing area on current map: keep the binding, show one semi-persistent alert:
  `<pawn>: assigned zone <zone> is missing on the current map.`
- Missing work type: keep the override, ignore it at apply time.
- Missing preset: keep the binding, mark the pawn as unresolved.
- Removed map: keep zone rows for audit; map is not current.
- Removed mod: preserve defName rows so re-enabling restores them.
- Destroyed pawn: follow `keepDestroyedPawnOrders`; do not resurrect orphan data.

Alerts are alerts, not temporary messages or cards, so the player can resolve
them later.

## 9. Persistence choice

### Global presets: JSON

Use JSON for:

- work preset/profile;
- schedule presets;
- clothing presets/rules.

Reasons:

- nested arrays and versions;
- explicit import/export;
- human-readable diffs;
- not tied to one save.

### Per-save state: Scribe XML

Use `OrderedWorkGameComponent` for:

- pawn bindings;
- sparse work/schedule/zone/clothing overrides;
- policy schema version.

Reasons:

- save branching is handled by the game;
- object references resolve correctly;
- no separate file to keep in sync with a save;
- no duplicate global state.

### ModSettings XML

Keep only global settings:

- recommendation pins;
- passion weights;
- UI preferences;
- diagnostics.

Do not put per-pawn data or reusable presets into ModSettings.

## 10. Compilation and performance

Runtime model:

```text
base preset
  + sparse overrides
  = compiled pawn policy
  -> write vanilla effect
```

- Compile once per pawn/domain and cache.
- Invalidate on preset change, override change, map change, def reload.
- Never resolve all pawns every tick.
- Apply on load/spawn/change, not every frame.
- Store only rows that differ.
- Schedule presets are 24 slots each, not 24 per pawn.
- Zone rows are one per pawn per map.
- Clothing rows are one per changed WorkType.

## 11. Slice plan

1. Define `PawnPolicy`, schema version and per-save tables; keep legacy order.
2. Add schedule presets and the timetable adapter.
3. Add zone binding per map and the missing-area alert.
4. Add work override rows; migrate legacy full snapshots.
5. Add drift detection and manual-lock semantics.
6. Add clothing rules only after the policy model is stable.

## 12. Non-goals

- No runtime `WorkGiverDef` clones.
- No parallel zone creation.
- No parallel timetable.
- No per-WorkType hour windows in the first schedule slice.
- No full clothing resolver or instant equip.
- No automatic overwrite of manual vanilla edits.
