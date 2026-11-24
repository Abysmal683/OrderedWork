# Equipment metadata panel — reference

Mounted in the neutral equipment editor and the pawn Equipment tab.

## Slots

```text
40 Summary                    | 60 Detail / Compare
body group · layer          | selected item
armor S/B/H                 | material / quality
global insulation rows      | StatsReportUtility
active set alternatives     | Info card
```

## Behavior

- Summary uses the first local alternative of each cell as the active item.
- Armor and insulation are read from the item Thing.
- In the pawn tab, the matching worn item is used when available, so material and quality are real.
- In the neutral editor, default material and `Normal` quality are used.
- Detail uses `StatsReportUtility.DrawStatsReport`.
- `Compare` splits the 60 pane into two item panes; `Pick` chooses the second.
- `Info card` opens `Dialog_InfoCard` per pane.
- Static game-object caches are forbidden; the panel builds Things on demand.
- Draw failures are caught with `Log.ErrorOnce` and a fallback label.

## Pending

- equipment/inventory summary sections;
- source-item icon overflow.

Related: `../how-to/use-pawn-equipment-tab.md`, `../explanation/clothing-equipment-design.md`.
