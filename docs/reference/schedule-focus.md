# Schedule focus reference

## Runtime fields

| Location | Field | Meaning |
|---|---|---|
| `PawnWorkOrder` | `order` | Stored base work order; never modified by focus |
| `PawnWorkOrder` | `hourFocusOverrides` | Sparse hour → ordered defNames; max 3 per hour |
| `PawnHourFocusOverride` | `hour` | Local hour, 0–23 |
| `PawnHourFocusOverride` | `workTypeDefNames` | Ordered focus entries |
| `PawnHourFocusOverride` | `source` | Policy provenance |
| `WorkFocusPreset` | `Slots` | 24 lists of at most 3 defNames |

`HourFocusStack.MaxFocus` is the single slot cap.

## Effective order

```text
valid focus entries in focus order
+ base order entries not already emitted
```

`BuildEffectiveOrder` skips entries outside the base list and entries rejected by the availability predicate. The base list is not mutated.

## Application contract

| Event | Behavior |
|---|---|
| Hour boundary | Resolve the entering hour and apply if the effective signature changed |
| No valid focus | Use the base order |
| Focus removed | Restore the base order at the next boundary |
| Missing WorkType | Skip that entry and continue |
| Per tick | No work |
| Current job | Never stopped |

## UI controls

| Control | Effect |
|---|---|
| Assignable rows | Up to 3 ordered focus entries |
| `Cambiar` | Replace one row from the pawn's eligible work list |
| Right-click row | Remove one focus entry |
| Right-click hour cell | Clear all focus for that hour |
| `Limpiar todo` | Remove all per-pawn focus overrides |
| `Copiar hora` | Copy the selected hour's focus into local memory |
| `Pegar hora` | Validate and replace the selected hour's focus |
| `Guardar preset` | Save 24 × 3 focus entries to JSON |
| `Cargar preset` | Load a preset, validating against the current pawn |

## Files

| Path | Role |
|---|---|
| `Source/OrderedWork.Core/HourFocusStack.cs` | Pure normalization and effective order |
| `Source/Config/WorkFocusPreset.cs` | Portable 24 × 3 preset model |
| `Source/Config/WorkFocusPresetStore.cs` | Atomic JSON store |
| `Source/Services/HourFocusService.cs` | Hour-boundary runtime overlay |
| `Source/UI/Dialogs/Dialog_WorkControlDetails.Focus.cs` | Editor drawing |
| `Source/UI/Dialogs/Dialog_WorkControlDetails.Focus.Actions.cs` | Copy/paste, clear, picker, order |
| `Source/UI/Dialogs/Dialog_WorkControlDetails.Focus.Presets.cs` | Preset capture/apply |
| `Source/UI/Dialogs/Dialog_FocusPreset.cs` | Preset list dialog |
| `Languages/<locale>/Keyed/OrderedWork.ScheduleFocus.xml` | UI strings |

## Storage

- Focus presets: one JSON file per preset under `Config/OrderedWork/focus`.
- Save path: temp file, then `File.Replace`; existing files get `.bak`.
- Schema and normalization run on load and save.
- Presets contain no pawn, map, colony or timetable data.
- Per-pawn focus lives in the save; it is not duplicated in JSON.

## Related documents

- `docs/explanation/schedule-focus.md` — rationale and boundaries.
- `docs/how-to/schedule-focus.md` — user workflow.
- `docs/reference/ui-layout-rules.md` — layout constraints.
