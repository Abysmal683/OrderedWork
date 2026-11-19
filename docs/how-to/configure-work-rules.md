# How to configure work rules

## Use
1. Open `Options -> Mod Settings -> Ordered Work`.
2. Pick the **Rules** tab. Reorder work types by dragging the vertical rows (the `≡` handle).
3. Split mode: pick the origin work type, type a new name (the `defName` preview appears below it), drag origin givers into `Split destination`, and click `Add split`. Origin rows already in the draft are drawn gray in their original position; dragging a destination row back to the origin removes it.
4. Merge mode: pick the origin and a different target (the origin is excluded from the target list). The destination shows the target's existing givers fixed at the top; drag origin givers in, reorder them, and click `Merge selected`. `Merge all` moves every origin giver. Each row shows its final position.
5. Order mode: pick a work type and drag its giver list to the desired order. `Reset order` removes the saved override; the original def order returns after a restart.
6. `Cancel draft` clears only the unsaved draft. Every saved row has its own reset (`Reset split`, `Reset merge`, `Reset order`), each with two-step confirmation.
7. The **General** tab holds `Auto recommend new colony`, `Auto-hide scrollbars`, `Palette stress test (debug)`, the dense-rank spike, and `Reset config mod all` (two-step confirmation; pawn 1..N priorities are untouched).
8. Restart RimWorld. Changes are applied at load time.
