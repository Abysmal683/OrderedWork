[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$RimWorldDir = $env:ORDEREDWORK_RIMWORLD_DIR,
    [string]$HarmonyDir = $env:ORDEREDWORK_HARMONY_DIR
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$props = @()
if (-not [string]::IsNullOrWhiteSpace($RimWorldDir)) {
    $props += "-p:RimWorldDir=$RimWorldDir"
}
if (-not [string]::IsNullOrWhiteSpace($HarmonyDir)) {
    $props += "-p:HarmonyDir=$HarmonyDir"
}

Write-Host "Building OrderedWork ($Configuration)..."
dotnet build "Source\OrderedWork.csproj" -c $Configuration @props
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Running tests..."
dotnet run --project "Tests\OrderedWork.Tests\OrderedWork.Tests.csproj" -c $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$files = @(
    "Languages\English\Keyed\OrderedWork.Config.xml",
    "Languages\Spanish\Keyed\OrderedWork.Config.xml",
    "Languages\SpanishLatin\Keyed\OrderedWork.Config.xml"
)
$maps = @{}
foreach ($file in $files) {
    [xml]$xml = Get-Content -LiteralPath $file -Raw
    $ids = @($xml.LanguageData.ChildNodes | Where-Object { $_.NodeType -eq "Element" } | ForEach-Object { $_.Name })
    $maps[$file] = $ids
    $duplicates = $ids.Count - ($ids | Select-Object -Unique).Count
    if ($duplicates -gt 0) {
        Write-Error "Duplicate language keys in ${file}: $duplicates"
        exit 1
    }
}
$english = $maps[$files[0]]
foreach ($file in $files[1..2]) {
    $missing = @($english | Where-Object { $maps[$file] -notcontains $_ })
    if ($missing.Count -gt 0) {
        Write-Error "Missing keys in ${file}: $($missing -join ', ')"
        exit 1
    }
}
Write-Host "Verification passed."