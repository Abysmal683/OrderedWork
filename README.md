# Ordered Work (Core) 0.3.0-alpha.12

> Status: **0.3.0-alpha.12**. Bug-fix iteration for **Work Control**: the horizontal lane scrollbar is fixed above the matrix (outside rows) and is reliably draggable/clickable; the search row restores a visible Work text filter beside the selector, drops the `Work:` prefix, uses `Not`, and shows a match count; drag drops now follow the actual hovered row (the selected group updates only when the drop is inside it); palette tooltips list relevant skill names and all global WorkGivers without a selected-pawn skill lookup; missing-skill log spam is avoided; lane tooltips split Skill/Passion/Level/Rank; and cards are ~14 px tighter. Colonist-bar order, resurrection reapply, simple/manual, return button, right-click max, selection, Recommend/Details, colors, auto-recommend and save/load are kept. Profiles, schedules, equipment and multi-pawn remain **not implemented**. **Do not use it in a main save.**

Basic RimWorld 1.7 work-management framework.

## What it does
- Orders work types and work givers. The top of each list runs first.
- Splits a work type into a new work type from a base type plus selected givers.
- Merges givers into an existing work type.
- Never deletes a work type or a work giver.

## Trait and incapacity safety
- Split work types copy `workTags`, `relevantSkills`, `requireCapableColonist`, `alwaysStartActive`,
  `disabledForSlaves`, `visible`, `visibleOnlyWithChildrenInColony`, life-stage work settings and
  `mechEnabledWorkTypes`.
- Traits with `disabledWorkTypes` / `requiredWorkTypes` also receive the new split work type.
- Backstory and trait tag-based work disables apply automatically through the copied `workTags`.
- Merging a giver changes which work type (and therefore which tags) govern it. Pick the target with
  the tags you want. Do not merge a giver into a type whose tags would let an incapable pawn do it.

## Install
1. Copy this folder to `RimWorld/Mods/OrderedWork`.
2. Enable it after work mods (Job Splitter, Some More Jobs, Compact Work Tab, etc.).
3. Back up the save before enabling.

## Work Control tab (0.3.0-alpha.12)
- Open the new **Work Control** bottom tab (between Work and Schedule). It does **not** replace the vanilla Work tab.
- **Pawn order** follows `Find.ColonistBar.GetColonistsInOrder()` filtered to visible free colonists, with `Find.CurrentMap.mapPawns.FreeColonists` as fallback. A revived pawn returns to its stable colonist-bar position; rows follow the bar if it changes.
- Layout mirrors the Work tab: one row per player free colonist. The fixed left column shows the pawn **portrait** (about 32×32, scaled to row height), an **ellipsized** name/title, and compact right-side age/health. Hovering the row shows a tooltip with the full name, age, title, health and active WorkType count. The rest of the row is the colonist's horizontal WorkType lane. Outer/internal margins are ~4 px.
- **Scrollbars** are thin and sit outside the content. The horizontal bar is fixed **above the matrix** (never below the last row) and is draggable/clickable; the vertical bar is in the right gutter. The vertical bar scrolls names and lanes together; the horizontal bar moves only the lanes. Mouse wheel works over the name column and the lanes. Mod Settings `Auto-hide scrollbars` (default on) shows them while hovering/scrolling and hides them otherwise. The last row stays fully visible.
- **WorkType palette**: 3 compact rows above the matrix, filled left→right then top→bottom in natural `naturalPriority` order, with a horizontal slide for overflow. All visible WorkTypes are kept; chips are ellipsized and never overflow.
- **Search row** below the palette and above the matrix, labeled `Pawn` and `Work`. `Pawn` filters by name (flexible short/full/colored/title/first/nick/last matching). `Work` has a visible text filter plus a WorkType selector (options show the plain WorkType name) matching `defName`, `label` and `labelShort`; it filters rows to pawns with that WorkType active. The `Not` button (tooltip “Show pawns without this work”) inverts the Work filter. Pawn AND Work filters combine; an `N match(es)` count is shown and `No pawns match` when empty.
- Cards are **flat single-line** rows (~146×30) with a fixed order: **passion icon → best relevant skill level → capitalized WorkType name → `×`**. WorkType names are capitalized; colors come from `WorkTypeColorRegistry` (deterministic defName hash, user overrides stored in Mod Settings for a future picker). Hover shows the full tooltip; no tooltip is shown while dragging.
- **Lane tooltip**: WorkType description, all `WorkGiverDef`s, and separate `Skill`, `Passion`, `Level` and `Rank` lines. A `PawnSpecificOverrideHook` is exposed for future per-pawn/per-giver overrides (none implemented yet). **Palette tooltip**: description, global WorkGivers, compressed name and the WorkType's relevant skill names (no selected-pawn skill lookup).
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

## Use
1. Open `Options -> Mod Settings -> Ordered Work`.
2. Reorder work types and givers with Up/Down.
3. Split: fill a new label, the base work type defName, and a comma-separated list of giver defNames.
4. Merge: fill a comma-separated list of giver defNames and the target work type defName.
5. Restart RimWorld. Changes are applied at load time.

## Technical spike: dense rank test (0.2.0-alpha.2)
- Load a colony first. In `Options -> Mod Settings -> Ordered Work`, click **Test dense rank on colonists (log)**. With no map loaded the window shows `Load a colony to run the dense-rank test.` instead of the button.
- The button writes dense rank `1..N` (where `N` is the number of visible work types, minimum 4) into each free colonist's private `Pawn_WorkSettings.priorities` map, bypassing the `SetPriority` clamp, then calls `Notify_UseWorkPrioritiesChanged()`. If the private field is unavailable it falls back to `SetPriority`.
- Inside a `try/catch`, any failure is logged as `[Ordered Work] Dense rank test failed: ...` so the Mod Settings scroll view always closes cleanly.
- It only logs; the Work Control tab above now provides the visual editor.
- Expected log shape: `[Ordered Work] Dense rank test <pawn>: max=<max>, count=<count>, first=<defName>=<priority>, last=<defName>=<priority>`. Compare `last` with its assigned rank (e.g. 23): if it survives, direct-map mode bypasses the vanilla 4 cap; if it reads 4, the value was clamped downstream.
- **Do not use this build in a main save**; test in a throwaway colony.

## Limitations (0.3.0-alpha.12)
- No profiles, schedules, equipment or multi-pawn yet.
- Pawn order follows the colonist bar; if the bar is unavailable or returns no visible free colonists, the map's `FreeColonists` order is used. Dead pawns are filtered out.
- Scrollbars are custom and 12 px thick; when auto-hidden they still reserve their gutters. Wheel direction follows Unity's scroll delta.
- The 3-row palette balances rows with `ceil(count/3)` and slides horizontally; chips are ellipsized rather than wrapped.
- The Work filter's text box filters the WorkType selector options; the selected WorkType then filters rows. It matches `defName`/`label`/`labelShort`.
- The drag placeholder is drawn on the hovered row. With 2+ selected rows, the whole group updates only when the drop is inside the selection; otherwise only the hovered row updates. Shift-drag marquee row selection is not implemented.
- Per-pawn/per-giver overrides are only a hook (`PawnSpecificOverrideHook`); no overrides are implemented yet. The color registry stores user overrides but has no picker yet.
- Selection is `Find.Selector`-driven; row selection is via the pawn name/portrait column, while lane cards remain editing controls.
- The vanilla **View: Draw** button is an overlay at the top-right of the vanilla Work tab and may cover part of the last column header (usually the remaining-space filler).
- **Auto recommend new colony** and **Auto-hide scrollbars** are global Mod Settings toggles. The old per-save `viewMode` field is kept only for save compatibility and is ignored.
- `Dialog_WorkControlDetails` is read-only and basic: name/title, top 8 skills, and the current WorkType order.
- The >4 cell applies only in manual mode (`useWorkPriorities`) and only to capable free colonists; simple mode and incapable/non-free-colonist pawns stay vanilla.
- If another mod also prefixes `WidgetsWork.DrawWorkBoxFor`, Harmony ordering may let that mod win, leaving the cell vanilla; the rest of the mod keeps working.
- The dynamic max falls back to 99 when the count fails; the fallback is logged.
- Pawn/Work filters are UI-only and not persisted.
- Initialization only scans the current map's free colonists; a colony initialized on one map does not backfill colonists on another map until they spawn.
- The vanilla capture hook covers `SetPriority`; a mod that changes priorities by writing the private map directly will not be captured.
- The "Sort by WorkType" header button is not implemented yet.
- If another mod replaces the Work tab, this mod does not replace it; it writes the same vanilla priorities.
- Not safe for a main save.

## Build and test
```
dotnet build Source/OrderedWork.Core/OrderedWork.Core.csproj -c Release
dotnet build Source/OrderedWork.csproj -c Release
dotnet run --project Tests/OrderedWork.Tests/OrderedWork.Tests.csproj -c Release
```
