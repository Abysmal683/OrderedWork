# Pre-work equipment analysis

## Goal

Delay a pawn's autonomous work just enough to dress first, without losing the chosen work job.

## Current behavior

- `Equip now` is manual.
- No pre-work trigger exists.
- Search area uses the current worktable target if present; otherwise the pawn.
- Automatic pre-work dressing cannot yet know a worktable before the work job exists.

## Hook options

### A. Broad `Pawn_JobTracker` hook

Hook `TryTakeOrderedJob` or `StartJob`.

Pros:

- sees the exact job that would start.

Cons:

- affects all jobs, not only work;
- recursion risk when queueing dress jobs;
- hard cancellation and re-entry;
- target/reservation invalidation while delayed;
- easy to create job loops.

High risk. Do not use as the first solution.

### B. ThinkNode / JobGiver before work

Insert a narrow pre-work node before the work giver.

Pros:

- the work job is not created yet, so it cannot be lost;
- after dressing, the normal think tree regenerates work;
- no direct job cancellation.

Cons:

- need to know the WorkType before a work job exists;
- a custom multi-step dress job is required;
- think-tree priority must not starve work.

This is the safest direction.

### C. Intercept the returned work job

Postfix the WorkGiver job result, or narrowly inspect the work job before it starts.

Pros:

- WorkType and worktable target are known;
- the search area can center on the actual worktable.

Cons:

- returning a different job requires preserving or regenerating the original;
- the original job may hold target assumptions;
- needs a one-shot marker to avoid requeueing forever.

Use only after A/B are evaluated and with a narrow work-job filter.

## Worktable anchor

If the work job is already assigned, its `targetA`/`targetB` may expose a `Building_WorkTable`.

Search around that cell, not the pawn. If no worktable is assigned yet:

- search around the pawn, or
- use an explicit configurable anchor per WorkType.

Vanilla has no reliable `WorkType -> worktable` mapping. Do not invent one.

## External mod check

No installed/workshop mod named exactly `Simple Logic` was found in the local workshop list. `Simple Sidearms` and `While You're Up / PUAH` exist, but neither is a worktable pre-work scheduler.

Need the exact Steam Workshop link/package ID before claiming its implementation.

## Recommendation

1. Keep `Equip now` manual.
2. Add a narrow ThinkNode/JobGiver for automatic pre-work dressing.
3. Delay work by returning a dress job; let the work job be regenerated.
4. If a worktable target is already known, center the search there.
5. Do not use a broad JobTracker prefix as the first implementation.
