# How to prepare a change

Run this preflight before adding a feature, extending an existing one, or changing persistence. The goal is to reuse the established method instead of inventing a parallel one.

## 1. Find the existing method

Search in this order:

1. `docs/reference/` for the contract and file/field names.
2. CodeGraph or targeted `rg` for the API, call sites and previous consumers.
3. `docs/explanation/design-notebook.md` and `CHANGELOG.md` for rejected paths and prior attempts.
4. The existing store, service or UI component that already owns the concern.

If an existing API or component covers the need, extend it. Do not create a second path.

## 2. Check the write path

For persistence or state changes, verify:

- which file/save owns the data;
- whether a schema or migration already exists;
- whether the write is atomic and backed up;
- whether the same data is already stored somewhere else;
- whether load/read paths normalize the value consistently.

Do not add a parallel file, duplicate field or second save path without a documented reason.

## 3. Check prior usage

Before changing a method signature or semantics:

- find every caller;
- check the existing tests;
- check the changelog for a previous attempt;
- verify whether another module already depends on the current behavior.

If the change would break an existing consumer, either preserve the contract or document the migration.

## 4. Implement only after the preflight

After the checks:

1. State which existing method/component is being reused.
2. If a new one is necessary, state why the old one cannot be extended.
3. Update the affected `docs/reference/` contract.
4. Add or update the relevant `docs/how-to/` procedure.
5. Record a decision in `docs/explanation/` when the choice is not obvious.

## 5. Stop conditions

Stop and resolve before coding when:

- two owners exist for the same data;
- an API is undocumented but already used;
- the write path is not atomic;
- a previous design explicitly rejected this approach;
- the new path would duplicate an existing store, service or UI component.

Related: `docs/reference/policy-data.md`, `docs/reference/save-and-config.md`, `docs/reference/ui-layout-rules.md`.
