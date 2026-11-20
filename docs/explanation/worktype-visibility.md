# Hidden and always-active WorkTypes

Status: design proposal. Not implemented yet.

## Current model

`WorkTypeConfig.Visible` currently means "show this WorkType in the ordered UI". A split or merge can set an origin to `Visible=false` when it becomes empty. The profile is reconciled with live visible WorkTypes; hidden live WorkTypes are not added and are not forced visible.

`WorkTypeDef.visible` is a UI flag. It is not the same as "this WorkType has no Givers", and it should not be used as a delete signal.

## Proposed model

Keep the concepts separate:

| Concept | Meaning | Storage |
|---|---|---|
| `Visible` | Show in Work tab and Work Control | `WorkTypeConfig.Visible` |
| `AlwaysActive` | Keep priorities enabled even when hidden | new `WorkTypeConfig.AlwaysActive` |
| `HasGivers` | Derived from the Giver list | no extra field |

### 1. Hide without disabling

- Hiding sets `Visible=false`; it does not remove the WorkType or its Givers.
- The WorkType remains in the profile and keeps its order.
- It must remain selectable in Split/Merge so it can be recovered.

### 2. Hidden and always active

- `AlwaysActive=true` sets the live `WorkTypeDef.alwaysStartActive` flag.
- It must be explicit and only valid for a hidden WorkType.
- The exact pawn-priority behavior must be verified in runtime; do not claim "always active" until it is tested.
- Work Control should either keep a hidden active lane available or expose a "hidden active" section; do not silently create work the user cannot inspect.

### 3. Use WorkTypes hidden by other mods

- Allow an explicit opt-in to show a hidden live WorkType.
- Reuse its tags, skills and giver ownership; never clone the WorkGiverDef.
- Show a warning that the source mod may hide it for gameplay reasons.
- Do not auto-reveal hidden WorkTypes during reconciliation.
- If the source mod stops providing the WorkType, keep the profile entry but report it; do not delete user data silently.

## Decisions recorded

- The hidden layer is applied after the normal policy layers: vanilla, Work Control, hourly work and permanent work. It is the highest hard layer.
- Hidden WorkTypes are not shown in Work tab or Work Control; they are not deleted.
- Their fixed priority is managed only in Mod Settings and requires a restart.
- Visible WorkTypes keep their own visible order; the hidden layer sits above it.

## Verified API behavior

- `Pawn_WorkSettings.SetPriority` rejects a disabled WorkType: it logs `Tried to change priority on disabled worktype` and returns without writing.
- `Pawn_WorkSettings.GetPriority` returns the stored priority; `WorkTypeIsDisabled`/`GetDisabledWorkTypes` decide capability. Hiding through `WorkTypeDef.visible` is not the same as disabling a WorkType.
- `Pawn_WorkSettings.Notify_DisabledWorkTypesChanged` disables the newly disabled types. A hidden layer must hook this so a pawn who becomes incapable drops the hidden priority, and a pawn who recovers gets it back.
- `workGiversDirty` plus `CacheWorkGiversInOrder()` refreshes the cached WorkGiver lists; `Notify_UseWorkPrioritiesChanged()` alone is not the cache refresh.
- The hidden fixed layer can be implemented as a separate `GetPriority` overlay: return the configured hidden rank only when the pawn is capable and the WorkType is configured hidden. This avoids shifting visible priorities and keeps the player's visible order intact.
- A disabled WorkType must remain skipped; forcing a priority on an incapable pawn is not valid and will not produce work.

## Open decisions

1. Should hidden active WorkTypes appear in Work Control, or only in a dedicated "Hidden work types" section?
2. Does "always active" mean only for new colonies, or for every pawn whenever the profile is applied?
3. Should an unhidden mod WorkType keep the mod's conditions exactly, or allow Ordered Work to override only visibility and order?

These require runtime verification before implementation.