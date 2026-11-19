# WorkControl rows/grid/interaction extraction

Goal: separate matrix layout, row rendering and drag/selection state.

## Tasks

- [x] `WorkControlMetrics` owns row/card constants and slot math.
- [x] `WorkControlRows` owns pawn column, lane cards, tooltips and held card.
- [x] `WorkControlGrid` owns viewport, external bars, row loop and scroll.
- [x] `WorkControlInteraction` owns palette/card drag, drop and add menu.
- [x] `MainTabWindow_WorkControl` reduced to orchestration and top controls.
- [x] Build 0/0 and tests pass.
- [ ] Runtime check of selection, palette drag and card reorder.

## Log

- L1. No behavior or public UI contract intentionally changed.
- L2. Old row/interaction partials removed.