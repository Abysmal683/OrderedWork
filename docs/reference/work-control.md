# Work Control reference

## UI components

| Component | Responsibility |
|---|---|
| `MainTabWindow_WorkControl` | tab lifecycle, top controls, filters and layout |
| `WorkControlGrid` | matrix viewport, external bars and row loop |
| `WorkControlRows` | pawn column, lane cards and tooltips |
| `WorkControlInteraction` | palette/card drag, drop and add menu |
| `WorkControlMetrics` | row/card constants and slot math |
## Work Control tab (0.3.0-alpha.313)
- Open the new **Work Control** bottom tab (between Work and Schedule). It does **not** replace the vanilla Work tab.
- **Pawn order** follows `Find.ColonistBar.GetColonistsInOrder()` filtered to visible free colonists, with `Find.CurrentMap.mapPawns.FreeColonists` as fallback. A revived pawn returns to its stable colonist-bar position; rows follow the bar if it changes.
- Layout mirrors the Work tab: one row per player free colonist. The fixed left column shows the pawn **portrait** (about 32×32, scaled to row height), an **ellipsized** name/title, and compact right-side age/health. Hovering the row shows a tooltip with the full name, age, title, health and active WorkType count. The rest of the row is the colonist's horizontal WorkType lane. Outer/internal margins are ~4 px.
- **Scrollbars** are thin and sit outside the content. The horizontal bar is fixed **above the matrix** (never below the last row); the vertical bar is in the right gutter. Input is handled **before** `Widgets.BeginScrollView` and drawing happens **after** the content, so the scroll view/content cannot consume a bar's `MouseDown`; the horizontal bar therefore stays interactive when the vertical bar appears. The bars keep separate drag state and show flags, reserve both gutters, scroll names/lanes vertically and lanes horizontally, auto-hide per Mod Settings, and keep the last row fully visible. Temporary `[OrderedWork][Scroll]` diagnostics log one line per bar `MouseDown`.
- **WorkType palette**: 3 compact rows above the matrix, filled **top→bottom then left→right** (column-major) in the saved Work-type order (`workTypeOrder`, the same order used by the Work tab), with its own horizontal slide for overflow and **Shift+wheel** quick scrolling. All visible WorkTypes are kept; chips are ellipsized and never overflow. The Work filter affects the palette only when the scope is **Palette** or **Both**; in the default **Line** scope the palette keeps every WorkType so it stays usable for assignment. The `Palette stress test (debug)` Mod Setting appends 60 visual-only WorkTypes to test this scrollbar; they are not in `DefDatabase` and cannot be dragged or assigned.
- **Search row** below the palette and above the matrix. `Pawn` filters by name only and never affects the palette. The **Work combo** opens centered with a `Filter Work` title: it shows `Any` only while a Work filter is active, excludes the currently selected WorkType, shows at most 10 rows with an integrated thin scrollbar. A `Filter: Line` / `Filter: Palette` / `Filter: Both` button (default **Line**, disabled until a Work filter exists) controls where the Work filter applies: `Line` filters pawn rows only, `Palette` filters palette chips only, `Both` filters both. `Not` inverts the Work filter wherever it applies. WorkTypes match by identity/defName, not substring. The `N match(es)` count appears for Pawn search or Line filtering; `No pawns match` appears when the row list is empty.
- Cards are **flat single-line** rows (135×30) with a fixed order: **passion icon → best relevant skill level → capitalized WorkType name → `×`**. WorkType names are capitalized; colors come from `WorkTypeColorRegistry` (deterministic defName hash, user overrides stored in Mod Settings for a future picker). Passion icons come from one resolver shared by lanes, the drag card and Details: with VSE/Alpha Skills it uses the colored `PassionDef.Icon`, then the gray `PassionDef.WorkBoxIcon` tinted by `PassionDef.color`; without them it uses vanilla `UI/Icons/PassionMinor`/`PassionMajor`, then the vanilla gray work-box icons with the existing warm tints. No icon is drawn for `Passion.None`. Hover shows the full tooltip; no tooltip is shown while dragging.
- **Lane tooltip**: WorkType description, all `WorkGiverDef`s, and separate `Skill`, `Passion`, `Level` and `Priority` lines. `Passion` uses the VSE/Alpha Skills `PassionDef.LabelCap` when available and falls back to vanilla `minor`/`major`/`none`. Per-pawn WorkGiver order overrides are implemented through `WorkGiverOrderService`; the color registry still has no picker. **Palette tooltip**: description, global WorkGivers, compressed name and the WorkType's relevant skill names (no selected-pawn skill lookup).
- **Drag targets**: dropping on an unselected row applies to that row only; with 2+ rows selected, dropping inside the selection applies to the whole selected group (same slot); dropping outside it affects only the hovered row. The original rules apply: existing moves/reorders, missing capable types are added, incapable pawns are skipped with one grouped message, and no duplicates.
- **Selection is `Find.Selector`-driven.** Rows highlight the pawns currently selected in game. Single click selects; shift-click toggles; double-click selects, moves the camera and closes Work Control. Selection is shared with the vanilla tab.
- The top-bar **Recommend** applies the skill recommendation to every selected pawn (disabled when nothing is selected). **Details** opens the read-only `Dialog_WorkControlDetails` for the first selected pawn.
- **View: Numeric** (top right) switches to the vanilla Work tab; the vanilla Work tab gets a **View: Draw** button to return. Both use `Find.MainTabsRoot`.
- **Auto recommend new colony** (Mod Settings, default on): on = new/default colonies and new pawns get the scored recommendation; off = they copy the current vanilla order instead.
- Drag a lane card to reorder (left = highest). The `×` removes a card (off, priority 0). The `+` card opens the candidate picker.
- **Incapable WorkTypes stay out of the lane** and out of default/stored/written orders. The `+` picker shows levels and greys incapable types as `Incapaz`.
- **Initialization runs at game start** (`GameComponentTick`). Existing colony copies the current Work-tab order without writing; with auto-recommend off, new/default colonies also copy; with it on, a new/default colony applies the recommendation once.
- A Harmony postfix on `Pawn_WorkSettings.SetPriority` captures genuine vanilla Work-tab edits and rebuilds the stored order, ignoring our writes, `EnableAndInitialize`, spawning and a 120-tick post-spawn window.
- **Resurrection/faction change**: an existing stored order is always reapplied first (logged as `Reapplied stored order`); only a pawn with no stored order gets `Recommended new pawn` (or a vanilla copy when auto-recommend is off). A resurrected pawn is never treated as brand new. Dead-but-not-destroyed entries are kept.
- Edits write dense rank 1..N directly into `Pawn_WorkSettings.priorities` and call `Notify_UseWorkPrioritiesChanged`.
- Each pawn's order is saved per colony in a `GameComponent`; save/load preserves it.
- No result table and no Sync button.

## Vanilla Work tab: simple/manual priorities (0.3.0-alpha.11)
- **Simple mode** (`Find.PlaySettings.useWorkPriorities == false`): our numeric prefix defers completely to vanilla, so checkboxes work (remove = 0, add = default).
- **Manual mode** (`useWorkPriorities == true`): the capable free-colonist cell is replaced by a clean-room numeric cell:
  - **Left-click** cycles `0 → 1 → … → max → 0`.
  - **Right-click** jumps from `0` to `max`; otherwise decrements by 1.
- `max = max(4, number of visible WorkTypes the pawn is capable of)`, capped at `99`. If the dynamic count fails, it falls back to `99` (logged once).
- Priorities **0–4** keep the vanilla priority colours (the box is tinted and the number uses `WidgetsWork.ColorOfPriority`); `0` is an empty cell like vanilla.
- Priorities **>4** use a distinct blue box, a cyan number and a border, plus a higher-priority tooltip: `Priority N of max (above vanilla 4)`.
- `Pawn_WorkSettings.SetPriority` is normalised to `0..max`. Values **above 4** are written straight into the private priorities map and notified/captured, so the original method (and its "invalid priority" log) is skipped; 0–4 still use vanilla. Our own `Apply` is excluded by the `IsApplying` guard, so there is no recursion.
- Incapable cells and non-free-colonist pawns are left to vanilla, preserving other UI mods' behavior as much as possible.
- The vanilla Work tab draws a **View: Draw** return button (top-right) that opens Work Control. The postfix uses the real `DoWindowContents(Rect rect)` parameter name.
- **Compact Work Tab** (`Mlie.CompactWorkTab`) patches the work-priority column (`DoCell`/`DoHeader`/`GetMinHeaderHeight`/`RemainingSpace`), not the Work window. It therefore uses the same `MainTabWindow_Work`, and the return button appears there from the same patch; no separate window patch is needed.
- If the expected `WidgetsWork` API or patch path is unavailable, the prefix logs once and returns to vanilla drawing.
- **Known limitation:** other mods that also prefix `WidgetsWork.DrawWorkBoxFor` may win the Harmony order; in that case the numeric cell may stay vanilla. The rest of the mod keeps working.
