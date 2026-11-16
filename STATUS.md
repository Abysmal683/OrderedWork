# Ordered Work — Current Status (after alpha.4 test)

Version: `0.3.0-alpha.4`, installed. Build 0/0, 7/7 tests. Git `c15f8ef`, tag `v0.3.0-alpha.4`.

## Confirmed by user in game

- Work Control tab loads; basic lane visible; read/write works.
- Both scrolls (shared horizontal lane + vertical rows) are acceptable.
- Cards are more compact; picker works.
- Recommendations and order apply correctly.
- `R` per-row recommendation works.
- GameComponent works; save/load works.
- Vanilla Work-tab capture hook works (changes flow).
- Respawn/resurrection reapplies stored order.
- Recruited colonist gets a recommendation.
- Score works: level with passion outranks other cases.

## Implemented in alpha.4 (now confirmed)

- Two-line cards; reduced height.
- Incapable WorkTypes filtered from lane/default/picker.
- Pins: Patient, PatientBedRest, Firefighter.
- Score: no-skill 0; level 0 none/minor/major -3/1/3; level>=1 level*2 + 2/4.
- Init at GameComponentTick; existing copy, new recommend.
- Hook capture with IsApplying/IsSpawning/IsInitializing + 120-tick spawn guard.
- R button; result table removed.

## Requested changes (not implemented)

1. **Margins**: reduce internal content margin to 2–4 px per side; keep compact.
2. **View modes**: discard “Simple”; use Draw / Manual / Both.
   - Default at new game: **Both** (Manual/Draw available).
   - Button in Work Control to open the vanilla Work tab.
3. **Pawn selection**: selected row border; double-click selects pawn and jumps camera; search box above matrix by pawn name (not the jobs search).
4. **Result table**: discarded; the manual Work tab is better.
5. **Vanilla Work tab interactive**:
   - Remove the 4-priority cap so manual changes can reach 1..N.
   - `Pawn_WorkSettings.LowestPriority` is const; `WidgetsWork.DrawWorkBoxFor`/`PawnColumnWorker_WorkPriority` cycle/colors assume 0–4.
   - Requires clean-room Harmony transpilers/prefixes; technical spike; test with manual Work tab and hook.
6. **Passion icons**: use `WidgetsWork.PassionWorkboxMinorIcon`/`MajorIcon` instead of `*`/`**`.

## Clarifications (saved)

1. **Modes**: proceed with the three: **Draw / Manual / Both**. Default on new game: **Both** (Draw + Manual available).
2. **Low priority**: the patch must cover the **vanilla Work tab** too, allowing manual 1..N there. Dynamic maximum if technically safe; fallback to fixed 99 only if dynamic causes serious problems.
3. **Search + row marking**: the search box highlights matching pawn rows only; it must **not select** them.
4. **Double click**: selects the pawn and **moves the camera** to the pawn.
5. **Result table**: discarded.
6. **Passion icons**: use the vanilla `WidgetsWork` minor/major icons.

## Next implementation order

1. Margins + selection border + highlight-only search.
2. Modes + button to Work tab.
3. Vanilla Work tab low-priority patch spike (dynamic max, fallback 99).
4. Passion icons.
5. Re-test both tabs, manual >4, hook, resurrection, save/load.

## Alpha.7/8 audit (current)

- alpha.7: vanilla Work-tab priority cap removed; dynamic max with 99 fallback; >4 cell/tooltip; capture hook. Build/tests pass; installed; tag `v0.3.0-alpha.7`.
- alpha.8: top `R`/`Details` based on selected row; read-only Details window. Build/tests pass; installed; tag `v0.3.0-alpha.8`.
- Resurrection log: stored order was reapplied (`Hunting=3`); a later `Hunting=4` came from an explicit `GenerateAndApplyRecommendation` (R), not from resurrection.
- No `[Ordered Work]` exceptions in the latest log.

## Open questions for next design

1. Right-click priority: current semantics are left=increment, right=decrement; from 0 right does nothing. What should right-click do: jump to max, open a priority menu, or something else?
2. Check view: should “Check” be a redirect to the vanilla Work tab (no duplicate logic) with a clean return button, or an embedded copy of the check grid inside Work Control?
3. Drag palette: where does it live, what does it list, does the drag card show passion/level of the pawn under the cursor, how should the invalid “Incapaz” case render?
4. Click behavior: does double-click close/minimize Work Control after moving the camera? Shift multi-select: what do Recommend / Mode / Details do with multiple rows (Details only first)?
5. Portrait: add the vanilla pawn portrait in the fixed name column; size/placement?


