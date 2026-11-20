# Ordered Work (Core)

Ordered Work is a RimWorld 1.7 work-management mod: it orders WorkTypes and WorkGivers, supports split/merge editing, and adds per-pawn Work Control, schedule presets, hourly focus and zone policy. It does not delete WorkTypes or WorkGivers.

> Current build: **0.3.0-alpha.316**. Test in a throwaway colony; do not use it in a main save.

## Start here

| I need to... | Read |
|---|---|
| Install the mod | [How to install](docs/how-to/install.md) |
| Use Work Control | [How to use Work Control](docs/how-to/use-work-control.md) |
| Configure work rules and split/merge | [How to configure work rules](docs/how-to/configure-work-rules.md) |
| Configure hourly focus | [How to configure hourly focus](docs/how-to/schedule-focus.md) |
| Build, test or install a dev build | [How to build and test](docs/how-to/develop.md) |
| Prepare a feature or extension | [How to prepare a change](docs/how-to/prepare-change.md) |
| Look up current behavior | [Features and limitations](docs/reference/features.md) · [Work Control](docs/reference/work-control.md) |
| Understand the design | [Design notebook](docs/explanation/design-notebook.md) · [Schedule focus](docs/explanation/schedule-focus.md) |

Full documentation map: [docs/README.md](docs/README.md).

## Quick install

1. Copy this folder to `RimWorld/Mods/OrderedWork`.
2. Enable it after work-related mods.
3. Back up the save before enabling.

## Build and test

```text
pwsh scripts/verify.ps1
```

The script builds, runs the 28 profile/Core tests and checks EN/ES/ES-LatAm key parity. Build paths can be overridden with `RimWorldDir` and `HarmonyDir`; see [verification.md](docs/reference/verification.md).

Install both `OrderedWork.dll` and `OrderedWork.Core.dll` from `Assemblies`.

## Project state

- [STATUS.md](STATUS.md) — current implementation state.
- [CHECKLIST.md](CHECKLIST.md) — verified and pending work.
- [CHANGELOG.md](CHANGELOG.md) — version history.
- [Credits](docs/reference/credits.md) — compatibility references and licensing posture.

Documentation conventions: [UI layout constraints](docs/reference/ui-layout-rules.md) and [Schedule focus reference](docs/reference/schedule-focus.md).
