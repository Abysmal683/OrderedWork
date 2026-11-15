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
2. **View modes**: discard “Simple”; use Draw / Manual / Both (clarify “3”).
   - Default at new game: likely Both (Manual/Draw available).
   - Button in Work Control to open the vanilla Work tab.
3. **Pawn selection**: selected row border; double-click selects pawn and jumps camera; search box above matrix by pawn name (not the jobs search).
4. **Result table**: discarded; the manual Work tab is better.
5. **Vanilla Work tab interactive**:
   - Remove the 4-priority cap so manual changes can reach 1..N.
   - `Pawn_WorkSettings.LowestPriority` is const; `WidgetsWork.DrawWorkBoxFor`/`PawnColumnWorker_WorkPriority` cycle/colors assume 0–4.
   - Requires clean-room Harmony transpilers/prefixes; technical spike; test with manual Work tab and hook.
6. **Passion icons**: use `WidgetsWork.PassionWorkboxMinorIcon`/`MajorIcon` instead of `*`/`**`.

## Open questions

1. Confirm modes: Draw / Manual / Both? Default Both or Draw at new game?
2. “Remove lowpriority”: allow manual 1..N in the vanilla Work tab, or only make our Work Control interactive?
3. Search box filters rows or only highlights matches?
4. Double-click selects and jumps camera, or only selects?
5. Priority max for manual Work tab: dynamic N, fixed 9/99, or configurable?

## Next implementation order

1. Margins + selection border + search.
2. View mode + button to Work tab.
3. Vanilla Work tab low-priority patch spike.
4. Passion icons.
5. Re-test both tabs, manual >4, hook, resurrection, save/load.
