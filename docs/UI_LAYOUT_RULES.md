# Ordered Work UI layout rules

Short rules for RimWorld IMGUI. Read this before changing layout, padding or scrolls.

## Rect is not a CSS box

- `Rect.ContractedBy(n)` returns a smaller rectangle; it does not move drawing.
- Draw inside a contracted rect with `GUI.BeginGroup(inner)` or add `inner.x/inner.y` to every coordinate.
- Background goes on the outer rect; content uses the inner rect.

## Margin vs padding

- **Margin** is outside the background. It separates modules.
- **Padding** is inside the background. It separates the background border from content.
- Standard tokens: `Margin=8`, `Padding=6`, `Gap=6`, `Scrollbar=12`, `Resizer=16`.
- Do not compensate for a missing margin by increasing padding.

## Scroll views

- `Widgets.BeginScrollView` paints its scrollbar inside the viewport. Shrinking `viewRect` alone does not reserve space.
- Use an external track (`WorkGridScrollView`) and reserve its width before computing the content viewport.
- Call `BeginScrollView(viewport, ref scroll, view, false)` when the external track owns the bars.
- Draw the external track after the content; handle its input before the content.
- Never let two scrollbars share the same right edge.

## Nested scrolls

- Outer scrollbar must be external.
- Inner local lists reserve their own scrollbar before drawing rows.
- When an outer scroll appears, content width must shrink before inner lists are laid out.
- Test with the outer scrollbar visible and hidden; layout must not jump.

## Lists and columns

- Draw zebra on every list, column and dropdown row.
- Add hover feedback for clickable rows.
- Long labels use `Widgets.LabelEllipses` plus a full-text tooltip.
- Dropdown rows need a wider default and a scrollbar reserve.

## Window edges

- The window resizer occupies the bottom edge. Do not assume the content bottom equals the window bottom.
- Reserve resizer height when a background must have symmetrical margins.
- Test at 1.0× and 2.0× UI scale.

## Related docs

- `docs/SCHEDULE_FOCUS.md` — behavior, data model, storage and focus-specific UI contract.

## Verification checklist

- Build 0 warnings / 0 errors.
- Core tests pass.
- Test narrow and wide window.
- Test long localized labels.
- Test scrollbar visible and hidden.
- Test nested lists at both scroll extremes.
- Test WASD outside text fields.
