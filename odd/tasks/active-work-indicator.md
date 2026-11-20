# Active work indicator

Goal: show which WorkType a pawn is doing without adding a new subsystem.

## Tasks

- [x] Add `ActiveWorkService` with a 5-tick per-pawn cache.
- [x] WorkPanel active row gets a left accent and border.
- [x] WorkControl lane card gets a border and status dot.
- [x] WorkControl tooltip reports "Active now".
- [x] No indicator in the global palette (multiple pawns).
- [x] Build 0/0 and tests pass.
- [ ] Runtime check with sleep, food, joy and work jobs.

## Notes

`pawn.CurJob.workGiverDef` is null for sleep, food, joy and some forced jobs; those correctly show no active WorkType.