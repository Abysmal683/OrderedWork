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


## Diagnostic categories

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
## Limitations (0.3.0-alpha.234)
- Profiles and basic schedule presets exist; Details has Overview, schedule, zone, override and read-only clothing status sections. Clothing application, equipment, bulk zone and multi-pawn are not implemented yet.
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
