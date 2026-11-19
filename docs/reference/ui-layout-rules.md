# UI layout constraints

Facts and invariants for Ordered Work IMGUI layout.

## Rect semantics

- `Rect.ContractedBy(n)` returns a smaller rectangle; it does not move drawing.
- Drawing inside a contracted rectangle requires `GUI.BeginGroup(inner)` or explicit `inner.x`/`inner.y` offsets.
- Backgrounds use the outer rectangle. Content uses the inner rectangle.

## Metrics

| Token | Value |
|---|---:|
| Margin | 8 |
| Padding | 6 |
| Gap | 6 |
| Scrollbar | 12 |
| Resizer | 16 |

Margin is outside the background. Padding is inside it.

## Scroll views

- `Widgets.BeginScrollView` paints its scrollbar inside the viewport.
- Reserving width in `viewRect` alone does not reserve space for that bar.
- External tracks use `WorkGridScrollView` and `Widgets.BeginScrollView(viewport, ref scroll, view, false)`.
- The track is reserved before the content viewport is computed.
- Track input is handled before content drawing; the track is drawn after content.

## Nested scrolling

- The outer scrollbar is external.
- Inner lists reserve their own scrollbar before drawing rows.
- When an outer bar appears, content width shrinks before inner lists are laid out.
- Two scrollbars must not share the same right edge.

## Lists and dropdowns

- Every list, column and dropdown row uses zebra.
- Clickable rows use hover feedback.
- Long labels use `Widgets.LabelEllipses` with a full-text tooltip.
- Dropdowns reserve scrollbar width and use a wider default.

## Window edges

- The resizer occupies the bottom edge.
- Background symmetry requires reserving resizer height.
- Verify at 1.0× and 2.0× UI scale.

## Related documents

- `docs/explanation/schedule-focus.md` — why the focus overlay works the way it does.
- `docs/how-to/schedule-focus.md` — user workflow.
- `docs/reference/schedule-focus.md` — fields, files and storage.
