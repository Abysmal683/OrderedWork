# Hidden and always-active WorkTypes

Status: implemented in `0.3.0-alpha.278`; runtime verification pending.

## Model

| Field | Meaning |
|---|---|
| `WorkTypeConfig.Visible` | Show the WorkType in the normal UI. |
| `WorkTypeConfig.Hidden` | Keep the WorkType active but remove it from Work tab and Work Control. |
| `WorkTypeConfig.Order` | The same config order used by the WorkType list and the hidden layer. |
| `WorkTypeDef.visible` | Runtime UI flag; it is not a delete signal and not the same as "no Givers". |

`Hidden` entries keep their profile entry, Givers and order. They remain selectable in Split, Merge and Order in Config mod. They do not appear in the in-game Work tab or Work Control. There is no in-game "hidden" section.

## Config workflow

- Right-click any row in the WorkType order list to toggle its hidden mark; the row shows `[hidden]`.
- The Rules tab includes a short hint explaining that hidden WorkTypes stay active but disappear from Work tab and Work Control.
- Reordering the row reorders the hidden layer as well.
- Applying a hidden change requires a restart for the safe boundary.

## Application layer

The hidden layer is applied after the normal layers:

1. vanilla priorities;
2. Work Control;
3. hourly work;
4. permanent work;
5. hidden fixed layer.

The hidden layer is implemented at priority lookup:

- `Pawn_WorkSettings.GetPriority` returns the hidden rank (1..n, based on config order) for a capable pawn.
- Visible priorities keep their normal numbers; the hidden layer does not renumber the player's visible order.
- Hidden WorkGivers are prepended to the cached WorkGiver order so a numeric tie cannot place visible work first.
- `WorkTypeDef.alwaysStartActive` is set for hidden entries.

## Incapability

- A pawn incapable of the WorkType is skipped: the overlay returns 0 and no hidden work is produced.
- `Pawn_WorkSettings.Notify_DisabledWorkTypesChanged` is hooked after the vanilla disable pass. The hook refreshes the work cache so a newly incapable pawn drops the hidden work and a recovered pawn gets it back.
- `workGiversDirty`/`WorkGiversInOrder` caching must be refreshed; the overlay alone does not invalidate the cached list.
- Forcing a disabled WorkType is not valid: `Pawn_WorkSettings.SetPriority` rejects it and writes nothing.

## API constraints

- `Pawn_WorkSettings.SetPriority` rejects disabled WorkTypes and logs `Tried to change priority on disabled worktype`.
- `Pawn_WorkSettings.GetPriority` is the correct overlay point.
- `Pawn_WorkSettings.Notify_DisabledWorkTypesChanged` calls `Disable`, which writes priority 0 through `SetPriority`.
- `Pawn_WorkSettings.CacheWorkGiversInOrder` rebuilds the cached WorkGiver lists; it must be refreshed after capability changes.
- `WorkTypeDef.visible` only controls UI visibility; hidden does not mean deleted or disabled.

## Known limits

- Hidden WorkTypes are intentionally not shown in Work Control; the Config order list is the only editor.
- A hidden WorkType with no Givers remains configured but produces no work.
- A source mod that adds a WorkType already hidden is not auto-added by reconciliation. Adding it through Config, then removing the hidden mark, is the deliberate opt-in path.
- Runtime verification is still required for capability changes and for Work tab/Compact Work Tab compatibility.