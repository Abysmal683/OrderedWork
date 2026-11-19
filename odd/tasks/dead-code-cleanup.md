# Dead code and legacy cleanup

Goal: remove safe dead keys/helpers without breaking save migration.

## Tasks

- [x] Remove unused keys except `OrderedWork.Clothing.Worn` (reserved for the clothing module).
- [x] Remove unused `ScheduleApplyController.Save`.
- [x] Keep `scheduleWorkOverrides` and `verboseLogging` until old-save migration is verified.
- [x] Keep `WorkConfigStore` as a compatibility facade.
- [ ] Remove legacy fields after the old-save migration test.
- [ ] Re-audit helpers after the Config panel split.

## Notes

`Clothing.Worn` is intentionally retained because clothing is the next planned module.