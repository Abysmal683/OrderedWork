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
