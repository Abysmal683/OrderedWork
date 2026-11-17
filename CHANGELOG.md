# Changelog

All notable changes to Ordered Work are documented here.
This format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [0.3.0-alpha.52] - 2026-11-16

### Changed
- Refactored `OrderedWorkMod.cs` from 1978 lines into partial-class files:
  - `OrderedWorkMod.cs` — core lifecycle/tabs/state (241 lines)
  - `OrderedWorkMod.SplitMerge.cs` — Split/Merge/Order UI and drag helpers (943 lines)
  - `OrderedWorkMod.Order.cs` — giver order, profile reset, profile apply helpers (379 lines)
  - `OrderedWorkMod.Settings.cs` — tabs, Rules/General/Profile panels (236 lines)
  - `OrderedWorkMod.Helpers.cs` — shared reorder rows, tooltips, WorkType normalization (306 lines)
- No behavior change; build and existing tests pass.

### Next
- With responsibilities separated, resume bug fixes for restore/pending/button states from the smaller files.

## [0.3.0-alpha.51] - 2026-11-16

### Fixed
- Apply/Cancel giver order now depend on an explicit dirty flag set only by a drag, not on reference/current differences. They stay disabled until the user changes the draft.
- Cancel restores the initial draft and clears dirty.
- Removed the unused WorkType-order diff implementation and its draft state.
- Restore cleanup now reassigns live giver owners and removes the giver from other profile WorkTypes before restoring the source.

### Known issue
- The reset list is profile-specific. `vanilla`/`default` have different pending/giver states; a restore shown under one profile is not the same change under another.

## [0.3.0-alpha.50] - 2026-11-16

### Fixed
- Order now renders only the giver reference/current two-column editor; the old WorkType diff call and duplicate giver list were removed.
- Apply/Cancel giver order are disabled until the draft changes.
- Restore reassigns the live giver owner as well as the profile entry.
- Removed the unused WorkType-order diff method and draft fields.

### Still not removed
- `splits`, `merges`, `giverOrder`, `workTypeOrder` settings lists remain until the editor is fully profile-backed.



### Changed
- Order mode now compares givers of the selected WorkType (origin = reference, destination = draft), not WorkType order. Apply/Cancel added.
- WorkType order remains in Rules.
- Restore removes a reference giver from every other profile WorkType before restoring the source.
- Delete split removes the giver from other profile types before returning it to the base.
- Removed the duplicate old giver-order list from Order.



### Fixed
- Order mode now actually calls the two-column reference/current editor; the previous call was missing, so the old giver-only screen remained.
- Order has separate Apply and Cancel actions; Apply saves active + pending and applies live.
- Restore now removes reference givers from other profile WorkTypes before restoring the source, so a Patient←Medic merge cannot leave the giver only in Medic.
- Delete split removes the giver from any other WorkType before returning it to the base.
- Removed the leftover Rules confirmation button.

## [0.3.0-alpha.47] - 2026-11-16

### Added
- Order mode now has the requested two-column WorkType comparison: left = reference order, right = editable destination order. Confirmation writes the order to active + pending and applies it live.
- Split name generation checks hidden `DefDatabase` defs, so reusing a deleted split name no longer silently fails.
- Deleting a split from the profile returns its givers to the base in reference order.
- Profile fallback persists active resolution.

### Still pending
- The old settings lists still bridge the editor and startup.
- Giver order remains a separate list below the new WorkType comparison.

## [0.3.0-alpha.46] - 2026-11-16

### Fixed
- Split defName generation now checks hidden `DefDatabase` defs too, so reusing a name after delete cannot silently skip creating the new split.
- Delete split returns givers to the base in the reference's original giver order, avoiding the “base remains changed” false reset.
- Added an explicit Rules-wide `Confirm WorkType order and write pending` action for order edits.
- All profile JSON was cleaned for a fresh startup reference; active fallback now persists default/vanilla resolution.

### Note
- Order editing still uses the drag list, not the requested origin/destination confirmation screen; the explicit confirm button is the interim guard. The two-column order comparison is the next UI pass.

## [0.3.0-alpha.45] - 2026-11-16

### Fixed
- Delete split now returns its givers to the base WorkType in the profile, so the base's related diff disappears instead of leaving two coupled reset entries.
- Hot profile apply after reset now hides `OrderedWork_*` defs removed from the profile.
- Active-profile fallback now persists `default`/`vanilla` selection when the requested profile is missing.
- All `OrderedWork` JSON profile/pending/reference/active files were deleted for a clean regeneration.

### Known test state
- Next RimWorld start should regenerate `reference.json` from fresh defs before applying the active profile.
- Order drag persistence still needs verification; the pending write hook exists but the UI does not yet show a dedicated pending-updated status.

## [0.3.0-alpha.44] - 2026-11-16

### Fixed
- Reset Restore/Delete now applies the changed profile to live defs immediately via `ApplyProfileNow`, so the effect is visible in-session; pending remains the restart-safe record.
- `ApplyProfile` hides stale `OrderedWork_*` defs removed from the active profile.
- Moved the test `reference.json` to `reference.old.json`; a fresh reference will be captured on the next RimWorld start before the active profile is applied.

### Test notes
- The fresh reference is only as clean as the def state at the next startup; do not save/apply another profile before that capture if a vanilla reference is required.

## [0.3.0-alpha.43] - 2026-11-16

### Fixed
- Split, Merge, WorkType reorder, giver-order commit, and Reset order now persist the live state to the active profile's pending JSON. Reset changes therefore see edits instead of a stale profile.
- Saving a profile also refreshes its pending snapshot.
- Reset list reads pending when present; after Restore/Delete it reflects the pending file on the next frame.
- Profile-specific reset lists are expected: a change made under `default` does not appear while `reset_test` is active.

## [0.3.0-alpha.42] - 2026-11-16

### Changed
- `Allow empty origin` is now a single Rules-wide checkbox in the Rules tab, persisted in settings; the Split/Merge-local duplicates were removed. Both validators use the same setting.



### Changed
- Removed the old informational saved-split/saved-merge lists from the Split and Merge sheets. Reset/change management lives only in Order.
- Test files confirm `reset_test` active, `reference`/`reset_test`/`reset_test.pending` aligned after reset; `default` keeps its split independently.

### Remaining legacy
- `OrderedWorkSettings.splits/merges/giverOrder/workTypeOrder` still feed the old editor and startup bridge. The next pass is the profile-backed editor, not a blind deletion.

## [0.3.0-alpha.40] - 2026-11-16

### Fixed
- Reset list reads the pending profile when it exists, so Restore/Delete feedback is visible next frame instead of looking inert.
- `WorkConfigStore.LoadPending` added.
- Profile reset writes pending and reflects it in the reset list; activation still requires restart.

### Known test fixture caveat
- The current `reference.json` came from the no-split test profile, not a guaranteed vanilla capture. Patient/Warden/Handling/Cooking/Hunting Restore entries can be false positives until a true pre-apply vanilla reference is captured.

## [0.3.0-alpha.39] - 2026-11-16

### Changed
- Order reset now uses `WorkConfigDiff` against `reference.json`: modified WorkTypes are listed; vanilla entries use Restore, split entries use Delete. Reset writes the active profile to pending for the next restart.
- Removed the legacy per-split/per-merge reset methods and their confirmation fields from the UI.
- Created a `reset_test` profile and a `reference.json` generated from the closest available no-split capture for manual reset testing. The test profile is not selected automatically.

### Remaining legacy
- `OrderedWorkSettings.splits/merges/giverOrder/workTypeOrder` still feed the old editor and startup bridge; they are not the final source of truth yet.

## [0.3.0-alpha.38] - 2026-11-16

### Added
- Startup now promotes the active profile's pending file, loads the active profile with requested→default→vanilla fallback, and applies the JSON profile to live defs.
- `ApplyProfile` materializes split WorkTypes, applies profile order, giver ownership/priority, labels/descriptions and visibility, then rebuilds caches. Legacy lists are repopulated for the existing editor.
- Active profile selection is now functional across restart.

### Known limitations
- Legacy `splits/merges` are still repopulated and used by the editor; full removal is the next pass.
- Reference diff is not yet connected to reset UI.
- Profile application is no longer hot-only: a restart promotes pending as designed. Hot editor actions can still mutate the current session.

## [0.3.0-alpha.37] - 2026-11-16

### Added
- `WorkConfigDiff` computes modified WorkTypes by comparing the active profile with `reference.json`. No persisted `modified` boolean: giver defName/order, visibility, order, label, description and split base are compared. Repeated giver classes/priorities are preserved.
- Design decision: reference diff wins on reliability; the ~100-giver comparison is cheap and cached by the caller when needed.

### Next
- Remove the legacy settings lists and make the JSON profile the source of truth (approved scope).



### Changed
- Split/Merge saved-entry lists are now informational only; their reset buttons were removed.
- Reset changed WorkTypes moved to the Order tab: split entries use **Delete**, merge entries use **Restore**, each with confirmation. Split delete restores givers to the base and hides the split; merge restore uses `MergeEntry.origins` when available.
- The reset section remains separate from profile JSON application; profile promotion/apply is still the next integration step.

## [0.3.0-alpha.35] - 2026-11-16

### Added
- General tab profile panel: lists saved JSON profiles, selects the active profile, saves the current live state as a named profile, and writes `<active>.pending.json`.
- `WorkConfigProfileBuilder` captures the current live WorkType/giver state into the canonical JSON shape (no operation history).
- Profile saves use atomic writes and keep timestamped backups of the previous profile.

### Not wired yet
- Pending is written but not promoted/applied at startup.
- The editor still uses the old settings lists; profile selection is not yet the live source of truth.
- Reference-based reset and compact `PawnWorkOrder` overrides remain next.

## [0.3.0-alpha.34] - 2026-11-16

### Added
- Save foundation for the global JSON model: canonical `WorkTypes` map keyed by `defName`, giver arrays, `Mods` list, `active`/`pending` profile paths, `reference.json`, atomic writes, schema version and fallback resolution.
- `Normalize` deduplicates only by `defName`; repeated `giverClass`/priority is preserved.
- Fallback load order is requested profile → `default` → generated `vanilla`, with explicit flags in the load result.

### Not wired yet
- The existing Mod Settings still uses the old `OrderedWorkSettings` lists. JSON profile selection, pending promotion, reference-based reset and the compact `PawnWorkOrder` overrides are the next integration steps.

## [0.3.0-alpha.33] - 2026-11-16

### Added
- Verified Core WorkGiver identity: no duplicate `defName`s; repeated giver classes and equal priorities are intentional and must not be deduplicated.
- General setting `Keep orders for destroyed pawns`, default off. Cleanup still keeps dead-but-not-destroyed pawns for resurrection; destroyed entries are removed unless the setting is enabled for return-mod compatibility.
- `SAVE_DESIGN.md` now documents giver identity, destroyed-pawn cleanup and the future global→per-save profile fallback.

## [0.3.0-alpha.32] - 2026-11-16

### Fixed
- Split and Merge now have an explicit `Allow empty origin` checkbox. Without it, the action is blocked and the inline warning explains why.
- `Merge selected` is back; `Merge all` is still available.
- `Reset order` restores the captured original `priorityInType` values and re-applies the config immediately.
- A split that no longer has live givers is hidden and removed from the order, so merging away a split's content no longer leaves a ghost.
- `originalGiverOrder` is captured before the first apply; new givers are appended when a split is created.

## [0.3.0-alpha.31] - 2026-11-16

### Fixed
- Restored `Merge selected` alongside `Merge all` and `Cancel draft`.
- Order mode now has a saved-split metadata editor: select a live split, edit name and description, and apply them to both `SplitEntry` and the live `WorkTypeDef`.
- Stale split/giver selections are cleared when a reset hides/removes a WorkType, so selectors no longer point at ghost types.
- Empty splits are hidden/skipped and show a warning; split descriptions are optional and clear after save/cancel.

### Known limitations
- `Merge all` can still empty the origin; vanilla origin handling and the accept/cancel dialog for an empty split origin are not implemented yet.
- Global def changes are still hot in the current session; the pending-file/next-load application guard remains a design item.

## [0.3.0-alpha.30] - 2026-11-16

### Fixed
- Stale split WorkTypes are removed from the WorkType order on settings open; old `OrderedWork_*` defs with no valid saved split are hidden and dropped. Empty saved splits are no longer created and show an inline warning.
- Merge-selected was removed: Merge now has **Merge all** and **Cancel draft**. Origin givers dropped on the destination now honor the insertion position; previously only destination rows could set the drop index.
- Split now has an optional description field, persisted in `SplitEntry.description` and applied to the WorkTypeDef; the draft fields clear after save/cancel.
- Hot apply/Reset split still restore givers to the base WorkType; new MergeEntry origins restore live reset for merges created after this version.

## [0.3.0-alpha.29] - 2026-11-16

### Fixed
- Mod Settings no longer jumps to the top when switching tab/mode or applying a draft: the outer scroll and measured content height are preserved.
- New splits are appended to `workTypeOrder` immediately; WorkType selectors and the palette see them after the hot apply/restart.
- Split/Merge saving now applies the pending changes immediately (hot) for UI feedback; `Reset split` restores its givers to the base WorkType and hides the split def live.
- `MergeEntry.origins` records source WorkTypes before a merge, so `Reset merge` can restore them live for entries created after this version. Old entries still use restart semantics.
- `Listing_Standard.maxOneColumn = true` prevents the hidden second column/no-scroll bug.

## [0.3.0-alpha.28] - 2026-11-16

### Fixed
- Mod Settings showed only the first column and no scrollbar because `Listing_Standard` automatically starts a second column when content exceeds its provisional first-frame height. Set `maxOneColumn = true` so the whole Rules tab is vertical and the measured outer scroll can reach it.

### Added
- Phase 1b: a third `Order mode` with an origin selector and one reorderable giver list replaces the old separate `Giver order` section in `Rules`.
- Phase 1b: Merge destination now shows the target's existing givers fixed at the top, followed by the movable/reorderable incoming origin givers. Row position numbers and a drag preview show the insertion position.
- Phase 1b: `MergeEntry.targetOrder` stores the final destination order (existing target givers first, then incoming). `OrderedWorkStartup.ApplyMerges` applies it to `priorityInType` after assigning `workType`; old saves with an empty `targetOrder` keep the previous behavior.
- Phase 1b: per-saved-row `Reset split` / `Reset merge` and `Reset order`, each with two-step confirmation. They only remove the saved entry/override; the effect appears on restart. `Cancel draft` stays separate.
- Phase 1b: every giver column has a 6 px scrollbar with a draggable thumb and a scroll hint. A plain wheel scrolls the outer settings; the column scrolls only over its bar or with Ctrl held.
- Phase 1b: the outer Mod Settings scroll is measured from `Listing_Standard.CurHeight`, so it is only as tall as the current tab content and resets when the tab or editor mode changes.

### Added (phase 1)
- Mod Settings now has two in-memory tabs: **Rules** (WorkType order, giver order, Split/Merge editor) and **General** (auto settings, palette stress test, dense-rank spike, reset). Tab selection is not saved.
- New unified two-column Split/Merge editor replaces the old text-field Split/Merge inputs. Left column is always Origin; right column is explicitly labelled `Split destination` or `Merge destination`.
- Split mode: origin WorkType dropdown, editable new name with a generated `defName` preview, drag Origin → Split to add, drag Split → Origin to remove, and `Add split` gated on a non-empty name, valid origin, and at least one origin giver in the draft.
- Merge mode: origin and different-target selectors (origin excluded from the target list), `Merge selected` (draft givers only) and `Merge all` (every origin giver).
- Saved split/merge entries are shown read-only below the editor and are never removed by `Cancel draft`.
- Draft drag visuals: blue dragged row, blue/green valid-drop tint, hover highlight, `≡` handle, giver tooltips, and no tooltip while dragging. Disabled actions show a visible inline warning.
- `Reset config mod all` also clears unsaved drafts; pawn 1..N priorities remain untouched.

### Changed
- Split/Merge drafts are UI-only and not scribed; saved `SplitEntry` / `MergeEntry` entries keep applying on restart. No hot split/merge.
- `Add split` still produces the existing `SplitEntry` shape and safe sanitized defName (`OrderedWork_<name>_<n>`), with collision-safe suffixing.

## [0.3.0-alpha.25] - 2026-11-16

### Fixed
- Shift+wheel now uses `laneViewport` (not the full grid viewport) for its horizontal maximum. The old call subtracted the pawn-column width from the available space, so it stopped that exact amount before the manual bar.
- Resize drag now polls `Input.GetMouseButton(0)` and the screen mouse position, so it continues after the cursor leaves the tab/window and stops only when the physical button is released.
- Reset config confirms on restart; the general reset does not touch pawn 1..N priorities.

### Pending design
- Per-pawn giver movement needs an override layer; changing global `WorkGiverDef.workType` would affect everyone, so no hot move is exposed yet.

## [0.3.0-alpha.24] - 2026-11-16

### Fixed
- Resize drag now uses the screen mouse position instead of the window-local mouse position, eliminating the feedback where the content origin moved while dragging.
- Lane content width is `(maxLane + 1)` slots instead of `(maxLane + 2)`, removing the extra ghost slot after the last card and `+`.
- Added a 6 px right gutter inside the pawn column so age/health no longer touch the lane divider; the divider itself stays at the column boundary.
- Added a safe `Reset config mod all` with a two-step confirmation. It resets mod settings (orders, givers, splits, merges, colors, stress test, panel height) but does not write pawn 1..N priorities.

## [0.3.0-alpha.23] - 2026-11-16

### Fixed
- Resize now starts from the actual window height instead of the inner content height; this removes the 36 px margin mismatch and should make the grip move the panel. Drag/up logs include the requested height.
- Removed the search-row Pawn/Work separator; the separator is now the vertical divider between the pawn column and the lane area, spanning header + body.
- Shift-wheel diagnostics now log `maxLane`, content width, lane viewport and `scrollX` to distinguish a content-width issue from a wheel issue. The previous log already reached `maxX`; the remaining cut is being treated as lane padding.

### Design
- Config edits are treated as applying safely on restart; the split WorkType creation stays on the startup path.
- Next minimal tool: hot-move WorkGivers for pawns with overrides, without creating/deleting WorkTypes.

## [0.3.0-alpha.22] - 2026-11-16

### Changed
- Removed the clipped `resize` text; the grip is only the 12 px bar. Added drag/up diagnostics to identify why the setting does not reach the window.
- Pawn/Work separator is now 2 px with a brighter color and a wider gap.
- Split/Merge design updated: left origin / right destination, explicit action label, `Merge selected` vs `Merge all`, two settings panels with local reset, and `Reset config mod all` in the general panel.
- Shift+wheel log proved it reaches `scrollX=1999` of `maxX=1999`; the remaining visual gap is the lane's right padding, not the wheel.

## [0.3.0-alpha.21] - 2026-11-16

### Changed
- Resize grip is now a 12 px bar labelled `resize`, with a `[OrderedWork][Resize]` MouseDown diagnostic.
- Shift+wheel logs `scrollX/maxX/delta` for the remaining end-of-scroll report.
- Added `SPLIT_MERGE_DESIGN.md` defining the unified two-column Split/Merge editor, validation, cancel-vs-delete rules, aggressive `Merge all`, and the restart/hot-apply question.

## [0.3.0-alpha.20] - 2026-11-16

### Fixed
- The resize grip now actually resizes the tab: the tab override re-runs `SetInitialSizeAndPosition()` every frame, so changing the saved height updates the bottom-anchored window.
- Mod Settings WorkType selector now uses the same compact 10-row dropdown method as Work Control (`Work type: ... ▼`), with the selected type excluded.
- Added a thin vertical separator and extra gap between the Pawn filter and the Work combo.
- Shift+wheel uses a longer step and snaps to the far-right position within 64 px, matching manual scrollbar reach.

## [0.3.0-alpha.19] - 2026-11-16

### Changed
- Mod Settings drag rows now show a distinct dragged row, a green drop-target row, a hover highlight and a `≡` handle. Hover tooltips show the WorkType description plus its WorkGiverDef list; giver rows show their WorkType. Tooltips are suppressed while dragging.
- The WorkType selector is now a real dropdown (`Work type: ... ▼`) instead of cycling through options.
- Work Control now opens at half of the available height by default, with a top grip to resize it. The minimum height keeps the top UI plus four pawn rows, and the chosen height is saved.
- Shift+wheel horizontal scrolling steps further and snaps to the far-right position when within 24 px, matching manual-bar reach.

### Still pending
- Split/Merge drag editors (origin/split lists, validation, merge target, per-entry remove/reset) are the next isolated pass. The old text-field split/merge UI remains for now.

## [0.3.0-alpha.18] - 2026-11-16

### Changed
- Work combo now uses RimWorld's natural centered `Window` position instead of a custom anchor; the `Filter Work` title remains. Removed the temporary `[OrderedWork][Combo]` placement log.
- Mod Settings work-type order and per-work-type WorkGiver order are now **vertical drag-and-drop stacks** with a `≡` handle; the old Up/Down buttons are gone. Reordering givers rewrites `priorityInType` and the saved `giverOrder` entries.
- Shared values with multiple dependents are centralized: `PassionIconSize` (lane/drag/Details), the three passion fallback tints, the dropdown bar width (same as the matrix scrollbar), and the debug palette count. Debug palette defs are cached instead of being rebuilt every frame.

## [0.3.0-alpha.17] - 2026-11-16

### Fixed
- Palette order now follows the user's saved `workTypeOrder` (the same order used by the Work tab), so the first column starts Patient, Doctor, Firefighter instead of naturalPriority order; column-major fill is unchanged.
- Shift+wheel now reads both `Event.current.shift` and the physical left/right Shift keys, and accepts `delta.x` as well as `delta.y`, so horizontal scrolling works on systems where Unity does not report the shift modifier.
- Keeping the mouse over either scrollbar now keeps both bars visible; auto-hide no longer drops the vertical bar when moving from the body to the horizontal bar (or the reverse).
- Lane tooltip and Details header now say `Priority` instead of `Rank`.
- Combo re-anchoring is re-applied in `PostOpen`; `[OrderedWork][Combo]` logs the anchor/window/screen geometry for the remaining placement report.

## [0.3.0-alpha.16] - 2026-11-16

### Fixed
- Palette chips now fill **top-to-bottom, then left-to-right** (column-major) instead of row-major.
- Added **Shift+wheel** horizontal scrolling for both the matrix and the palette; normal wheel remains vertical for the matrix.
- Added a `Palette stress test (debug)` Mod Setting that appends 60 visual-only WorkTypes to test the palette's own horizontal scrollbar. These entries are not in `DefDatabase`, cannot be dragged and are not assignable.
- Re-applies the combo anchor in `PostOpen` and logs `[OrderedWork][Combo]` anchor/window/screen geometry to diagnose the remaining placement report.

### Added
- `CREDITS.md` documents the compatibility references and licensing posture: no third-party code or assets are copied; VSE/Alpha Skills are read at runtime through their public `PassionDef` contract.

## [0.3.0-alpha.15] - 2026-11-16

### Fixed
- The matrix horizontal bar now keeps its drag when the palette bar has no overflow: palette and matrix horizontal drags use separate owners, so the palette no longer cancels the matrix drag every frame.
- The Work combo opens 6 px below its button with a `Filter Work` title; `Any` is shown only while a Work filter is active, and the selected WorkType remains excluded.
- The `Filter: Line/Palette/Both` button is disabled until a Work filter is active; added a `Clear` button that resets Pawn search, Work filter, `Not`, and the filter scope.
- VSE/Alpha Skills passion tooltips now use the inherited `Def.LabelCap` (with `label` fallback) instead of falling back to a numeric enum value; passion icons are 14 px.
- Temporary scroll diagnostics now include the actual bar geometry and confirm the palette/matrix owner split.

## [0.3.0-alpha.14] - 2026-11-16

### Fixed
- Work Control now requests the full screen above the 35 px main-tab bar instead of the default centered size.
- The Work combo opens directly under its button (clamped to the screen; flips above the button when it would overflow the bottom), keeps `Any` first, and excludes the currently selected WorkType from the list while still showing at most 10 rows with a thin scrollbar.
- Added a three-state `Filter: Line` / `Filter: Palette` / `Filter: Both` control (default **Line**). `Line` filters pawn rows only and keeps the palette complete; `Palette` filters palette chips only; `Both` filters both. `Not` inverts the Work filter wherever it applies. Pawn search always filters only pawn rows.
- WorkTypes now match by identity/defName instead of substring, so `Patient` no longer matches `PatientBedRest`.
- Scrollbars now handle input **before** `Widgets.BeginScrollView` and draw **after** the content, so the horizontal bar remains interactive when the vertical bar appears. Temporary `[OrderedWork][Scroll]` `MouseDown` diagnostics log either bar's geometry.
- Lane cards are now 135 px wide; ghost/placeholder, `+` card, remove button, ellipsis, lane content width and tooltips follow the new width.

### Added
- Shared passion icon resolver used by lane cards, the palette drag card and Details: VSE/Alpha Skills `PassionDef.Icon` first, then gray `PassionDef.WorkBoxIcon` tinted by `PassionDef.color`, then vanilla `UI/Icons/PassionMinor`/`PassionMajor`, then vanilla gray work-box icons. No icon for `Passion.None`. Reflection lookups are cached and silently fall back.
- `PassionText` uses VSE `PassionDef.LabelCap` when available; repeated comparisons rank custom passions by `PassionDef.color` (Major > Minor > Main > Disabled).

### Kept
- Colonist-bar order, hover-based drag targets, ghost/colors, simple/manual Work-tab fixes, return button, right-click max, selection, Recommend/Details, auto-recommend, save/load and resurrection. No profiles, schedules, equipment or multi-pawn.

## [0.3.0-alpha.13] - 2026-11-16

### Fixed
- Work search is now a single combo (the separate Work label/text and search field are gone). The dropdown shows at most 10 WorkTypes plus `Any` with an integrated thin scrollbar; the selected WorkType filters palette chips and pawn rows. `Not` is a real checkbox with tooltip “Show pawns without the selected work”.
- Horizontal and vertical scrollbars are independent: separate drag state and show flags, both gutters reserved, so the vertical bar no longer blocks the horizontal one. The horizontal bar stays fixed above the matrix; names scroll only vertically, lanes share the horizontal offset, auto-hide remains, and the last row stays complete.
- Lane cards are 20% tighter (~117 px); ghost, remove button, ellipsis and tooltips use the new width.
- Vanilla passion icons are tinted with distinct warm colors (minor yellow, major orange) in the lane, palette drag card and Details, preserving icon alpha and not tinting the card.

### Kept
- Colonist-bar order, hover-based drag targets, ghost/colors, simple/manual Work-tab fixes, return button, right-click max, selection, Recommend/Details, auto-recommend and save/load.

## [0.3.0-alpha.12] - 2026-11-16

### Fixed
- Horizontal lane scrollbar now works with many rows: it is fixed above the matrix (outside the rows), 12 px thick, draggable/clickable, with auto-hide kept. The vertical bar stays in the right gutter; names scroll only vertically, lanes share the horizontal offset, and the last row remains fully visible.
- Restored the visible Work text filter beside the WorkType selector; selector options show the plain WorkType name (no `Work:` prefix); the inverse button is `Not`; an `N match(es)` count is shown next to `No pawns match`.
- Drag/drop targets now follow the actual hover: an unselected hovered row gets the drop alone; with 2+ selected rows the whole group updates only when the drop is inside the selection; otherwise only the hovered row updates.
- Palette tooltips no longer derive a skill from the selected pawn or call the logging skill lookup. They list relevant skill names and all global WorkGivers. UI skill lookups use a guarded `SafeBestSkill` that skips missing `SkillRecord`s instead of triggering the fallback log.
- Lane tooltips split `Skill`, `Passion`, `Level` and `Rank`; description/all WorkGivers and the pawn override hook remain.
- Vanilla passion icon colors are preserved (white tint around the texture); cards are ~14 px tighter and the ghost uses the new width.

### Kept
- Colonist-bar pawn order, resurrection reapply-first, simple/manual Work-tab fixes, return button, right-click max, selection, Recommend/Details, colors, auto-recommend and save/load.

## [0.3.0-alpha.11] - 2026-11-16

### Changed
- Pawn rows now follow `Find.ColonistBar.GetColonistsInOrder()` filtered to visible free colonists, with `Find.CurrentMap.mapPawns.FreeColonists` as fallback. Revived pawns return to their bar position instead of jumping to the end.
- Replaced the built-in scrollbars with thin custom scrollbars outside the content (right gutter and below the lane), so the last row is not cut. Wheel works over names and lanes; names only scroll vertically; `Auto-hide scrollbars` Mod Settings option (default true).
- Palette is now a 3-row grid in natural `naturalPriority` order with horizontal slide and compact ellipsized chips.
- Added a Pawn/Work search row below the palette: Pawn name filter, WorkType selector plus text filter (`defName`/`label`/`labelShort`), and an inverse `¬` "without this work" button; filters combine with AND.
- Ghost drag is now an empty placeholder block at the exact drop slot; the held card follows the cursor with content, and the same renderer is used for palette drops and lane reorder. No tooltip while dragging.
- Cards are tighter (~160×30), WorkType names are capitalized, and `WorkTypeColorRegistry` gives each WorkType a deterministic colour (user overrides stored in Mod Settings for a future picker). Lane tooltips show description, all WorkGivers, best skill and current rank (with a `PawnSpecificOverrideHook`); palette tooltips show description, global WorkGivers, compressed name and context skill.
- Reduced portrait/name gap and ellipsis padding; age/health stay compact on the right with a full row tooltip.

### Fixed
- Resurrection/faction change now always checks for a stored order first and reapplies it; only a pawn with no stored order is treated as new. Clear `Reapplied stored order` / `Recommended new pawn` logs; dead-but-not-destroyed entries are kept.

### Kept
- Simple/manual Work-tab fixes, `View: Draw` return button, right-click max, `Find.Selector` selection, Recommend/Details, pins/scoring, hooks, save/load and the vanilla >4 priority patch.

## [0.3.0-alpha.10] - 2026-11-16

### Fixed
- Simple Work-tab mode (`Find.PlaySettings.useWorkPriorities == false`) now defers to vanilla: checkboxes work and no numeric cells appear. Manual mode keeps the 0..max numeric cells.
- Removed "Trying to set work to invalid priority" log spam: `SetPriority` above 4 is intercepted, written straight into the private priorities map, notified and captured; the original method (and its log) is skipped. Simple-mode 0–4 still uses vanilla.
- `View: Draw` return button postfix now uses the real `DoWindowContents(Rect rect)` parameter name (the old `inRect` caused a Harmony initialization failure). Compact Work Tab uses the same `MainTabWindow_Work`, so the button appears there too.

### Added
- Single compact **WorkType palette row** above the matrix (all visible types, horizontal scroll) with drag-to-lane:
  - floating drag card shows the hovered pawn's passion/level, or red `Incapaz`;
  - ghost insertion marker at the exact drop slot;
  - valid drops add at the slot; existing WorkTypes move/reorder; adjacent ghost is a no-op;
  - multi-selection applies the same WorkType/position to every selected pawn;
  - incapable pawns are skipped and reported with one grouped message.
- Portable pawn column: ellipsized name/title, compact right-side age/health, and a row tooltip with full name, age, title, health and active WorkType count.

### Kept
- Selection via `Find.Selector`, Recommend/Details, View: Numeric/Draw redirects, search filtering, flat cards, pins/scoring, incapacity filtering, hooks, respawn and save/load.

## [0.3.0-alpha.9] - 2026-11-16

### Changed
- Vanilla Work-tab right-click now jumps from priority `0` to `max`; otherwise it decrements by 1. Left-click still cycles `0 → 1 → … → max → 0`.
- Renamed the top-bar `R` button to **Recommend**; it now applies the recommendation to **all selected free colonists**.
- The fixed pawn column now draws each pawn's portrait (~32×32, scaled to row height) alongside name and title.
- Replaced the Draw/Manual/Both cycle with a clear view control: **View: Numeric** in Work Control opens the vanilla Work tab, and the vanilla Work tab draws a **View: Draw** button that opens Work Control. Both use `Find.MainTabsRoot`; neither tab is hidden.
- Initialization and new-pawn behavior now follow the Mod Settings checkbox **Auto recommend new colony** (default on). Off = copy the vanilla order with no automatic recommendation; on = current scored recommendation. The top Recommend button always works.

### Added
- Selection is now driven by `Find.Selector`: selected rows highlight, single click selects, shift-click toggles, and double-click selects, pans the camera and closes Work Control. Selection stays in sync with the vanilla tab.
- Mod Settings checkbox `Auto recommend new colony`.
- `WorkTabViewPatch` adds the vanilla Work tab's `View: Draw` return button.

### Kept
- Search filtering, flat single-line cards, shared horizontal scroll, outer vertical scroll, pins/scoring, incapacity filtering, Details window, hooks, respawn and the vanilla >4 priority patch.

## [0.3.0-alpha.8] - 2026-11-16

### Changed
- Removed the per-row `R` button. The recommendation is now a top-bar **R** button that acts only on the selected Work Control row/pawn; it is disabled when nothing is selected and never guesses a pawn.
- Added a top-bar **Details** button, enabled only with a selected row. It opens `Dialog_WorkControlDetails`, a read-only `Window` (`doCloseX`, `resizeable = false`, `absorbInputAroundWindow`, with a Close button) showing the pawn name/title, top skills with levels and passion icons, and the WorkType order with rank, WorkType name, best skill/passion and status (active/off/incapable).

### Unchanged
- Flexible filtering pawn search, flat single-line cards, row selection and double-click select/camera, one shared horizontal scroll, outer vertical scroll, Draw/Manual/Both modes, Open Work tab, and the vanilla Work-tab >4 priority patches.

## [0.3.0-alpha.7] - 2026-11-16

### Added
- Vanilla Work-tab manual priorities can now exceed 4. A clean-room Harmony prefix on `WidgetsWork.DrawWorkBoxFor` draws the capable free-colonist cell and cycles left-click `0 → 1 → … → max → 0`; right-click decrements.
- `WorkOrderService.GetManualMaxPriority(Pawn)`: dynamic `max(4, visible capable WorkTypes)`, capped at 99, with a logged 99 fallback on failure.
- `>4` cells draw a distinct blue box, cyan number and border, with a higher-priority tooltip `Priority N of max (above vanilla 4)`.

### Changed
- `Pawn_WorkSettings.SetPriority` is normalised to `0..max` and its postfix calls `Notify_UseWorkPrioritiesChanged` once (skipped while `IsApplying`); the existing capture rebuilds the stored order with no recursion.
- Priorities 0–4 keep vanilla priority colours; `0` stays an empty cell.
- Incapable cells and non-free-colonist pawns are left to vanilla.

### Known limitations
- Another mod prefixing `WidgetsWork.DrawWorkBoxFor` may win the Harmony order, leaving the vanilla cell capped at 4.
- Dynamic max falls back to a fixed 99 if the count fails.

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
