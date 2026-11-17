# Ordered Work (Core) 0.3.0-alpha.77

> Status: **0.3.0-alpha.77**. Bug-fix and polish iteration for **Work Control**: the tab opens at half of the available height above the 35 px main-tab bar, with a top grip for a saved resize (minimum four pawn rows); the Work combo opens centered with a `Filter Work` title, shows `Any` only while a Work filter is active, excludes the already-selected WorkType and still shows at most 10 rows with a thin scrollbar. A three-state `Filter: Line` / `Filter: Palette` / `Filter: Both` control (default **Line**, disabled until a Work filter exists) scopes the Work filter, `Not` inverts it wherever it applies, Pawn search always filters only pawn rows, and WorkType identity is matched exactly (`Patient` does not match `PatientBedRest`). A `Clear` button resets all filters. Cards are 135 px wide with 14 px passion icons. Passion icons use a shared resolver: VSE/Alpha Skills `PassionDef.Icon` first, then `WorkBoxIcon` tinted by `PassionDef.color`, then vanilla colored textures, then vanilla gray work-box icons; tooltips use `Def.LabelCap`; no icon for `None`. Scrollbar input runs before the scroll view and drawing after it; palette and matrix horizontal drags use separate owners, so the palette cannot cancel the matrix drag. Colonist-bar order, hover-based drag targets, ghost/colors, simple/manual, return button, right-click max, selection, Recommend/Details, auto-recommend, save/load and resurrection are kept. Profiles, schedules, equipment and multi-pawn remain **not implemented**. **Do not use it in a main save.**

Basic RimWorld 1.7 work-management framework.

Compatibility references and licensing posture: see [`CREDITS.md`](CREDITS.md). Ordered Work contains no third-party code or assets; VSE/Alpha Skills passion icons are read at runtime through their public API.

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

## Work Control tab (0.3.0-alpha.77)
- Open the new **Work Control** bottom tab (between Work and Schedule). It does **not** replace the vanilla Work tab.
- **Pawn order** follows `Find.ColonistBar.GetColonistsInOrder()` filtered to visible free colonists, with `Find.CurrentMap.mapPawns.FreeColonists` as fallback. A revived pawn returns to its stable colonist-bar position; rows follow the bar if it changes.
- Layout mirrors the Work tab: one row per player free colonist. The fixed left column shows the pawn **portrait** (about 32×32, scaled to row height), an **ellipsized** name/title, and compact right-side age/health. Hovering the row shows a tooltip with the full name, age, title, health and active WorkType count. The rest of the row is the colonist's horizontal WorkType lane. Outer/internal margins are ~4 px.
- **Scrollbars** are thin and sit outside the content. The horizontal bar is fixed **above the matrix** (never below the last row); the vertical bar is in the right gutter. Input is handled **before** `Widgets.BeginScrollView` and drawing happens **after** the content, so the scroll view/content cannot consume a bar's `MouseDown`; the horizontal bar therefore stays interactive when the vertical bar appears. The bars keep separate drag state and show flags, reserve both gutters, scroll names/lanes vertically and lanes horizontally, auto-hide per Mod Settings, and keep the last row fully visible. Temporary `[OrderedWork][Scroll]` diagnostics log one line per bar `MouseDown`.
- **WorkType palette**: 3 compact rows above the matrix, filled **top→bottom then left→right** (column-major) in the saved Work-type order (`workTypeOrder`, the same order used by the Work tab), with its own horizontal slide for overflow and **Shift+wheel** quick scrolling. All visible WorkTypes are kept; chips are ellipsized and never overflow. The Work filter affects the palette only when the scope is **Palette** or **Both**; in the default **Line** scope the palette keeps every WorkType so it stays usable for assignment. The `Palette stress test (debug)` Mod Setting appends 60 visual-only WorkTypes to test this scrollbar; they are not in `DefDatabase` and cannot be dragged or assigned.
- **Search row** below the palette and above the matrix. `Pawn` filters by name only and never affects the palette. The **Work combo** opens centered with a `Filter Work` title: it shows `Any` only while a Work filter is active, excludes the currently selected WorkType, shows at most 10 rows with an integrated thin scrollbar. A `Filter: Line` / `Filter: Palette` / `Filter: Both` button (default **Line**, disabled until a Work filter exists) controls where the Work filter applies: `Line` filters pawn rows only, `Palette` filters palette chips only, `Both` filters both. `Not` inverts the Work filter wherever it applies. WorkTypes match by identity/defName, not substring. The `N match(es)` count appears for Pawn search or Line filtering; `No pawns match` appears when the row list is empty.
- Cards are **flat single-line** rows (135×30) with a fixed order: **passion icon → best relevant skill level → capitalized WorkType name → `×`**. WorkType names are capitalized; colors come from `WorkTypeColorRegistry` (deterministic defName hash, user overrides stored in Mod Settings for a future picker). Passion icons come from one resolver shared by lanes, the drag card and Details: with VSE/Alpha Skills it uses the colored `PassionDef.Icon`, then the gray `PassionDef.WorkBoxIcon` tinted by `PassionDef.color`; without them it uses vanilla `UI/Icons/PassionMinor`/`PassionMajor`, then the vanilla gray work-box icons with the existing warm tints. No icon is drawn for `Passion.None`. Hover shows the full tooltip; no tooltip is shown while dragging.
- **Lane tooltip**: WorkType description, all `WorkGiverDef`s, and separate `Skill`, `Passion`, `Level` and `Priority` lines. `Passion` uses the VSE/Alpha Skills `PassionDef.LabelCap` when available and falls back to vanilla `minor`/`major`/`none`. A `PawnSpecificOverrideHook` is exposed for future per-pawn/per-giver overrides (none implemented yet). **Palette tooltip**: description, global WorkGivers, compressed name and the WorkType's relevant skill names (no selected-pawn skill lookup).
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
2. Pick the **Rules** tab. Reorder work types by dragging the vertical rows (the `≡` handle).
3. Split mode: pick the origin work type, type a new name (the `defName` preview appears below it), drag origin givers into `Split destination`, and click `Add split`. Origin rows already in the draft are drawn gray in their original position; dragging a destination row back to the origin removes it.
4. Merge mode: pick the origin and a different target (the origin is excluded from the target list). The destination shows the target's existing givers fixed at the top; drag origin givers in, reorder them, and click `Merge selected`. `Merge all` moves every origin giver. Each row shows its final position.
5. Order mode: pick a work type and drag its giver list to the desired order. `Reset order` removes the saved override; the original def order returns after a restart.
6. `Cancel draft` clears only the unsaved draft. Every saved row has its own reset (`Reset split`, `Reset merge`, `Reset order`), each with two-step confirmation.
7. The **General** tab holds `Auto recommend new colony`, `Auto-hide scrollbars`, `Palette stress test (debug)`, the dense-rank spike, and `Reset config mod all` (two-step confirmation; pawn 1..N priorities are untouched).
8. Restart RimWorld. Changes are applied at load time.

## Mod Settings — Rules and General (unreleased phase 1b)
- The settings window has two in-memory tabs: **Rules** (WorkType order, Split/Merge/Order editor) and **General** (auto settings, palette stress test, dense-rank spike, reset). Tab selection is not persisted.
- The editor has `Split mode` / `Merge mode` / `Order mode`. Split/Merge use two columns with the left column always **Origin**; the right header is `Split destination` or `Destination`.
- Merge destination = existing target givers marked `(fixed)` at the top, then the incoming origin givers, which are draggable and reorderable. `MergeEntry.targetOrder` stores the final order (existing first, incoming after) and `OrderedWorkStartup.ApplyMerges` applies it after assigning `workType`. Old saves without `targetOrder` keep the old behavior.
- Each giver column has a 6 px scrollbar with a draggable thumb and a hint tooltip. A plain wheel scrolls the outer settings; the column scrolls only while the pointer is over its bar or Ctrl is held over the column. The outer settings scroll is sized from `Listing_Standard.CurHeight`, so it only spans the current tab content.
- Dragged rows are blue, valid drop rows get a blue/green tint, hovered rows highlight, and every movable giver row has a `≡` handle plus a tooltip (`defName` + WorkType). No tooltip is shown while a draft row is being dragged.
- `Add split` is enabled only with a non-empty name, a valid origin, and at least one draft giver that belongs to that origin. `Merge selected` and `Merge all` require a target different from the origin. Disabled actions show an inline warning.
- Drafts are UI-only (not scribed). Saved `SplitEntry` / `MergeEntry` entries remain scribed and apply on restart. Resets only remove the saved entry; no WorkType is created or deleted in the editor.
- `Reset split` stops recreating the split WorkType after restart. `Reset merge` returns foreign givers to their original type after restart. `Reset order` removes the saved giver override and restores the original def order after restart. All three need a restart and use two-step confirmation.


## Technical spike: dense rank test (0.2.0-alpha.2)
- Load a colony first. In `Options -> Mod Settings -> Ordered Work`, click **Test dense rank on colonists (log)**. With no map loaded the window shows `Load a colony to run the dense-rank test.` instead of the button.
- The button writes dense rank `1..N` (where `N` is the number of visible work types, minimum 4) into each free colonist's private `Pawn_WorkSettings.priorities` map, bypassing the `SetPriority` clamp, then calls `Notify_UseWorkPrioritiesChanged()`. If the private field is unavailable it falls back to `SetPriority`.
- Inside a `try/catch`, any failure is logged as `[Ordered Work] Dense rank test failed: ...` so the Mod Settings scroll view always closes cleanly.
- It only logs; the Work Control tab above now provides the visual editor.
- Expected log shape: `[Ordered Work] Dense rank test <pawn>: max=<max>, count=<count>, first=<defName>=<priority>, last=<defName>=<priority>`. Compare `last` with its assigned rank (e.g. 23): if it survives, direct-map mode bypasses the vanilla 4 cap; if it reads 4, the value was clamped downstream.
- **Do not use this build in a main save**; test in a throwaway colony.

## Limitations (0.3.0-alpha.77)
- No profiles, schedules, equipment or multi-pawn yet.
- Pawn order follows the colonist bar; if the bar is unavailable or returns no visible free colonists, the map's `FreeColonists` order is used. Dead pawns are filtered out.
- Scrollbars are custom and 12 px thick; when auto-hidden they still reserve their gutters. The horizontal bar is above the matrix; the vertical is in the right gutter; they use separate drag state and show flags.
- The 3-row palette balances rows with `ceil(count/3)` and slides horizontally; chips are ellipsized rather than wrapped.
- The Work combo keeps 10 WorkType rows plus `Any` visible with a thin dropdown scrollbar, excludes the currently selected WorkType, and opens centered. The Work filter is stored by defName and its scope is UI-only (not persisted).
- The drag placeholder is drawn on the hovered row. With 2+ selected rows, the whole group updates only when the drop is inside the selection; otherwise only the hovered row updates. Shift-drag marquee row selection is not implemented.
- Per-pawn/per-giver overrides are only a hook (`PawnSpecificOverrideHook`); no overrides are implemented yet. The color registry stores user overrides but has no picker yet.
- Selection is `Find.Selector`-driven; row selection is via the pawn name/portrait column, while lane cards remain editing controls.
- Passion icons depend on the installed mods: VSE/Alpha Skills is discovered by soft reflection (no compile-time dependency), and without it the resolver falls back to vanilla textures. If neither the colored nor the gray texture is available for a custom passion, no icon is drawn.
- The vanilla **View: Draw** button is an overlay at the top-right of the vanilla Work tab and may cover part of the last column header (usually the remaining-space filler).
- **Auto recommend new colony** and **Auto-hide scrollbars** are global Mod Settings toggles. The old per-save `viewMode` field is kept only for save compatibility and is ignored.
- `Dialog_WorkControlDetails` is read-only and basic: name/title, top 8 skills, and the current WorkType order.
- The >4 cell applies only in manual mode (`useWorkPriorities`) and only to capable free colonists; simple mode and incapable/non-free-colonist pawns stay vanilla.
- If another mod also prefixes `WidgetsWork.DrawWorkBoxFor`, Harmony ordering may let that mod win, leaving the cell vanilla; the rest of the mod keeps working.
- The dynamic max falls back to 99 when the count fails; the fallback is logged.
- Pawn/Work filters are UI-only and not persisted.
- Mod Settings tab selection and Split/Merge/Order drafts are UI-only and not persisted; saved split/merge entries are still scribed and applied on restart.
- Saved resets (`Reset split`, `Reset merge`, `Reset order`) only remove the saved entry/override. They do not undo WorkType creation, giver moves or `priorityInType` writes during the current session; the effect appears after a restart.
- In Order mode the exact original `priorityInType` values are not stored, so `Reset order` cannot restore them immediately. It removes the saved override, and fresh defs produce the original order on the next restart.
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
