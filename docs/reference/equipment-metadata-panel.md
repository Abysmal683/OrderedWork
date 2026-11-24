# Equipment metadata panel — reference

Mounted in the neutral equipment editor below the candidate panels.

## Slots

```text
40 Summary                    | 60 Detail
body group · layer          | selected item
armor S/B/H                 | material / default quality
active set alternatives     | StatsReportUtility
                            | Info card
```

## Current behavior

- Summary uses the first local alternative of each cell as the active item.
- Armor values are read from the item's real default-material `Thing`.
- Material is shown for `MadeFromStuff` items.
- Quality defaults to `Normal` when the item has `CompQuality`.
- Detail uses `StatsReportUtility.DrawStatsReport`.
- `Info card` opens `Dialog_InfoCard`.

## Not yet mounted

- global insulation/equipment/inventory sections;
- shared 30/30 comparison;
- actual pawn material/quality;
- source-item icon overflow.

These belong to the next metadata slice and the pawn Equipment tab.

Related: `../how-to/edit-equipment-sets.md`, `../explanation/clothing-equipment-design.md`.
