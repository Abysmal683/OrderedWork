# Ordered Work — Design Notebook

> Historical design notebook. Current contracts live in `docs/reference/`; use this file for rationale and rejected paths.

Historical design notebook. Current contracts are in `docs/reference/`. The implementation snapshot is `0.3.0-alpha.317`; Zones and Schedule have implemented slices, while clothing application and assigned-workbench priority remain pending.

## Goal

One RimWorld 1.7 work mod that combines:

1. Job/work-type reordering.
2. Rule-based automatic assignment.
3. Priority range beyond vanilla 1–4.
4. Hourly scheduling.
5. A per-pawn panel for direct, drag-ordered priorities.
6. Split/merge of work types without ever deleting work.

Primary UX: order, not numbers. Fast to configure, readable, optionally autonomous.

## Approved policy decisions (alpha.96)

These decisions supersede older exploratory notes below.

### UI

- Work Control keeps one integrated tab; no separate global `Original` view.
- Top-level views: `Work`, `Policies` (`Scheduler` + `Zones`), and later `Clothing`.
- Returning to vanilla is an integrated per-view control, not a separate mode.
- Clothing policy configures clothing by WorkType; per-pawn clothing overrides belong in pawn detail.
- Scheduler and Zones are grouped because both are per-pawn policy views.

### Zone

- Do not create zones. Only assign existing `Area_Allowed` areas.
- One allowed area per pawn, per map.
- Store per `Map.uniqueID` and area identity.
- Missing assigned area on the current map produces one semi-persistent alert:
  `<pawn>: assigned zone <zone> is missing on the current map.`
- Do not warn that an area blocks WorkTypes.
- Room assignment is discarded.
- Bulk/emergency assignment is allowed; no automatic per-WorkType zone.

### Schedule

- Vanilla `Pawn.timetable` (24 `TimeAssignmentDef` slots) is the source of truth.
- The editor uses two parallel lanes (approved Option B): current vanilla timetable plus a selected preset/working draft ghost.
- Save/load named `SchedulePreset` objects containing only the 24 vanilla assignments.
- Presets are global in `Config/OrderedWork/schedules/<id>.json` (one file per preset); apply to selected pawns.
- The workflow is: load/choose preset -> preview -> apply to selected pawns.
- Do not add per-WorkType hour windows in the first schedule implementation; keep that as a later optional layer.
- Do not create a second parallel timetable.

### Override foundation

Define the sparse per-pawn override model before the schedule UI:

```text
PawnPolicyOverride
  pawnId
  manualLocks
  source: Manual | Schedule | Zone | Emergency
  areaByMap
  schedulePresetId
```

Precedence:

```text
Manual lock > Emergency > Schedule/Zone policy > base lane
```

Full override UI is later; the contract and persistence come first.

### Restoration

- Disabling Ordered Work removes our configuration, not the last vanilla state applied.
- Scheduled work windows are cleared.
- The last applied zone remains if it was the dynamic/current zone; automatic zones are removed.
- Manual player changes are never overwritten during restoration.

## Layers

### Base (global definition layer)

- Applies to every pawn.
- Global order of work types and of work givers inside a type.
- Same visual column/row order for all pawns.
- Per-pawn priority assignment still exists, but no per-pawn order.
- Storage: `WorkTypeDef.naturalPriority` and `WorkGiverDef.priorityInType` (defs), or a small global order list applied at load.
- Optimization: cheapest. No per-pawn config, no save growth, no per-tick work.
- Risk: changing defs at runtime invalidates caches; use load-time or explicit rebuild.
- References: Work Type Editor, Personal Work Categories, Some More Jobs, Job Splitter, Complex Jobs.

### Group (profile/role layer)

- A named profile bundles:
  - Ordered jobs (work types and/or work givers), first wins.
  - Optional apparel/loadout group.
  - Optional allowed area, drug policy, food policy.
  - Optional schedule/time windows.
  - Optional rules/automation and emergency state.
- Assigned to many pawns; a pawn may hold several ordered profiles.
- Storage: profile definitions global (GameComponent or Def), pawn→profile assignments per pawn.
- Optimization: profile definitions are shared; per-pawn state is small; effective order is compiled and cached.
- Correction: WorkRoles does roles/ordered jobs, bill support, per-colonist panel, time/location rules — it does **not** manage apparel. Better Pawn Control manages work/schedule/outfits/areas/drugs/food as policies; Compositable Loadouts manages apparel tags/loadouts/states. Fuse these concepts in our own Profile, reference their data, do not claim WorkRoles does apparel.
- Do not run WorkRoles + Better Pawn Control work policies together: WorkRoles owns priorities and blocks third-party SetPriority; BPC writes priorities. Fuse in our layer or disable one side.
- References: WorkRoles, Better Pawn Control, Compositable Loadouts, Grouped Pawns Lists.

### Individual (specialty layer)

- Same role, different internal order per pawn.
- Example: Engineer and Blacksmith share Crafting; the Blacksmith puts Smithing/forge before other work.
- Per-pawn overrides:
  - Work-type order/priority.
  - Work-giver order (advanced/deferred).
  - Hour windows.
  - Locks/manual overrides.
  - Specialty label.
- Storage: sparse overrides on top of the profile; only changed entries.
- Optimization: compile effective order into per-pawn arrays/lists; cache; invalidate on change/hour/location.
- References: Sleek Pawn Focus Card, Pawn Centric Work Priorities, Work Tab per-pawn/per-giver, WorkRoles compiled order.

### Automation

- Rules can live at profile or individual level.
- Conditions: skill, passion, learning, trait, gene, xenotype, health/capacity, age, ideoligion, title, role, hour, location, colony need.
- Fitness: skill, passion, learning, urgency, work speed/yield, etc.; later entries break ties.
- Actions: assign profile/role, set order/priority, enable/disable, set schedule, apply emergency state.
- Amounts: min/target workers, count or percent, critical, specialist, commitment.
- Modes: Observe, Suggest, Semi, Auto.
- Locks: per cell, per pawn, per work type; automation never overwrites locked values.
- Cadence: hourly or configurable; manual refresh; never per tick.
- References: Automatic Work Assignment, Work Manager, Adaptive Work Priorities, Adaptive Priorities, Smart Priorities, AutoPriorities, Automated Work Assignment.

## Storage model

### Proposed

- `Profile` definitions (global, per save):
  - id, label, ordered job entries, optional loadout/outfit/area/drug/food references, time windows, rules, color/icon.
- `PawnProfileSet` (per pawn, saved):
  - ordered profile assignments with state and pinned flag.
- `PawnWorkOverrides` (per pawn, saved, sparse):
  - work-type order/priority overrides; optional giver overrides; hour overrides; locks; specialty label.
- `CompiledPawnOrder` (runtime cache, not saved):
  - `int[] priorityByWorkTypeIndex` (0 = off, 1..Max).
  - ordered `WorkGiver` lists (normal/emergency).
  - attribution map (giver → profile) for UI.
- `WorkSettings` (ModSettings):
  - max priority, default priority, UI preferences, automation defaults.
- Base order:
  - applied to defs at load; no per-pawn storage.

### Effective order

1. Start from base order.
2. Apply profile order (first profile wins per job).
3. Apply individual overrides.
4. Apply time window.
5. Apply automation only where unlocked.
6. Compile to `priorityByWorkTypeIndex` and ordered giver lists.

### How the game sees it

- `GetPriority` prefix returns compiled per-work-type rank.
- Ordered giver lists feed `CacheWorkGiversInOrder`/the work-giver manager.
- Vanilla 0–4 priority map is mirrored for removal/fallback.
- Batch writes; notify once; avoid per-tick invalidation.
- Time changes invalidate at hour boundaries; location changes on map change.
- No per-pawn × all givers × 24 arrays by default.

## Existing mods: storage and optimization

| Mod | Storage | Granularity | Save cost | Runtime cost |
|---|---|---|---|---|
| Work Tab | Per-pawn `WorkPriority` per WorkGiver with `int[24]`; saved as 24-digit string; Harmony Get/Set prefixes | Per WorkGiver + hour | High | Dictionary/array lookup on frequent GetPriority |
| PriorityMaster | No custom storage; vanilla `Pawn_WorkSettings` DefMap; transpiles max/default constants 3/4 and patches Lowest/Default fields | Per WorkType | Low | Negligible |
| WorkRoles | Global `RoleStore` roles + per-pawn `PawnRoleSet` assignments; compiled per-pawn `int[] PriorityByIndex` + `VanillaByIndex` + WorkGiver lists; runtime cache | Role/order + per WorkType rank; giver order via compiled list | Low | O(1) array reads; invalidation/revision gates; hourly/location boundaries |
| WorkRoles priority ownership | `PrioritySetWatcher` blocks third-party SetPriority on managed pawns; writes vanilla DefMap directly for fallback | — | — | Conflict with priority setters |
| Automatic Work Assignment | `WorkSpecification` list in map manager/settings; conditions/fitness/amounts/priorities; writes vanilla priorities | Per spec/worktype | Low | Periodic/manual; caches |
| Work Manager | Per-save settings; writes vanilla priorities; shifts | Per worktype | Low | Hourly/daily; 30+ work types bug |
| Adaptive Work Priorities | Per-pawn learned affinities + settings; writes priorities | Per cell | Medium | Hourly; caches |
| Adaptive Priorities | Per-pawn/settings; writes priorities | Per worktype | Medium | Hourly; caches |
| Enhanced Work Tab | UI tab + presets/profiles; auto-optimizer/time/zone; 1–9 | Per worktype/time | Medium | Event-driven UI |
| Sleek Work Priorities | UI focus cards; existing priorities; per-job order; 4/5/6/9 | Per worktype/job | Low | UI caches |
| Work Type Editor | Def changes + presets; live | Global defs | Low | Def invalidation; live risk |
| Better Pawn Control | Policies for work/schedule/outfits/areas/drugs/food; one-click; per-save | Per policy | Low | One-click application |
| Compositable Loadouts | `LoadoutManager` GameComponent (tags), per-pawn loadout; minimal AI patches | Per pawn tags | Low | Staggered resolution; performant |
| Job Splitter / Complex Jobs / SMJ | XML defs + XML Extensions settings | Global defs | Low | No runtime |
| Mech Work Tab | UI/vanilla mech priorities | Per mech | Low | UI |

## Decision log

### Accepted

- D1: Replace the Work tab for the full vision (Base + Group + Individual + automation + time + range).
- D2: Three layers: Base, Group, Individual.
- D3: Order is primary UI; numbers are secondary/hidden.
- D4: Base order is global def order; no per-pawn storage.
- D5: Group uses profiles/roles + per-pawn assignments; profile definitions shared.
- D6: Individual uses sparse per-pawn overrides on top of profiles; specialty.
- D7: Priority range native, configurable; default 9, max 99.
- D8: Time uses profile active hours + per-pawn overrides, invalidated at hour boundaries; not per-giver × 24 arrays.
- D9: Compile effective order into per-pawn arrays + ordered giver lists; cache; no per-tick scans.
- D10: Automation levels (Observe/Suggest/Semi/Auto), locks, hourly/manual; never overwrite locks.
- D11: Split/merge/reorder defs without deleting work; copy tags/skills/life stages/mech; duplicate trait disabled/required work types.
- D12: Clean-room implementation with credits; no copying NC/unknown code.
- D13: Soft integrations for apparel/loadouts/areas/drugs; reference, do not reimplement.
- D14: Detect incompatible tab/priority mods and warn; do not silently conflict.

### Discarded

- X1: Full compatibility with Work Tab/Better/Enhanced/WorkRoles/Compact/PriorityMaster/MWP while replacing the tab. Reason: priority ownership/tab ownership conflict.
- X2: Per-pawn × all WorkGivers × 24 arrays by default. Reason: save/CPU cost; Work Tab's model is high granularity but heavy.
- X3: Storing per-pawn order in WorkTypeDef/WorkGiverDef. Reason: defs are global.
- X4: Live def editing by default. Reason: cache invalidation and save risks; Work Type Editor shows live is possible but costly.
- X5: Automation on every tick. Reason: performance; hourly/dirty events suffice.
- X6: Copying Work Manager (CC-BY-NC-SA), PriorityMaster (no visible license), or unknown-source mods. Reason: licensing; clean-room.
- X7: Running WorkRoles + Better Pawn Control work policies together. Reason: both own/write priorities; WorkRoles blocks third-party SetPriority.
- X8: Forcing apparel/loadout changes from a role without opt-in. Reason: apparel mods own that; avoid surprise gear changes.
- X9: Per-pawn per-giver numeric priorities in v1. Reason: per-worktype rank + compiled giver list covers the use cases; defer.
- X10: Numeric priorities as the primary UI. Reason: user wants order/drag; numbers remain internal.
- X11: One monolithic role+apparel def with hard dependency on the apparel mod. Reason: soft integration; profiles reference external data.
- X12: Hard dependency on XML Extensions. Reason: own settings/data; XML Extensions optional for legacy XML mods.

### Deferred

- Y1: Per-pawn per-giver order overrides.
- Y2: Live def editor (reorder/split/merge without restart).
- Y3: Mech tab.
- Y4: Import/export of profiles/rules.
- Y5: Multiplayer validation.
- Y6: Work tab grouping/search/coverage/undo polish.
- Y7: Compatibility adapters for external automation/priority mods.
- Y8: Per-giver time schedules.

## Integration and credits

- Base/def layer: Work Type Editor, Personal Work Categories, Some More Jobs, Job Splitter, Complex Jobs.
- Group/roles: WorkRoles (roles, ordered jobs, per-colonist panel, bill support, time/location rules), Better Pawn Control (work/schedule/outfit/area/drug/food policies), Compositable Loadouts (tags/loadouts/states), Grouped Pawns Lists.
- Individual/UI: Sleek Work Priorities (Focus Cards, Pawn Focus Card, scale, grouping, coverage), Pawn Centric Work Priorities, Work Tab.
- Priority range: PriorityMaster, More Work Priorities, Sleek, Enhanced Work Tab.
- Time: Work Tab, Enhanced Work Tab, WorkRoles, Work Manager, Adaptive Priorities.
- Automation: Automatic Work Assignment, Work Manager, Adaptive Work Priorities, Adaptive Priorities, Smart Priorities, AutoPriorities, Automated Work Assignment.
- Def editor: Work Type Editor, Personal Work Categories, SMJ, Job Splitter, Complex Jobs.
- Mech: Mech Work Tab, Chinese pure-UI mech panel.
- UX: Grouped Pawns Lists, Useful Marks, Numbers, RimHUD.
- Licenses: Work Tab MIT (code) + CC-BY-SA 4.0 (original content); Automatic Work Assignment MIT; Work Manager CC-BY-NC-SA; PriorityMaster no visible license; WorkRoles repo license must be checked before reuse. Clean-room preferred.

## Open questions

1. Apparel provider: Better Pawn Control (policies) or Compositable Loadouts (tags/loadouts), or both as optional providers?
2. Profile set: can a pawn hold multiple ordered profiles (WorkRoles style), or one profile with multiple roles inside?
3. Apparel application: profile-driven automatic, or explicit opt-in per profile?
4. Per-giver individual overrides: v1 or deferred? Recommendation: deferred; per-worktype specialty first.
5. Default automation level: Observe/Suggest/Semi; recommendation: Suggest.
6. Replace tab confirmed? Recommendation: yes, with explicit incompatibilities.
7. Max priority default: 9 with 99 option; recommendation: 9.

## Phases

- v0.2 Core: Base + Group + Individual data, compiled order, per-pawn panel, priority range, time windows, locks; no automation.
- v0.3 Automation: rules engine, levels, profiles/roles, emergency, hourly evaluation.
- v0.4 Full Work tab: grid, drag, focus cards, search, groups, coverage, presets.
- v0.5 Def editor live: reorder/split/merge without deletion, presets, migrations.
- v1.0: compatibility adapters, performance, localization, credits, multiplayer.

## Data model: presets, links, overrides, role slots

Core decision: database-like, normalized, stable IDs. Presets are the durable source of truth; pawns link to them and store only sparse overrides.

### Entities

- `ProfilePreset`
  - `id` (GUID), `name`, `schemaVersion`, `revision`, `parentId` (optional inheritance), `tags`, `color`.
  - Ordered job entries: `{kind: WorkType|WorkGiver, defName, order, priority?, enabled}`.
  - `scheduleTemplateId`, `loadoutRef`, `areaRefId`, `drugPolicyId`, `foodPolicyId`, `ruleIds`.
  - Soft-delete/tombstone; never delete work types or givers.
- `RoleSlot` (colony-level, optional but recommended)
  - `id`, `name`, `presetId`, expected skills/capabilities, priority, area/loadout refs, `assignedPawnId?`, `state`, `autoAssign`.
  - Survives pawn death; becomes vacant; auto-binds a replacement if enabled.
- `PawnBinding`
  - `pawn`, ordered `presetIds`, optional `roleSlotId`, state, pinned, `lastAppliedRevision`.
- `PawnOverrides` (sparse, copy-on-write)
  - Work-type order/priority overrides, optional giver overrides, hour overrides, loadout/area/drug overrides, locks, specialty label.
- `ScheduleTemplate`
  - `id`, 24-hour blocks, exceptions, emergency windows.
- `AutomationRule`
  - `id`, scope (colony/preset/slot/pawn), enabled, conditions, fitness, actions, mode (Observe/Suggest/Semi/Auto), priority.
- `CompiledPawnOrder` (runtime only, not saved)
  - `int[] priorityByWorkTypeIndex`, ordered normal/emergency WorkGiver lists, attribution map.
- `BaseOrder`
  - global work-type/giver order; defs or a global list applied at load.

### Link vs copy

- Default: **link** (`PawnBinding.presetIds`) + **sparse overrides** (copy-on-write).
  - Editing a preset updates linked pawns; overrides survive.
- **Fork** creates a new `ProfilePreset` with a new `id` for intentional divergence (pawn or group).
- Full per-pawn copies are discarded: duplication, drift, and save growth.
- Snapshots are for import/export/backup, not the live source of truth.

### Save vs recompute

- Saved: presets, role slots, bindings, overrides, schedule templates, automation rules, settings, base order if user-customized, schema version.
- Recomputed on load/change: effective order, priorities, giver lists, trait/incapacity filters, current-hour selection, availability, UI caches.
- Never save derived priorities as the source of truth; they are a projection.

### Death, imprisonment, new pawns

- Same pawn (downed, caravan, cryptosleep, prisoner): binding and overrides persist.
- Death: park the binding/overrides in `dormantPawnSets[pawn]` if a corpse can return; restore on resurrection; otherwise the role slot becomes vacant and can auto-bind a replacement.
- New pawn: auto-assign preset/role slot from rules/skills/defaults; no manual rebuild.
- Cleanup on corpse destruction; tombstones for manual recovery.
- Work type/giver/profiles removed: fallback to base order, keep unknown IDs, warn.

### Storage estimate

- 20 presets × ~100 job entries × ~20 bytes ≈ 40 KB.
- 100 pawns × binding ~50 bytes ≈ 5 KB.
- Sparse overrides ~100–400 bytes per pawn ≈ 10–40 KB.
- 20 role slots ~2 KB; 100 rules ~50 KB.
- Total ~100–150 KB, versus Work Tab-style per-giver × 24 arrays (~720 KB save strings and multi-MB runtime for 100 pawns).
- Runtime compiled order ~2–4 KB per pawn, not saved.

### Interface pieces

- Base: global work-type/giver order; drag; no per-pawn storage.
- Profiles: preset library/editor; ordered jobs; schedule/loadout/area/drugs; fork/copy; import/export.
- Role slots: colony roles, vacancy, expected skills, auto-assign.
- Pawns: grid + per-pawn card; link preset; overrides; specialty; time; automation.
- Automation: rules, test, levels, locks.
- Schedule: 24-hour timeline/templates.
- Loadout/area/drugs/food: references to Compositable Loadouts/Better Pawn Control/vanilla; opt-in application.

## Pillars and vanilla write-through

Principle: one source of truth per domain. Our mod owns presets, bindings, overrides, schedule templates and rules; the effects are written to vanilla tables, not mirrored into parallel systems.

### Pillars

1. **Schedule (horario)**
   - Vanilla target: `Pawn.timetable` (`Pawn_TimetableTracker`) with `GetAssignment`, `SetAssignment`, `CurrentAssignment`.
   - Two layers:
     - Vanilla timetable: Work / Joy / Sleep / Meditate / etc. per hour.
     - Work priority windows: our compiled hourly role priorities, applied during Work hours; do not invent a second timetable.
   - Precedence: if the timetable says Sleep/Joy, work priorities do not apply; if Work, role priorities apply.
   - Storage: `ScheduleTemplate` in the preset; per-pawn overrides sparse.

2. **Role/cargo**
   - Vanilla target: `Pawn.workSettings.priorities` plus `Pawn.GetDisabledWorkTypes`, `WorkTypeIsDisabled`, `WorkTagIsDisabled`.
   - Role can order jobs, enable/disable work, and set priorities; it **cannot grant** work a pawn is incapable of (traits/backstories/health).
   - Storage: ordered jobs in the preset; priority 0 for “won’t do”; capability remains vanilla.

3. **Equipment (equipamiento)**
   - Vanilla target: `Pawn.outfits.CurrentApparelPolicy` (`ApparelPolicy`); external target: Compositable Loadouts tags/loadouts; optional BPC outfit policy.
   - Equipment is a reference from the preset; application is opt-in and asynchronous (pawns equip over time; no instant guarantee).
   - Do not reimplement apparel resolution.

4. **Zone**
   - Vanilla target: `Pawn_PlayerSettings.allowedAreas` (`Dictionary<Map, Area>`), exposed as `AreaRestrictionInPawnCurrentMap` / `EffectiveAreaRestrictionInPawnCurrentMap`.
   - One allowed area per pawn, per map. Assign only existing `Area_Allowed`; never create a zone.
   - Store per `Map.uniqueID` plus stable area identity.
   - Missing assigned area on the current map = semi-persistent alert, not a temporary message/card.
   - Do not warn that an area blocks WorkTypes.
   - Do not create a parallel zone system; maps/areas are vanilla.

### Precedence and conflicts

- Timetable assignment (Sleep/Joy/Work) > work priorities.
- Role/capability > priorities: a disabled work type cannot be granted.
- Equipment and zone are constraints, not overrides of schedule/role.
- Manual locks beat automation; automation only touches unlocked cells.
- Emergency profiles can temporarily override schedule/loadout/area, then restore.

### Noise and scope

- Grouped Pawns Lists / Colony Groups: grouping and schedule UI; parallel state; not integrated. Grouping is a view/filter, not a data owner; RoleSlots/Profile tags cover our needs.
- Better Pawn Control: writes vanilla policies; use only as an adapter for outfit/area/drug/food, not as a second work/schedule owner.
- Compositable Loadouts: loadout provider; reference.
- WorkRoles: roles/priorities owner; concepts only; do not run together.
- Enhanced/Sleek/Better Work Tab: UI concepts; do not run together if we replace the tab.
- AWA/Work Manager/Adaptive: automation concepts; optional adapters later.
- Work Type Editor/Job Splitter/Complex/SMJ: def layer; compatible.
- PriorityMaster/More Work Priorities: native range; detect and avoid duplicate ownership.
- Work Tab: concept only; incompatible if we own priorities.

### MVP scope guard

- v0.2: work roles + schedule templates (vanilla timetable + priority windows) + bindings/overrides + death parking + per-pawn panel; equipment/area references only.
- v0.3: opt-in equipment/area application via adapters; RoleSlots.
- v0.4: automation rules.
- v0.5: full tab UI.
- Out of scope: parallel schedule/apparel/area/drug/food systems, zone creation, Grouped Pawns clone, WorkRoles compatibility, full tab/priority mod compatibility. Policy tabs assign vanilla state only.

## WorkRoles openness and license

- License: **AGPL-3.0**. Strong copyleft. Copying/adapting code or linking `WorkRoles.Core` makes our mod AGPL and requires corresponding source. Clean-room reimplementation avoids this.
- Data-open:
  - `RoleDef` is a `Def` with XML fields: ordered `entries` (`WorkType:DefName` or `WorkGiver:DefName`), `autoAssign`, `colorRef`, `iconPath`, `tuning`, `blocker`, `group`, `activeHours` (24-char bitstring), `locations`; explicitly supports third-party defs.
  - `RoleGroupDef` and `PaletteDef` are XML-definable.
  - `RoleIO`/`RoleFile` import/export to `WorkRoles.xml` under the user data root; roles, groups, palette, training paths, game/player settings; merge/overwrite; stable file IDs; round-trip tests.
  - `WorkRoles.Core` is a netstandard2.0 pure-logic library with tests; `WorkRoles` is the game/UI assembly.
  - Build instructions and tests are public.
- Not open as an API:
  - UI is a full `MainTabWindow_WorkRoles` replacement; no plugin/embed API.
  - `ExternalPawnFacts` and `RoleSetupBridge` are `internal`; not public extension points.
  - WorkRoles owns priorities for managed pawns (`PrioritySetWatcher` blocks third-party `SetPriority`) and replaces the Work tab; it cannot run alongside another priority/tab owner.
  - `DeathRoles` parks role sets in `dormantPawnSets[pawn]` for resurrection and robot station inheritance.
- Integration options:
  - **Clean-room** own `ProfilePreset` schema inspired by RoleDef fields; credit.
  - **One-way import adapter** for WorkRoles `WorkRoles.xml` exports: map roles/groups/activeHours/locations/blockers/tuning to our presets; no runtime dependency; credit.
  - **Own PresetDef XML** for modders (entries, groups, blockers, activeHours, locations, tuning, color/icon).
  - **Detect WorkRoles** and warn; do not run both; offer migration then disable one.
- Decision: clean-room + import/export compatibility; do not link AGPL core or copy UI. Forking WorkRoles is possible but means AGPL and maintaining a large codebase.

## Testing strategy

### Outside the game (pure logic)
- Core library targets netstandard2.0/net10 with no game dependency; interfaces for `IJobCatalog`, `IPawnFacts`, `ISchedule`, `IEquipment`, `IArea`.
- Unit tests: order compilation, priority mapping, overrides, schedule windows, rule evaluation, import/export round-trip, validation, migration, death-parking state, performance.
- Tools: NUnit/xUnit/TUnit; `Krafs.Rimworld.Ref` for compile-only references; `Zlepper.RimWorld.ModSdk.Testing` if useful.
- CI: `dotnet test`, XML lint, analyzers, warnings-as-errors; no game needed.

### In-game automated
- RimTest Redux (1.6; verify 1.7) for `[TestSuite]`/`[Test]` unit tests inside the game; run-at-startup in dev.
- Pickle (RimWorld 1.5/1.6/1.7) for Gherkin scenario/E2E tests against a live save; real UI/ticks; JUnit/CI; good for save/load, jobs, death, apparel, zones.
- Custom dev-only test mod if needed: spawn pawns, assert priorities/jobs, run scenarios; not shipped.

### Hot testing
- Dev mode, quicktest saves, debug actions; exploratory UX/edge cases only.
- Never the only proof; regressions belong in automated tests.

### Test pyramid
- ~70% pure core unit tests.
- ~20% in-game integration/scenario tests.
- ~10% hot/manual UX.
- In-game tests cover Harmony patches, `Pawn_WorkSettings`, job selection, save/load, death/resurrection, traits/incapacity, mechs/NPC isolation, mod matrix, UI smoke.

## Non-destructive integration (managed pawns)

- Only manage player-faction humanlikes with a `PawnBinding` (colonists/slaves; player mechs only if mechanical support is enabled). Never manage NPCs, visitors, enemies, animals, or non-player mechs.
- `GetPriority` prefix checks managed status; unmanaged pawns take the vanilla path untouched. `SetPriority` intercepts only managed pawns. No global job-giver replacement.
- Write-through to vanilla tables: `Pawn.workSettings`, `Pawn.timetable`, `Pawn.outfits.CurrentApparelPolicy`, `Pawn.playerSettings.AreaRestrictionInPawnCurrentMap`, drug/food policy. Compiled cache is runtime only; vanilla priorities are mirrored for removal/fallback.
- Def-level split/merge is global; opt-in and warned. Do not add new work types to `mechEnabledWorkTypes` by default; do not delete work types/givers. NPC/mech behavior stays as vanilla as possible.
- Areas: apply only to managed pawns; reference existing areas; no global area deletion; map-specific.
- Equipment: apply only to managed pawns; reference external loadout/outfit/policy; opt-in; asynchronous; no forced removal of gear.
- Schedule: write only managed pawns' timetable; no global schedule.
- Death: park binding/overrides; restore on resurrection; RoleSlot vacancy/auto-assign; preset survives.
- Detect WorkRoles/Work Tab/Better/Enhanced/Compact/PriorityMaster/MWP; disable priority takeover and warn; no silent conflict.
- UI: additive Work Control window first; vanilla Work/Schedule/Assign remain usable. Optional takeover later, with fallback for unmanaged pawns.

## Modules and UI unification

- M0 Core: profiles, bindings, overrides, RoleSlots, serialization, validation, import/export; pure tests.
- M1 Work adapter: managed set, GetPriority/SetPriority, compiled order, fallback/mirror; in-game tests.
- M2 Schedule adapter: vanilla timetable + hour priority windows.
- M3 Additive UI: profile library, pawn panel, schedule editor, status; no takeover.
- M4 Equipment/area references: status + manual/opt-in apply via adapters.
- M5 Automation: rules, levels, locks, hourly evaluation.
- M6 Import/export + WorkRoles migration.
- M7 Optional full Work tab takeover.
- Unify the UX, not the data: one Profile editor with sections (schedule, work, equipment, area, policies, rules); each section writes to its domain adapter. Do not merge apparel/area/drug/food into a parallel data blob.
- Equipment/policies are the most complex integration; start with references and status, then opt-in application.

## Work hierarchy (evidence)

- `WorkTypeDef` = the Work-tab column/office. Fields: `relevantSkills` (a **list**), `workTags`, `naturalPriority`, `workGiversByPriority`, `visible`, `requireCapableColonist`, `disabledForSlaves`.
- `WorkGiverDef` = a task/activity inside a type. Fields: `workType`, `priorityInType`, `workTags`, `requiredCapacities`, `emergency`, `canBeDoneByMechs`, `nonColonistsCanDo`, `directOrderable`; **no `skill` field**.
- Skill association is `WorkTypeDef.relevantSkills` (one or more), not a per-giver mapping. A type can contain tasks that use different skills; grouping by skill would cross types and duplicate columns.
- The Work tab assigns priority per `WorkTypeDef`; `WorkGiverDef.priorityInType` orders tasks within the type. The game selects jobs via WorkGivers, not skills.

## MVP visual: ordered work types

- Primary unit: `WorkTypeDef` cards/chips, not individual WorkGivers. This avoids hundreds of loose tasks.
- Drag lane left → right = priority order. Leftmost = first. Absent from the lane = won’t do (priority 0).
- “Can’t” vs “won’t”:
  - Can’t: `Pawn.GetDisabledWorkTypes` / `WorkTypeIsDisabled` (trait/backstory/health). Show grey in a separate “can’t” section; not assignable.
  - Won’t: capable but absent from the lane; write priority 0.
- WorkGivers are collapsed under each type: show a task count; expand read-only for now. Per-giver reorder/disable is a later module.
- Skill is an overlay/filter (`relevantSkills`), not a grouping layer. Search/filter by skill/mod/assigned.
- Mapping to vanilla:
  - Order position → unique priority 1..N; absent → 0; call `SetPriority` and `Notify_UseWorkPrioritiesChanged`.
  - Vanilla max is 4; for a true linear order patch `LowestPriority`/`DefaultPriority` (PriorityMaster-style) or use a compiled `GetPriority`. MVP: 1..N with max patch; the original Work tab is secondary.
  - If only 1..4 is used, the order collapses into tiers and is not a true left-to-right order; that is the tradeoff.
- Data: first slice stores a per-pawn ordered list of WorkTypeDef defNames. No profiles/schedule/equipment yet; death persistence comes with the profile/RoleSlot module.
- Tests: pure order→priority mapping; in-game managed/unmanaged; incapacity grey; save/load; original priorities/job selection; Pickle scenario.

## Priority range: can it be indefinite?

- The engine stores priorities as `int` in `Pawn_WorkSettings.priorities`; `GetPriority` returns the int; `JobGiver_Work` compares them. There is no hard engine cap in the comparison.
- The practical caps are:
  - `Pawn_WorkSettings.LowestPriority` / `DefaultPriority` (default 4 / 3) and the clamp in `SetPriority`/UI code.
  - `WidgetsWork.ColorOfPriority`, `TipForPawnWorker`, `DrawWorkBoxFor`, and `PawnColumnWorker_WorkPriority.HeaderClicked` assume a small range (colors/boxes/cycling).
  - Other mods that assume 0–4 or 1–9.
- Options:
  1. **Fixed high cap** (e.g., 99/999): patch `LowestPriority`/`DefaultPriority` and UI color/cycle code; simple, but still a cap and can confuse other mods.
  2. **Dense rank 1..N per pawn** (recommended): N = number of active/ordered work types. Patch the clamp; map ranges to colors; no gaps; effectively unlimited for real colonies.
  3. **Custom compiled `GetPriority`** (WorkRoles-style): return rank from the compiled order; mirror vanilla 0–4 for display/removal; no numeric cap; best when our UI is the primary Work tab.
- Recommendation: **dense rank 1..N**, not `int.MaxValue`, not an arbitrary huge cap. Set `LowestPriority` to a safe dynamic/soft maximum (at least max work types), patch the UI color/cycle functions, and use our UI as primary; the vanilla Work tab is a secondary mirror.
- “Indefinite” is true logically (int range) but not practically useful: the useful maximum is the number of work types (vanilla + mods), and other mods/UI may assume small ranges. Do not promise literal infinity; promise “no 4-cap, order-preserving”.
- Test with 20, 100, and 300+ work types; verify `SetPriority`/`GetPriority`, job selection, vanilla UI errors, save/load, and other mods’ assumptions.
- Decision: **dense rank 1..N is the chosen model**. Visual soft cap/colors are not a design goal; numbers are hidden in our UI. The soft max is technical only (clamp); the vanilla Work tab is secondary and gets a safety clamp/mirror, not a visual scale.

## Skill ownership (evidence)

- `WorkGiverDef` has **no skill field**. It owns routing/scanning: `workType`, `priorityInType`, `workTags`, `requiredCapacities`, `emergency`, `canBeDoneByMechs`, etc.
- Skill sources:
  1. **`RecipeDef.workSkill`** — primary skill for bill work (speed/quality/learning); `RecipeDef.skillRequirements` — list of minimum skill gates (can require multiple skills); `RecipeDef.workSkillLearnFactor`.
  2. **`Bill.allowedSkillRange`** — per-bill worker skill range; `Bill.recipe`.
  3. **`WorkTypeDef.relevantSkills`** — list of skills relevant to the whole Work-tab column; aggregate/UI metadata, not a per-task mapping.
  4. **`JobDef.joySkill`** — only joy jobs.
  5. **WorkGiver/JobDriver code** — non-recipe work may use skills directly; no def-level declaration.
- `JobDriver_DoBill.ActiveSkill` resolves the bill recipe’s work skill.
- Multi-skill danger:
  - A WorkType can list several `relevantSkills` (e.g., Crafting may span Crafting/Artistic/Intellectual); a single skill label is wrong for the type.
  - A recipe has one `workSkill` but can have multiple `skillRequirements`; show primary + requirements, not one skill.
  - A DoBill WorkGiver can serve recipes with different work skills; do not assign one skill to the giver.
  - Modded code-based givers may use multiple skills with no data; mark unknown/complex.
- UI rule: show skills at the **WorkType** level as a set (`relevantSkills`), and only in advanced bill details show recipe work skill(s)/requirements. Never group or assign by a single skill, and never treat `WorkGiverDef` as the skill owner.

## Skill provenance and relevantSkills

- `WorkTypeDef.relevantSkills` is **authored XML**, not captured or derived. The Def loader reads the `<relevantSkills>` list of `SkillDef` defNames and resolves the references; no runtime scan of WorkGivers, recipes or jobs populates it.
- If a mod adds a WorkGiver/recipe with a new skill but does not update the WorkTypeDef, `relevantSkills` stays stale. It is a hint/list of **related** skills for the column, not a hard requirement or per-job mapping.
- It is used as Work-tab/metadata (skill overlay/tooltips), not to decide job selection or gate work.
- Actual skill sources remain: `RecipeDef.workSkill` (primary), `RecipeDef.skillRequirements` (minimum gates, may be multiple), `Bill.allowedSkillRange`, `JobDef.joySkill` (joy only), and WorkGiver/JobDriver code for non-recipe work. `WorkGiverDef` has no skill.
- Derived view (optional, label clearly): for a WorkType, iterate `RecipeDef`s where `requiredGiverWorkType == workType`; collect `workSkill` and `skillRequirements`. This is “skills detected in recipes”, not the authored `relevantSkills`, and may be incomplete for code-based givers. Cache it; never write it back as if it were authored.
- UI vocabulary:
  - **Habilidades relacionadas (tipo)**: `relevantSkills` authored.
  - **Habilidades de recetas (derivadas)**: collected from `RecipeDef`.
  - **Requisitos mínimos**: `skillRequirements` / `Bill.allowedSkillRange`.
  - **Sin datos (código)**: non-bill givers whose skill is only in code.
- Do not say “necesita X” from `relevantSkills`; say “relacionada”. Do not collapse multiple skills into one. Do not block assignment by skill; incapacity is the blocker, recipe/bill requirements are handled by the game.

## Skill provenance linked to the existing design

Ownership chain (no duplication):

- `WorkTypeDef.relevantSkills` = authored, global, **related** skills. Lives on the def; never copied into presets.
- `RecipeDef.workSkill` + `skillRequirements` = actual bill skill/requirements. Lives on the recipe.
- `Bill.allowedSkillRange` = per-bill worker range.
- `WorkGiverDef` = routing/scanning only; **no skill**.
- `JobDriver`/`Job` = runtime execution; code-level skill use.
- `ProfilePreset`/`PawnBinding`/`PawnOverrides` = job references/order only; **no skill storage**.
- `SkillProvenance` = read-only cross-cutting service: derives recipe skills, caches at load, never writes back.

Where it plugs in:

- Base layer: show authored `relevantSkills` on WorkType cards; it is global and cheap.
- Group layer: `RoleSlot.expectedSkills` is user-authored metadata (suggestions), not a hard gate. Presets reference jobs; skills are shown/derived, not stored.
- Individual layer: per-pawn skills come from `Pawn.skills`; overrides never store skill. Automation conditions use pawn skills, not `relevantSkills` as truth.
- UI: WorkType card shows “Habilidades relacionadas” (authored); advanced detail shows “Habilidades de recetas” (derived) and “Requisitos mínimos”; code givers show “sin datos”. Never one-skill labels for multi-skill types.
- Automation: use actual pawn skill for conditions/fitness; `relevantSkills` can be a soft weight/hint, never a hard requirement.
- Tests: pure `SkillProvenance` service (authored vs derived vs requirements vs unknown); in-game display/no-gating; modded stale `relevantSkills`.
- MVP: authored `relevantSkills` only (simple, accurate as “related”); derived recipe skills in a later module. No skill grouping, no skill gating, no skill storage in presets.

## Multiple relevant skills and UI display

- `WorkTypeDef.relevantSkills` is a `List<SkillDef>`; it can contain two or more. Example: Hunting can list Shooting and Animals. It is authored XML, not derived.
- It means **related**, not “trains” or “requires”. A related skill may be used for speed/quality/eligibility, or may not gain XP at all; XP is task/recipe/driver-specific.
- UI wording: “Habilidades relacionadas” with chips for each skill; tooltip “relacionada, el XP depende de la tarea”. Never show only one when there are two; never imply both level up.
- Derived recipe skills are a separate set (“Habilidades de recetas”), and requirements are separate (“Requisitos mínimos”).

## View model and draw pipeline

- Current v0.1 has **no visual draw**: it is a text-based Mod Settings list. The planned pipeline is:
  1. Read `DefDatabase<WorkTypeDef>.AllDefsListForReading` (do not capture or mutate defs).
  2. Build a `WorkTypeCardVM` per type: `defName`, `labelShort`, `description`, `naturalPriority`, `relevantSkills`, `workTags`, `visible`, task count/list, `canPawnDo` (`Pawn.GetDisabledWorkTypes`/`WorkTypeIsDisabled`), current dense rank, derived recipe skills/requirements.
  3. Draw with Unity `Widgets`/`GUI` in our window; cache VMs; refresh on def/pawn/profile changes, not per frame.
  4. Apply the dense rank to `Pawn_WorkSettings`.
- The vanilla Work tab draws columns from the same defs through `MainTabWindow_Work`, `PawnColumnWorker_WorkPriority`, and `WidgetsWork`; it is a view of `WorkTypeDef.naturalPriority`/`visible` and per-pawn priorities. Our UI is a separate view; we do not “transform defs into draw”.
- Metadata sources: `WorkTypeDef` (authored), `WorkGiverDef` (tasks), `RecipeDef` (derived skills/requirements), `Pawn` (skills/incapacity/rank).

## Dynamic WorkType generation

- The first visual draw should use **existing WorkTypes only**. Dynamic generation is a later Base-layer module; designing the view model now is enough to accept new types later.
- Load-time generation is feasible: create `WorkTypeDef`, move `WorkGiverDef.workType`, copy `workTags`, `relevantSkills`, `lifeStageWorkSettings`, `mechEnabledWorkTypes`, and `TraitDef.disabledWorkTypes/requiredWorkTypes`; never delete; opt-in; restart; warn.
- Runtime generation (Work Type Editor style) is possible but high-risk: def caches, `DefMap<WorkTypeDef,int>`, pawn work settings, save compatibility. Defer until the core is stable.
- No deletion of work types/givers; no global changes without explicit opt-in; NPC/mech behavior preserved as much as possible.

## Card-based per-pawn editor layout

- In the per-pawn editor, WorkTypes become **cards/chips**, not columns.
- Layout:
  - **Pool above**: all WorkTypes the pawn can do, sorted by **base order** (`naturalPriority`), searchable/filterable. This is the catalog, not the priority.
  - **Priority lane below**: selected WorkTypes in custom order; left → right = rank. Drag from pool to lane, within lane to reorder, out of lane = off (priority 0).
  - **“Can’t” section**: incapacity (`GetDisabledWorkTypes`/`WorkTypeIsDisabled`); grey, not draggable.
- Off vs can’t: off = capable but removed (will not do); can’t = incapable (cannot be assigned).
- Cards show label, relevant skill chips, task count; expand for detail (WorkGivers, derived recipe skills/requirements) later. Numbers hidden.
- Many cards: compact chips, search, filters (skill/mod/status), horizontal scroll for the lane, virtualized rendering; no per-frame `DefDatabase` scans.
- The **matrix/grid** (pawns × WorkTypes) remains useful as an overview for comparing pawns; the card editor is the per-pawn detail. Do not discard the grid permanently.
- The vanilla Work tab stays secondary; no takeover in the first slice. Writes go to `Pawn_WorkSettings` with dense rank.

## Low-risk first visual: suitability colors

- No cards, lanes or drag first. A simple table/list with cells or rows colored by **pawn suitability**.
- Semantics:
  - **Green** = good for this pawn (high relevant skill / passion).
  - **Red** = bad for this pawn (low skill / no passion), but still assignable.
  - **Grey** = incapable (`GetDisabledWorkTypes`/`WorkTypeIsDisabled`); not assignable.
  - **Neutral** = no relevant skill/unknown; default color.
  - Selection highlight is separate from suitability; drag/selection comes later.
- Multi-skill WorkTypes: color by the **best relevant skill** (max) by default; tooltip shows each skill and level; configurable to average. Red never blocks; only grey blocks.
- Accessibility: color alone is not enough; add icon/label/tooltips and colorblind palette.
- View choice:
  - **Separate “Work Colors” window first** (recommended): no conflict with vanilla/Compact Work Tab/Work Tab; writes priorities.
  - **Paint the vanilla Work tab** later with a small cell patch; more integrated but conflicts with tab replacements; detect and warn.
- Data: pure `SuitabilityService` (pawn facts + WorkType relevantSkills) + cached `WorkCellVM`; no per-frame scans.

## Componentization for moving the UI

- Build small renderers, not one monolithic `OnGUI`:
  - `WorkCellRenderer.Draw(Rect, WorkCellVM)`
  - `WorkRowRenderer`, `WorkColumnRenderer`, `WorkTableRenderer`
  - `SuitabilityPalette`, `SelectionState`
- Renderers only draw within a `Rect`; no game mutation, no assumptions about the parent window. View models are cached; logic lives in services.
- Moving a component to another window/tab (separate view, our tab, or a vanilla cell patch) is mostly layout math, not a rewrite.
- IMGUI manual layout is the main cost; data/render separation keeps it low–medium. Drag/input is a separate handler and can be reused later; cards/lists can reuse the same cell renderer.
- Do not hardcode absolute positions or couple rendering to `Pawn_WorkSettings` writes.

## Vanilla work storage and query (reference)

- `Pawn.workSettings` is a `Pawn_WorkSettings`.
- `Pawn_WorkSettings.priorities` is a `DefMap<WorkTypeDef,int>`: **one int per WorkType**, 0 = off, 1..4 = priority. It is saved with the pawn via `ExposeData`. There is **no per-WorkGiver priority** in vanilla.
- Fields: `priorities`, `workGiversDirty`, `workGiversInOrderNormal`, `workGiversInOrderEmerg`, `LowestPriority`, `DefaultPriority`, `MaxInitialActiveWorks`.
- Methods: `GetPriority(WorkTypeDef)`, `SetPriority(WorkTypeDef,int)`, `WorkIsActive(WorkTypeDef)`, `Notify_UseWorkPrioritiesChanged()`, `Notify_DisabledWorkTypesChanged()`, `CacheWorkGiversInOrder()`, `EnableAndInitialize()`.
- Column order: `WorkTypeDef.naturalPriority` (descending) + `visible`. Giver order inside a type: `WorkGiverDef.priorityInType` (descending); `WorkTypeDef.workGiversByPriority` is the cached list.
- Query/selection: `JobGiver_Work.GetPriority`/`TryGetPriority` walks the cached `workGiversInOrderNormal`/`workGiversInOrderEmerg` lists; each `WorkGiver.TryIssueJobPackage` proposes a `Job`; `Pawn_JobTracker` starts it.
- Incapacity: `Pawn.GetDisabledWorkTypes()`, `WorkTypeIsDisabled()`, `WorkTagIsDisabled()`, plus `WorkGiverDef.requiredCapacities`; these cannot be granted by priority.
- `WorkGiverDef` = task definition (`workType`, `priorityInType`, `emergency`, `workTags`, `requiredCapacities`); `WorkGiver` = runtime scanner; `Job` = concrete work. `WorkGiverDef` has no skill.
- Pseudo-code:
  ```
  var ws = pawn.workSettings;
  foreach (var wt in DefDatabase<WorkTypeDef>.AllDefsListForReading
             .Where(w => w.visible)
             .OrderByDescending(w => w.naturalPriority)) {
      int priority = ws.GetPriority(wt); // 0 = off
      if (priority <= 0) continue;
      foreach (var wg in wt.workGiversByPriority) {
          // JobGiver_Work uses the cached lists; wg.priorityInType is global
      }
  }
  ```

## UI references and reuse

- **Numbers** (Mehni): dynamic table/columns, sorting/filtering, color cells, presets, tooltips, performance. Great reference for the table shell. Source/license not confirmed here; use patterns only unless verified.
- **Compositable Loadouts**: tags/loadouts, drag/drop, per-pawn assignment, search/status. Great reference for the per-pawn list/card. Use patterns; verify license before code reuse.
- **Better Pawn Control**: policies/outfit/area/drug/food; **MIT**; optional code adaptation if needed.
- **WorkRoles**: role groups/chips, per-colonist panel, death parking; **AGPL**; concepts only, no code.
- **Sleek / Better Work Tab**: focus cards, drag, coverage; source/license unknown; concepts only.
- Reuse UX patterns and layout ideas; do not copy code/assets from AGPL/unknown/NC projects. Credit the references.

## Own window architecture

- Custom `MainTabWindow`/`Window` with `DoWindowContents(Rect)`; no vanilla Work tab replacement in the first slice.
- `WorkTableView` + `WorkTableVM`; renderers: `WorkCellRenderer`, `WorkRowRenderer`, `WorkHeaderRenderer`, `WorkTableRenderer`; `SuitabilityPalette`, `SelectionState`, `FilterState`, `SortState`.
- Cache rows/cells/labels; virtualize visible rows; use `Widgets.BeginScrollView`; no per-frame `DefDatabase` scans.
- Table overview (pawns × WorkTypes) first; per-pawn list/card later. Moving components between windows/tabs is layout math because renderers take a `Rect` and a VM.

## Per-pawn list UI (loadout-style)

- One pawn at a time: a full-screen `MainTabWindow` (new tab) or a resizable `Window`. New tab recommended for full-screen space without replacing the vanilla Work tab.
- Layout:
  - **Left pane**: pawn list with portraits/names, search/sort, scrollbar; selected pawn highlighted. A compact `Widgets.HorizontalSlider` can switch pawns on narrow screens, but a scrollable list is more usable with many pawns.
  - **Main pane**: selected pawn header (name, skills, traits/incapacity); ordered WorkType rows; `+` opens a searchable picker of available WorkTypes; drag rows to reorder; remove row = off (priority 0); grey “can’t” rows/section.
  - **Status**: active count, task count, warnings; dense rank hidden.
- Rows show WorkType label, relevant skill chips, task count, suitability color; tooltip with skills/incapacity. Expand later for WorkGivers/derived recipe skills/requirements.
- `+` picker: base order (`naturalPriority`), search/filter, disabled/incapable marked; selecting adds at the end or at the selected position.
- Data: per-pawn ordered WorkType list; dense rank; apply to `Pawn_WorkSettings`; no profiles yet; unmanaged pawns untouched.
- Performance: only the selected pawn’s list; cached VMs; virtualized visible rows; no per-frame `DefDatabase` scans.
- References: Compositable Loadouts (per-pawn list, add/remove, tags), Numbers (filters/colors/performance), WorkRoles (full-screen/pawn selector; concepts only, AGPL).

## Two-view UI: Overview + Work Control

- **Overview** (low noise): full-screen comparison of all pawns; table/list with colors and filters; no drag. Purpose: quick compare and find who is good/bad/incapable. Click a pawn or cell to open the detail.
- **Work Control** (detail): one selected pawn; ordered WorkType list; `+` picker; drag reorder; remove=off; grey can’t; selected WorkType details (skills, tasks, derived recipe skills/requirements, weapon context, incapacity reasons); apply.
- Shell: one full-screen `MainTabWindow` with internal tabs/views “Overview” and “Work Control”; pawn sidebar with search/sort/scrollbar and optional compact slider; no vanilla Work tab replacement.
- Navigation: Overview → click pawn/cell → Work Control; Back to Overview; pawn switcher in detail.

## Contextual suitability (weapons/range)

- Generic suitability: relevant skills/passion, capacities, traits, incapacity.
- Contextual profiles for weapon-dependent work (Hunting, combat-style work):
  - `Shooting` skill, passion, sight/manipulation, traits.
  - Equipped weapon from `Pawn.equipment.Primary`; `VerbTracker.PrimaryVerb.verbProps`: `range`, `accuracyShort/Medium/Long`, `warmupTime`, `burstShotCount`, `damage`, `stoppingPower`.
  - Hunting example: a shotgun has short range/low long accuracy; flag “short-range weapon” vs a megasloth; a rifle is better. Do not auto-assign; show color/warning and reason.
- Overview color can use the combined score for profiled WorkTypes (e.g., Hunting) and generic for others; detail shows the breakdown. Cache per pawn/WorkType; invalidate on weapon/skill/health changes; no per-frame scans.
- No skill storage; weapon/context are runtime facts; profiles are data-driven and configurable.

## Light overview vs detail stats

- **Overview (light)**: color only by **skill**, no weapon/stats. Multi-skill WorkTypes use the best relevant skill (max) by default; tooltip shows all. No-skill WorkTypes (Hauling/Cleaning) are neutral; quick assign. Click a pawn/cell to open Work Control.
- **Work Control (detail)**: one pawn; hover/select a WorkType to show relevant characteristics in a side panel:
  - Relevant skills: level/passion (`relevantSkills`; multiple shown).
  - Work speed: `WorkSpeedGlobal`, `GeneralLaborSpeed`, plus a WorkType-specific stat when mapped (e.g., ConstructionSpeed, MiningSpeed, PlantWorkSpeed, MedicalTendSpeed, ResearchSpeed, AnimalGatherSpeed).
  - Capacities: Manipulation, Sight, Moving, Consciousness; pain; injuries (`Pawn.health.capacities`, `Pawn.health.hediffSet`).
  - Traits/health warnings and incapacity reasons (`GetDisabledWorkTypes`/`WorkTypeIsDisabled`).
  - Quality: recipe/bill-specific and stochastic; show an **estimate** only where applicable (crafting/cooking/medical), label “estimada”; for non-bill work show “N/A”. Do not fake a single quality ratio.
- `WorkTypeProfile` data-driven mapping WorkTypeDef → relevant stats/capacities/traits; fallback to relevantSkills + WorkSpeedGlobal + GeneralLaborSpeed + Manipulation/Sight.
- Stats are runtime facts (`Pawn.GetStatValue`, capacities); cache per pawn/WorkType; invalidate on skill/health/equipment changes; no per-frame scans.
- Light version excludes equipment/weapon context; optional later.

## UI drawing and layout

- RimWorld UI is **pure C# IMGUI** (`Verse.Widgets`, `UnityEngine.GUI`, `Rect`, `Texture2D`). There is no official visual designer for mod windows; you write `DoWindowContents(Rect)` and draw.
- Recommended shell: full-screen `MainTabWindow`; `DoWindowContents(inRect)` gets the whole area. A resizable `Window` is the alternative.
- Layout is manual `Rect` math, but dynamic:
  - Fixed header height (e.g., 44 px), status height (e.g., 28 px); body fills the rest.
  - Sidebar width = `Mathf.Clamp(body.width * 0.24f, 220f, 320f)`; main = remaining; detail panel = `Mathf.Clamp(main.width * 0.32f, 280f, 420f)`.
  - Scroll with `Widgets.BeginScrollView`/`EndScrollView`; virtualize visible rows; fixed row height (e.g., 32–40 px).
  - Responsive breakpoints: below ~1000 px collapse the sidebar to a dropdown/slider; below ~600 px stack the detail panel as a tab.
  - `UI.screenWidth`/`Height`, `Prefs.UIScale`, `Text.CalcSize`, `Text.Anchor`, `Text.WordWrap`.
- Use small renderers (`WorkCellRenderer`, `WorkRowRenderer`, `WorkHeaderRenderer`, `WorkTableRenderer`, `StatsPanelRenderer`) taking a `Rect` + VM; no monolithic `OnGUI`; no absolute positions; no game mutation in renderers.
- Caching/performance: cache labels, sizes, VMs; no per-frame `DefDatabase` scans/LINQ; draw only visible; tooltips via `TooltipHandler.TipRegion`; colors via `GUI.color`/`Widgets.DrawBoxSolid`.
- Not recommended: Unity uGUI/UI Toolkit/AssetBundles for this mod (heavy, input/scale issues), ImGui (possible via Waylith, but unnecessary), or external WPF/WinForms (cannot embed in-game).
- Tools: Figma/Penpot for wireframes; IDE (Rider/VS/VS Code); Dev Mode; Pickle/RimTest for UI scenarios; `Player.log`; no runtime visual editor. Iteration is build → restart → resize/test.

## Development iteration and C# reach

- Responsive layout is confirmed: percentages + min/max clamps + breakpoints.
- C# + IMGUI can draw everything needed (rects, icons, text, scroll, drag, tooltips, animations, custom controls). The limitation is manual layout and the restart cycle, not expressiveness.
- No standard hot reload for mod DLLs in RimWorld: code changes require restart. XML/defs may be reloadable in Dev Mode; C# cannot be assumed to hot reload.
- Reduce the cycle:
  - Keep layout math in pure functions (`LayoutEngine`) and view models; unit-test them outside the game.
  - Use Figma/Penpot or a small HTML/WPF mock for visual iteration before porting to IMGUI.
  - Fast build/deploy script; dev quicktest save; `-savedatafolder`; start directly into a test colony.
  - Use Pickle/RimTest for repeatable UI/state scenarios; screenshots + `Player.log` for visual errors.
- First visual: full-screen `MainTabWindow`, responsive, per-pawn list, Overview colors, Work Control stats.



















## Vanilla job system baseline (evidence)

Flow:

```
Work tab UI
  -> Pawn_WorkSettings.SetPriority
  -> priorities (DefMap<WorkTypeDef,int>)
  -> Notify_UseWorkPrioritiesChanged
  -> CacheWorkGiversInOrder
  -> workGiversInOrderNormal / workGiversInOrderEmergency
  -> JobGiver_Work
  -> WorkGiver
  -> Job
```

Game fields/methods verified by reflection on `Assembly-CSharp.dll`:

- `Pawn.workSettings` is a field on `Pawn`.
- `Pawn_WorkSettings` fields: `priorities`, `workGiversDirty`, `workGiversInOrderEmerg`, `workGiversInOrderNormal`, `LowestPriority`, `DefaultPriority`, `MaxInitialActiveWorks`.
- `Pawn_WorkSettings` methods: `ExposeData`, `EnableAndInitializeIfNotAlreadyInitialized`, `EnableAndInitialize`, `SetPriority`, `GetPriority`, `WorkIsActive`, `Notify_UseWorkPrioritiesChanged`, `Notify_DisabledWorkTypesChanged`, `CacheWorkGiversInOrder`.
- `Pawn.GetDisabledWorkTypes`, `WorkTypeIsDisabled`, `WorkTagIsDisabled` use cached disabled work types from traits/backstories/health.
- `WorkTypeDef.naturalPriority` orders columns; `WorkGiverDef.priorityInType` orders givers inside a type; `WorkGiverDef.workType` decides the column.
- `MainTabWindow_Work` is UI only; `PawnColumnWorker_WorkPriority` and `WidgetsWork` draw/click the cells.

Persistence:

- `Pawn_WorkSettings` is saved with the pawn via `ExposeData`; the `priorities` `DefMap` is the actual config.
- Same pawn object (downed, caravan, cryptosleep, prisoner) keeps its work settings; the Work tab may hide prisoners.
- Death/destruction loses the pawn's work settings; a corpse `InnerPawn` and resurrection may keep the same pawn, but this is not a guaranteed recovery path. A new pawn gets `EnableAndInitialize` defaults.
- Work Tab per-pawn trackers are lost with the pawn; no parking.
- WorkRoles `DeathRoles` parks role sets in `dormantPawnSets[pawn]` when a corpse can return, restores them on resurrection, inherits robot station roles, and cleans up when the corpse is destroyed.

Mod changes (evidence):

- Work Tab: `GetPriority`/`SetPriority` prefixes; per-pawn per-giver `WorkPriority` with `int[24]`, saved as a 24-digit string; clamps the vanilla argument to 4; checks `Pawn.AllowedToDo`.
- PriorityMaster: transpiles constants 3/4 in Get/Set/Enable/UI; patches `LowestPriority`/`DefaultPriority`; max 1-99.
- WorkRoles: global `RoleStore` + per-pawn `PawnRoleSet`; compiled `Entry` with `PriorityByIndex`, `VanillaByIndex`, normal/emergency WorkGiver lists; cache/invalidation; `PrioritySetWatcher` blocks third-party `SetPriority`; time/location boundaries; `DeathRoles`.
- Automatic Work Assignment: `WorkSpecification` with conditions, fitness, amounts, priorities, post-processors; periodic/manual; MIT.
- Better Pawn Control: policies for work/schedule/outfits/areas/drugs/food; defaults on join; one-click; writes priorities; not compatible with Complex Jobs/job-expanding mods except Work Tab.
- Compositable Loadouts: `LoadoutManager` GameComponent tags, per-pawn loadout; minimal AI patches; staggered resolution.
- Enhanced Work Tab: 1.6; UI/auto/time/zone/1-9; tab replacement.
- Sleek Work Priorities: 1.7; focus cards/per-pawn card; existing priorities; disables with tab replacements.
- Adaptive Work Priorities: 1.7; learns from orders, levels, locks, emergency.
- Work Type Editor: live def mutation; no deletion in our version.
- Job Splitter / Complex Jobs / SMJ: XML defs; global.

Design implication:

- The source of truth should be profiles/roles, not the per-pawn numeric grid.
- Per-pawn assignments + dormant parking + auto-assign on new pawn; per-pawn overrides optional.
- A role slot independent of a pawn (personal idea) can auto-bind a replacement and avoid rebuilding dense config.

## Current UI decisions (latest)

- Shell: **new bottom tab** (`MainTabWindow`) named “Work Control”; full-screen; no vanilla Work tab replacement.
- First slice: **Overview minimal + Work Control minimal**.
- Work Control: **vertical per-pawn list**. Priority reads **top → bottom**, top = first. The text inside each row reads left → right. Add a visible hint: “Arriba = primero · afuera = no lo hace · gris = no puede”.
- Work Control rows: label, relevant skill chips, task count, suitability color, selected/hover; `+` picker; drag reorder; remove = off; grey “can’t” section; numbers hidden.
- Overview: no fixed Work tab columns/matrix. Choose a **skill of interest**, then choose **one WorkType** to focus; show/rank pawns for that WorkType; click to open Work Control. Future: multi-WorkType comparison.
- Stats now: relevant skills, work speed (`WorkSpeedGlobal`, `GeneralLaborSpeed`, specific), capacities, pain/health, incapacity; quality estimate later.
- Death: basic parking/preservation in the first slice so config is not lost; mechs excluded; profiles/schedule/equipment/dynamic WorkTypes deferred.
- Technical gaps to validate: dense rank >4/clamp, vanilla UI with N>4, per-pawn storage/parking, notify/current job behavior, compatibility with Compact Work Tab/Work Tab, performance with many WorkTypes, stats mapping.

## v0.1 test finding

- The installed v0.1 is active and its settings file exists, but `workTypeOrder` contains the default order (Firefighter, Patient, Doctor, ...). No custom order was saved.
- v0.1 applies only at startup (`StaticConstructorOnStartup`); it does not apply live. Changing the text list requires closing settings and restarting.
- v0.1 changes global `WorkTypeDef.naturalPriority`; it does not write per-pawn `Pawn_WorkSettings`. An existing save should still see global def order after restart, but the visible Work tab may not reorder if the UI does not use `naturalPriority` as expected.
- If a custom order still does not reorder the vanilla Work tab after restart, run a technical spike to verify whether the column order comes from `naturalPriority` or another source; if not, use a UI/column-order patch or our own tab.

### User test result (v0.1)

- Moved a WorkType in the text settings, but the Work tab did not reflect the order.
- Priority cells cycle 0–4 (vanilla); there is no dense rank in v0.1.
- No notification appears; v0.1 has no notification feature.
- Conclusion: v0.1 does **not** implement the visual Work Control, dense rank or notifications. It only changes global defs; the full mod list includes Compact Work Tab/UIRethemed Work, which may own the visual column order. Do not keep testing v0.1; build a technical spike in a minimal profile and only ask for a user test when a meaningful slice exists.


## Global layer editor (split/merge) — design

- WorkTypes are **solid blocks**, and WorkGivers are nested solid blocks. Each block has up/down controls; drag can come later.
- Actions on a WorkType block:
  - **Split** → opens a dedicated split editor: new name, description, base WorkType, selected WorkGivers; preview; create. Moves selected givers; source may become empty.
  - **Merge WorkType** → choose target WorkType; move **all** source givers to target; source becomes empty; never drop a giver.
  - **Merge WorkGiver** → select one or more givers and a target WorkType; move them; source may become empty.
- Safety:
  - Validate source/target, not same, target visible; preview counts/diff; block on duplicates/conflicts.
  - Snapshot/undo; no partial apply.
  - If a source becomes empty: do **not** auto-delete. Mark it empty/hidden. Remove only with explicit confirmation and only if no givers, no active trait/backstory references, and no pawn assignments; otherwise block and warn.
  - No active WorkGiver loss; no orphan givers; no silent priority changes.
  - Copy/duplicate trait `disabledWorkTypes`/`requiredWorkTypes` and life-stage/mech entries for splits; preserve giver fields.
- Global layer: applies to all pawns; load-time/restart; no runtime per-pawn changes.
- Future: **prefabs/presets** to save/apply a global layout for users who do not want to tune it manually.

## Latest refinements: lane primary, table secondary, outfits, zones, schedule boosts

- **Primary input is the horizontal lane**, not the matrix. A pawn’s WorkTypes are cards/chips ordered left→right with add/drag; each level is unique (dense rank 1..N). The table below is a **read-only result view** (debug/verification), not the primary editor.
- **Global order becomes secondary**: it is the default/base for new or unmanaged pawns and for users who like natural priority; the per-pawn lane overrides it. WorkGivers inside a WorkType keep their global `priorityInType`; the lane orders WorkTypes, not Jobs. This is a plan for which job-provider groups to try first, not a direct Job ordering.
- **WorkType-level outfit/tool metadata**:
  - Each WorkType card may reference a **recommended outfit/loadout** and a **recommended tool/weapon**.
  - These are metadata/recommendations first, because a pawn wears one outfit at a time and can carry one primary weapon; multiple WorkTypes can conflict.
  - The **Profile/Role** owns the actual outfit/loadout and area; WorkType cards show recommendations and conflicts.
  - Do not auto-switch outfits per job by default; it is slow and produces churn. Optional explicit apply later.
  - Medicine/drug policies are deferred to the Medical Hub, not this mod.
- **Zones and Schedule**:
  - See **Approved policy decisions (alpha.96)** at the top of this file.
  - Zones assign existing allowed areas only; no zone creation, no block-WorkType warning.
  - Schedule v1 edits the vanilla 24-slot timetable with a two-lane preset/draft view.
  - Per-WorkType hour windows are deferred and are not part of the first schedule implementation.

## Latest refinements v4: auto-equip stations, zones, per-WorkType windows

### Correction: there is no vanilla mannequin equip station

- No `Mannequin`/`ArmorRack` ThingDef exists in vanilla `Data`.
- `VFEM2_MannequinStand` is from **Vanilla Furniture Expanded - Medieval 2**; it is a tailoring linkable (work speed/cost bonus), not an equip/return station.
- **Armor Racks** (Khamenman) is the mod with owner assignment and Equip/Transfer commands; its page says 1.7 is not updated. Do not assume it as a hard dependency.

### Auto-equip rules

- Per-pawn, per-WorkType rule:
  - `enabled`, `outfit/loadoutRef`, `tool/weaponRef`, `stationRef`/room, optional time window.
  - Per-pawn override beats global/default; useful for mechanitor, king, specialist.
- Trigger on **WorkType change**, not on every job: if the pawn starts working a different WorkType and its current outfit/tool differs, go to the assigned station, equip the expected set, and later return the previous set. Cooldown; skip if drafted, emergency, downed, or station inaccessible.
- Provider abstraction:
  - **Own Work Locker/Outfit Station** (recommended for control): stores one outfit + tool, assigned to a pawn/room; custom equip/return jobs; no dropping.
  - **Armor Racks** integration if a 1.7-compatible version exists.
  - Generic shelf/stockpile fallback; no quick swap.
- Mannequin visual can be used for our own station; do not rely on vanilla.
- Medicine/drug policies stay in the Medical Hub.

### Zones

- Assign existing allowed areas only; **do not create zones**.
- One allowed area per pawn, per map.
- Bulk/emergency assignment of an existing area.
- No per-WorkType zone recommendation.
- Missing area on the current map produces a semi-persistent alert; no temporary cards.
- Do **not** warn that an area blocks WorkTypes.

### Schedule

- Vanilla `Pawn.timetable` is the source of truth: 24 `TimeAssignmentDef` slots.
- Two parallel lanes: current vanilla timetable + selected preset/working draft ghost.
- Named `SchedulePreset` objects store only the 24 vanilla assignments.
- Presets are global in `Config/OrderedWork/schedules/<id>.json` (one file per preset); select a preset, preview, then apply to selected pawns.
- No per-WorkType hour windows in the first implementation; defer them as an optional layer.
- No separate 24h matrix and no second parallel timetable.

## Latest refinements v5: Outfit Stand, room/workplace, advanced pawn tab

### Outfit Stand is Odyssey DLC (not base vanilla)

- `Building_OutfitStand` is from **Odyssey**. It stores one outfit (apparel + weapons) and can swap gear slots with a pawn.
- Jobs: `UseOutfitStand` (`JobDriver_UseOutfitStand`) and `PutApparelOnOutfitStand` (`JobDriver_PutApparelOnOutfitStand`).
- API/fields: `innerContainer`, `settings`, `allowRemovingItems`, `AddApparel`, `RemoveApparel`, `GetGizmos`, `GetFloatMenuOptionToEquipWeapon`.
- Swapping takes time based on equip delay; it swaps all overlapping slots, so it can be used to toggle between work and off-duty outfits.
- Kid outfit stand exists for Biotech children.
- If Odyssey is absent, fall back to Armor Racks (if 1.7) or a generic shelf; no base-game mannequin.

### Auto-equip with Outfit Stand rules

- Per-pawn, per-WorkType: `outfit`, `tool/weapon`, `stand/room`, `enabled`, `special window`.
- Trigger on **WorkType change**, not every job. If the expected outfit/tool differs and a reachable stand has it, create `UseOutfitStand`; after the work period/WorkType change, use the stand again to swap back.
- Time budget: aim for equipped and working in **under 0.5 in-game hours**. If path + equip would exceed the configured limit, skip and log; keep the pawn working.
- Do not swap while drafted, in emergency, downed, or when the stand is unreachable. Cooldown; no dropping.
- Multiple outfits per pawn are the hard case: one stand toggles between two outfits; one stand per WorkType leaves outfits distributed across stands. Recommendation: **one work outfit per pawn/role (one stand) in v1**; per-WorkType outfit recommendations; multi-stand/locker later.
- Specialists/mechanitor/king: per-pawn override.
- Medicine/drugs remain in the Medical Hub.

### Room/workplace assignment

- Related mods: **AssignedWorkbenches** (Set Owner on benches, assigned pawns only), **Personal Doors/Locks** (room access), **Do Your Research** (specific bench), **Better Workbench Management** (bills).
- A room/workplace assignment is useful but can become noise and hard restrictions (loops, blocked paths).
- Recommendation: **defer**. Use the outfit stand placed near the work room/station; if a preferred-room feature is added later, make it a **soft priority hint**, not a hard restriction, and do not duplicate AssignedWorkbenches/Personal Doors.

### Advanced per-pawn tab content

- WorkTypes list; inside each:
  - outfit + tool rule;
  - special priority time windows;
  - general schedule (vanilla Work/Joy/Sleep + modded assignments);
  - key info of the selected WorkType (skills, speed, capacities, task count, derived recipe skills/requirements);
  - general restricted zone;
  - optional room/workplace preference.
- The lane remains the quick view; the advanced tab is the detail editor.
- Below the lane, the result table stays read-only.

## Latest refinements v6: emergency equipment, stand ownership, lane visuals, multi-pawn jobs

### Equipment and emergency

- Future: Simple Sidearms-like weapon/outfit switching when the pawn is not properly equipped, or when an emergency starts.
- Security/Response may trigger the emergency path: pawns with an indicator grab nearby armor/gear to improve survival; the WorkType outfit priority is still respected for activatable/specialist cases.
- Keep the normal work path non-clunky: order is a mid-game improvement, not a requirement from minute one; the mod must let the colony work normally while order is added.

### Outfit stand per workstation

- One outfit stand per **workstation/room**, linked to the pawn who works there; the pawn does not search random stands.
- Ownership:
  - the outfit + stand belong to one pawn;
  - while worn, the outfit is temporarily owned by that pawn;
  - if an emergency interrupts, the pawn recovers its outfit from the stand/location where it was left.
- This avoids theft and searching, and keeps a predictable swap location.
- Trigger on WorkType change; stand near the workstation; target under 0.5 in-game hours equipped and working; skip if the path/equip exceeds the limit.
- Room assignment as a separate feature is discarded; assigned workbenches/rooms decide who works where.

### Lane visual options

- Quality/passion indicator:
  - show the relevant skill’s **passion flame**; with multi-passion mods, show the passion that represents the pawn for that task;
  - quality stars only where the WorkType produces quality items, labeled as an estimate; otherwise passion/skill.
- Compact mode: icon + full name on hover/tooltip; horizontal slide/scroll for long names so the lane never overflows.
- Skill level: setting to always show the level, or show on hover like vanilla.
- These are view-only options; cached; no data change.

### Schedules

- Per-WorkType start/end priority windows remain; global order is the tie-breaker; fallback to normal when no jobs.
- Unique workbenches per pawn can add noise; keep this optional and let assigned benches decide.
- With many colonists, the schedule can help prioritize or save a shift, but it must not force or lock work.

### Multi-pawn jobs

- Vanilla work is **single-pawn**: WorkGiver → Job → JobDriver. Group activities use **Lord/LordJob/LordToil** (rituals, parties, sieges, group duties); `JobDriver` can target another pawn but still has one actor.
- Multi-pawn work mods patch reservations/WorkGivers or create extra helper jobs; examples researched:
  - **MultiConstruction** (3562530209) — multiple builders/repairers on one blueprint; diminishing returns; author warns it may be broken.
  - **MultiHunter** (3560890077) — multiple hunters on one animal; overwrites reservation; taming/training side effects reported.
  - **Team Builders** (3570660265) — Call Builders/Min Builders; does not work with MultiConstruction.
  - **Bulk Load For Transporters** (3559292908) — multi-pawn loading; requires Pick Up And Haul.
  - **Colony Groups** / **WorkGroups** — grouping/role assignment, not native multi-actor jobs.
- Integration strategy: compatibility/detection first; do not reimplement their reservation model; mark team-capable WorkTypes/roles; coordinate schedules and outfit stations; use their APIs or Lord jobs; test minimal profiles for reservation conflicts and job spam.
- Multi-pawn is a later module; it must not destabilize the single-pawn lane.

## Final alpha.4 decision — new game default and view mode

- New game/default colony: **Draw automatic** — generate the skill recommendation (with Patient/PatientBedRest/Firefighter pins) and apply it once.
- Existing colony on first install: copy the vanilla Work-tab order; do not auto-write.
- The view mode can later be switched to **Manual/Vanilla**; the player can keep the vanilla Work tab, with Work Control as a view/optional editor.
- Future mode setting: Draw (default for new games) / Manual / Both; do not remove the vanilla Work tab.
- Alpha.4 user test: initialization at game start, pins, + picker levels, incapable filtering, and resurrection reapply passed.
- Close for today: next session starts with the view-mode setting, then row selection/double-click/search and the collapsible/optional result table.






