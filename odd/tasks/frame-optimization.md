# Frame optimization

Goal: remove per-frame profile IO and lane rebuilding.

## Applied

- [x] Cached active profile and selected profile name with invalidation.
- [x] WorkGiverOrderService uses the cached active profile.
- [x] WorkControlRows lane cache keyed by PawnWorkOrder.orderVersion, refreshed at most every 60 ticks for capability changes.
- [x] SetOrder increments the runtime order revision.
- [x] Documented frame vs tick boundaries.

## Next

- [ ] Work Control filter snapshot.
- [ ] Details Overview snapshot.
- [ ] Optional UI refresh interval.
- [ ] Stopwatch logging under the WorkControl category.
- [ ] Runtime frame-time check with a large colony.