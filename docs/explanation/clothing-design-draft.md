# Clothing policy — design draft

Status: Phase 1.5 and read-only Details preview implemented; editing/application/jobs pending.

This file collects the current clothing data, implemented UI, prior design decisions, constraints, and open questions so the next pass starts from a single source.

## 1. Current implementation

### Data already reserved

`PawnWorkOrder` already stores:

```text
clothingPresetId       string, currently empty
clothingOverrides      List<PawnClothingOverride>
```

`PawnClothingOverride` already stores:

```text
workTypeDefName
apparelPolicyId
source                 Preset | Manual | Schedule | Emergency | Legacy
```

It is serialized in the save through `Scribe`, normalized, and included in policy validation.

There is **no** `ClothingPreset` model, store, resolver, or application layer yet.

### Current UI

`Dialog_WorkControlDetails.Clothing` is read-only. It shows:

- vanilla `Pawn.outfits.CurrentApparelPolicy`;
- currently worn apparel;
- `clothingPresetId` or `not configured`;
- stored clothing overrides;
- a note that automatic application is not implemented.

The Overview and Overrides tabs show clothing preset/counts but do not edit them.

### Current documented intent

From `docs/reference/policy-data.md`:

- `Pawn.outfits.CurrentApparelPolicy` is the applied clothing effect.
- Ordered Work owns the intent: clothing preset/rules and sparse overrides.
- Per-WorkType clothing is **intent plus a swap/equip layer**, not a parallel apparel resolver.
- Reserved clothing data should avoid a second save migration later.
- Clothing is deliberately later than work/schedule/zone.

## 2. Established design constraints

- Do not reimplement RimWorld apparel resolution.
- Do not clone `ApparelPolicy`/`ThingFilter` as a new authoritative model.
- Do not silently overwrite player edits.
- Do not change clothing by tick.
- Do not force gear during emergency/drafted/downed states without an explicit rule.
- Do not assume every pawn can reach a stand or storage.
- Do not make assigned workbenches/bills part of the clothing resolver.
- WorkType/Giver order controls job scanning; it does not assign benches or bills.
- Respect `RecipeDef.workSkill` and `Bill.allowedSkillRange` as vanilla constraints.

## 3. Prior design notes

From `docs/explanation/design-notebook.md`:

- Outfit/loadout is a **Profile/Role** concern, not a per-job automatic default.
- WorkType cards may show **recommended** outfit/tool, but recommendations are not automatic swaps.
- The Profile/Role owns the actual outfit/loadout and area.
- Trigger on **WorkType change**, not every job.
- If the current outfit/tool differs, the pawn should go to its assigned station, equip, work, and later return the previous set.
- Use a cooldown; skip if drafted, in emergency, downed, or the station is inaccessible.
- V1 recommendation: one work outfit per pawn/role, one stand; per-WorkType recommendations only.
- Multi-stand/locker and per-WorkType hard switching are later.
- Preferred-room/bench affinity should be a soft hint, not a hard restriction.

### Outfit Stand facts

`Building_OutfitStand` is **Odyssey DLC**, not base vanilla.

Relevant APIs/jobs:

- `UseOutfitStand` / `JobDriver_UseOutfitStand`
- `PutApparelOnOutfitStand` / `JobDriver_PutApparelOnOutfitStand`
- `innerContainer`
- `settings`
- `allowRemovingItems`
- `AddApparel`
- `RemoveApparel`
- `GetGizmos`
- `GetFloatMenuOptionToEquipWeapon`

Swapping takes equip delay and swaps overlapping slots. It can toggle between work and off-duty outfits.

## 4. External compatibility

Potential providers/adapters:

- **Vanilla `ApparelPolicy`** — first provider; smallest scope.
- **Better Pawn Control** — policy/outfit/area/drug/food; soft integration.
- **Compositable Loadouts** — tags/loadouts/per-pawn assignment; reference, do not duplicate its reservation model.

Rules:

- Compatibility/detection first.
- Reference external data; never copy a full loadout into Ordered Work as authoritative state.
- Optional adapters only after vanilla is stable.
- Do not require an external mod for the base feature.

## 5. Proposed model

### Global preset

```text
clothing_preset
  id
  name
  schema_version
  rules: WorkTypeDef -> apparel policy reference
```

Initial provider uses the vanilla `ApparelPolicy` identity.

### Per-pawn state

```text
PawnWorkOrder
  clothingPresetId
  clothingOverrides:
    workTypeDefName
    apparelPolicyId
    source
```

### Resolution order

```text
explicit pawn override
  > clothing rule for active WorkType
  > pawn clothing preset
  > current vanilla apparel policy
```

A missing provider/policy falls back without deleting the intent.

## 6. Application policy

### Recommended first version

- Provider: vanilla `ApparelPolicy`.
- Opt-in per profile/pawn.
- Trigger on WorkType change and game load.
- No per-tick switching.
- No automatic swap when a WorkType has no rule.
- Skip incapable/downed/drafted unless explicitly configured.
- Cooldown after a swap.
- If the pawn cannot reach the required stand/storage, leave the current outfit and record a warning.

### Later

- Outfit stand path with return-to-previous-outfit.
- External provider adapters.
- Multiple outfits / per-WorkType stands.
- Emergency/security override.

## 7. UI plan

`Details -> Clothing` should become editable:

- current vanilla policy;
- active preset selector;
- per-WorkType rule list;
- per-pawn overrides;
- source badges (`Preset`, `Manual`, `Schedule`, `Emergency`);
- missing provider/policy warnings;
- explicit `Apply now` only as a manual action.

Do not add a separate top-level Clothing tab in the first slice.

## 8. Tables, bills, workbenches

Clothing is independent from bench assignment.

- WorkType/WorkGiver order chooses what job the pawn looks for.
- Bills remain vanilla `Bill` objects on workbenches.
- `WorkGiver_DoBill` remains the job source.
- Do not assign a pawn to a specific bench.
- Do not override `Bill.allowedSkillRange`.
- If a future “preferred bench/room” is added, make it a soft priority hint only.

## 9. Open questions

1. **Provider**: start with vanilla `ApparelPolicy`, or integrate Compositable Loadouts/BPC from day one?
2. **Automation**: opt-in automatic application, or manual apply only in v1?
3. **Trigger**: WorkType change only, or also schedule-hour changes and load?
4. **Restore behavior**: after work, return the previous outfit, or leave the work outfit on?
5. **No stand**: skip silently, show a warning, or fall back to vanilla policy?
6. **Emergency/drafted/downed**: hard skip, or an explicit emergency outfit rule?
7. **Outfit granularity**: one outfit per pawn/role, or one per WorkType?
8. **Hidden WorkTypes**: should a hidden WorkType be allowed to own a clothing rule? If the pawn is incapable, skip it.
9. **Missing policy**: keep the override and warn, or clear the reference?
10. **External provider wins**: when a loadout mod changes the pawn, should Ordered Work record drift and not fight it?
11. **UI ownership**: preset editing in Mod Settings, per-pawn overrides in Details, or both?
12. **Save migration**: current reserved fields are enough, or add a schema version now?

## 10. Decisions still to make

Recommended first decision:

```text
Provider: vanilla ApparelPolicy
Automation: opt-in
Trigger: WorkType change + load
Granularity: one work outfit per pawn/role
Stand: optional; inaccessible stand warns and skips
External mods: adapters later
Benches/bills: untouched
```

No code should start until the provider and automation questions are answered.

## 11. Comparable mods

Surveyed 2026-11-19. These are references, not mandatory dependencies.

| Mod | What it does | Closest to our need | Notes |
|---|---|---|---|
| **Outfit Manager** (LordKuper) | Context-aware equipping using WorkType, job priority and environment; configurable work-type rules and score factors | Very close | Harmony + LordKuper.Common; CC-BY-NC-SA; source available. Strong reference for WorkType-aware application. |
| **Automatic Outfit Manager** | Work-area rules, work outfit vs personal outfit, gear return, primary weapon support, buffers, personal snapshots | Closest end-to-end automation | Work-area based rather than WorkType-based; RimWorld 1.6/1.7; large feature surface. |
| **Dress Code [NS]** | Work apparel policies, automatic outfit change before work; daily vs work policy | Closest per-WorkType policy | Chinese/English; requires DLC features in places; overlapping work scheme warnings. |
| **Outfitted 1.6** | Score-based apparel selection per outfit policy/role using stats, temperature and priorities | Automatic selection, not per-WorkType | Different model: score weights rather than explicit WorkType trigger; can combine with BPC. |
| **Auto Apparel Pickup** | Picks apparel by job/stat needs; separate Equip Armor gizmo | Automatic stat-based pickup | Reports of abandonment/1.6 era; useful as an example of failure modes, not a dependency. |
| **Automatic Swap Outfit** | Outfit stand swaps on area entry/exit; excludes jobs | Area-based stand automation | Requires Odyssey; not WorkType-based; comments mention drafted-only behavior. |
| **Gear Up And Go / Reforked** | Draft, equip active outfit, move; BPC preset switching | Explicit combat/gear-up workflow | Not automatic per WorkType; good model for opt-in command and restore behavior. |
| **Combat Draft** | Equips combat outfit and goes to position, restores after undraft | Situational combat-only automation | Vanilla outfit system; demonstrates explicit trigger and restore. |
| **Compositable Loadouts** | Per-pawn loadouts, tags, loadout manager | Loadout provider, not WorkType trigger | Installed; useful as an optional provider/adapter. |
| **Better Pawn Control** | Policies for work/schedule/outfits/areas/drugs/food | Policy assignment layer | Not a per-WorkType automatic outfit engine by itself. |
| **Loadout Cabinet** | Exosuit/loadout storage | Storage only | No policy trigger. |
| **Caravan Gear** | Caravan gear preparation | Caravan-only | Not daily WorkType clothing. |
| **Simple Sidearms - Reworked** | Weapon switching/sidearms | Weapon-side comparison | Not apparel policy. |
| **Vanilla Outfit Stand** | Manual outfit/tool swap; Odyssey DLC | Equipment station primitive | Base for optional stand workflow, not automatic WorkType policy. |

### What this means for Ordered Work

Ordered Work should not become a second full outfit optimizer.

The strongest niche is:

1. Read the active WorkType from the Ordered Work work order.
2. Select a stored `ApparelPolicy`/external loadout reference.
3. Delegate the actual equip/return to vanilla, Outfit Stand, or a detected provider.
4. Keep the decision explicit, opt-in, cooldown-bound and reversible.
5. Never compete with Outfit Manager/AOM/Compositable Loadouts when they already own the outfit.

The key product choice is whether Ordered Work is:

- **A policy layer**: store rules, display intent, delegate to an installed provider; or
- **A self-contained provider**: implement its own equip/return jobs and outfit stand logic.

The policy-layer route is smaller and composes better with existing mods. The self-contained route duplicates a lot of mature code.
## 12. Mechanism notes from downloaded mods

These are implementation observations, not endorsements.

### Outfit Manager (LordKuper)

- GitHub README describes it as a rewrite that adds **work type score evaluation to the apparel scoring algorithm**.
- It does not appear to introduce a separate outfit job; it influences the pawn's apparel choice and lets the vanilla optimizer perform the wear operation.
- Strongest pattern to borrow: **score/weight the candidate apparel for the active WorkType**, rather than hard-assigning a job.
- Requires `LordKuper.Common`; license CC-BY-NC-SA.

### Dress Code

- Ships explicit `WearDressCode`, `RemoveApparelDressCode` and `OptimizeApparelDressCode` job drivers.
- Inserts a `ThinkTreeDef` under `Humanlike_PostDuty` with `ThinkNode_ConditionalDressCode`.
- Adds assignments UI columns (`AllowDressCode`, `DressCode`) and a storage comp (`KeepRoomForApparel`).
- Uses per-WorkType work apparel policies, daily policies, race remapping and reservation layers.
- Strongest patterns: explicit policy per WorkType, reservation, job-driven change, and a daily/work distinction.
- Cost: invasive ThinkTree/job patches; compatibility surface is larger.

### Automatic Outfit Manager

- Uses area rules rather than WorkType rules.
- Provides special storage filters and hauling WorkGivers for managed apparel/weapons.
- Supports work outfits, personal outfits, lockers, task buffers, gear return and overlap warnings.
- Strongest patterns: work/non-work separation, return-to-personal gear, storage filters, retained stock and task buffers.
- Cost: large stateful system; likely overkill for the first Ordered Work slice.

### Auto Apparel Pickup

- Source is included; ships an `AAP_EquipArmour` job and an apparel search utility.
- Stat/job-driven pickup with a separate armor gizmo.
- Useful warning: automatic per-job apparel selection can cause churn and abandonment; needs explicit trigger/buffer rules.

### Automatic Swap Outfit

- Requires Odyssey.
- Extends the outfit stand with an assignment comp and an `AutoSwapOutfitToStand` job.
- Swaps on area entry/exit, not WorkType; can exclude jobs.
- Useful as the stand/area fallback model.

### Combat Draft / Gear Up And Go

- Use explicit player commands to equip a combat outfit and go to a location.
- Preserve or restore the prior outfit after undraft.
- Useful for emergency/combat policy, not normal daily work.

### Compositable Loadouts

- Loadout/tag manager with a `ThinkNode_LoadoutRealisation` and loadout state columns.
- Tags compose; auto wear/pickup/equip is its domain.
- Best treated as an optional **provider/loadout source**, not a competitor for WorkType selection.
## 13. Decisions from design discussion

Recorded decisions:

- The set contains **apparel and a tool/weapon**.
- The pawn returns the borrowed apparel to the place it took it from.
- **WorkType change is the trigger.** Schedule/hour data is only used to account for the time spent equipping.
- The module is **active by default**; the player can disable it.
- Search order:
  1. assigned mannequin/outfit stand;
  2. nearby storage within a configurable detection radius.
- If no stand/locker supplies a valid set, the pawn may use a valid set found in nearby storage.
- Apparel permissions remain authoritative. The system must integrate with them, not bypass them.
- After work, the pawn returns the work set and re-equips the previous personal outfit when available. If it is missing, choose the best valid permitted alternative.
- WorkType policy is the assignment layer. The loadout editor is the visual layer for composing the set.
- The UI should follow the Nice Inventory Tab direction:
  - visual body/slot grid;
  - show available apparel for each slot/layer;
  - choose pieces by slot;
  - priorities per piece;
  - onion-layer visualization for coverage;
  - compose a set visually before saving it to a WorkType.
- Per-pawn overrides can force/no clothing, prioritize pieces and keep items. This is a sparse override layer on top of the WorkType policy.
- Inventory-like retained items are a later extension; do not mix them into the first clothing slice.

### Nice Inventory Tab reference

Downloaded `3609897594` (`NiceInventoryTab`) uses `ApparelSlotFilter` defs with:

- grid `x`/`y`;
- body-part whitelist/blacklist;
- apparel layers;
- priority.

That is a strong reference for the visual slot composer. Do not copy assets or code without checking its license. Recreate the interaction model in Ordered Work.

## 14. Refined architecture

### Definitions

```text
ClothingPreset
  id
  name
  slots:
    body part / layer
    candidate apparel ThingDefs or filters
    priority
  tool requirement

WorkTypeClothingRule
  workTypeDefName
  clothingPresetId
  optional tool requirement

PawnClothingOverride
  workTypeDefName
  apparelPolicyId / preset reference
  forced / disabled / priority / keep
```

### Runtime

```text
WorkType changes
  -> select clothing preset for that WorkType
  -> enumerate valid apparel for each slot/layer
  -> apply pawn apparel policy/permissions
  -> find assigned stand or nearby storage
  -> create equip job / use stand
  -> record source and previous outfit
  -> after work: return set, restore previous or best valid personal outfit
```

### Search and applicability

- Validate each apparel candidate against:
  - pawn apparel policy/permissions;
  - body part/layer compatibility;
  - biocoding/ideoligion/royalty restrictions;
  - current condition/quality requirements if configured;
  - reachability and storage.
- A stand is preferred over loose storage.
- Radius is measured from a configured center: assigned workstation/room first, pawn position fallback.
- Cache availability on the pawn/preset; invalidate on storage changes or WorkType transition.

### Tool/weapon

- Store a tool/weapon requirement per set.
- Respect primary equipment slot and vanilla/ideoligion restrictions.
- Do not manage sidearms/ammunition in the first slice.
- The tool must be returned with the set or left in the assigned storage according to the same source rule.

## 15. Open questions after the new decisions

1. What exactly is “tool”: primary weapon only, or also work tools from other mods?
2. Default detection radius and center: assigned workbench, work room, or pawn?
3. How is “time to equip” measured: job duration, estimated ticks, or schedule buffer?
4. What does “best alternative” mean when the previous outfit is missing: quality, condition, armor, insulation, or preset priority?
5. Can a WorkType have multiple clothing presets (normal/emergency/seasonal)?
6. Which layer wins when two valid garments compete for the same slot/layer?
7. What happens when a stand contains only part of the set?
8. Should storage search be limited to a storage zone/filter or any reachable stockpile?
9. How do external loadout providers interact with the default-on module?
10. Which medicine/inventory mod defines the “keep items” inventory behavior?
## 16. Priority-layer model update

The set is not one rigid outfit. It is an ordered collection of apparel rules.

- A rule layer can contain multiple pieces across different body slots.
- Resolution walks the layers in priority order and fills each valid slot.
- A lower-priority layer is a fallback, not an automatic replacement of a higher-priority piece unless explicitly allowed.
- Priorities apply per **piece**, not only per complete set.
- Both the pawn override layer and the WorkType clothing layer can contribute rules; their merge/precedence still needs definition.
- This should feel like a loadout composer, not a single outfit dropdown.

Tools are also part of the set:

- Any equippable item usable for work, not only weapons.
- Vanilla primary equipment plus tool references.
- Simple Sidearms - Reworked is a compatibility/provider target for spare weapons/tools and nonviolent pawns; do not reimplement its sidearm inventory.
- Return the tool to its source and restore the previous equipment where possible.

## 17. Questionnaire

Answer these to freeze scope before implementation.

### A. Layer resolution

1. Are “priority layers” ordered rule tiers, body apparel layers (skin/middle/shell), or both?
2. Can one priority tier contain several pieces for different body slots?
3. Can it contain two competing pieces for the same slot/layer?
4. When a lower tier finds a valid piece for an empty slot, does it fill it, or only if no higher-tier piece exists at all?
5. When a higher-tier piece and a lower-tier piece compete, does higher priority always win?
6. Does a pawn override replace the WorkType layers, prepend to them, or coexist as another tier?
7. What wins ties: explicit priority, quality, condition, insulation/armor score, or pawn preference?

### B. Tools and equipment

8. Does “tool” mean any `CompEquippable` item, or only weapons/tools with work-relevant stats?
9. Should a set hold one primary tool, or multiple tools/sidearms?
10. Can nonviolent pawns use work tools, and should Simple Sidearms manage their spare tools?
11. What happens to the previous weapon/tool after the work set is returned?
12. Which Simple Sidearms API/version should be supported first: 1.7 Reworked only, or also original?

### C. Search and return

13. Detection center: assigned workstation, work room, pawn position, or a configured point?
14. Default radius in cells?
15. Stand/mannequin first, then stockpiles/lockers? Any other source?
16. If only part of the set is available, equip the partial set or skip?
17. Return each piece to the exact source, or to any compatible storage in the radius?
18. If the source is gone/full, where should it go?
19. After returning, restore the previous personal outfit exactly, or allow a better valid replacement?

### D. Trigger and time

20. WorkType change is the trigger. Does it also trigger on load, schedule change, or manual “refresh”?
21. Is the dressing time measured from the actual job duration, estimated ticks, or both?
22. Should the schedule reserve time for dressing, or only record/report it?
23. What if the work starts before dressing finishes: cancel, pause, or continue?

### E. Permissions and failures

24. Is the pawn’s apparel policy a hard allowlist for every searched piece?
25. Do ideology/royalty/biocode/body-type restrictions also apply per piece?
26. If no valid piece exists, skip silently, show a warning, or fall back to vanilla?
27. If the pawn is downed/drafted/emergency, skip or use an emergency set?

### F. UI

28. Should the visual grid show body slots, layer tiers, or both?
29. Can a WorkType have multiple presets (normal/emergency/seasonal)?
30. Where do per-pawn force/disable/priority/keep overrides live: Details only, or also a global list?
31. Do we need import/export for clothing presets?
32. Which inventory/medicine mod defines the “keep items” behavior?

### G. Compatibility and scope

33. Default-on module: if Compositable Loadouts/Outfit Manager is installed, should Ordered Work delegate or still run its own provider?
34. Should external provider selection be automatic, manual, or a setting?
35. First slice scope: apparel layers + one tool + return + WorkType trigger, with no retained inventory?
36. Are multi-pawn bulk assignment and preset sharing in the first slice or later?
## 18. Round 2 decisions and feedback

### Confirmed model

- Clothing is an **outfit stack**, not one set.
- Each stack layer can be a complete outfit, one item, or a partial set.
- Layers apply left-to-right and fill slots that are still available.
- Within a layer, the piece priority decides; ties go to stats, then quality.
- Multiple pieces can be assigned to different slots/layers.
- A simple layer may be shared by many WorkTypes; specific layers may belong to one.
- The visual grid must show available slots, candidates, and coverage/conflicts.
- The WorkType clothing editor is a separate Mod Settings tab from the per-pawn Details tab.
- Per-pawn overrides are the mandatory/locked layer for cases such as royals, mechanitors, psychics and prisoners.
- The module is default-on and can be disabled.
- Triggers: WorkType change, hourly refresh, manual refresh.
- Time spent dressing is job-time; the schedule is not reserved or rewritten.
- Search: assigned stand/mannequin, then compatible storage within a radius, then loose ground items.
- Radius starts at 20-50 cells and should be adjustable per building.
- Partial sets are allowed.
- Return each piece to a compatible storage; a complete stand set returns to the stand when possible.
- If no storage is available, leave the item where the next outfit is placed; if there is no next outfit, the pawn keeps it.
- No valid piece = skip and continue with the current gear.
- Emergency/downed/drafted: use an assignable emergency set.
- First slice may include retained inventory or defer it to the medicine/inventory integration.

### Key feedback

The listed order:

```text
vanilla outfit
  -> mandatory outfit
  -> work outfit
  -> free outfit
```

cannot be a simple left-to-right “fill empty slots” stack if the mandatory layer must **protect** pieces from being removed. A normal later layer can only fill empty slots; it cannot replace or lock an earlier piece.

Therefore the mandatory/override layer needs separate semantics:

```text
LOCKED MANDATORY PIECES
  -> WORKTYPE OUTFIT STACK
  -> PERSONAL/VANILLA BASE
  -> FREE FALLBACK
```

or an equivalent “locked” marker evaluated before the stack.

This needs a decision: mandatory as **pre-emptive lock**, or mandatory as a normal layer that only fills empty slots.

### Quality exception

“Mandatory can change for quality of the same piece” should mean:

- only the same `ThingDef`;
- same slot/layer;
- strictly better quality/condition according to the chosen rule;
- never a different item and never removing the mandatory slot entirely.

### Tools

- Tools are all equippable items usable by the pawn; not just weapons.
- Multiple tools are allowed if they serve different functions.
- Nonviolent pawns may use work tools but cannot attack with them.
- `Simple Sidearms - Reworked` is a reference/provider for spare tools and weapon switching.
- A known failure mode is remembered tools that the pawn refuses to release.
- Recommended: own tool provider first; make Simple Sidearms integration an explicit toggle, default off if it conflicts.

### Search anchor

- Table/assigned workstation is the first anchor.
- If no table, pawn position.
- Jobs without a table (mining, hunting, fishing, combat) need an alternative:
  1. an equipment-finding building; or
  2. storage shelves assigned to a role/WorkType.

Both are valid; first slice should choose one and not invent both.

### Equip timer

- A timed job is viable: `Job.expiryInterval` / timed driver ends dressing and lets the pawn resume work.
- Need to verify the exact vanilla API for expiry and interruption before implementation.
- The scheduler only needs refresh behavior; it does not reserve time.

### UI split

- **Mod Settings -> Clothing**: WorkType stack editor and preset library.
- **Details -> Clothing**: per-pawn mandatory/override/emergency/personal rules.
- They are different screens and different data, though both use the same slot-grid component.

## 19. Decisions still open

1. Mandatory layer: pre-emptive lock, or normal left-to-right layer?
2. Exact precedence of emergency vs mandatory vs work vs personal.
3. What “better quality same piece” permits exactly.
4. First-slice tool scope: one primary tool, or multiple tools immediately?
5. Simple Sidearms integration default: off, on, or detect-only.
6. Search anchor for tableless jobs: equipment building or assigned shelves.
7. Return with no storage: ground at next outfit location vs pawn keeps item.
8. Which medicine/inventory mod defines retained inventory.
## 20. Round 3: terminology and lock decision

Confirmed:

- **Vanilla outfit/ApparelPolicy is not a competing tier.** It is the permission layer that says which apparel the pawn is allowed to wear.
- Ordered Work selects the actual piece from the permitted candidates.
- **Mandatory/override is a pre-emptive lock.** It is evaluated before the work stack and cannot be removed by later tiers.
- A mandatory slot may be replaced only by the **same piece** with better quality/condition, according to the final rule.
- **WorkType clothing** is triggered by WorkType and must respect the mandatory lock and the apparel policy permissions.
- **Free** is the remaining state after mandatory + work rules; its exact behavior still needs definition.
- The module is default-on and disableable.
- Tools are separate equipment rules with the same permission/lock discipline.
- Provider is an execution adapter/chain of responsibility: it equips and returns; it does not choose the policy.
- Retained inventory, medicines and drugs are explicitly outside clothing and belong to a future medicine/inventory mod.

### Corrected layering

```text
1. ApparelPolicy permission gate
2. Mandatory lock: slot occupied, protected
3. WorkType outfit stack: fills remaining permitted slots
4. Free: undefined; either leave as-is or fill from permitted fallback
```

The mandatory lock is not “another outfit row”; it is a reservation of a slot/piece.

## 21. New questions

1. If a mandatory piece is **not permitted** by the pawn’s ApparelPolicy, does the lock win or does the policy win?
2. Does “same piece better quality” mean same `ThingDef`, same slot, or same visual/category item?
3. Does **free** mean:
   - leave the remaining slots as they are;
   - fill remaining slots with any permitted apparel; or
   - allow vanilla apparel optimization?
4. Can a mandatory lock reserve a **body layer** (e.g. “no outer layer”) rather than a specific piece?
5. Can mandatory lock include tools, or is the lock apparel-only?
6. Are multiple mandatory pieces allowed in the same slot if they use different body layers?
## 22. Policy and free-slot decisions

Confirmed:

- **The ApparelPolicy is law.** Ordered Work never forces apparel that the pawn’s policy denies.
- If a mandatory lock cannot be satisfied because the policy denies the piece:
  - do not force it;
  - do not silently replace the policy;
  - show an alert that the pawn has a contradictory clothing policy.
- A mandatory lock competes only inside the permitted set.
- **Free does not mean “best outfit chosen by Ordered Work”.**
- Free means: the slot was not assigned by any clothing rule.
- A free slot is left to the pawn/vanilla apparel behavior, still filtered by the ApparelPolicy.
- Example: if shoes were not assigned, a pawn may naturally wear allowed shoes.
- Ordered Work only manages the slots/pieces it explicitly assigned; it must not claim ownership of free items.

### Managed vs free

```text
assigned slot -> Ordered Work chooses/equips/returns
free slot     -> no Ordered Work rule; vanilla/policy decides
```

The remaining conflict to define:

- If an assigned work piece needs a slot currently occupied by a **free** item, does the assigned piece:
  1. displace the free item and record it for restoration;
  2. skip because the slot is already occupied; or
  3. fail and alert.

This is the next scope decision.
## 23. Final priority order

Confirmed order, highest to lowest:

```text
1. Locked / reserved (mandatory)
2. Emergency
3. WorkType
4. Free
```

- Locked/reserved always occupies its slot first.
- Emergency can replace work/free, but cannot replace a locked/reserved piece.
- WorkType applies while working; it can replace free but not emergency or locked.
- Free is the last fallback: only slots without a higher rule, and only items allowed by the vanilla ApparelPolicy.
- The vanilla ApparelPolicy is authoritative for **quality and durability** as part of its filter. Ordered Work does not invent a parallel quality/durability gate.
- A work piece may displace a free item because work outranks free; the displaced free item must be tracked for restoration.
- A locked piece denied by the ApparelPolicy is a policy contradiction: alert, do not force, and do not silently replace the policy.

### Resolution example

```text
Locked boots
  > emergency armor
  > work coat
  > free/vanilla shirt, pants, shoes
```

The exact restoration of a displaced free item remains a tracking requirement, not a priority question.
## 24. Emergency v1

The emergency layer stays above work/free and below locked/reserved.

Proposed first trigger set:

1. **Drafted and stationary** — the pawn is drafted and the player is not issuing a move/attack order.
2. **Manual gizmo** — a player-triggered “gear up” action, similar to an ammo/reload command.
3. **Undrafted / no longer in emergency** — restore the previous state.

Behavior:

- Auto mode does not interrupt an active move order.
- While stationary, search for a valid emergency set near the pawn/current room first.
- If no valid set is nearby, skip and warn; do not path across the map automatically.
- The manual gizmo may use a wider search radius.
- Emergency gear respects locked pieces and ApparelPolicy permissions.
- Emergency does not override a locked slot.
- On exit, return emergency gear and restore the work/personal/free state.

Later expansions:

- raid/security mod integration;
- downed/medical emergency sets;
- combat movement coverage;
- station/locker routing.

Open detail:

- Does “stationary out of combat” mean **drafted stationary**, **undrafted idle**, or both?
- Should auto drafted-stationary be default-on or opt-in?
## 25. Emergency/response and medicine mod evaluation

### Evaluation criteria

For every candidate mod, record:

- exact problem it solves;
- trigger model (drafted, raid, job, area, manual gizmo);
- whether it changes vanilla state or owns a parallel system;
- return/restore behavior;
- save safety and API/source license;
- DLC or hard dependencies;
- conflict risk with Ordered Work;
- whether Ordered Work should reference, delegate, or ignore it.

### Emergency/response candidates

| Mod | Role | Initial classification |
|---|---|---|
| **Combat Draft** | Equip combat outfit, draft and move, restore after undraft | Strong reference; candidate optional provider |
| **Gear Up And Go / Reforked** | Explicit gear-up command, BPC preset switching, movement | Strong reference; optional command bridge |
| **Simple Sidearms - Reworked** | Spare weapons/tools, nonviolent work tools, retrieval | Tool provider/reference; not clothing |
| **TakeCover** | Drafted cover movement | Out of scope; do not couple |
| **Rules of Engagement** | Combat behavior/response | Needs deeper source/research before classification |
| **Reload / Ammo Readout / Ammo UI** | Ammunition/reload gizmo patterns | Reference for explicit gizmo and cooldown, not apparel |
| **Automatic Outfit Manager** | Work/non-work area outfits, return, lockers | Strong reference for emergency/restore design |

### Medicine/integration candidates

| Mod | Role | Initial classification |
|---|---|---|
| **Choose Your Medicine** | Medicine quality by need | Medical policy provider candidate |
| **Pharmacist: Represcribed** | Medicine level by injury severity | Medical policy provider candidate |
| **Medical Tab (Continued / Reborn)** | Medical overview UI | Reference/optional integration |
| **Progression: Drugs** | Drug research/overlap | Out of scope for clothing |
| **Priority Treatment Ressurected** | Doctor work priority | Out of scope; do not couple |
| **Multi-Doctor Surgery** | Surgery staffing | Out of scope |
| **Nice Inventory Tab** | Visual inventory/slot UI | UI reference for loadout composer |
| **Compositable Loadouts** | Loadout tags and auto wear/equip | Loadout provider candidate |

### Preliminary conclusion

- Emergency clothing should be its own layer, not a dependency on Combat Draft.
- The first emergency implementation should be a manual gizmo and an optional drafted-stationary trigger.
- Medical policy should not be part of the clothing resolver. It is a separate provider/domain integration.
- Do not hard-depend on any emergency or medicine mod.
- If an external provider is detected, expose a setting: use it, use internal provider, or disable that domain.

### Open evaluation questions

1. Does “medicine integration” mean medicine quality policy, drug policy, medical inventory retention, or all three?
2. Which medicine mod is the reference: Choose Your Medicine, Pharmacist, or another?
3. Should emergency use the same clothing stack editor or a separate compact emergency preset?
4. Does the emergency gizmo use the same search radius as normal clothing?
5. Should Combat Draft/Gear Up be integrated as providers, or only used as behavioral references?
6. Is “Rules of Engagement” relevant to clothing/response, or only combat AI?
## 26. Emergency response model

Emergency is not only “drafted outfit”. It has three layers of behavior:

```text
1. Draft / undraft state
2. Proximity threat
3. Reaction mode: Attack | Flee | Ignore
```

### Draft / undraft

- Draft can trigger an emergency gear check.
- Undraft returns emergency gear and restores WorkType/personal/free state.
- A drafted pawn with a move/attack order is not interrupted for optional gear changes.

### Threat proximity

- A nearby threat can trigger an emergency check even outside a simple draft toggle.
- The check should be conservative: radius, threat type and line of sight are configurable.
- The pawn should not path across the map while a threat is active.

### Reaction modes

- **Attack**: ensure emergency gear is sufficient, then engage.
- **Flee**: prioritize immediate escape; do not start a long equipment search.
- **Ignore**: keep current behavior; no emergency gear change.

### Gear sufficiency gate

Before searching, compare the current gear against the emergency requirements.

- If current gear already satisfies the emergency threshold, do not search.
- Do not strip/replace good gear just because a technically different item exists.
- The gate should consider required pieces, quality/condition, and policy permissions.
- This prevents “already armed but running to a locker” behavior.

### Priority

```text
Locked
  > Emergency
  > WorkType
  > Free
```

Emergency never removes Locked, and never bypasses ApparelPolicy.

### Scope split

- Emergency clothing/tool use: our provider.
- Security/medical mods: domain adapters, not reimplemented.
- If a security or medicine mod already owns a complete domain, expose a setting to delegate and disable our partial provider.
## 27. Simplification review

The proposed `ClothingStack -> ClothingTier -> ClothingRule` model can be flattened without losing behavior.

### Keep

- Multiple priority layers: represented by ordered rules.
- Piece priority: rule order inside the preset.
- Locked / Emergency / WorkType phases: represented by a phase/kind field.
- Multiple outfit layers or reusable outfit blocks: represented by an optional visual group or `OutfitRef`.
- Tools: same rule shape with a tool target.
- Free: absence of a rule; no `FreeRule` object.
- ApparelPolicy permissions: external filter, not a stack tier.
- Preview/conflict reporting: derived from resolution.

### Proposed flat model

```text
ClothingPreset
  id
  name
  schemaVersion
  rules: ordered ClothingRule[]

ClothingRule
  phase: Locked | Emergency | Work
  priority: int
  group: string?        // optional visual outfit block
  target: Apparel | Tool
  slot / body layer
  candidates: ThingDef / filter refs
  mandatory: bool
```

Resolution:

```text
1. Sort rules by phase, then priority.
2. Evaluate Locked first, Emergency second, Work third.
3. Fill available slots/layers.
4. Leave unmatched slots as Free.
5. Filter every candidate through ApparelPolicy.
```

### What is lost

- No separate `ClothingStack` object.
- No separate `ClothingTier` object.
- No `Free` rule object.
- No mandatory three-level hierarchy.

### What is preserved

- Multiple layers.
- Piece priority.
- Locked pre-emption.
- Emergency override.
- WorkType trigger.
- Tools.
- Free fallback.
- UI grouping via `group`.
- Future reusable outfit blocks via optional `OutfitRef`, added only when needed.

### When to add the second level back

Add an `OutfitDefinition` only if:

- the UI needs reusable named outfit blocks shared across presets;
- users need to reorder entire outfits as a unit;
- import/export needs stable outfit references.

Until then, one preset with ordered rules is sufficient and much simpler.
## 28. Phase 1.5 status

Implemented:

- `ClothingPresetStore` JSON persistence.
- `ClothingContextBuilder` read-only adapter:
  - ApparelPolicy permits apparel.
  - `ThingDef.HasComp(typeof(CompEquippable))` supplies tool candidates.
  - Apparel body-part/layer catalog supplies known slots.
  - Tool rule slots are added to the catalog.
- Null-safe normalization for presets/rules/candidates.
- 39 tests total.

Not implemented:

- UI;
- job/equip/return;
- provider execution;
- emergency;
- Simple Sidearms;
- medicine/security;
- retained inventory.
## 29. UI plan

### Principle

Start with a **read-only preview** in Details. Do not start with the full editor.

The resolver already exists; the first UI should expose its result and make conflicts visible. Editing comes after the preview is useful.

### Screen 1 — Details -> Clothing preview

Reuse `Dialog_WorkControlDetails.Clothing`; replace the current read-only text with:

```text
[Pawn] [WorkType: current]
  ApparelPolicy: current policy / not configured
  Preset: current / none
  Refresh preview [button]

  Body slot grid
  Stack rules / selected pieces
  Conflicts
  Free slots
```

Components:

- `ClothingPreviewPanel`
- `ClothingSlotGrid`
- `ClothingSelectedList`
- `ClothingConflictList`
- `ClothingSummary`

States:

- selected / locked / emergency / work / free;
- policy denied;
- slot occupied;
- missing candidates;
- no preset.

No `Equip now`, no jobs, no pathfinding.

### Screen 2 — Mod Settings -> Clothing editor

Separate tab, not mixed into WorkType order:

```text
Left: WorkType list
Center: body slot grid
Right: selected rules / priority
Bottom: candidate palette
```

Features:

- select WorkType;
- select/create preset;
- add/remove rule;
- choose phase (`Locked`, `Emergency`, `Work`);
- choose slot/body layer;
- add candidates;
- reorder rules;
- group labels;
- preview conflicts;
- duplicate/delete preset.

Use the Nice Inventory Tab grid as interaction reference, not as copied code/assets.

### Screen 3 — Per-pawn overrides

In Details, after preview:

- force / disable a rule;
- prioritize a piece;
- set emergency preset;
- keep item (later inventory slice).

### Visual language

- Green: selected.
- Blue: locked.
- Amber: emergency.
- Gray: free/unassigned.
- Red: conflict/policy denied.
- Dim: candidate unavailable.

### Implementation order

1. Read-only Details preview.
2. Slot-grid component driven by `ClothingContextBuilder`.
3. Stack/conflict lists.
4. Global editor.
5. Per-pawn overrides.

No execution UI until the preview is validated.
## 30. Read-only preview implemented

- `ClothingPreviewPanel` in Details -> Clothing.
- Shows active WorkType, ApparelPolicy, preset (assigned or preview fallback).
- Uses cached context/resolution (60 ticks).
- Draws slot grid, selections, conflicts and free-slot count.
- No equip, jobs, pathfinding or state mutation.

### Next UI

1. Details preview polish:
   - preset selector/refresh;
   - slot tooltips and candidate details;
   - conflict details;
   - emergency preview state.
2. Mod Settings -> Clothing editor:
   - WorkType list;
   - preset library;
   - body grid;
   - rule stack;
   - candidate palette;
   - phase/priority controls;
   - validation and duplicate/delete.
3. Per-pawn overrides:
   - force/disable;
   - priority;
   - emergency assignment;
   - keep items later.
## 31. Functional UI pass

Implemented:

- `WorkTypeConfig.ClothingPresetId` stores the WorkType assignment.
- `Dialog_ClothingOverview` lists every WorkType and assigned preset.
- `Dialog_ClothingEditor` is the shared editor:
  - WorkType selector;
  - preset selector;
  - create/delete preset;
  - add/delete/reorder rules;
  - phase, target, slot, body layer, priority, mandatory and comma-separated candidates;
  - save preset;
  - assign preset to WorkType.
- Work Control has a `Clothes` button opening the overview.
- Details preview resolves the WorkType assignment first.

Still basic:

- no body-grid rule composer in the editor;
- no ThingDef picker yet;
- no per-pawn overrides;
- no equip/return jobs;
- no emergency or external providers.
## 32. Candidate picker

- Added a searchable `Dialog_ThingDefPicker`.
- Apparel target lists `ThingDef.IsApparel`.
- Tool target lists non-apparel `CompEquippable` defs.
- One candidate is appended per pick; comma-separated text remains editable.
- No visual body grid or multi-select yet.
## 33. Visual editor pass

Implemented:

- Editor slot grid built from live apparel body parts/layers.
- Clicking a slot selects an existing matching rule or creates one.
- Rule list and grid share the same selected rule.
- `ClothingIconResolver` reads `ThingDef.uiIcon`.
- `ClothingInfoService` exposes label, defName, body layers, body parts and description.
- Picker and grid show icons plus essential tooltips.
- No multi-select, drag-and-drop, layer painting or equip jobs yet.
## 34. Multi-select candidate picker

- `Dialog_ThingDefPicker` supports checkbox multi-select.
- Initial candidates are preselected.
- `Add selected` appends all selected defs to the rule.
- Picker remains searchable and keeps icons/tooltips.
- No drag/drop or layer painting yet.
## 35. Touch cards and grid removal

- Candidate rows are now information cards, not button rows.
- Tapping a card toggles its selection; `Add selected` applies the set.
- Cards show icon, label/defName, layers and body parts.
- Right-clicking a grid cell removes the matching rule.
- Drag/drop is intentionally not used.
- Multi-select remains card-toggle based.