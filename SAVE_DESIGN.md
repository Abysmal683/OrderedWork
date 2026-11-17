# Ordered Work — save/config design

Status: profile JSON is wired to the Rules UI. Global rule preferences live in ModSettings XML; profile state lives in JSON; per-pawn orders live in the save.

## 1. Two separate scopes

### Global / installation
WorkType order, giver order, splits, merges, descriptions and visibility are global mod configuration.

Stored outside the colony save:

```text
Config/OrderedWork/
  profiles/
    default.json          active default profile
    default.pending.json  edits not applied until RimWorld starts
    <name>.json
    <name>.pending.json
  reference.json          captured vanilla/original state
  active-profile.txt      selected profile name
  backups/
```

The active profile is selected globally. A colony save never stores the global profile contents. If a future requirement makes the profile per colony, add a `profileName` field to the colony save and resolve it at load; do not silently switch profiles.

### Per save / per pawn
Only pawn-specific data lives inside the save:

```text
PawnWorkOrder
  Pawn reference
  order: [WorkTypeDefName...]
  giverOverrides: [
    { giverDefName, targetWorkTypeDefName }
  ]
  thingIdNumberFallback
```

`Pawn` uses `Scribe_References.Look`, which is the unique save-scoped identity and survives resurrection. `ThingIDNumber` is diagnostic/failure fallback only; names are never identities.

## 2. Files

### active profile
Full current state. Editing targets this profile; changes are written to its `pending` file, not applied to live defs.

### pending file
Full candidate state for the active profile. It is written atomically. On RimWorld startup:

1. resolve profile by fallback chain;
2. validate pending;
3. apply pending to the live defs;
4. promote pending to active;
5. keep a last-good backup.

If the game closes forcibly, the live current colony was not mutated by the pending file.

### reference file
Immutable-ish snapshot of the original vanilla/modded state used by reset and diff.

- Captured once.
- New givers/types added by mods are appended, never inserted, and reported.
- Missing mods are reported; entries are not silently deleted from the profile.
- Reset uses `reference` to restore vanilla WorkTypes and to decide whether a type is a split.

## 3. JSON schema

```json
{
  "schemaVersion": 3,
  "profile": "default",
  "createdUtc": "2026-11-17T00:00:00Z",
  "mods": [
    { "packageId": "Ludeon.RimWorld", "name": "Core" },
    { "packageId": "author.mod", "name": "Some Mod" }
  ],
  "workTypes": {
    "Patient": {
      "vanilla": true,
      "base": null,
      "label": "paciente",
      "description": null,
      "visible": true,
      "order": 1,
      "mod": "Ludeon.RimWorld",
      "givers": [
        { "defName": "PatientGoToBed", "order": 1, "mod": "Ludeon.RimWorld" }
      ]
    },
    "OrderedWork_Paciente_1": {
      "vanilla": false,
      "base": "Patient",
      "label": "Paciente emergencia",
      "description": "Prioriza emergencias",
      "visible": true,
      "order": 24,
      "mod": "nightmare123487.OrderedWork",
      "givers": [
        { "defName": "DoctorTendEmergency", "order": 1, "mod": "Ludeon.RimWorld" }
      ]
    }
  }
}
```

Rules:

- `workTypes` is a map keyed by `defName`; no split/merge history lists.
- A giver appears exactly once in the whole profile.
- `order` is the final display order; gaps are normalized on load.
- `originals` are not embedded per profile; `reference.json` is separate.
- `mods` lists every contributing package id so missing/added mods can be detected.
- Per-giver `mod` allows isolating a giver removed with its mod.
- No Pawn data in JSON.

## 4. Reconciliation on load

Order:

1. Read active/pending.
2. Validate schema and required keys.
3. Remove duplicate givers: keep the first owned entry and log.
4. Remove givers whose def is missing; append a report entry.
5. Hide WorkTypes whose `visible=false`.
6. Delete splits with no givers.
7. Vanilla WorkType with no givers: `visible=false`.
8. Missing mods: report; do not rewrite the file automatically.
9. New givers from loaded mods: append at end with `order = max+1`; report a popup/list.
10. New WorkTypes: append at end; report.
11. Normalize order.
12. Apply to defs and rebuild caches.

Popups must list the concrete names, not just "some mods changed".

## 4.0 Modified state\n\nDo not persist a `modified` boolean. It goes stale after merges, mod changes, profile copies or manual edits. Compute modification by comparing the active profile with `reference.json`; there are only ~100 givers, so the cost is negligible and the result is always consistent. `modified` may be shown as a derived UI value, never written to disk.\n\n## 4.1 Giver identity

- Key givers by **defName**, never by `giverClass`.
- Core has no duplicate WorkGiverDef defNames, but it intentionally repeats giver classes: `WorkGiver_DoBill` appears in 18 defs, and `ConstructDeliverResourcesToFrames`/`ToBlueprints` exist in both Construction and Hauling under different defNames. Duplicate `workType+priority` also occurs. Do not deduplicate by class or priority.
- A split/merge moves the selected defName only. It must preserve sibling defs with the same class and intentional priority ties.

## 4.2 Destroyed pawns

`PawnWorkOrder` uses the `Pawn` reference as identity. Cleanup removes entries whose pawn is destroyed by default. A General setting `Keep orders for destroyed pawns` can preserve them for mods that return fully deleted pawns. Dead-but-not-destroyed pawns always keep their order.

## 5. Reset model

Order tab lists only changed WorkTypes, computed by diffing the active profile against `reference.json`.

- Vanilla changed: button `Restore` returns the reference givers, order and visibility.
- Split: button `Delete` removes it and returns its givers to its base.
- No separate historical split/merge entry to reset.
- `Allow empty origin` stays as an explicit checkbox in the Split/Merge sheet, not General.

## 6. Profile manager

General tab:

- active profile dropdown;
- New profile (copy current/default);
- Duplicate;
- Rename;
- Delete (default protected);
- Save profile;
- Select active.

Load chain:

1. selected named profile;
2. `default` with alert;
3. generated vanilla profile with alert.

A profile is never switched silently.

## 7. Open decision

Should the active profile be global for the installation, or selected per colony save?

Recommendation: global for the installation now; per-save profile binding later only if explicitly required, because it needs a stable save id and a profile selection on load.

## Global rule preferences (ModSettings XML)

These are global for every profile and are not part of the profile JSON:

```text
Config/Mod_OrderedWork_OrderedWorkMod.xml
  recommendationPins
  passionWeights
  prioritizeLowSkillPassion
  verboseDiagnostics
  UI preferences (height, colors, auto settings)
```

They are read live by the recommendation service, so changing pins or passion weights applies without restarting. The XML is written by RimWorld when settings are saved or the game closes.

## Passion-mod compatibility

`Source/Services/PassionSource.cs` enumerates VSE `PassionManager.AllPassions` through reflection. Vanilla `None/Minor/Major` are the fallback when VSE is absent. Mods that register `PassionDef` through VSE (including Alpha Skills) expose their passions and labels automatically. A `DefDatabase<VSE.Passions.PassionDef>` scan is the fallback when a mod does not populate `AllPassions`; entries are deduplicated by `defName`. `PassionWeightPolicy` owns defaults, migration and reset; `PassionWeightsView` owns the table; `RecommendationScorer` owns scoring.

- Weights are keyed by `defName`, never by enum numeric value.
- Unknown custom passions default to `Level 0 = 0`, `Per level = 0`, `Min level = 0`.
- Names containing `Major`, `Minor`, `None`, `Disabled` or `Disinterested` receive the matching vanilla defaults.
- Entries for disabled mods are retained, not deleted, so re-enabling the mod preserves the player's weights.
- No third-party code or assets are copied.