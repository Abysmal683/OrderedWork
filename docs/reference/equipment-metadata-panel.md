# Equipment metadata panel — reference

Mounted in the neutral equipment editor below the candidate panels.

## Slots

```text
40 Summary                    | 60 Detail / Compare
body group · layer          | selected item
armor S/B/H                 | material / default quality
global insulation rows      | StatsReportUtility
active set alternatives     | Info card
```

## Current behavior

- Summary uses the first local alternative of each cell as the active item.
- Armor values are read from the item's real default-material `Thing`.
- Global insulation rows are per item.
- Material is shown for `MadeFromStuff` items.
- Quality defaults to `Normal` when the item has `CompQuality`.
- Detail uses `StatsReportUtility.DrawStatsReport`.
- `Compare` splits the 60 pane into two item panes.
- `Pick` chooses the comparison item from the active catalog.
- `Info card` opens `Dialog_InfoCard` per pane.

## Not yet mounted

- equipment/inventory summary sections;
- actual pawn material/quality;
- source-item icon overflow.

These belong to the pawn Equipment tab and the next metadata slice.

Related: `../how-to/edit-equipment-sets.md`, `../explanation/clothing-equipment-design.md`.
