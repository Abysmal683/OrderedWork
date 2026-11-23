# Localization boundaries

User-facing UI text is keyed. Technical identifiers and support data stay fixed.

## Keyed text

| Source | File |
|---|---|
| General UI | `Languages/<locale>/Keyed/OrderedWork.xml` |
| Schedule focus | `Languages/<locale>/Keyed/OrderedWork.ScheduleFocus.xml` |
| Config mod | `Languages/<locale>/Keyed/OrderedWork.Config.xml` |

English, Spanish and SpanishLatin must stay synchronized. Logs remain English.

## Deliberately not localized

| Item | Reason |
|---|---|
| Section IDs such as `general-core` | Internal lookup keys, never shown |
| File and directory paths | Filesystem identifiers |
| `defName`, package IDs, keys | Stable game/mod identifiers |
| Harmony and debug logs | Support diagnostics read by developers |
| Symbols and formats (`x`, `≡`, `?`, `+`, `N`, `:00`) | Not language copy |
| Internal default names (`new schedule`, `new focus`) | Editable placeholders; not translated labels |

Do not add keys for these unless the item becomes player-visible copy with a real translation need.

## File encoding

All keyed XML files must be UTF-8 and declare `encoding="utf-8"`. A UTF-16 declaration on a UTF-8 file is a validation error.

## Key namespace

- Existing clothing/apparel keys stay under `OrderedWork.Clothing.*`.
- New Equipment-tab and equipment/inventory keys use `OrderedWork.Equipment.*`.
- Do not mass-rename existing keys; use an alias if a rename is unavoidable.

Keys are semantic identifiers, not display strings. Never use the English sentence as the key.

## Formatting

- Use complete phrases with placeholders such as `{0}` instead of concatenating translated fragments.
- Do not put punctuation, labels or sentence order in code when a translator may need to change it.
- Reuse `OrderedWorkText.T(key, fallback)` for the English fallback.

## Verification

`scripts/verify-localization.ps1` checks:

- missing and orphaned keys against English;
- duplicate and empty keys;
- XML encoding declarations against the actual bytes;
- code keys that are absent from English;
- possible hardcoded UI strings as warnings.

Run it directly or as part of `scripts/verify.ps1`.
