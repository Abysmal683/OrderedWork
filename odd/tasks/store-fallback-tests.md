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
- L3. BakTest fixture must use WorkType defNames (Hauling, Firefighter, Construction, Mining), not TimeAssignmentDefs; the first attempt used Anything/Sleep and correctly failed validation.
- L4. Recovery repairs the main file from .bak; save never overwrites a valid backup with an invalid main file.