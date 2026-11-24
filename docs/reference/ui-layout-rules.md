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
- `UiDragList` uses the shared external `WorkGridScrollView` track, with one owner per list.
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

## Migration status

Migrated to `UiScrollPane`: local preset dialogs, pawn picker, WorkType dropdown, WorkPanel Skills/Stats/Unable and Details Overview. Schedule and Zone use external vertical and horizontal tracks when their content is compressed.

Deliberately native/custom: the settings global scroll, SplitMerge column bars, Details Overrides/Clothing, the focus horizontal strip and the WorkType combo's thin bar. These either own a different input model or never nest inside another local scroll.
## Mandatory layout rule

Use the shared layout helpers before writing raw rectangles:

- `UiRows` — vertical sequence;
- `UiFlex` — weighted columns;
- `UiInset` — margins/padding;
- `UiScrollPane` — scrollable content;
- `UiTable` — fixed columns;
- `UiButtonBar` / `UiFieldRow` / `UiTabBar` — controls;
- `UiZebra` / `UiColors` — rows and visual state.

Raw `Rect` coordinates are reserved for the window root, fixed anchors, and components that genuinely own a custom input model. Do not hardcode a Y coordinate for a section that follows variable-height content.

## Relative-position vocabulary

Describe layouts by relationships, not pixels.

Use:

- **Screen / panel**
- **Top bar, left pane, center pane, right pane, footer**
- **Above / below / left of / right of**
- **Same row as**
- **Before / after**
- **Aligned to the left / right edge**
- **Full width**
- **Takes the remaining space**
- **Fixed width / flexible width**
- **Gap**
- **Scrolls with the page / has its own scroll**
- **Default / hover / selected / disabled / confirming**

### Template to dictate a layout

```text
Screen:
Panel:
Vertical order:
  1. ...
  2. ...
  3. ...
Rows:
  - row: [left] + [center] + [right]
  - widths: [fixed 80] + [flex 1] + [flex 2]
Anchors:
  - ...
Scrolling:
  - ...
States:
  - ...
```

### Example

```text
Screen: Mod Settings -> Clothing
Vertical order:
  1. WorkType + preset selectors
  2. slot grid
  3. selected rule editor
  4. footer actions
Rows:
  - selector row: flex 1 + flex 1 + fixed 80 + fixed 80
  - footer: flex 1 + fixed 160
Scrolling: the rule editor has its own scroll if needed.
```

### Screenshot instructions

If you send a screenshot, use letters/numbers:

```text
A = top bar
B = left grid
C = right rule list
D = footer
```

Then say:

```text
Mové B debajo de A.
C a la derecha de B, 1:1.
D abajo de todo, ancho completo.
```

That is enough to reconstruct the layout without pixel coordinates.
## Gadgets

Reusable card, grid and search gadgets live in `ui-gadgets.md`. Add new repeated visual behavior there before copying it into another screen.
