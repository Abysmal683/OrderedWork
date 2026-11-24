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

## Common Sense precedent

The mod is **Common Sense** (Steam `1561769193`), not Simple Logic. Its source is at `catgirlfighter/RimWorld_CommonSense`.

Relevant implementation:

- Harmony patch: `Pawn_JobTracker.StartJob`.
- Prefix conditions:
  - colonist;
  - `jobQueue.Count == 0`;
  - not `fromQueue`;
  - not `newJob.playerForced`;
  - valid, reachable, non-forbidden target.
- Creates an opportunistic cleaning job.
- Preserves the original job with:

```csharp
__instance.jobQueue.EnqueueFirst(newJob);
__instance.jobQueue.EnqueueFirst(job);
return false;
```

- `return false` suppresses the original start.
- The cleaning job runs first, then the original job from the queue.
- It only triggers when the queue is empty and the job is not player-forced, avoiding loops.
- It accesses the pawn through a `Pawn_JobTracker` subclass crutch because the field is private.

This is a proven shape for “delay, do not lose” on `StartJob`.

For equipment, the same shape works, but multiple `Wear` jobs may be needed:

```text
queue head: [Wear A, Wear B, original work job]
```

Enqueue the original first, then enqueue wear jobs in reverse order so the first dress job is at the head.

Do not use `TryTakeOrderedJob`; `StartJob` plus an empty queue is more precise and follows the Common Sense precedent.

## Capturing the worktable

At `StartJob`, the work job is known:

- inspect `newJob.targetA.Thing` and `newJob.targetB.Thing`;
- if one is `Building_WorkTable`, center the search there;
- if one is `Building_Bed` for medical work, center on the bed/room;
- if no worktable target, do not use the pawn blindly.

## No worktable cases

### Fishing / hunting

The target is an animal or fishing spot, usually far from the clothing stockpile. Searching around the pawn is likely wrong.

Use a storage anchor instead:

1. nearest valid outfit stand;
2. home area / stockpile center;
3. pawn only as last resort.

### Cleaning

The target is filth. If it is in/near the home area, pawn/room anchor is acceptable. If it is far away, do not force a long trip just to dress; skip or use the home anchor.

### Medical

A medical bed is a building target. Use the bed/room anchor; this is the easy case.

### Work without a fixed bench

Use an explicit search anchor policy:

```text
Worktable/Bed target
  -> outfit stand / stockpile near it
  -> home area/stockpile
  -> pawn
```

Do not invent a `WorkType -> worktable` mapping.

## Recommendation

1. Add a narrow `StartJob` prefix modeled on Common Sense.
2. Only delay autonomous, non-forced work jobs with an empty queue.
3. Preserve the original in the queue before the dress jobs.
4. Center worktable/medical searches on the actual target building.
5. For fishing, hunting and far cleaning, prefer home/stockpile anchor over pawn.
6. If no apparel is found, start the original job unchanged.
7. Keep the equipment master toggle and search-area toggle gating all of it.
