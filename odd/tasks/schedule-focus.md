# Schedule Focus — per-hour work priority overlay

Branch: `feat/schedule-focus`
Test runner: `dotnet run --project Tests/OrderedWork.Tests/OrderedWork.Tests.csproj -c Release`
Delivery: single PR slice on the feature branch; ask before push/PR.

## Specs

- S1. The pawn's current work order (`PawnWorkOrder.order`) is the absolute source of truth. The hourly focus must never rewrite, remove, or reorder that stored list.
- S2. Hourly focus is a sparse per-pawn overlay: only changed hours are stored. Each hour stores at most 3 ordered WorkType defNames. The legacy single-WorkType `scheduleWorkOverrides` entries must migrate into the new list shape.
- S3. The effective order is: valid focus WorkTypes in focus order, then the base order without duplicates. A focus entry is valid only when it exists in the pawn's base order and the pawn is capable of it. If the first focus is unavailable, the next valid focus is used; if none are valid, the base order's first entry remains first.
- S4. Runtime applies the focus at the next hour boundary, never mid-hour and never per tick. It must not call `StopJob`, force work, or interrupt sleep, food, or recreation. It only changes priorities. The base order is restored when the hour leaves focus.
- S5. The Schedule tab exposes an inline compact editor: 3 assignable rows, each with `Cambiar`, direct passion/level metadata, drag reorder, right-click removal (returns the WorkType to its base position), and `+ Agregar` while below 3. Below it, the visible effective order is horizontal: first 5 entries and scroll for the rest.
- S6. `Copiar hora` copies only the 3 assignable focus entries of the selected hour. `Pegar hora` replaces the target hour's focus and validates against the target pawn: incapable or absent from the base list entries are omitted. If all entries are invalid, the paste is cancelled, the previous focus is preserved, and the player is notified. Partial omission applies the valid entries and notifies the player.
- S7. A hard focus preset is a separate JSON file store containing 24 hours, each with at most 3 ordered WorkType defNames. It contains no pawn, map, colony, incapacity, or vanilla timetable data. It uses atomic writes and its own schema. Schedule presets and focus presets are independent; either component works without the other.
- S8. New UI strings live in a separate keyed `OrderedWork.ScheduleFocus.xml` file for English, Spanish, and SpanishLatin. Technical log output stays English.
- S9. Build must be 0/0 and the test runner must pass. Add pure tests for normalization, effective-order fallback, max 3, and legacy migration where testable.

## Tasks

- [x] T1. Add pure focus stack policy and tests. (S2-S3)
- [x] T2. Add hour-focus data model, legacy migration, and normalization. (S1-S2)
- [x] T3. Add separate focus preset model/store with atomic JSON. (S7)
- [x] T4. Add runtime hour-boundary service and quiet apply path. (S3-S4)
- [x] T5. Add inline focus row/editor, visible strip, copy/paste-hour messages. (S5-S6)
- [x] T6. Add focus preset save/load dialog. (S7)
- [x] T7. Add separate localization key files. (S8)
- [x] T8. Build, tests, version/docs/install. (S9)

## Log

- L1. User request: implement schedule focus overlay with 3 assignable hourly WorkTypes, horizontal visible effective order (5 + scroll), right-click removal, direct passion/level metadata, prev/next hour, temporary copy/paste hour, separate hard 24x3 focus presets, independent schedule/focus components, atomic files, tests/logs/localization.

- L2. First runtime smoke test found a stale installed OrderedWork.Core.dll; fixed the install. UI follow-up: compact schedule height, separated sections, current-hour highlight, numeric focus cells, copy-empty guard, focus messages, vertical Work tab, and search focus release.
