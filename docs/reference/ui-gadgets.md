# UI gadgets — reference

These gadgets are shared by the equipment editor and future Equipment-tab views. Use them instead of ad-hoc rectangles, colors or card loops.

## UiCard

```csharp
bool clicked = UiCard.Draw(
    rect,
    label,
    subLabel: null,
    selected: false,
    icon: null,
    tooltip: null);
```

- Draws the shared card background/border.
- Handles selected state.
- Places an optional icon before the text.
- Uses `Widgets.LabelEllipses`.
- Returns the left-click result.
- Use for apparel cells, candidate rows and set rows.

## UiGrid

```csharp
int columns = UiGrid.Columns(area, 54f, 6f);
Rect cell = UiGrid.Cell(area, index, 54f, 6f);
float height = UiGrid.Height(area, count, 54f, 6f);
```

- Keeps cells square.
- Computes columns/rows from the available area.
- Use for the apparel cell matrix.

## UiSearchField

```csharp
search = UiSearchField.Draw(rect, search, tooltip);
```

- Shared text field with optional tooltip.
- Use for candidate/search panels.

## Layout rules

- Build rects with `UiRows`, `UiFlex`, `UiTable`, `UiInset`, `UiGrid`.
- Use `UiButtonBar` for actions.
- Use `UiTabBar` for tabs.
- Use `UiScrollPane` for scrollable content.
- Use `UiZebra` for long rows.
- Use `UiColors`; do not add literal colors in screens.
- New repeated visual behavior must become a documented gadget instead of being copied.

Related: `ui-layout-rules.md`.
