# Nullable migration

## What `Nullable` does

`Nullable` is compile-time analysis, not a runtime null check.

- `enable`: annotates reference types and emits warnings for possible null use.
- `disable`: turns the analysis off for a file.
- `annotations`: enables annotations but suppresses warnings.

It does not make `null` values safe at runtime. It tells the compiler where a `null` may flow so you can add a guard, initialize a field or mark the API contract with `?`.

## Why the main project produced 876 warnings

A dry run with `Nullable=enable` on the existing RimWorld-facing project reported 876 warnings:

| Code | Count | Meaning |
|---|---:|---|
| CS8600 | 278 | Casting a possible null to a non-null type |
| CS8618 | 184 | Non-nullable field left uninitialized |
| CS8625 | 144 | Passing null to a non-nullable parameter |
| CS8603 | 140 | Returning a possible null from a non-nullable method |
| CS8601 | 64 | Assigning a possible null to a non-nullable target |
| CS8604 | 36 | Passing a possible null argument |
| CS8602 | 26 | Dereferencing a possible null |
| CS8605/CS8619 | 4 | Remaining generic/value cases |

Most of these are not new bugs. They are legacy fields, Unity/RimWorld APIs without nullable annotations, and optional values represented by `null` before nullable existed.

## Current state

- `OrderedWork.Core`: `Nullable=enable`, annotated and warning-free.
- `OrderedWork` main: project property is `enable`, but the existing files use `#nullable disable` while they are migrated. New files should omit that directive and be nullable-safe.
- Tests: unchanged.

This is a staged rollout: it stops the 876-warning avalanche while allowing new code and refactored files to opt in immediately. Migrated batches: five Config files, plus all eighteen Services files, including `HiddenWorkTypeService`, `PassionWeightPolicy`, `WorkOrderService.Apply` and `WorkOrderService.Lifecycle`. All Config, Services, Harmony, Game and Startup files are migrated; UI Layout, Shared, Profile and Palette are migrated. 44 files still opt out (UI Dialogs 5, WorkPanel 9, MainTab 7, Mod 21, Localization 1, root 1).

## How to migrate a file

1. Remove `#nullable disable` from the file.
2. Build that project and collect only that file's warnings.
3. Fix in this order:
   - `CS8618`: initialize fields or mark them `?`.
   - `CS8625`: make the parameter nullable or guard before the call.
   - `CS8600`: use `as T?`, `is T`, or a null check.
   - `CS8603`: annotate the return as `T?` or guarantee a value.
   - `CS8601/8602/8604`: add a guard or use `!` only after an established invariant.
4. Rebuild; do not add blanket `!` to silence warnings.
5. Repeat by file, not by project.

## External APIs

For Unity/RimWorld members that are not nullable-annotated, use `MaybeNull`/`NotNull` at the boundary or a local `!` only after a real check. Do not annotate whole third-party interfaces.

## CI gate

`scripts/verify.ps1` runs the build and tests. Once the migration is complete, the project can remove the remaining `#nullable disable` directives and enable nullable warnings as errors.