# Build verification

## `scripts/verify.ps1`

Run from the repository root:

```powershell
pwsh scripts/verify.ps1
```

It performs:

1. `dotnet build Source/OrderedWork.csproj -c Release`
2. `dotnet run --project Tests/OrderedWork.Tests/OrderedWork.Tests.csproj -c Release`
3. EN/ES/ES-LatAm key parity and duplicate-key checks for `OrderedWork.Config.xml`

It does **not** install the mod and it is **not** a CI workflow.

## Build path overrides

The project has local defaults, but no hardcoded path is required for another machine:

```powershell
pwsh scripts/verify.ps1 `
  -RimWorldDir "D:\path\to\RimWorld" `
  -HarmonyDir "D:\path\to\Harmony\Assemblies"
```

Equivalent MSBuild properties are `RimWorldDir` and `HarmonyDir`.

## Nullable and analyzers

| Project | Nullable | Analyzers |
|---|---|---|
| `OrderedWork.Core` | enabled | enabled; warning-free |
| `OrderedWork` main | enabled; Config/Services/Harmony/Game/Startup migrated; 80 legacy files opt out | not enabled |
| Tests | disabled | not enabled |

Nullable in the main RimWorld-facing project remains a later incremental task because it needs a separate warning pass over UI/Def code.

## CI

No host workflow is configured yet. The local verification command is the current gate; a CI job should call the same script once a host is chosen.