# WorkConfigStore split

Goal: separate path layout, JSON IO and profile resolution while preserving the public facade.

## Tasks

- [x] Add `WorkConfigPaths`.
- [x] Add `WorkConfigSerializer`.
- [x] Add `WorkConfigProfileService`.
- [x] Add `WorkConfigResolver`.
- [x] Reduce `WorkConfigStore` to a compatibility facade.
- [x] Preserve profile schema, backup policy and active/pending fallback.
- [x] Build 0/0 and tests pass.
- [ ] Runtime profile save/load and pending promotion check.

## Log

- L1. Paths own the filesystem layout.
- L2. Serializer owns JSON, temp write, replace, backups and reference initialization.
- L3. Resolver owns active/pending/editing selection and selected-profile file.
- L4. Profile service owns visible ordering, normalization and vanilla reference profile construction.