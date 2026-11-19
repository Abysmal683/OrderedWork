# How to configure hourly focus

Use this when a pawn should prioritize specific WorkTypes during one hour without changing their saved work order.

## Add focus to an hour

1. Open the pawn detail panel and select **Horario**.
2. Click the target hour in the **Foco** row.
3. Use `+ Agregar trabajo` and choose up to three WorkTypes from the pawn's work list.
4. Drag rows to change focus order; use `Cambiar` to replace one row.
5. Wait for the next hour boundary. Focus applies automatically.

An unavailable WorkType is skipped; the next focus is used, then the base order's first entry.

## Remove focus

- Right-click one assignable row to remove that entry.
- Right-click an hour cell to clear the whole hour; the message lists the removed entries.
- `Limpiar todo` removes every focus override for the pawn.

Clearing focus never changes `PawnWorkOrder.order`.

## Copy one hour

1. Select the source hour.
2. Click `Copiar hora`.
3. Select another hour or pawn.
4. Click `Pegar hora` and confirm the message.

Paste replaces the target hour's focus. Entries not in the target's work list or not capable are omitted. If none are valid, the previous focus is preserved.

## Save and load a focus preset

1. Click `Guardar preset`.
2. Enter a name and save.
3. Select another pawn and click `Cargar preset`.
4. Choose the preset; valid entries are applied and invalid entries are reported.

Presets contain only 24 hours × 3 ordered WorkTypes. They do not contain pawns, maps, schedules or apparel policies.

## Verify

- The target pawn's saved work order is unchanged.
- Focus applies at the next hour boundary, not immediately.
- Sleep, food and recreation are not interrupted.
- Copying an empty hour is rejected.
- All-invalid paste preserves the previous focus.
- See `docs/reference/schedule-focus.md` for fields and `docs/reference/ui-layout-rules.md` for layout constraints.
