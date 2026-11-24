[CmdletBinding()]
param(
    [string]$Root = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"
$errors = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()

function Add-Error([string]$message) {
    $errors.Add($message)
}

function Add-Warning([string]$message) {
    $warnings.Add($message)
}

function Read-KeyedLanguage([string]$languageDir) {
    $keyedDir = Join-Path $languageDir "Keyed"
    if (-not (Test-Path -LiteralPath $keyedDir)) {
        Add-Error "Missing Keyed directory: $keyedDir"
        return @{}
    }

    $map = @{}
    $files = Get-ChildItem -LiteralPath $keyedDir -File -Filter "*.xml"
    foreach ($file in $files) {
        $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
        $text = [System.IO.File]::ReadAllText($file.FullName)
        $head = $text.Substring(0, [Math]::Min(200, $text.Length))
        $hasUtf16Bom = ($bytes.Length -ge 2) -and (($bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE) -or ($bytes[0] -eq 0xFE -and $bytes[1] -eq 0xFF))
        $hasUtf8Bom = ($bytes.Length -ge 3) -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
        $encodingMatch = [regex]::Match($head, 'encoding\s*=\s*["'']([^"'']+)["'']')
        if ($encodingMatch.Success) {
            $declared = $encodingMatch.Groups[1].Value.Trim().ToLowerInvariant()
            if ($declared -match "utf-16" -and -not $hasUtf16Bom) {
                Add-Error "Encoding declaration is utf-16 but file is not UTF-16: $($file.FullName)"
            }
            if ($declared -match "utf-8" -and $hasUtf16Bom) {
                Add-Error "Encoding declaration is utf-8 but file has a UTF-16 BOM: $($file.FullName)"
            }
        }

        try {
            [xml]$xml = $text
        }
        catch {
            Add-Error "Invalid XML in $($file.FullName): $($_.Exception.Message)"
            continue
        }

        if ($null -eq $xml.LanguageData) {
            Add-Error "Missing LanguageData root in $($file.FullName)"
            continue
        }

        foreach ($node in $xml.LanguageData.ChildNodes) {
            if ($node.NodeType -ne "Element") { continue }
            $key = $node.Name
            $value = $node.InnerText
            if ([string]::IsNullOrWhiteSpace($value)) {
                Add-Error "Empty translation for '$key' in $($file.FullName)"
            }
            if ($map.ContainsKey($key)) {
                Add-Error "Duplicate key '$key' in $($file.FullName)"
                continue
            }
            $map[$key] = $value
        }
    }

    return $map
}

$languageRoot = Join-Path $Root "Languages"
if (-not (Test-Path -LiteralPath $languageRoot)) {
    Add-Error "Missing Languages directory: $languageRoot"
}
else {
    $languageDirs = @(Get-ChildItem -LiteralPath $languageRoot -Directory)
    $englishDir = Join-Path $languageRoot "English"
    if (-not (Test-Path -LiteralPath $englishDir)) {
        Add-Error "Missing English language directory: $englishDir"
    }
    else {
        $english = Read-KeyedLanguage $englishDir
        foreach ($languageDir in $languageDirs) {
            if ($languageDir.FullName -eq $englishDir) { continue }
            $language = Read-KeyedLanguage $languageDir.FullName
            foreach ($key in $english.Keys) {
                if (-not $language.ContainsKey($key)) {
                    Add-Error "Missing '$key' in $($languageDir.Name)"
                }
            }
            foreach ($key in $language.Keys) {
                if (-not $english.ContainsKey($key)) {
                    Add-Error "Orphan key '$key' in $($languageDir.Name)"
                }
            }
        }

        $codeKeys = @{}
        $sourceRoot = Join-Path $Root "Source"
        if (Test-Path -LiteralPath $sourceRoot) {
            $sourceFiles = Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter "*.cs"
            foreach ($file in $sourceFiles) {
                $lines = Get-Content -LiteralPath $file.FullName
                for ($i = 0; $i -lt $lines.Count; $i++) {
                    $line = $lines[$i]
                    foreach ($match in [regex]::Matches($line, 'OrderedWorkText\.T\(\s*"([^"]+)"')) {
                        $codeKeys[$match.Groups[1].Value] = $true
                    }
                    foreach ($match in [regex]::Matches($line, '"([^"]+)"\s*\.Translate\(')) {
                        $codeKeys[$match.Groups[1].Value] = $true
                    }

                    if ($line -match '(Widgets\.Label|Widgets\.ButtonText|Widgets\.CheckboxLabeled|TooltipHandler\.TipRegion|Messages\.Message|new FloatMenuOption)\([^;]*"[^"]{3,}"' -and
                        $line -notmatch 'OrderedWorkText\.T|ConfigText\(|\.Translate\(') {
                        Add-Warning "Possible hardcoded UI string at $($file.FullName):$($i + 1)"
                    }
                }
            }
        }

        foreach ($key in $codeKeys.Keys) {
            if (-not $english.ContainsKey($key)) {
                Add-Error "Code key '$key' is missing from English keyed files"
            }
        }
    }
}

foreach ($warning in ($warnings | Select-Object -First 20)) {
    Write-Host "WARN $warning" -ForegroundColor Yellow
}
if ($warnings.Count -gt 20) {
    Write-Host "WARN ... $($warnings.Count - 20) more" -ForegroundColor Yellow
}

foreach ($errorMessage in $errors) {
    Write-Host "FAIL $errorMessage" -ForegroundColor Red
}

if ($errors.Count -gt 0) {
    Write-Host "Localization verification failed: $($errors.Count) error(s), $($warnings.Count) warning(s)." -ForegroundColor Red
    exit 1
}

Write-Host "Localization verification passed: 0 errors, $($warnings.Count) warning(s)." -ForegroundColor Green
