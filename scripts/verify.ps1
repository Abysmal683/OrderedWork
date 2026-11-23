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

Write-Host "Verifying localization..."
& (Join-Path $PSScriptRoot "verify-localization.ps1") -Root $root
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Verification passed."