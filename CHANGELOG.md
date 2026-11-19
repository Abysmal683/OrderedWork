# Changelog

All notable changes to Ordered Work are documented here.
This format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [0.3.0-alpha.163] - 2026-11-19

### Refactor — Work panel layout and drag lists
- Added `WorkPanelLayout` as the single source for header, body, column and stacked rects.
- Added `UiDragList` for consistent row drawing, scroll, selection, drag/drop and commit.
- WorkTypes and WorkGivers now use the same list component, fixing the scroll/hitbox selection mismatch.
- Moved WorkType-specific stats to the lower 30% of the Givers column.
- Top 5 now lists skills, not WorkTypes; filters remain Lv, P and Lv.P.
- Removed Apply/Cancel; WorkType and WorkGiver changes are committed immediately.
- Moved Recommend next to Givers.
- Reset all WorkGivers now uses a two-step confirmation, expires after 5 seconds, and sits next to the selected Reset.
- Restored Changed markers in both WorkType rows and Giver rows.

## [0.3.0-alpha.162] - 2026-11-19

### Fix — Work panel visual feedback pass 2
- Moved WorkType-specific stats to the bottom 30% of the WorkGiver column.
- Kept Top 5 WorkTypes as the full metadata column; no search button.
- Fixed drag feedback so the green target only appears after real drag movement; blue remains the dragged row.
- Removed giver-row `Changed` labels and the header `Changed` label.
- WorkType rows now show `Changed` only when that WorkType's WorkGiver order differs from the global JSON baseline.
- Apply/Cancel now compare against the last saved per-pawn draft, not against the global baseline, so they stay disabled when nothing was edited.
- Guarded all stat reads to avoid the work-table stat crash.

## [0.3.0-alpha.161] - 2026-11-19

### Fix — Work editor feedback pass
- Removed the WorkType per-row `Changed` marker; only WorkGiver rows compare against the global JSON order.
- Replaced per-row giver chevrons with a single `Givers` column toggle; selecting a WorkType opens the giver column for that type.
- WorkType rows now accept drag from the row or handle and show a drag/drop tint; WorkGivers got the same tint.
- `Top 5 trabajos` now lists WorkTypes by level, passion or combined recommendation score (without pins); the search button was removed.
- Fixed the `DrawStats` NullReferenceException caused by evaluating the work-table-only cooking stat on a pawn.
- Stats reads are now guarded and use pawn-safe stats.

## [0.3.0-alpha.160] - 2026-11-19

### Feature — per-pawn Work editor
- Added the `Trabajo` detail tab with a self-contained three-column editor.
- WorkTypes: drag order, add capable types, remove to priority 0, per-row `Changed`, and a 70/30 active/incapable split.
- WorkGivers: per-WorkType drag order, per-row `Changed`, selected reset and a separated global reset; givers are never deleted.
- WorkGiver overrides are now persisted per pawn in `PawnWorkOrder.workGiverOrders` and use the global JSON profile as the reset baseline.
- Added `Recommend` for the WorkType draft without touching WorkGiver overrides.
- Added a read-only metadata column with top 4 relevant skills, level/passion filter modes, WorkType-aware search, and basic relevant stats.
- `WorkPanel` is self-contained for future grid hosting.

## [0.3.0-alpha.159] - 2026-11-18

### Debug spike — per-pawn WorkGiver order
- Added an opt-in diagnostics spike that reorders Hauling WorkGivers for one selected pawn.
- Uses Harmony postfixes on `WorkGiversInOrderNormal` and `WorkGiversInOrderEmergency`; it does not mutate `priorityInType` or any global def.
- Added Mod Settings controls to apply, log and clear the spike for the selected pawn.
- Added `[OrderedWork][Spike]` logs for the applied order and the first runtime reorders.
- External/non-target WorkGivers keep their original positions; only the target WorkType group is reordered.

## [0.3.0-alpha.158] - 2026-11-18

### Refactor — single optional VSE adapter
- Removed the duplicate VSE reflection and visual API from `WorkTypePaletteView.Passion`.
- `PassionSource` now owns optional VSE labels, icons, work-box icons and colour names; it caches the reflection API once.
- VSE/Alpha Skills remains optional: without it, vanilla passion icons and labels still work.
- No new hard dependency; no Harmony patch added.
- VSE is still not referenced by the project.

## [0.3.0-alpha.157] - 2026-11-18

### Feature — pawn detail status
- Overview now summarises work, schedule and clothing presets plus override and zone counts.
- New `Overrides` tab lists work, schedule, hourly-work and clothing overrides with their source.
- New `Ropa` tab shows the vanilla apparel policy, worn apparel, stored clothing preset and clothing overrides read-only.
- Clothing application remains intentionally pending; the panel reports stored policy without pretending it is applied.

## [0.3.0-alpha.156] - 2026-11-18

### Refactor — code ownership cleanup
- `RulesUI` split into `RulesTab`, `GeneralTab` and `ProfilesPanel` partials.
- `Order` split into `OrderEditor` and `GiverOrder` partials.
- Removed unused `UiGrid` and `UiSection` helpers.
- Removed the last obsolete `DrawTabMarker`.
- Previous action-button and zebra behavior remains.

## [0.3.0-alpha.155] - 2026-11-18

### Refactor — single action-button dependency
- Removed the `DrawActionButton` alias; all mod partials call `UiButtonBar.Button` directly.
- Split/Merge, Order and Zone action buttons regained contextual tooltips.
- No visual or behavioral change beyond tooltips.
- CodeGraph retains one button implementation to locate.

## [0.3.0-alpha.154] - 2026-11-18

### Fixed — Reset recommendation weights state
- Actually wired `PassionWeightPolicy.HasChangesFromDefaults` into the Reset button.
- Reset is disabled when current entries match the current defaults.
- Reset is enabled when a weight differs or an extra removed-mod entry exists.
- Added a temporary completion message and state-specific tooltip.
- No scoring changes.

## [0.3.0-alpha.153] - 2026-11-18

### Added — Reset recommendation weights state
- `PassionWeightPolicy.HasChangesFromDefaults` compares current entries against the current defaults, including extra removed-mod entries.
- Reset is disabled and tooltip explains "already match defaults" when there are no changes.
- Reset uses `UiButtonBar` and reports a temporary completion message when it runs.
- No behavior changes to scoring.

## [0.3.0-alpha.152] - 2026-11-18

### Fixed — enabled buttons now pass the real label
- The enabled branch was still calling `Widgets.ButtonText` with an empty label; the disabled branch drew the overlay text. That produced the inverted result.
- Enabled: native button with the real label.
- Disabled: native atlas tinted dark, empty native label, normal label drawn on top.
- No behavior changes.

## [0.3.0-alpha.151] - 2026-11-18

### Fixed — enabled buttons keep their labels
- `UiButtonBar.Button` only uses the empty native button plus overlay when disabled.
- Enabled buttons pass the real label directly to `Widgets.ButtonText`.
- Disabled body tint and normal text from .150 are preserved.
- No behavior changes.

## [0.3.0-alpha.150] - 2026-11-18

### Fixed — native disabled button shape restored
- Disabled action buttons no longer use flat `CustomButtonText`.
- They draw the native button atlas with a dimmed `GUI.color`, then redraw the label in normal white.
- This restores the rounded native body and removes the custom hover-gold label behavior.
- All action buttons continue through `UiButtonBar.Button`.
- No behavior changes.

## [0.3.0-alpha.149] - 2026-11-18

### Fixed — CustomButtonText uses the correct API for disabled body tint
- Disabled action buttons now use `Widgets.CustomButtonText` with explicit background, text, border and fill colors.
- The body is darkened, the label stays normal white, and border size is zero (no thin internal line).
- Enabled buttons remain native `Widgets.ButtonText`.
- Passion tooltip hit-testing now uses the screen-space row rect instead of `Mouse.IsOver` inside the scroll group.
- No behavior changes.

## [0.3.0-alpha.148] - 2026-11-18

### Fixed — disabled button body tint without dimming text
- Disabled action buttons now render the native button background with a dark `GUI.color` tint, then draw the label in normal white on top.
- This changes the body tint instead of only dimming the letters.
- Applied through the single `UiButtonBar.Button` implementation to all action buttons.
- No behavior changes.

## [0.3.0-alpha.147] - 2026-11-18

### Fixed — disabled button body tint and passion tooltip hit test
- Disabled action buttons tint the button background through `GUI.backgroundColor`, not only the label.
- `UiButtonBar` is the single action-button implementation; Work order, Split/Merge, Profiles, Diagnostics, Zone, Details, Recommend and Search filters use it.
- Search `Filter: Line` and `Clear` now use the same disabled body tint.
- Passion row tooltip hit-testing uses the screen-space row rect after accounting for the scroll viewport; this prevents the tooltip from appearing over Split/Merge buttons.
- No behavior changes.

## [0.3.0-alpha.146] - 2026-11-18

### Fixed — single disabled renderer and Search button tint
- `DrawActionButton` now delegates to `UiButtonBar.Button`; no duplicated implementation remains.
- `UiButtonBar.Button` uses `GUI.enabled`, so disabled Work order, Split/Merge, Profiles, Diagnostics, Zone, Details and Recommend all share the same native dimming.
- Search `Filter: Line` and `Clear` now use `UiButtonBar.Button` instead of local GUI.enabled blocks.
- Native tooltips remain on disabled buttons.
- No behavior changes.

## [0.3.0-alpha.145] - 2026-11-18

### Fixed — disabled state now uses GUI.enabled
- `UiButtonBar.Button` wraps the native button with `GUI.enabled`, so disabled actions are visually dimmed and non-interactive.
- Details and Recommend top controls now use `UiButtonBar`; disabled state matches Schedule/Apply and config actions.
- Consolidated the duplicate action-button implementations; CodeGraph confirms one `DrawActionButton` plus one `UiButtonBar.Button`.
- Existing tooltips remain available while disabled.
- No behavior changes when enabled.

## [0.3.0-alpha.144] - 2026-11-18

### Fixed — single action button implementation
- Removed the duplicate `DrawActionButton` overload from Schedule; all actions now resolve to `UiButtonBar.Button`.
- `UiButtonBar.Button` is a single native `Widgets.ButtonText` wrapper with optional tooltip.
- Split/Merge Add/Cancel, Work order, Profiles, Diagnostics and Zone now share one enabled/disabled representation.
- Added disabled explanations for Add split/Cancel draft.
- Removed custom borders/backgrounds responsible for the thin line over disabled buttons.
- No behavior changes.

## [0.3.0-alpha.143] - 2026-11-18

### Fixed — all action buttons use native vanilla states
- `DrawActionButton` is now a thin wrapper over `Widgets.ButtonText(active)` with optional tooltip.
- Removed the custom black background/border that caused thin lines and inconsistent disabled rendering.
- Profiles, Diagnostics, Work order, Split/Merge, Order and Zone now share the native gold/dark button style.
- Split/Merge Add/Cancel now expose tooltips.
- Zebra, deferred passion tooltips and work order draft remain.

## [0.3.0-alpha.142] - 2026-11-18

### Fixed — unified vanilla action buttons, deferred passion tooltips and visible zebra
- `DrawActionButton` and `UiButtonBar` now use native `Widgets.ButtonText` states: gold enabled, dark disabled, no custom black outline.
- Work order Apply/Cancel use the same action style and disabled tooltips.
- Passion row tooltips are collected inside the scroll group and submitted after `GUI.EndGroup`, preventing tooltip bleed into Split/Merge Apply buttons.
- Zebra alpha is now 0.085 and is actually applied to passion rows, Work order, Diagnostics, Split/Merge and Order rows.
- Passion list still uses 10 visible rows.

## [0.3.0-alpha.141] - 2026-11-18

### Changed — visible outlined action buttons and stronger zebra
- `DrawActionButton` now uses a dark outlined surface for both enabled and disabled states; text stays bright when enabled and muted when blocked.
- Work order, Split/Merge, Order, Profiles, Diagnostics and Zone use that same action style.
- Zebra alpha increased from 0.035 to 0.085 so alternating rows are actually visible.
- Work order Apply/Cancel receive disabled-state tooltips.
- This is a visual-only batch; no behavior changes.

## [0.3.0-alpha.140] - 2026-11-18

### Added — zebra giver lists, unified disabled actions and action tooltips
- Zebra rows applied to Split/Merge giver columns and Order giver lists.
- Work order Apply/Cancel, Order Apply/Cancel, Profiles and Diagnostics actions now use the same `DrawActionButton` visual, including the dark disabled state.
- Disabled action tooltips explain why they are unavailable.
- Split/Merge draft actions expose their warning or save/cancel intent as a tooltip.
- Work type order remains draft-only until Apply.

## [0.3.0-alpha.139] - 2026-11-18

### Added — zebra rows and safe Work type order draft
- Work type order is now a local draft with Changed status, Apply work order and Cancel work order.
- Drag only edits the draft; nothing is written until Apply.
- Zebra backgrounds added to Work type order, passion weights and Diagnostics rows.
- Diagnostics checkboxes now carry explicit tooltips.
- Shared `UiZebra` helper added; colors remain in `UiColors`.
- Profiles spacing and Zone fixed map width remain.

## [0.3.0-alpha.138] - 2026-11-18

### Fixed — exact giver-column color, Profiles spacing and Zone map width
- `UiColors.ColumnBackground` now matches the surface actually used by Split/Merge columns.
- Split/Merge column surface and Order background use the same `UiColors` constant.
- Profiles adds 6/6/4px gaps between selector, Save-as field and action buttons.
- Zone map uses a fixed 48px label + 230px map button via `UiTable`; no stretching.
- CodeGraph index synced locally after the refactor.
- No functional changes.

## [0.3.0-alpha.137] - 2026-11-18

### Added — shared UI colors and layout corrections
- Added `UiColors` as the shared palette for panel body, column background, column headers, warnings, disabled text and empty cells.
- Order giver columns use the same column color as Split/Merge.
- Profiles section gets breathing room before/after its content.
- Zone map selector uses fixed 48px label + 230px map button from `UiTable`; it no longer stretches.
- CodeGraph index initialized locally; `.codegraph/` ignored by Git.
- Split/Merge/Order tabs remain native; no functional changes.

## [0.3.0-alpha.136] - 2026-11-18

### Fixed — Order column contrast, Profiles spacing and Zone map width
- Order giver columns use a darker background so they contrast with the section.
- Profiles panel gets vertical breathing room inside its collapsible section.
- Zone map selector uses a fixed 230px column from `UiTable` instead of a relative flex width; it no longer stretches.
- `.codegraph/` added to `.gitignore`; local CodeGraph index initialized for OrderedWork.
- No functional changes.

## [0.3.0-alpha.135] - 2026-11-18

### Fixed — column backgrounds and delete footer alignment
- Added column backgrounds to Split, Merge and Order giver panels so their extent is visible.
- Delete confirmation footer is anchored to the bottom edge, matching the normal name/Save footer position.
- Zone Apply/Manage remain compact fixed-width buttons.
- No functional changes.

## [0.3.0-alpha.134] - 2026-11-18

### Refactor — finish Zone, Save footer and General actions
- Zone now lays out its rows with `UiRows`, map columns with `UiFlex`, and Apply/Manage with `UiButtonBar`.
- Save delete footer uses `UiInset`, `UiRows`, `UiFlex` and `UiButtonBar`.
- General diagnostics/danger actions use `UiButtonBar`.
- Split/Merge/Order mode selector remains native `UiTabBar`; split fields use `UiFieldRow`.
- No behavior changes intended.

## [0.3.0-alpha.133] - 2026-11-18

### Refactor — Zone, Save footer and General actions
- Zone Apply/Manage now use `UiButtonBar` while preserving the existing zone behavior.
- Save-delete footer uses `UiInset`, `UiRows` and `UiFlex`.
- General Diagnostics/Danger buttons use `UiButtonBar`.
- Split/Merge/Order tabs remain native `UiTabBar`; Overview remains on `UiFlex`.
- No behavior changes intended.

## [0.3.0-alpha.132] - 2026-11-18

### Refactor — Split/Merge/Order tabs and Overview columns
- Split/Merge/Order mode selector now uses native `UiTabBar`/`TabDrawer` tabs instead of ordinary buttons.
- Split name/description use `UiFieldRow`.
- Order columns and Apply/Cancel use `UiFlex`/`UiButtonBar`.
- Details Overview table columns use `UiFlex`; skill/status alignment no longer depends on manual widths.
- Existing split refresh, metadata and config behavior remain.

## [0.3.0-alpha.131] - 2026-11-18

### Refactor — Profiles panel uses layout helpers
- Save-as row now uses `UiFieldRow`.
- Save profile and Write pending actions use `UiButtonBar`.
- Removed the last manual label/text-field X offsets in the Profiles panel.
- No behavior change; internal config inset and collapsible sections remain.

## [0.3.0-alpha.130] - 2026-11-18

### Fixed — real config inset and compact Save
- Config now actually wraps `Listing_Standard` in a 16px inset scroll body; the previous body inset had not compiled into the running view.
- Save dialog height is calculated from preset count and its list stops above the name row; the oversized empty bottom is removed.
- Schedule grid now derives hour column width from `UiGrid`.
- Config keeps native Cerrar with bottom padding; Details/Save keep only native X.
- Collapsible sections and Diagnostics grouping remain.

## [0.3.0-alpha.129] - 2026-11-18

### Fixed — actual config margin and Save height
- Config scroll body is now really inset by 16px before `Listing_Standard` begins; the previous body.ContractedBy edit had not been applied.
- Save dialog row layout now really uses `UiTable`; no stale xMax offsets remain.
- Save dialog height formula and list height leave only a normal bottom margin instead of a large empty area.
- Config tabs/body and native Cerrar spacing remain.

## [0.3.0-alpha.128] - 2026-11-18

### Changed — internal margins, Save sizing and git hygiene
- Config content now has a real 14px internal inset inside its body; tabs touch the body.
- Save dialog height adapts to preset count, removing the large empty lower area.
- Added `.editorconfig` with LF endings and C# indentation.
- Added `.githooks/pre-commit` structural checks for whitespace, conflict markers and staged bin/obj files.
- Normalized text files to LF; CRLF-only status noise is gone.

## [0.3.0-alpha.127] - 2026-11-18

### Changed — spacing and section order
- Save dialog is shorter, releasing unused bottom space; its list still scrolls.
- Config scroll content inset increased to 14px and section content has more breathing room.
- Details content inset increased to 12px.
- Verbose diagnostics and Palette stress test moved into Diagnostics.
- Allow empty origin moved into Split / Merge / Order as its first option.
- Manual Close remains removed from Details and Save dialog; Config keeps RimWorld's native Cerrar with bottom padding.

## [0.3.0-alpha.126] - 2026-11-18

### Changed — Save dialog uses UiTable
- Schedule preset rows now derive name/date/Overwrite/Delete widths from `UiTable`.
- Removed the last manual xMax offsets in that row.
- Collapsible Config sections and native close spacing remain.

## [0.3.0-alpha.125] - 2026-11-18

### Fixed — Config sections and native close spacing
- Applied real collapsible sections to Rules and General.
- General grouped into Core settings, Profiles, Diagnostics and Danger zone.
- Added bottom content padding so RimWorld's native Cerrar button no longer cuts the final text.
- Removed the manual Close buttons from Details and the Save dialog; both use the native X.
- Kept Config's native Cerrar because it belongs to RimWorld's mod settings window.
- Split metadata remains in Order mode; no new layout components without evidence.

## [0.3.0-alpha.124] - 2026-11-18

### Added — collapsible sections and close cleanup
- Config now exposes actual collapsible sections with title, +/- and divider.
- Removed the duplicate manual Close buttons from the pawn Details and Save dialog; both rely on the native X.
- Kept native Close on RimWorld's mod settings window.
- Sections: Rules, Work types and order, Recommendation pins, Recommendation passion weights, Split/Merge/Order.
- Added `UiCollapsibleSection` and section state in the mod settings view.

## [0.3.0-alpha.123] - 2026-11-18

### Added — collapsible config sections
- Added `UiCollapsibleSection`: title, +/- control and divider.
- Config sections are now collapsible: Work types and order, Recommendation pins, Recommendation passion weights, Split/Merge/Order.
- `PassionWeightsView` can omit its duplicate internal title when hosted in a section.
- Kept only high-evidence layout helpers; skipped low-evidence wrappers.

## [0.3.0-alpha.122] - 2026-11-18

### Added — focused layout components
- Added `UiFlex` for weighted columns.
- Added `UiSection` for header + inset body sections.
- Added `UiFieldRow` for label/field alignment.
- Added `UiButtonBar` for enabled/disabled action buttons.
- Split metadata now uses `UiFieldRow`/`UiButtonBar`.
- Kept `UiInset`, `UiRows`, `UiGrid`, `UiTable` and `UiTabBar`; skipped low-evidence `UiWrap`, `UiCard`, `UiBadge`, `UiDivider` and `UiScroll`.

## [0.3.0-alpha.121] - 2026-11-18

### Fixed — stale giver-order panel after split/merge/mode switch
- Added `ResetGiverOrderDraft` to invalidate the draft, origin snapshot and cached WorkType.
- Split, Merge, mode switches, Profile Restore/Delete and WorkType selection now reset the draft immediately.
- Split and Merge save paths refresh Order mode without requiring a WorkType round-trip.
- Config tab/body spacing, split metadata and restore/delete messages from .120 remain.

## [0.3.0-alpha.120] - 2026-11-18

### Added — layout wrapper and split metadata
- Added `UiTabBar`, the tab/body wrapper over native `TabDrawer`.
- Details and Config tabs now use the wrapper; tab body and content share the same top.
- Added split metadata editing in Order mode for custom split WorkTypes: label, description and readonly defName.
- Restore and Delete split now emit temporary messages.
- Layout primitives and native tab wrapper are ready for migration of Save dialog, Schedule and Zone.

## [0.3.0-alpha.119] - 2026-11-18

### Changed — tab/body flush and layout spacing
- Details tabs now sit directly above the content body; content gets an 8px inner margin.
- Config tabs/body now share the same top edge; content is inset by 8px.
- Config tabs moved down to clear the window title.
- Added restore/delete split feedback and saved/overwritten/deleted preset messages.
- Added layout primitives (`UiInset`, `UiRows`, `UiGrid`, `UiTable`) as the base for the next refactor.

## [0.3.0-alpha.118] - 2026-11-18

### Added — layout primitives and restore/delete feedback
- Added IMGUI layout helpers: `UiInset`, `UiRows`, `UiGrid`, `UiTable` under `Source/UI/Layout`.
- These are the base for migrating dense panels away from hardcoded Rects.
- Added temporary messages for config Restore and Delete split actions.
- Native tabs, compact labels, date column, duplicate overwrite and title/config spacing from .117 remain.

## [0.3.0-alpha.117] - 2026-11-17

### Changed — tab/body spacing and feedback
- Removed the heavy body tint from the pawn Details panel.
- Config body now meets the tab strip directly and content is inset by 8px.
- Details content is inset from the panel edge; tabs remain below the title.
- Added temporary completion messages for Save, Overwrite, Delete, config reset, Split, Merge and Giver order.
- Presets keep native tabs, date column, duplicate-name overwrite and owner-name fallback.
- Schedule drag is disabled on the Horario tab; native drag remains available on Overview/Zona.

## [0.3.0-alpha.116] - 2026-11-17

### Changed — tab spacing, body and schedule drag policy
- Native tabs are moved down so their rounded top no longer covers the title.
- Added a lighter body background below the tabs in Details and Config.
- Details now toggles native `draggable`: enabled on Overview/Zona, disabled while Horario is active.
- Removed the failing custom title drag path; no content-drag interception remains.
- Save dialog date column and duplicate-name overwrite remain in place.

## [0.3.0-alpha.115] - 2026-11-17

### Changed — native tabs, date layout and duplicate preset names
- Tabs now use RimWorld `TabDrawer`/`TabRecord` for vanilla rounded atlas and colors.
- Schedule save rows widen the date column; old presets use file modification time when ModifiedUtc is absent.
- Loading presets deduplicates by name, keeping the newest record.
- Saving a duplicate name overwrites the existing preset instead of creating another.
- Empty name on Overwrite uses the selected preset name; opening Save from an unnamed pawn uses the pawn name.
- Detail window uses native draggable=false plus `GUI.DragWindow` only on the title strip.

## [0.3.0-alpha.114] - 2026-11-17

### Changed — native RimWorld tabs and title-only window drag
- `OrderedTabStrip` now wraps `Verse.TabDrawer` and `Verse.TabRecord`; tabs use the vanilla rounded atlas, border and selected color.
- Details uses native tabs; Config uses native tabs.
- Window uses `draggable=false` and `GUI.DragWindow` only on the title strip.
- Window minimum/resize remains native through WindowResizer; content cannot start a window drag.
- Save/Overwrite: empty name uses the selected preset; duplicate names overwrite; fallback owner name is passed when opening Save.
- Schedule presets display ModifiedUtc, with filesystem fallback for old files.
- Delete remains a labelled button with opaque inline confirmation.

## [0.3.0-alpha.113] - 2026-11-17

### Fixed — title-only drag, tabs and schedule naming
- Detail window now disables native dragging and uses `GUI.DragWindow` only on the title strip.
- Shared large tab strip is active in Details and Config.
- Schedule Save uses the selected preset name when empty; saving a duplicate name overwrites instead of creating duplicates.
- Presets load a fallback modification timestamp from the file and write ModifiedUtc on save.
- Delete remains a labelled button with an opaque inline confirmation footer.
- Compact W/J/S/M/A labels and tooltips remain.

## [0.3.0-alpha.112] - 2026-11-17

### Fixed — tab strip, content isolation and unnamed overwrite
- Pawn detail now uses the shared large tab strip; Settings already uses it.
- The whole content area below the title consumes mouse drag, limiting native Window drag to the title bar.
- Schedule cells also consume paint drag.
- Overwrite no longer requires the name field: it uses the selected preset name when the field is empty.
- Delete uses the opaque inline confirmation footer.
- Schedule labels remain compact with full tooltips.

## [0.3.0-alpha.111] - 2026-11-17

### Changed — native window and shared tab strip
- Adopted the BetterInfoCard approach for the panel: native `draggable=true`, `resizeable=true`, and `WindowResizer.minWindowSize` via reflection.
- Removed custom title drag and per-frame windowRect minimum fighting.
- Clears `Widgets.mouseOverScrollViewStack` when window size changes.
- Schedule cells consume mouse drag so native window dragging is not started from the timetable.
- Added `OrderedTabStrip` with a vanilla-like selected-tab underline; used in pawn detail and mod settings.
- Schedule Delete is a normal button with an opaque inline confirmation footer.
- Overwrite falls back to the selected preset name when the name field is empty.
- Compact schedule labels use W/J/S/M/A at narrow widths with full tooltips.

## [0.3.0-alpha.110] - 2026-11-17

### Fixed — compact schedule cells and delete footer
- Schedule cells use one-letter labels at narrow widths and full labels only when they fit; tooltips keep the full assignment.
- Delete confirmation is drawn as an opaque footer and temporarily hides the name/Save/Close row, removing the visual overlap behind Confirm/Cancel.
- Preset rows keep separate Overwrite/Delete buttons.

## [0.3.0-alpha.109] - 2026-11-17

### Fixed — evidence-based schedule dialog and window
- Logs showed Delete/Overwrite clicks reached the dialog but no save/delete completed.
- Overwrite now falls back to the target preset name when the name field is empty.
- Delete uses an inline confirm/cancel step instead of a separate modal that was not appearing.
- Save dialog rows reserve space for the normal Delete button; the trash hitbox is gone.
- Title drag now uses held-mouse movement, not only the MouseDrag event.
- Minimum size is applied through WindowResizer when available; manual clamp is fallback only.
- Zone buttons are rebalanced: map 230, area 300, Apply and Manage 130 each.
- Hours are centered and alternate; rows retain tooltips.

## [0.3.0-alpha.108] - 2026-11-17

### Fixed — explicit buttons and window resizing
- Schedule preset Delete is now a normal labelled button; no trash icon hitbox.
- Save rows reserve separate space for Overwrite and Delete.
- Zone section has a separate `Mapa:` label, a larger map button, a wider area selector, and Apply/Manage on one row with equal widths.
- Detail window uses a reflected WindowResizer minimum of 500x700, avoiding per-frame minimum fights.
- Title drag also accepts the local title rectangle; dragging the schedule must not move the window.
- Scheduled hours use centered headers and alternating column tints.

## [0.3.0-alpha.107] - 2026-11-17

### Fixed — window movement and schedule clarity
- Detail window now uses a custom title-only drag with `draggable = false`; dragging the schedule no longer moves the window.
- Minimum size raised to 500x700 and clamped to the screen maximum.
- Content leaves bottom space for Close and scrollbars; hour labels are centered and columns alternate.
- Added explicit working click path and logs for Save-dialog Overwrite/Delete; Delete still asks for confirmation.
- Save dialog no longer inherits a stale draft name when the parent name is empty.
- Zone Apply/Manage are on the same row with equal widths; map/area controls were compacted.
- Missing area fallback remains active and disables Apply.

## [0.3.0-alpha.106] - 2026-11-17

### Fixed — schedule dialog and panel interaction
- Schedule hour headers are centered and alternate column backgrounds improve readability.
- Save dialog row selection no longer consumes clicks meant for Overwrite/Delete; actions now close or refresh correctly.
- The save dialog no longer inherits a stale draft name when the parent name is empty.
- Schedule `Overwrite` and `Delete` have working callbacks and logs.
- Detail window sets `draggable`, clamps to 300x300, and clamps to the screen maximum.
- Tabs fit the minimum width; schedule/zone content scrolls when the window is resized.
- Missing schedule preset falls back to the current timetable; missing zone falls back to the current applied area and disables Apply.

## [0.3.0-alpha.105] - 2026-11-17

### Safety and UI fixes
- Missing schedule preset: resolver falls back to the pawn's current timetable; the link is preserved.
- Missing zone area: binding is preserved, UI falls back to the current applied area and Apply is disabled until an existing area is chosen.
- Missing defs and links are skipped without deleting stored references.
- Schedule rows now show hour labels and full assignment tooltips.
- Save dialog row selection no longer overlays the Overwrite/Delete buttons; Overwrite and Delete close/refresh correctly and Delete asks for confirmation.
- Schedule save dialog inherits the current name.
- Zone section was compacted (map/area/apply/manage buttons).
- Modded assignment colors fall back to a deterministic generated color when the def color is missing.
- Tab widths fit the 300px minimum; detail window clamps to 300x300.

## [0.3.0-alpha.104] - 2026-11-17

### Changed — schedule save/load dialog and window behavior
- Schedule Save now opens a vanilla-style dialog with search, preset rows, per-row Overwrite, delete confirmation and a bottom Save/Close bar.
- The save dialog inherits the current schedule name.
- Delete requires confirmation and no longer leaves a disabled row artifact.
- Modded TimeAssignmentDef colors fall back to a deterministic generated color when the def color is missing.
- Detail window explicitly enforces a 300x300 minimum and logs draggable/resizable/min-size state in verbose mode.
- Zone picker remains limited to Area_Allowed and Area_Home.

## [0.3.0-alpha.103] - 2026-11-17

### Changed — save dialog, colors and zone filtering
- Schedule Save now opens a dialog with Save as new, Replace, Delete and Cancel.
- Schedule Save/Apply use the standard disabled visual state; Apply is enabled only by real timetable changes.
- Assignment cells use the original `TimeAssignmentDef.color`, fixing the Work color mismatch.
- Zone Apply is disabled until the selected area differs from the applied area.
- Zone picker is restricted to `Area_Allowed` and `Area_Home`; built-in roof/pollution/snow areas are excluded.
- Zone management button is renamed to `Manage zones`.
- Detail panel is explicitly draggable/resizable with a 300x300 minimum; content scrolls when resized.
- Added verbose logs for schedule save/replace/delete and zone area enumeration.

## [0.3.0-alpha.102] - 2026-11-17

### Changed — detail panel ergonomics
- Detail window is draggable, resizable and clamped to a 300x300 minimum.
- Schedule and Zone content use scroll views when the window is small; the schedule keeps a minimum content width.
- Schedule Load/New/Copy/Name/Save/Apply are in two rows to reduce density.
- Cell labels are compact (`Any`, `Med`, etc.) with full assignment tooltips.
- Load shows `(none)` unless a preset/name is selected; New and Copy clear the name.
- Apply is enabled by real timetable changes; Save requires a valid name.
- Zone selector shows regular and Home areas only for now.
- Zone Apply is disabled when the selected area equals the applied area.

## [0.3.0-alpha.101] - 2026-11-17

### Fixed — schedule apply state
- Apply is enabled by real timetable changes only; a valid preset name is not required to apply a draft.
- Saving still requires a valid name.
- Draft apply writes the timetable without creating a dangling preset link.

## [0.3.0-alpha.100] - 2026-11-17

### Changed — schedule/zone refinement
- Schedule controls now separate `Load` from `New`; New clears the name and Save is disabled until a valid name exists.
- Schedule cells use compact labels with full labels and defNames in tooltips.
- Apply is enabled only when the draft actually differs from the current pawn timetable.
- Zone selector shows a visible neutral swatch for unrestricted and includes all map areas, including Home/custom areas.
- Zone section shows the last applied zone for the selected map.
- Zone manager button is labelled for create/delete and opens the vanilla manager for expand/reduce/edit.
- Fixed detail header spacing and applied the same section layout to Zona.

## [0.3.0-alpha.99] - 2026-11-17

### Changed — pawn detail schedule and zone sections
- Removed the top-level Work/Schedule switch and the old vertical schedule test view.
- `Details` is now a larger sectioned panel: Overview, Horario, Zona.
- Horario has three rows: read-only Origen, editable Modif and reserved priority-by-hour.
- Modif uses a compact colored assignment brush with per-cell tooltips; click/drag paints hours.
- Schedule presets remain global JSON files; Apply writes the pawn timetable.
- Zona has a map selector, colored area selector and a link to the vanilla area manager for create/delete/expand/reduce.
- Zone bindings are stored per map; assigning a different map stores the link without writing the current map.
- Added `PawnScheduleWorkOverride` storage for the future priority-by-hour layer (not edited yet).

## [0.3.0-alpha.98] - 2026-11-17

### Added — basic Schedule view
- Work Control now has `Work` / `Schedule` views.
- Schedule view lists global presets, captures the first selected pawn timetable, saves a named preset, previews current vs draft by hour and applies to selected pawns.
- Hour editing uses vanilla `TimeAssignmentDef` options.
- Applying a schedule writes `Pawn.timetable` through `ScheduleAdapter`; the pawn record stores only `schedulePresetId`.
- Presets remain one JSON file each under `Config/OrderedWork/schedules/<id>.json`.
- No zone UI, clothing UI or apply-on-load yet.

## [0.3.0-alpha.97] - 2026-11-17

### Added — policy storage foundation
- `PawnWorkOrder` now carries a schema version, stable pawn ID, work preset link, sparse work overrides, schedule preset link, sparse schedule overrides, zone bindings and clothing placeholders.
- Per-save policy lists are normalized on load; invalid hours, duplicate keys and null rows are dropped, while missing defs/areas/presets are preserved.
- `SchedulePreset` and `SchedulePresetStore` added; one JSON file per preset in `Config/OrderedWork/schedules/<id>.json`.
- Schedule JSON writes use temp+replace, keep a `.bak`, validate schema and isolate corrupt presets.
- Save component records a policy schema version.
- No policy UI or application adapter yet.

## [0.3.0-alpha.96] - 2026-11-17

### Refactor — palette split
- Split `WorkTypePaletteView` into core palette/drag, tooltip/rank and passion-visual partials.
- Behavior unchanged; `DisplayName` remains in the core partial.
- Work Control file sizes: core 307, tooltip 142, passion 415.

## [0.3.0-alpha.95] - 2026-11-17

### Adjusted — defaults for bad/slow passions
- `learnRateFactor` remains the primary metric.
- A custom passion below `None` (`learnRateFactor < 0.35`) or marked `isBad` starts at `-4` instead of `-2`.
- Slow/bad customs get `perLevelBonus >= 1`, so they improve with level and can become competitive.
- Vanilla `None/Minor/Major` defaults remain `-3/0/0`, `1/2/0`, `3/4/0`.
- `PassionWeightPolicy` migration v3 updates only entries matching the previous automatic formula; player edits are preserved.
- Added a test pinning the Apathy-style curve (`-4` at level 0, 21 at level 10 vs None 20).
- Suite now 15/15.

## [0.3.0-alpha.94] - 2026-11-17

### Refactor — Work Control and order service
- Split `OrderedWorkMod.SplitMerge.cs` into orchestration, columns, drag and persistence partials.
- Split `WorkOrderService` into core, apply and lifecycle partials.
- Extracted the pure score formula to `OrderedWork.Core.RecommendationScorePolicy`.
- Added seven scoring-policy tests; suite now 14/14.
- No intended behavior change.
- Documented `WorkGiverDef` cloning as rejected; no runtime Def clones.

## [0.3.0-alpha.93] - 2026-11-17

### Refactor — passion pipeline
- Split `PassionWeightCatalog` into `PassionSource`, `PassionWeightPolicy`, `PassionWeightsView` and `RecommendationScorer`.
- `PassionSource` owns VSE/Alpha discovery and runtime metadata.
- `PassionWeightPolicy` owns defaults, one-time migration and reset.
- `PassionWeightsView` owns the 10-row table, sprites, tooltips, scrollbar and reset button.
- `RecommendationScorer` owns `BestSkillLevel` and pure score calculation.
- No behavior change intended; the failed Shift+wheel path stays removed.

## [0.3.0-alpha.92] - 2026-11-17

### Cleanup — reverted shift+wheel attempt
- Removed the failed nested Shift+wheel routing from Settings.
- Scrollbar drag remains available for passion weights and Split/Merge columns.
- Plain wheel is left to the outer settings scroll.
- No shift-wheel behavior is advertised or consumed.

## [0.3.0-alpha.91] - 2026-11-17

### Fixed — deterministic nested wheel routing
- Sections use `Mouse.IsOver` instead of raw rect containment, matching the proven Work Control hit testing.
- Plain wheel over the passion table or Split/Merge columns is consumed and forwarded to the outer settings scroll.
- Shift+wheel is consumed by the inner table/column.
- This removes reliance on nested scroll-view wheel ownership.

## [0.3.0-alpha.90] - 2026-11-17

### Added — learn-rate based defaults for modded passions
- Unknown modded passions now derive defaults from `PassionDef.learnRateFactor`, the skill learning multiplier.
- Vanilla `None/Minor/Major` keep the approved `-3/0/0`, `1/2/0`, `3/4/0` defaults.
- Custom defaults use `level 0 = round(4 * learnRate - 3)` and `per level = round(4 * learnRate - 2)`.
- Existing neutral custom entries from older builds migrate once; edited values are preserved.
- The tooltip shows the live `Learn rate` multiplier.

## [0.3.0-alpha.89] - 2026-11-17

### Fixed — nested wheel capture
- Passion weights no longer use a nested scroll view that steals the plain wheel.
- Nested scroll input is handled before the outer settings scroll: only Shift+wheel is consumed.
- The passion table uses a custom clipped viewport and scrollbar.
- Split/Merge column wheel state is processed before the outer scroll as well.
- Plain wheel over both sections now moves the outer settings panel.

## [0.3.0-alpha.88] - 2026-11-17

### Changed — nested scroll input
- Passion weights scroll only with Shift+wheel; a plain wheel scrolls the outer settings panel.
- Split/Merge columns scroll only with Shift+wheel over the column.
- Plain wheel over those sections is forwarded to the outer settings scroll.
- Updated the column scrollbar tooltip.

## [0.3.0-alpha.87] - 2026-11-17

### Added — passion description tooltip
- Hovering the passion icon/name shows the live `PassionDef.description` from VSE/Alpha.
- Description text is read at runtime through reflection; no third-party text is copied into Ordered Work.
- No tooltip is shown when the passion def does not expose a description.

## [0.3.0-alpha.86] - 2026-11-17

### Changed — passion weights view
- Passion weight rows are capped at 10 visible rows.
- The table gets its own scrollbar when more passions are registered.
- The global settings scroll no longer grows with every passion mod.

## [0.3.0-alpha.85] - 2026-11-17

### Fixed — passion sprites in weight rows
- The resolved VSE/vanilla passion sprite is now drawn before each weight label.
- The icon uses the same cache and color/tint path as the Work tab.

## [0.3.0-alpha.84] - 2026-11-17

### Fixed — VSE configuration load
- `LabelCap` is a `TaggedString`; it is now read safely instead of aborting passion enumeration.
- Passion enumeration is per-entry fault tolerant.
- Added a `DefDatabase<VSE.Passions.PassionDef>` fallback for mods that do not populate `AllPassions`.
- Weight rows show the resolved passion sprite before the label.

## [0.3.0-alpha.83] - 2026-11-17

### Changed — passion-mod compatibility
- Rules resolves all VSE/Alpha passion defs with icons and stable `defName` keys.
- Unknown custom passions receive a neutral default entry and can be weighted immediately.
- Missing entries are created before opening Rules and before recommendation scoring.
- Removed passion mods keep their stored weights for when the mod is re-enabled.
- Global weights and pins remain in ModSettings XML; profile JSON stays unchanged.

## [0.3.0-alpha.82] - 2026-11-16

### Added — passion weights
- Rules lists all registered passions (vanilla/VSE/Alpha) with Level 0, per-level bonus and minimum level fields.
- `PassionWeightCatalog` enumerates VSE `PassionManager.AllPassions` by reflection and falls back to None/Minor/Major.
- Recommendation score uses Level0Score / PerLevelBonus / MinLevel; below MinLevel it behaves like no passion.
- Negative weights are allowed; no-skill WorkTypes remain score 0.
- `Reset recommendation weights` restores defaults.
- Add-pin button tooltip lists the default pins.

### Added — automatic recommendation rules
- Rules now has a `Prioritize low-level passion over work without skill` checkbox.
- Recommendation pins are configurable: drag to order, `x` to remove, `Add recommendation pin` dropdown for any visible WorkType.
- Recommendation scoring uses the configured pins and the passion-policy toggle.
- General adds `Verbose diagnostics in Player.log`, default off; temporary Order/Reset/Scroll/Resize lines are now routed through `DebugLog` and silenced by default.

### Refactor / cleanup
- Split Work Control into core/search/rows/interaction partials.
- Removed obsolete `WorkViewMode` and unused `OrderedWorkSettings.initialized`.
- Updated CHECKLIST, STATUS and SUMMARY to the profile-backed architecture.
- Final legacy inventory: no persisted split/merge/giver/WorkType order lists; profile WorkTypes/Givers/Order and reference only.

### Legacy removal — final split DTO
- Removed `SplitEntry`; `EnsureSplitWorkTypes` now creates split WorkTypes directly from `WorkTypeConfig`.
- Removed the last `SplitEntry` fallback and legacy comment.
- No persisted legacy work lists remain: profile WorkTypes/Givers/Order and reference are the only source.

### Legacy removal — workTypeOrder
- Removed `OrderedWorkSettings.workTypeOrder` and all persisted references.
- Rules WorkType order now reads/writes profile `WorkTypeConfig.Order`.
- Palette order, profile capture, startup fallback and dense-rank test now use the profile order.
- Removed legacy `EnsureWorkTypeOrder`, `MoveWorkType` and `ApplyOrder` paths.

### Remaining
- `SplitEntry` remains only as a transient DTO for WorkType creation; no persisted split list.
- Final cleanup can rename/remove that DTO and consolidate profile editor services.

### Legacy removal — split stage
- Startup now loads/promotes/applies the JSON profile.
- `SaveSplit` writes a canonical `WorkTypeConfig` and moves givers inside the profile.
- `ApplyProfile` creates split WorkTypes from the profile using a transient split DTO.
- Removed `OrderedWorkSettings.splits` and all persisted split list references.
- Removed the old split metadata UI (will return profile-backed later).
- Remaining legacy: `Settings.workTypeOrder` only.

### Changed
- Confirmed Reset order duplicates the selected WorkType Restore already shown in Reset changes; removed the large Reset order button and its confirmation state. Use Restore in the unified list.
- Legacy giver-order structures remain removed from alpha.75.

### Remaining legacy
- `splits`;
- `workTypeOrder`.

### Legacy removal — stage 2
- Removed `GiverOrderEntry`, `OrderedWorkSettings.giverOrder`, `originalGiverOrder`, legacy capture/apply loops and `CommitGiverOrder`.
- Order reset now restores giver order from `reference` and applies the profile.
- Remaining legacy: `splits` and `workTypeOrder`.

### Legacy removal — stage 1
- Removed `MergeEntry`, `GiverOrigin`, `OrderedWorkSettings.merges`, legacy `ApplyMerges` and all runtime references. Merge now exists only as profile state.
- Remaining legacy: `splits`, `giverOrder`, `originalGiverOrder`, `workTypeOrder` pending their profile-backed replacements.

### Fixed
- Re-added `ReturnSurplusGivers` in the restore path; it had been lost in a refactor. Givers present in the restored target but absent from its reference now move to their reference WorkType, appended at the end.
- Reconcile live/profile before reset, so the live Firefighter extra is represented in the profile before Restore.
- Delete split remains target-only.

### Fixed
- Added live-profile reconciliation: givers that exist only in live WorkGiverDefs (the Firefighter extra) are returned to their reference WorkType in the profile before Merge/Reset is rendered or saved.
- Reset and Merge now operate on reconciled profile state, eliminating the one-step-behind UI.
- This targets the order-dependent Restore Firefighter/Patient failure.

### Fixed
- Active profile name is initialized at Mod Settings entry and by Order/Merge/Reset, not only when General was opened. This was why reset/merge logged profile=default while test_1 was selected and changes landed in the wrong profile.
- Merge editor and SaveMerge use the active profile/pending; live giver ownership is reconciled from profile + reference.

### Fixed
- Merge editor now reads the profile/pending giver lists instead of live WorkGiverDefs.
- SaveMerge now moves givers in the canonical profile, saves active+pending and applies immediately; it no longer relies on legacy MergeEntry application.
- ApplyProfile now reconciles every live WorkGiverDef: profile ownership wins; givers absent from the profile return to their reference owner.
- This addresses the live Firefighter extra giver that Order/Reset could not see.

### Fixed
- Split/Merge now capture and apply the resulting profile live, so Merge Patient→Firefighter updates Patient immediately rather than only pending.
- Reset refresh and selected-WorkType fallback apply after live profile changes.
- Delete split remains target-only; no reciprocal cleanup.
- Version label remains single in General.

### Fixed
- Removed the last fallback writes to `active-profile.txt`; selecting test_1 no longer silently reverts to vanilla after restart.
- Split/Merge Cancel dirty is cleared on Apply and on mode reset.
- Reset saves active+pending, refreshes Order, clears the draft and falls back if the selected WorkType disappeared.
- Delete split no longer calls reciprocal cleanup on its own giver list; exception path removed.
- Version label appears once, in General.

### Fixed
- Removed the duplicate version label from the main window; General is the single source.
- Split/Merge Cancel dirty is cleared after Apply as well as Cancel.
- Reset now saves active+pending, clears the Order draft baseline and refreshes the selected WorkType.
- Delete split no longer calls reciprocal cleanup; the Collection-modified exception path is removed.
- Build/install verification now compares source and installed DLL SHA256.

### Fixed
- Mod version is drawn from the main settings window, not the partial Settings file, and is now present in the compiled DLL.
- Verified source before build; the installed DLL is compared by SHA256 with the project DLL after copy.
- Delete split source no longer calls `RemoveGiverFromOtherTypes` on its own list.
- Profile fallback no longer overwrites `active-profile.txt`.

### Fixed
- Verified source before build: the old `RemoveGiverFromOtherTypes` Delete-split call was still present in the compiled file. Removed from source and confirmed by grep before build.
- Restore/delete reset enumeration should no longer throw; Delete split only affects its objective.
- Added `Mod version` to General; the installed DLL hash is verified against the project DLL after install.

### Fixed
- Profile fallback no longer overwrites `active-profile.txt`. A missing/invalid profile is resolved for the session but the requested selection is preserved, preventing “test switched back to vanilla”.
- Added the mod version to the General settings panel.
- Order reset now writes active + pending and refreshes the draft; reset logs and pending status are explicit.
- Delete split no longer uses reciprocal cleanup, so it cannot modify its own list while enumerating; this was the source of the Collection-modified exception.
- Restore keeps surplus-return and empty-split cleanup.

## [0.3.0-alpha.63] - 2026-11-16

### Fixed
- Log diagnosis: reset threw `Collection was modified; enumeration operation may not execute` at `OrderedWorkMod.Order.cs:247` during Delete split. Delete was calling `RemoveGiverFromOtherTypes` while iterating the split's own giver list, and the helper removed entries from that same list.
- Delete split now removes only the selected split, appends its givers to the base, reassigns live ownership, and never calls reciprocal cleanup.
- Restore keeps the reciprocal cleanup and surplus-return behavior.
- Order left side uses `giverOrderInitial`; Apply/Cancel use the visible `DrawActionButton`.
- Reset refreshes Order and writes both active and pending.

## [0.3.0-alpha.62] - 2026-11-16

### Fixed
- After Restore/Delete, Order now writes both active and pending, clears the giver-order draft baseline, and reloads the selected WorkType so the Order editor reflects the reset immediately.
- If the reset removes/hides the selected WorkType, Order falls back to the first visible WorkType.
- Apply now logs `profileSaved` and `pendingSaved` separately, making a pending write failure visible.
- `giverOrderDirty` is kept in sync with the sequence difference.

## [0.3.0-alpha.61] - 2026-11-16

### Fixed
- Delete split now affects only the selected split: it removes that split, returns its givers to its base at the end, and does not touch other splits/givers.
- Restore now returns surplus givers (present in the target but absent from its reference) to their reference WorkType, appended at the end of that WorkType's list.
- Removing a giver from a split during restore deletes the split if it becomes empty, with a `[Reset] removed empty split` log.
- Order baseline/dirty/visible-button fixes from alpha.60 remain.

## [0.3.0-alpha.60] - 2026-11-16

### Fixed
- Order left side now truly uses `giverOrderInitial` (last applied), not the reference profile; Apply refreshes it to the new order.
- Order dirty is the sequence comparison itself; buttons enable/disable as the draft differs from the last applied order.
- Restore now removes the reference giver from all other profile WorkTypes, reassigns the live WorkGiverDef, and restores the source in reference order.
- Delete split returns givers to the base at the end and removes them from other profile types.
- An empty split produced by reciprocal cleanup is removed and logged.

### Still need design review
- Cascading splits (a split based on another split) and restoring a source that leaves multiple derived splits empty.
- Exact ordering when a restored giver has to be appended to a non-reference WorkType.

## [0.3.0-alpha.59] - 2026-11-16

### Fixed
- Order source still had the conditional `changed && Widgets.ButtonText`; replaced with the same unconditional `DrawActionButton` used by Split/Merge. Buttons are now visible dark-disabled and hard non-clickable.
- Order dirty is recomputed as `!draft.SequenceEqual(lastApplied)`, so returning to the last saved order disables Apply/Cancel again.
- Order left side now uses the last-applied baseline; after Apply it refreshes to the new order.
- Restore now removes the original giver from other profile WorkTypes and reassigns the live `WorkGiverDef.workType` before applying.
- Delete split does the same reciprocal cleanup.
- Added `[OrderedWork][Order] applied` and `[OrderedWork][Reset]` logs.

## [0.3.0-alpha.58] - 2026-11-16

### Fixed
- Split and Merge now set their dirty flag when a giver is added, removed, or reordered in the destination. Cancel draft therefore enables together with Merge selected/all and disables again after cancel/save.
- No change to Order buttons; those were handled in alpha.57.

## [0.3.0-alpha.57] - 2026-11-16

### Fixed
- Order Apply/Cancel finally use the same unconditional `DrawActionButton` pattern that Split/Merge already use. The previous conditional `changed && Widgets.ButtonText` removed the buttons entirely when unchanged.
- Order buttons are now visible as dark disabled controls and are not clickable; dragging enables them.
- Split/Merge disable behavior is unchanged and confirmed working by the user.

## [0.3.0-alpha.56] - 2026-11-16

### Fixed
- Order now draws the same visible dark disabled button shape as Split/Merge via `DrawActionButton`; Apply/Cancel never disappear when unchanged and never steal clicks.
- Order origin side now shows `Origin (last applied)` from the last confirmed baseline; after Apply it updates to the new order while Reset still compares `reference.json`.
- Split and Merge Cancel draft now use the same visible disabled button and hard guard; no `GUI.enabled`-only controls remain in these actions.
- Giver drag dirty flag and Order action logs remain active.

## [0.3.0-alpha.55] - 2026-11-16

### Fixed
- The previous installed build still contained the old `GUI.enabled` button code; the source file was not the edited version despite the refactor. Order now calls `DrawActionButton(..., changed)` so Apply/Cancel are hard-disabled and cannot consume clicks.
- Giver drag now sets `giverOrderDirty`; Apply/Cancel reset it after use.
- Added `[OrderedWork][Order]` logs for drag and apply, including active profile and pending path.
- Existing refactor remains intact.

## [0.3.0-alpha.54] - 2026-11-16

### Fixed
- Order Apply/Cancel are now guarded by `dirty && Widgets.ButtonText`, so they are hard-disabled, not just grey.
- Giver drag sets `giverOrderDirty`; Apply/Cancel reset it.
- Split/Merge Cancel uses an explicit dirty guard and no longer accepts clicks with an empty draft.
- Split/Merge add/remove now marks the corresponding draft dirty.
- Refactor remains behavior-preserving apart from these guards.

## [0.3.0-alpha.53] - 2026-11-16

### Changed
- Order Apply/Cancel use a real edit-dirty flag set by dragging; disabled until the draft changes and reset after Apply/Cancel.
- Split/Merge drafts track dirty state; Cancel draft stays disabled until a giver is added/removed.
- Refactor remains behavior-preserving; no claim yet on restore/pending fixes.

### Next focused test
- Test Order in isolation before touching Split/Merge.



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
