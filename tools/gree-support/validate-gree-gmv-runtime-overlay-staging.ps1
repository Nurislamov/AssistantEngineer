param(
    [string]$RepoRoot = (Resolve-Path ".").Path
)

$ErrorActionPreference = "Stop"

function New-Issue($Severity, $Code, $Path, $Message) {
    [pscustomobject]@{
        severity = $Severity
        code = $Code
        path = $Path
        message = $Message
    }
}

function Prop($Object, [string]$Name) {
    if ($null -eq $Object) { return $null }
    if ($Object.PSObject.Properties.Name -contains $Name) { return $Object.$Name }
    return $null
}

function Arr($Value) {
    if ($null -eq $Value) { return @() }
    return @($Value)
}

function BoolTrue($Value) {
    if ($null -eq $Value) { return $false }
    if ($Value -is [bool]) { return [bool]$Value }
    return ([string]$Value).Trim().ToLowerInvariant() -eq "true"
}

function RepoPath([string]$RepoRoot, [string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) { return $null }
    if ([System.IO.Path]::IsPathRooted($Path)) { return $Path }
    return Join-Path $RepoRoot ($Path -replace '/', '\')
}

Write-Host "Gree GMV runtime overlay staging validator"
Write-Host "RepoRoot: $RepoRoot"

if (-not (Test-Path $RepoRoot)) {
    throw "RepoRoot not found: $RepoRoot"
}

if (-not (Test-Path (Join-Path $RepoRoot ".git"))) {
    throw "Not a git repository: $RepoRoot"
}

$issues = @()
$overlayRoot = Join-Path $RepoRoot "data\reference\gree-official-support-error-catalog\staging\runtime-overlay"
$overlayPath = Join-Path $overlayRoot "approved-runtime-overlay-preview.json"
$readmePath = Join-Path $overlayRoot "README.md"

if (-not (Test-Path $overlayPath)) {
    $issues += New-Issue "error" "missing-overlay" "staging/runtime-overlay/approved-runtime-overlay-preview.json" "Overlay file not found."
}

if (-not (Test-Path $readmePath)) {
    $issues += New-Issue "error" "missing-readme" "staging/runtime-overlay/README.md" "README file not found."
}

$overlay = $null
if (Test-Path $overlayPath) {
    try {
        $overlay = Get-Content $overlayPath -Raw -Encoding UTF8 | ConvertFrom-Json
    } catch {
        $issues += New-Issue "error" "invalid-overlay-json" "approved-runtime-overlay-preview.json" $_.Exception.Message
    }
}

if ($null -ne $overlay) {
    if ([int](Prop $overlay "schemaVersion") -ne 1) { $issues += New-Issue "error" "invalid-schema" "overlay" "schemaVersion must be 1." }
    if ([string](Prop $overlay "status") -ne "staging-runtime-overlay-preview") { $issues += New-Issue "error" "invalid-status" "overlay" "status must be staging-runtime-overlay-preview." }
    if ([string](Prop $overlay "stage") -ne "ED-24GEC.5D") { $issues += New-Issue "error" "invalid-stage" "overlay" "stage must be ED-24GEC.5D." }
    if ([string](Prop $overlay "brand") -ne "Gree") { $issues += New-Issue "error" "invalid-brand" "overlay" "brand must be Gree." }
    if ([string](Prop $overlay "model") -ne "GMV") { $issues += New-Issue "error" "invalid-model" "overlay" "model must be GMV." }
    if (BoolTrue (Prop $overlay "runtimeEnabled")) { $issues += New-Issue "error" "runtime-enabled" "overlay" "runtimeEnabled must be false." }
    if (BoolTrue (Prop $overlay "diagnosticsRuntimeEnabled")) { $issues += New-Issue "error" "diagnostics-runtime-enabled" "overlay" "diagnosticsRuntimeEnabled must be false." }

    $entries = @(Arr (Prop $overlay "entries"))
    if ($entries.Count -ne 17) {
        $issues += New-Issue "error" "entry-count" "overlay" "Expected 17 entries, got $($entries.Count)."
    }

    $requiredCodes = @("A0","C0","C7","E1","H5","L1","o1","U0","E0","F3","P0","P1","P2","U2","U3","U4","U5")
    $codes = @($entries | ForEach-Object { [string](Prop $_ "code") } | Sort-Object -Unique)

    foreach ($code in $requiredCodes) {
        if ($codes -notcontains $code) {
            $issues += New-Issue "error" "missing-code" "overlay" "Missing code $code."
        }
    }

    foreach ($entry in $entries) {
        $code = [string](Prop $entry "code")
        $entryPath = "overlay#$code"

        if ([string](Prop $entry "status") -ne "staging-overlay-preview") {
            $issues += New-Issue "error" "invalid-entry-status" $entryPath "Entry status must be staging-overlay-preview."
        }

        if (BoolTrue (Prop $entry "runtimeEnabled")) {
            $issues += New-Issue "error" "entry-runtime-enabled" $entryPath "Entry runtimeEnabled must be false."
        }

        if (BoolTrue (Prop $entry "diagnosticsRuntimeEnabled")) {
            $issues += New-Issue "error" "entry-diagnostics-runtime-enabled" $entryPath "Entry diagnosticsRuntimeEnabled must be false."
        }

        $runtimePath = [string](Prop $entry "runtimePath")
        $approvedPath = [string](Prop $entry "approvedPath")

        foreach ($ref in @($runtimePath, $approvedPath)) {
            if ([string]::IsNullOrWhiteSpace($ref)) {
                $issues += New-Issue "error" "missing-path" $entryPath "runtimePath/approvedPath is required."
                continue
            }

            if (-not (Test-Path (RepoPath $RepoRoot $ref))) {
                $issues += New-Issue "error" "missing-target" $entryPath "Target not found: $ref"
            }
        }

        if ($runtimePath -like "*gmv-mini*") {
            $issues += New-Issue "error" "mini-target" $entryPath "GMV Mini runtime target is not allowed in GMV overlay entries."
        }

        if ($code -eq "C0" -and $runtimePath -ne "data/equipment-diagnostics/error-knowledge/gree/gmv6/debugging/c0.json") {
            $issues += New-Issue "error" "invalid-c0-target" $entryPath "C0 must target gmv6/debugging/c0.json."
        }

        $entryOverlay = Prop $entry "overlay"
        if ($null -eq $entryOverlay) {
            $issues += New-Issue "error" "missing-overlay-object" $entryPath "Entry overlay object is required."
        } else {
            foreach ($field in @("referenceSource", "referenceStage", "referenceApprovedPath", "primaryRawCardPath", "titleRu", "meaningRu", "severityRu", "userSafeAnswerRu", "technicianAnswerRu")) {
                if ([string]::IsNullOrWhiteSpace([string](Prop $entryOverlay $field))) {
                    $issues += New-Issue "error" "empty-overlay-field" $entryPath "$field is required."
                }
            }

            foreach ($field in @("possibleCausesRu", "checksRu", "serviceNotesRu")) {
                if (@(Arr (Prop $entryOverlay $field)).Count -eq 0) {
                    $issues += New-Issue "error" "empty-overlay-array" $entryPath "$field is required."
                }
            }

            if ([string](Prop $entryOverlay "referenceStage") -ne "ED-24GEC.4B") {
                $issues += New-Issue "error" "invalid-reference-stage" $entryPath "referenceStage must be ED-24GEC.4B."
            }

            if (BoolTrue (Prop $entryOverlay "runtimeEnabled")) {
                $issues += New-Issue "error" "overlay-runtime-enabled" $entryPath "overlay.runtimeEnabled must be false."
            }

            if (BoolTrue (Prop $entryOverlay "diagnosticsRuntimeEnabled")) {
                $issues += New-Issue "error" "overlay-diagnostics-runtime-enabled" $entryPath "overlay.diagnosticsRuntimeEnabled must be false."
            }
        }
    }

    $blocked = @(Arr (Prop $overlay "blockedMappings"))
    if ($blocked.Count -ne 1) {
        $issues += New-Issue "error" "blocked-count" "overlay" "Expected exactly one blocked mapping."
    } else {
        $blockedEntry = $blocked[0]
        $blockedPath = [string](Prop $blockedEntry "runtimePath")

        if ([string](Prop $blockedEntry "code") -ne "C0") {
            $issues += New-Issue "error" "invalid-blocked-code" "overlay" "Blocked code must be C0."
        }

        if ($blockedPath -ne "data/equipment-diagnostics/error-knowledge/gree/gmv-mini/indoor/c0.json") {
            $issues += New-Issue "error" "invalid-blocked-path" "overlay" "Blocked path must be GMV Mini C0."
        }

        if (-not (Test-Path (RepoPath $RepoRoot $blockedPath))) {
            $issues += New-Issue "error" "missing-blocked-target" "overlay" "Blocked target not found: $blockedPath"
        }
    }
}

$runtimeChanges = @(git status --short -- "data/equipment-diagnostics")
foreach ($line in $runtimeChanges) {
    if (-not [string]::IsNullOrWhiteSpace($line)) {
        $issues += New-Issue "error" "runtime-modified" "data/equipment-diagnostics" "Runtime diagnostics files must not be modified: $line"
    }
}

$errorCount = @($issues | Where-Object { $_.severity -eq "error" }).Count
$warningCount = @($issues | Where-Object { $_.severity -eq "warning" }).Count

Write-Host ""
Write-Host "Summary:"
Write-Host "  Overlay exists:    $(Test-Path $overlayPath)"
Write-Host "  README exists:     $(Test-Path $readmePath)"
Write-Host "  Entries:           $(if ($null -ne $overlay) { @(Arr (Prop $overlay 'entries')).Count } else { 0 })"
Write-Host "  Blocked mappings:  $(if ($null -ne $overlay) { @(Arr (Prop $overlay 'blockedMappings')).Count } else { 0 })"
Write-Host "  Errors:            $errorCount"
Write-Host "  Warnings:          $warningCount"

if ($issues.Count -gt 0) {
    Write-Host ""
    Write-Host "Issues:"
    $issues | Sort-Object severity, code, path | Format-Table severity, code, path, message -AutoSize
}

if ($errorCount -gt 0) {
    throw "Gree GMV runtime overlay staging validation failed with $errorCount error(s)."
}

Write-Host ""
Write-Host "PASS: Gree GMV runtime overlay staging validation completed."