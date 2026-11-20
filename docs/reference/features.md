# Features and limitations

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


## Active work indicator

The Work detail row and WorkControl lane card mark the WorkType currently used by the pawn's `CurJob`. Sleep, food, joy and jobs without a `workGiverDef` show no marker. The global palette is intentionally unmarked because it can represent multiple pawns.## Diagnostic categories

Diagnostics are enabled per subsystem in **Mod Settings → Diagnostics**:

| Category | Covers |
|---|---|
| Work Control | matrix layout, resize and scroll diagnostics |
| Schedule / focus | schedule presets, focus application and resolution |
| Stores | reserved for preset/store diagnostics |
| Passion / VSE | registry discovery and fallback |
| Config mod | configuration edits and resets |
| Harmony | reserved for patch diagnostics |

A legacy `verboseLogging=true` setting is migrated to all categories when no category has been chosen. Warnings and errors are never gated by this switch.
## Passion diagnostics

With **Verbose diagnostics in Player.log** enabled, startup emits one line:

```text
[OrderedWork][Passion] VSE manager=True|False registered=N names=...
```

Use it when testing with VSE/Alpha Skills enabled, disabled or removed. Without verbose diagnostics, only failures emit warnings.
## Limitations (0.3.0-alpha.329)
- Profiles and basic schedule presets exist; Details has Overview, schedule, zone, override and read-only clothing status sections. Clothing application, equipment, bulk zone and multi-pawn are not implemented yet. `Reset config mod all` clears UI settings, passion weights and drafts, then rebuilds the active profile from `reference.json`; other profile/pending/reference files remain.
- Hidden WorkTypes are Config-only: they are excluded from Work tab and Work Control and run through a fixed priority overlay. Incapable pawns skip them.
- The Work tab column order is applied once at `PawnColumnDefGenerator` generation time from the profile `naturalPriority`; the previous per-frame `PawnTable.get_Columns`/`PawnTableOnGUI` hooks were removed. Alternate Work-tab replacements still need runtime compatibility verification.
- Pawn order follows the colonist bar; if the bar is unavailable or returns no visible free colonists, the map's `FreeColonists` order is used. Dead pawns are filtered out.
- Scrollbars are custom and 12 px thick; when auto-hidden they still reserve their gutters. The horizontal bar is above the matrix; the vertical is in the right gutter; they use separate drag state and show flags.
- The 3-row palette balances rows with `ceil(count/3)` and slides horizontally; chips are ellipsized rather than wrapped.
- The Work combo keeps 10 WorkType rows plus `Any` visible with a thin dropdown scrollbar, excludes the currently selected WorkType, and opens centered. The Work filter is stored by defName and its scope is UI-only (not persisted).
- The drag placeholder is drawn on the hovered row. With 2+ selected rows, the whole group updates only when the drop is inside the selection; otherwise only the hovered row updates. Shift-drag marquee row selection is not implemented.
- Per-pawn WorkGiver order overrides are implemented through `WorkGiverOrderService`; the color registry still has no picker. Clothing application and assigned-workbench priority remain pending.
- Selection is `Find.Selector`-driven; row selection is via the pawn name/portrait column, while lane cards remain editing controls.
- Passion icons depend on the installed mods: VSE/Alpha Skills is discovered by soft reflection (no compile-time dependency), and without it the resolver falls back to vanilla textures. If neither the colored nor the gray texture is available for a custom passion, no icon is drawn.
- The vanilla **View: Draw** button is an overlay at the top-right of the vanilla Work tab and may cover part of the last column header (usually the remaining-space filler).
- **Auto recommend new colony** and **Auto-hide scrollbars** are global Mod Settings toggles. The old per-save `viewMode` field is kept only for save compatibility and is ignored.
- `Dialog_WorkControlDetails` has Work, Overview, Schedule, Zone, Overrides and read-only Clothing tabs. Clothing application is not implemented.
- The >4 cell applies only in manual mode (`useWorkPriorities`) and only to capable free colonists; simple mode and incapable/non-free-colonist pawns stay vanilla.
- If another mod also prefixes `WidgetsWork.DrawWorkBoxFor`, Harmony ordering may let that mod win, leaving the cell vanilla; the rest of the mod keeps working.
- The dynamic max falls back to 99 when the count fails; the fallback is logged.
- Pawn/Work filters are UI-only and not persisted.
- Mod Settings tab selection and Split/Merge/Order drafts are UI-only and not persisted. Saved changes live in the active profile JSON and its pending file.
- Split rows use `Edit` and two-step `Delete`; delete returns Givers to `Base` and is blocked when `Base` is missing. Merge and Order candidate rows use `Revert` with two-step confirmation.
- Merge Revert returns extra Givers to their reference owner and restores the selected WorkType from `reference.json`. Order Revert restores the reference Giver sequence without adding or removing Givers.
- Saved actions run the startup application pipeline immediately where possible. A restart remains the safe boundary for generated WorkType definitions and DefDatabase changes.
- Split metadata edits `Label`, `Action word` and `Description`; `defName` and `Base` remain immutable.
- Initialization only scans the current map's free colonists; a colony initialized on one map does not backfill colonists on another map until they spawn.
- The vanilla capture hook covers `SetPriority`; a mod that changes priorities by writing the private map directly will not be captured.
- The "Sort by WorkType" header button is not implemented yet.
- If another mod replaces the Work tab, this mod does not replace it; it writes the same vanilla priorities.
- Not safe for a main save.
