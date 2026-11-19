# Log categories

Goal: replace one global verbose switch with subsystem-specific diagnostics.

## Tasks

- [x] Add `OrderedWorkLogCategory` flags.
- [x] Migrate legacy `verboseLogging` to all categories on load.
- [x] Add category checkboxes in Diagnostics.
- [x] Tag Work Control, Schedule, Passion and Config diagnostics.
- [x] Keep technical warnings/errors unchanged.
- [x] Document the categories.
- [ ] Runtime check with one category enabled at a time.

## Log

- L1. Categories: WorkControl, Schedule, Stores, Passion, Config, Harmony.
- L2. Legacy verbose=true maps to All when no category has been chosen.