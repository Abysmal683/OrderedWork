# How to configure work rules

## Use

1. Open `Options -> Mod Settings -> Ordered Work`.
2. Pick the **Rules** tab. Reorder WorkTypes by dragging the vertical rows (the `≡` handle).
3. Split mode: pick the origin WorkType, enter a name, the optional `Action word` and an optional description. The `defName` preview appears below the fields. Drag origin Givers into `Split destination` and click `Add split`.
4. Split rows show `Edit` and two-step `Delete`. `Delete` returns the split's Givers to its `Base`; it is blocked when `Base` is missing.
5. Merge mode: pick the origin and a different target. The destination shows the target's existing Givers fixed at the top. Drag origin Givers in, reorder them, and click `Merge selected`; `Merge all` moves every origin Giver.
6. Merge candidates are listed below the editor. Select a row to load it, or use `Revert` with confirmation to return its Givers to the reference layout.
7. Order mode: pick a WorkType and drag its Giver list. `Apply giver order` saves the sequence; `Cancel giver order` restores the editor draft.
8. Order candidates are listed below the editor. Select a row to review it, or use `Revert` with confirmation to restore the reference order.
9. Candidate lists and Order columns show 10 rows at a time. Use the scrollbar or `Ctrl+wheel` over that area; a plain wheel scrolls the settings page.
10. The **General** tab holds `Auto recommend new colony`, `Auto-hide scrollbars`, `Palette stress test (debug)`, the dense-rank spike, and `Reset config mod all` (two-step confirmation; pawn priorities are untouched).
11. Restart RimWorld as a safe boundary. Saved actions also apply through the startup pipeline immediately where possible.