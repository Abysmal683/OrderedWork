# ProfilesPanel component

Goal: move profile list/save/pending state out of `OrderedWorkMod`.

## Tasks

- [x] Add `ProfilesPanel` with its own initialization and `newProfileName` state.
- [x] Pass active profile, shared status and persist callback explicitly.
- [x] Remove the old profile partial and `newProfileName` from the mod.
- [x] Preserve profile selection, save, pending and reference display.
- [x] Build 0/0 and tests pass.
- [ ] Runtime check of select/save/write-pending.

## Log

- L1. Active profile and status remain shared because other tabs consume them.
- L2. The panel owns only its local name/initialization state.