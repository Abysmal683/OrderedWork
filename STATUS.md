# Ordered Work — Current Status

Version: `0.3.0-alpha.80` (cleanup target). Build 0/0; core tests 7/7.

## Completed architecture

- JSON profiles are the global source of truth: `WorkTypes` map keyed by `defName`, one owner per giver, `WorkTypeConfig.Order`.
- `active`, `pending` and `reference` JSON; atomic writes and backups.
- Startup promotes pending and applies the profile.
- `WorkConfigDiff` computes modified WorkTypes against `reference`.
- Legacy persisted lists are gone: `splits`, `merges`, `giverOrder`, `originalGiverOrder`, `workTypeOrder`.
- Split and merge are final profile state, not operation history.
- Work Control and Mod Settings are split into partial classes by responsibility.

## User-confirmed behavior

- Profile persistence and pending application.
- Version displayed once.
- Order Apply/Cancel dirty guards and reset.
- Split create/restart/delete.
- Merge/restore.
- Unified reset, no duplicated reset button.
- Palette, filters, cards, scrollbars, passion resolver.

## Pending verification

- Final profile-only split/merge/order path after `alpha.80`.
- `test_1`/pending/reference alignment after a full create→restart→delete→restart cycle.
- No startup/profile fallback anomalies.
- No in-game exceptions; logs remain clean.

## Next scope

1. Per-pawn WorkGiver overrides, normal list only.
2. Profile-backed split metadata editing.
3. Compact per-save pawn override storage.
4. Optional palette-filter control, only if needed.
5. Emergency/custom-job work only on explicit request.
