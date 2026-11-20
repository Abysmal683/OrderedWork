# Split / Merge / Order editor — scope

Status: implemented in Mod Settings. The `Rules` tab holds the WorkType order and the three-mode `Split / Merge / Order` editor. The editor is profile-backed; drafts are UI-only.

## Shared layout

- Mode switch: `Split`, `Merge` and `Order`.
- Split and Merge use two columns:
  - Left: **Origin** WorkType selector and its Givers.
  - Right: the destination list.
- Lists and columns show at most **10 rows**. A scrollbar and `Ctrl+wheel` scroll the hovered list or column; a plain wheel scrolls the outer settings.
- Candidate lists appear below the editor warnings/actions.
- Action buttons have tooltips explaining their effect.
- `Cancel draft` only clears unsaved draft state. It never changes a saved profile entry.
- Disabled action labels are centered in the native button body.

## Split

- Required before `Add split` enables:
  - a non-empty name;
  - a valid origin WorkType;
  - at least one origin Giver in the split draft.
- Optional `Action word` maps to the WorkType `gerundLabel`; when blank it falls back to the display name. It is editable while creating and in the split metadata editor.
- Optional `Description` is shown in WorkType tooltips.
- The generated `defName` and `Base` are immutable after creation.
- Existing splits are listed with `Edit` and a two-step `Delete`. Deleting a split removes its profile entry and returns its Givers to `Base`; deletion is blocked when `Base` is missing.
- The split metadata editor can change only `Label`, `Action word` and `Description`.

## Merge

- Merge candidates are WorkTypes whose Giver set differs from `reference.json` in `missing` or `extra` counts. Split WorkTypes are excluded.
- Selecting a row loads the existing Merge editor. `Revert` uses a two-step confirmation and restores the selected WorkType from the reference.
- Extra Givers are returned to the reference WorkType that owns them; missing Givers are removed from their current profile owner and restored to the selected WorkType.
- Merge moves Giver ownership; it never deletes a WorkType or WorkGiver. The origin may end up with no Givers.
- The destination preserves the target's existing Givers first, followed by incoming Givers in the chosen order.

## Order

- Order candidates are WorkTypes whose Giver set matches the reference but whose order differs.
- Selecting a row loads the existing Order editor. `Revert` uses a two-step confirmation and restores the reference Giver sequence.
- `Apply giver order` reorders only the Givers already present; it does not add or remove Givers.
- `Cancel giver order` restores the editor draft to its last applied sequence.

## Persistence and safety

- Saved actions write the active profile and its pending file.
- `ApplyProfileNow` applies the same pipeline used at startup so the settings UI receives immediate feedback where possible. A restart remains the safe boundary because generated WorkType definitions and DefDatabase mutations cannot be cleanly rolled back.
- Split, Merge and Order never write to pawns and never delete a WorkType or WorkGiver.
- There are no persisted split/merge/order histories. The profile stores final state; `reference.json` is the comparison and revert baseline.