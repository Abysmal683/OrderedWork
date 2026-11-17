# Split / Merge editor — scope

Status: design for the next isolated pass. The current v0.3.0-alpha.x settings UI still exposes the old text-field split/merge editor until this is implemented.

## Shared layout

One two-column editor is used for both Split and Merge:

- Left column: **Origin** WorkType selector + vertical list of its WorkGivers.
- Right column: vertical target list.
  - Split: the draft split list; the split name/defName fields sit above it.
  - Merge: the target WorkType selector (excludes the selected origin) and its list.
- Both columns are compact and read top-to-bottom; the columns sit side by side.
- Bottom controls: `Reset draft`, `Save draft`, and the mode action (`Add split` / `Merge`).
- No existing split or merge is deleted by the per-entry action. The visible cancel action cancels the current draft only.

## Split

- Required before `Add split` enables:
  - non-empty, editable name;
  - a valid base/origin WorkType;
  - at least one WorkGiver that belongs to the selected origin WorkType.
- Drag from Origin to the split list. The origin row stays in place but turns gray, preserving its original position; the split row is drawn in the normal color.
- Drag from Split back to Origin to remove it.
- `Add split` creates a new WorkType from the base WorkType and the selected givers, matching the existing `SplitEntry` data model.
- An empty split cannot be saved. The UI warns instead of silently creating an empty WorkType.
- Cancel clears the draft, not saved split entries.

## Merge

- Origin selector is always present; the right column selects a different target WorkType (the selected origin is excluded).
- Drag individual givers from Origin to Target and use `Merge selected`.
- `Merge all` is the aggressive action: move every giver of the origin WorkType to the target.
- A target WorkType cannot be the same as origin and cannot be empty.
- Merge does not delete either WorkType; it moves giver ownership. The origin may become a WorkType with no givers.
- Per-entry remove/cancel resets the draft; existing saved merges remain editable only through their own saved entry controls.

## Validation and safety

- All actions are disabled until their required inputs are valid.
- Invalid/empty drafts show an inline warning and do not write to `OrderedWorkSettings`.
- `Reset draft` never touches saved entries.
- `Reset order` and `Reset all` are separate destructive controls and require confirmation.
- Split/Merge data is applied by `OrderedWorkStartup`; the editor must state whether changes require a restart. Hot-apply can be added later only with an explicit rebuild/reload path.

## Open decision

Merge WorkTypes currently means “move every giver from origin to target.” Confirm this before implementation so the aggressive action and the data model stay aligned.
