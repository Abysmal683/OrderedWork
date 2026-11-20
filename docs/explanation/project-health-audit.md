# Project health audit — 2026-11-19

This is a working audit, not a user guide. It records the structural findings that should guide the next refactor pass.

## Snapshot

- 121 C# source files, about 660 KB.
- 1 test file plus profile samples, about 13 KB, 27 cases.
- Build: 0 errors, 0 warnings.
- Language parity: 206 keys in English, Spanish and Spanish Latin, with no missing keys or duplicates.
- Checklist: 48 open items; many are roadmap, but runtime coverage remains behind implementation.

## Healthy parts

- `OrderedWork.Core` isolates pure order/rank/recommendation/focus rules and has useful tests.
- Config, startup, services and UI are separated by responsibility.
- Profile persistence is JSON-backed with atomic writes and backups.
- UI is already split into components and partial classes; the recent documentation now matches the profile-backed model.
- No obvious hot-path performance problem: profile operations are small and Work Control has a bounded lane cache.

## Main debt

### 1. SplitMergePanel is oversized

`Source/Mod/SplitMerge/SplitMergePanel.cs` is about 1,136 lines and 66 members, mixing Split, Merge, Order, candidates, metadata, persistence, scrolling and revert actions. Split it into partials by mode and persistence without changing behavior.

### 2. Profile commit is duplicated

The sequence `SaveProfile` → `SavePending` → `ApplyProfileNow` appears in several UI/services paths. Centralize it in one host/service method to avoid saving only half the state.

### 3. Dead code

`Source/Mod/OrderedWorkMod.Order.cs` still contains methods without callers:

- `ApplyLiveProfileFromSettings`
- `ReturnSurplusGivers`
- `RemoveGiverFromOtherTypes`
- `CloneWorkType`

`PersistLivePending` is still used. Remove or relocate the dead methods.

### 4. Test coverage is narrow

The 23 tests cover Core policies, but not profile normalization/reconciliation, split/merge/revert transformations, reference ownership, serializer fallback or migration. These are the areas where UI regressions are most expensive.

### 5. Build is not portable

`Source/OrderedWork.csproj` references absolute paths under `D:\SteamLibrary`. Move them to configurable MSBuild properties or environment variables before relying on CI or another machine.

### 6. Visibility reconciliation

Profiles are built from visible WorkTypes. A mod that adds or reveals a WorkType after profile creation can leave the profile incomplete. The current fix appends missing visible WorkTypes/Givers on load and preserves a live hidden state for existing vanilla types. Hidden WorkTypes are not added.

## P0 status (`0.3.0-alpha.284`)

- Profile commit centralized in `OrderedWorkMod.CommitProfile`.
- Dead profile methods removed from `OrderedWorkMod.Order.cs`.
- `SplitMergePanel` split into persistence, candidate and column partials.
- Pure `WorkConfigReconciler` tests added for merge revert, order revert and normalization.
- Build paths are overridable through `RimWorldDir` and `HarmonyDir`; Core analyzers are enabled.
- Added `scripts/verify.ps1` for build, tests and language-key parity; the spike was removed. Still open: an actual CI job and a runtime regression pass.

## Priority

1. Reconcile and test the profile model: missing WorkTypes/Givers, visibility, split/merge/revert ownership.
2. Centralize profile commits.
3. Split `SplitMergePanel`.
4. Remove dead code.
5. Portable build paths and analyzers/nullable incrementally.
6. Close the runtime checklist before starting `PawnPolicy`.

## Non-goals

- Do not rewrite the architecture wholesale.
- Do not add runtime `WorkGiverDef` clones.
- Do not force hidden mod WorkTypes visible.
- Do not treat `Visible=false` as permission to delete profile data.