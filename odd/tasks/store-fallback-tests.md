# Store fallback and migration tests

Goal: protect preset data and profile migration before further refactors.

## Tasks

- [x] Add `.bak` recovery to `AtomicJsonRecordStore`.
- [x] Test corrupt main file falling back to `.bak`.
- [x] Test roundtrip and backup creation.
- [x] Test unsupported schema rejection.
- [ ] Runtime check of profile pending/promotion.
- [ ] Runtime check of legacy focus migration on an old save.

## Notes

Scribe-based `PawnWorkOrder` migration cannot run in the Core test project because it depends on RimWorld types. Its remaining verification is a runtime old-save test.