# Hourly schedule focus

The hourly focus is a temporary priority overlay for the pawn's existing work order. It lets the player promote up to three WorkTypes in a given hour without rewriting the pawn's saved order. Schedule and focus are independent: either can be used without the other.

## Quick path

1. Open a pawn's detail panel and select **Horario**.
2. Click an hour in the **Foco** row.
3. Add up to three WorkTypes in the inline editor.
4. Wait for the next hour boundary; the focus applies silently.
5. Use **Copiar hora** / **Pegar hora** or **Guardar preset** for reuse.

## Details

| Topic | Decision |
|---|---|
| Source of truth | `PawnWorkOrder.order`; focus never rewrites it |
| Per-pawn storage | Sparse `hourFocusOverrides`, max 3 ordered defNames per hour |
| Effective order | Valid focus entries first, then base entries without duplicates |
| Invalid focus | Skip it and continue with the next focus; fall back to base #1 |
| Application | Once per hour boundary; never per tick or frame |
| Current job | Never `StopJob`; sleep, food and recreation continue |
| Schedule dependency | None; focus works without a schedule preset |
| Focus preset | Separate 24 × 3 JSON template; not a live link |
| Copy/paste | Current hour only, local memory; validation at apply time |
| Clear | Right-click an hour cell clears it; **Clear all** clears every override |

## Data flow

```text
PawnWorkOrder.order
        +
hourFocusOverrides[hour]
        ↓
HourFocusStack.BuildEffectiveOrder
        ↓
WorkOrderService.Apply(quiet)
        ↓
vanilla priorities at the next hour boundary
```

The effective order is derived, not persisted.

## UI contract

- **Asignable** rows: up to 3, ordered, draggable.
- Each row has passion/level metadata and a `Cambiar` action.
- Right-click an assignable row removes that entry.
- Right-click an hour cell clears all focus for the hour and reports the ordered names.
- **Visible** strip: effective order, first five entries, horizontal scroll for the rest.
- `Copiar hora` copies only the assignable hour focus.
- Paste validates against the target pawn's base list and capabilities.
- If no pasted entry is valid, the previous focus is preserved.
- Partial invalid paste applies valid entries and reports omissions.

## Files

```text
Source/OrderedWork.Core/HourFocusStack.cs
Source/Config/WorkFocusPreset.cs
Source/Config/WorkFocusPresetStore.cs
Source/Services/HourFocusService.cs
Source/UI/Dialogs/Dialog_WorkControlDetails.Focus.cs
Source/UI/Dialogs/Dialog_WorkControlDetails.Focus.Actions.cs
Source/UI/Dialogs/Dialog_WorkControlDetails.Focus.Presets.cs
Source/UI/Dialogs/Dialog_FocusPreset.cs
Languages/<locale>/Keyed/OrderedWork.ScheduleFocus.xml
```

## Storage rules

- Focus presets are one JSON file per preset under `Config/OrderedWork/focus`.
- Writes use a temp file plus `File.Replace`; existing files get a `.bak`.
- Schema and normalization run on load and save.
- Presets contain no pawn, map, colony or timetable data.
- Per-pawn overrides live in the save, not duplicated in JSON.

## Extension points

- Multiple focus entries per hour already fit the list model; keep the cap in `HourFocusStack.MaxFocus`.
- A new consumer should read `PawnWorkOrder.order`, never the temporary vanilla priorities.
- Do not add schedule behavior to the focus store or focus behavior to the schedule store.

## Checklist

- [ ] Base order unchanged after focus and after clear.
- [ ] First focus unavailable falls through to the next focus.
- [ ] All focus unavailable falls through to base #1.
- [ ] Apply happens only at an hour boundary.
- [ ] Copy hour with no entries is rejected.
- [ ] All-invalid paste preserves the old focus.
- [ ] Partial paste reports omitted entries.
- [ ] Clear all removes only focus overrides.
- [ ] Build 0/0 and tests pass.
