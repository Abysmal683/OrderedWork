# Config mod localization

Branch: `feat/schedule-focus`
Goal: remove user-facing hardcoded strings from the Config mod and move them to keyed translations.

## Rules

- Keep behavior unchanged.
- Use `ConfigText(key, fallback)` for static strings.
- Use `string.Format(ConfigText(key, fallback), args)` for dynamic strings.
- Do not localize section IDs, file paths, defNames, log-only text or package IDs.
- Keep English, Spanish and SpanishLatin in sync.
- Build and test after each file group.

## Tasks

- [x] T1. Add `OrderedWork.Config.xml` for English/Spanish/SpanishLatin.
- [x] T2. General tab.
- [x] T3. Rules tab and tab strip.
- [x] T4. Profiles panel.
- [x] T5. Order editor.
- [x] T6. Giver order and helper tooltips.
- [x] T7. Split metadata.
- [x] T8. Split/Merge main panel.
- [x] T9. Split/Merge columns.
- [x] T10. Split/Merge drag warnings.
- [x] T11. Split/Merge persistence messages.
- [x] T12. Work-tab overlay strings and reset message.
- [x] T13. Verify no user-facing hardcoded strings remain.
- [x] T14. Build, tests, install and docs/changelog.

## Log

- L1. User request: first extract Config mod strings, keep checklist, then continue with refactor.
