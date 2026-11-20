# Clothing stack — Phase 1

Branch: `feat/clothing-stack`

## Specs

- S1: A clothing preset is a flat ordered list of rules.
- S2: Rule phase order is `Locked`, `Emergency`, `Work`.
- S3: A rule targets apparel or a tool.
- S4: Apparel rules select by body slot and body layer; tools select by tool slot.
- S5: A rule can contain multiple ordered candidate defNames.
- S6: The ApparelPolicy is authoritative; a candidate it denies is never selected.
- S7: A denied Locked rule produces a policy-contradiction conflict and is not forced.
- S8: A lower-priority rule cannot replace an already-filled slot/layer.
- S9: Multiple apparel rules can fill different body layers of the same slot.
- S10: Unassigned slots remain Free; Phase 1 does not invent a Free rule.
- S11: The resolver is pure and returns selections, conflicts and free slots.
- S12: No jobs, pathfinding, equip/return, UI or save migration in Phase 1.

## Tasks

- T1: Add pure clothing models in `OrderedWork.Core`. Done.
- T2: Add pure resolver and slot-key logic. Done.
- T3: Add tests for phase order, policy denial, layers, duplicate slots, tools and free slots. Done (9 tests).
- T4: Build and run all tests; commit the work unit. Done.

- T5: Add `ClothingPresetStore` JSON persistence. Done.\n- T6: Add nullable RimWorld adapter for policy/defs/slots. Done.\n- T7: Add read-only Details Clothing preview. Done.\n- T8: Add shared clothing editor and Work Control overview. Done.\n- T9: Add searchable ThingDef candidate picker. Done.\n- T10: Add editor slot grid, clothing icons and essential tooltips. Done.\n\n## Log

- L1: User requested implementation after approving the flat model: presets contain ordered rules; `Locked > Emergency > Work > Free`; policy is law; tool rules included.
- L2: Phase 1 is intentionally model/resolver only; no execution or UI.\n- L3: Build 0/0; 37 tests pass; resolver handles null inputs and ApparelPolicy denial.\n- L4: Phase 1.5 adds JSON persistence and a read-only ThingDef/ApparelPolicy adapter. 39 tests pass.\n- L5: Read-only Details preview uses a 60-tick cached context/resolution and does not equip.\n- L6: WorkType assignment stored in `WorkTypeConfig.ClothingPresetId`; editor and Work Control overview are functional; no equip jobs.\n- L7: Candidate picker searches ThingDefs by label/defName and appends one candidate at a time.\n- L8: Editor grid selects/creates rules by slot; icons use ThingDef.uiIcon; tooltips expose label, layers, body parts and description.
