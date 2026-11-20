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

## Open decisions

1. Should hidden active WorkTypes appear in Work Control, or only in a dedicated "Hidden work types" section?
2. Does "always active" mean only for new colonies, or for every pawn whenever the profile is applied?
3. Should an unhidden mod WorkType keep the mod's conditions exactly, or allow Ordered Work to override only visibility and order?

These require runtime verification before implementation.