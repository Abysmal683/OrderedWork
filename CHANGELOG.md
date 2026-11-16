# Changelog

All notable changes to Ordered Work are documented here.
This format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [0.3.0-alpha.6] - 2026-11-16

### Changed
- **Flat single-line cards**: card height ~30 and row height ~42. Fixed order on one line: passion icon → best skill level → WorkType name (ellipsized, full name in tooltip) → `×`. When the WorkType has no relevant skill, only the name and `×` are drawn (no level/passion placeholders). Subtle background with hover/drag highlight and no card border.
- **Search now filters**: a non-empty pawn search draws only matching rows and shows a centered `No pawns match` when empty. It no longer highlights; it never selects pawns, changes orders or changes priorities. Matching is flexible across `LabelShort`, full/short name, colored name, title and first/nick/last name components.

### Added
- Search-aware selection: a selected pawn that is filtered out keeps its internal selection and is simply not drawn.
- Refined double-click: if the pawn is already selected in game, nothing happens; otherwise the previous game selection is cleared, the pawn is selected, and the camera jumps to it. Single click remains visual-only.

### Deferred
- Vanilla Work-tab low-priority-cap patch and Draw/Manual/Both mode changes remain out of scope for this iteration.

## [0.3.0-alpha.5] - 2026-11-16

### Changed
- Reduced outer/internal margins to ~4 px, card height to ~60 and row height to ~78 while keeping the two-line card (ellipsized name + skill line).
- Passion `*`/`**` text markers replaced by the vanilla `WidgetsWork.PassionWorkboxMinorIcon` / `PassionWorkboxMajorIcon` 12×12 textures next to the skill level.
- View mode is now a persisted per-save `WorkViewMode` (`Draw`, `Manual`, `Both`, default `Both`) with a header control cycling Draw → Manual → Both → Draw. Manual mode copies the vanilla order at initialization instead of auto-recommending.

### Added
- Pawn-name search field above the matrix that highlights matching rows only (no selection, filtering or order changes).
- Visual row selection on click (highlight + border), with double-click selecting the pawn in game and moving the camera (`Find.Selector.Select` + `CameraJumper.TryJump`).
- "Open Work tab" header button that switches to the vanilla Work tab via `Find.MainTabsRoot.ToggleTab`; both tabs remain available.

### Deferred
- Vanilla Work-tab low-priority-cap patch (manual 1..N in the vanilla tab) remains for the next spike.

## [0.3.0-alpha.4] - 2026-11-16

### Fixed
- Compact card height (~64) and row height (~86) so each card is exactly two lines with no empty line below the skill line.
- Resurrection/partial-order oscillation: the vanilla capture now ignores `EnableAndInitialize` (`IsInitializing`), spawning (`IsSpawning`), our own writes (`IsApplying`) and a 120-tick window after spawn/resurrection, so a partially initialized vanilla Work tab can no longer overwrite a stored order and low-skill 0/1 entries are preserved.

### Changed
- `+` picker now lists the remaining visible WorkTypes with their best relevant skill level (`-` when none) and shows incapable types greyed out as `Incapaz` (disabled, not addable).
- Recommendation scoring: `Patient`, `PatientBedRest` and `Firefighter` are pinned first (when capable, in that order), then capable types are scored (no relevant skill `0`; level 0 no/minor/major passion `-3`/`1`/`3`; level ≥1 `level*2 + 2/4`), tie-broken by `naturalPriority` descending. Existing-colony copy is untouched.
- Colony initialization moved from tab-open to `GameComponentTick` once a map exists, so opening the vanilla Work tab first no longer prevents initialization; the tab call remains idempotent.

### Added
- `IsInitializing` flag plus Harmony prefix/postfix around `Pawn_WorkSettings.EnableAndInitialize` and `EnableAndInitializeIfNotAlreadyInitialized`.
- Per-pawn `captureSuppressedUntilTick` (runtime-only) set on spawn/resurrection and faction change to suppress stale vanilla capture.

## [0.3.0-alpha.3] - 2026-11-14

### Fixed
- WorkType cards are now exactly two visual lines: an ellipsized name (full name in the tooltip) and the best relevant skill level plus passion marker. The name no longer wraps into a third line.

### Changed
- Incapable WorkTypes are filtered out of the lane, the default order, stored-order reads/writes and the `+` picker, so they never appear as grey cards and cannot be dragged or added.
- `OrderedWorkGameComponent` now persists a one-time `initialized` flag.
- `Cleanup` keeps entries for dead (but not destroyed) pawns so resurrections can reapply.

### Added
- `WorkOrderService.EnsureColonyInitialized`: on first open, an existing colony copies its vanilla Work-tab order without writing; a new/default colony applies a skill recommendation once.
- Skill recommendation for new colonists and an isolated per-row `R` button ("Generate recommendation") that affects only that pawn.
- Harmony postfix on `Pawn_WorkSettings.SetPriority` that rebuilds the stored order from vanilla Work-tab edits, guarded by `IsApplying` and skipped while a pawn is spawning.
- Harmony postfix on `Pawn.SpawnSetup`: reapplies a stored order on resurrection and recommends/applies for a new colonist with no stored order.
- Harmony postfix on `Pawn.SetFaction` covers recruited colonists (and other newly player-controlled pawns) that never re-spawn, recommending/applying an order if they have none.

## [0.3.0-alpha.2] - 2026-11-14

### Fixed
- `OrderedWorkGameComponent` now exposes a `(Game game)` constructor so RimWorld can instantiate it; the parameterless constructor caused `MissingMethodException: Constructor on type ... not found` in `Player.log`. The `GetOrCreate` fallback also uses `new OrderedWorkGameComponent(Current.Game)`.

### Changed
- Work Control layout now mirrors the vanilla Work tab: one row per free colonist, a fixed pawn name column on the left and a WorkType card lane on the right.
- Replaced the single-pawn selector with all free colonists shown as rows.
- One shared horizontal scroll moves every row's lane together; one outer vertical scroll covers all rows. Compact cards (~190×80) with full wrapped WorkType names.
- Header now labels the pawn and work-order columns; the former per-pawn result table is no longer drawn.

### Added
- Each card shows the best relevant skill level for that pawn/WorkType, or `—` when there is no relevant skill.
- Passion marker hook: temporary `**` (major) / `*` (minor) text behind a single method so a future icon can replace it.
- Card tooltip lists each relevant skill and level; incapable cards show a generic `Incapaz` label and tooltip.

### Removed
- Read-only result table from the main Work Control view (deferred to a future collapsible panel).

## [0.3.0-alpha.1] - 2026-11-14

### Added
- New full-screen **Work Control** bottom tab (`MainTabWindow_WorkControl`) with a `MainButtonDef` at order 15, between Work and Schedule. It does not replace the vanilla Work tab.
- Pawn selector for player free colonists only; selecting a pawn loads its lane and never applies priorities.
- Horizontal, scrollable WorkType lane with full names; add (`+`) picker, remove (`×`, off), drag reorder, dense rank and grey incapable types.
- A second outer vertical scroll enclosing the pawn list, lane and result table; the horizontal lane scroll position is preserved when switching pawns.
- Per-pawn order persistence through `PawnWorkOrder` and `OrderedWorkGameComponent`, with basic null/dead pawn cleanup.
- `WorkOrderService` normalizes with `DenseRank`, builds dense ranks and writes directly into the private `Pawn_WorkSettings.priorities` map (falling back to `SetPriority`), then calls `Notify_UseWorkPrioritiesChanged`.
- Read-only result table below the lane showing rank, WorkType and status.

## [0.2.0-alpha.2] - 2026-11-13

### Fixed
- Removed the `Pawn_WorkSettings.LowestPriority` reflection write; that field is `const`, so `FieldInfo.SetValue` threw `FieldAccessException: Cannot set a constant field` and broke the Mod Settings window.
- The dense-rank test now writes ranks directly into the private `Pawn_WorkSettings.priorities` `DefMap<WorkTypeDef,int>`, bypassing the `SetPriority` clamp; it falls back to `SetPriority` when the field is unavailable.
- The test button is hidden and replaced with `Load a colony to run the dense-rank test.` when `Find.CurrentMap == null`.
- Wrapped the test body in `try/catch` and log `[Ordered Work] Dense rank test failed: ...` so a failure cannot escape `DoSettingsWindowContents` and unbalance `BeginScrollView`/`EndScrollView`.
- Dense-rank logs now include the last work type's rank so a surviving high rank (vs. a clamp to 4) is visible.

## [0.2.0-alpha.1] - 2026-11-13

### Added
- Pure `OrderedWork.Core` project with `DenseRank.NormalizeOrder` and `DenseRank.BuildRanks`.
- Console test harness (`Tests/OrderedWork.Tests`) covering normalization, dense ranks, appended ids, and null inputs.
- Technical-spike Mod Settings button (`Test dense rank on colonists (log)`) that writes dense rank 1..N to free colonists' `Pawn_WorkSettings`, raises `Pawn_WorkSettings.LowestPriority`, and logs the result.
- Version metadata (`About.xml`) and changelog tracking.

### Changed
- `OrderedWork.Core` targets `net10.0` to match the mod and test projects (offline build without `NETStandard.Library`).
- README documents the spike and warns not to use it in a main save.

## [0.1.0] - 2026-11-13

### Added
- Initial def-layer prototype: order work types and work givers, split a work type from a base type plus givers, merge givers into an existing work type.
- Split work types copy work tags, skills, life-stage availability and mech availability so traits and incapacities keep working.
