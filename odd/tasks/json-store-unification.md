# JSON store unification

Goal: one atomic JSON record implementation for all preset stores.

## Tasks

- [x] Add `AtomicJsonRecordStore<T>` in `OrderedWork.Core`.
- [x] Migrate `SchedulePresetStore` to the shared store.
- [x] Migrate `WorkFocusPresetStore` to the shared store.
- [x] Migrate `WorkPresetStore` to the shared store.
- [x] Preserve per-store schema, normalization, id policy and sort order.
- [x] Add pure tests for roundtrip, backup and schema rejection.
- [x] Build 0/0 and tests pass.
- [x] Install and document.

## Log

- L1. Shared store owns `.tmp`, `File.Replace`, `.bak`, schema validation and load/save/delete.
- L2. Each store keeps its own directory, schema bounds, normalization, filename policy and sort order.