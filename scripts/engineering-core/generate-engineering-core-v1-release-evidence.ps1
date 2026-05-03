param(
    [string] $OutputPath = "docs/reports/EngineeringCoreV1ReleaseEvidence.md"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$manifestPath = "docs/releases/EngineeringCoreV1Manifest.json"
$diagnosticsPath = "docs/calculations/EngineeringCoreV1DiagnosticsCatalog.json"

if (-not (Test-Path $manifestPath)) {
    throw "Manifest not found: $manifestPath"
}

if (-not (Test-Path $diagnosticsPath)) {
    throw "Diagnostics catalog not found: $diagnosticsPath"
}

$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$diagnosticsCatalog = Get-Content $diagnosticsPath -Raw | ConvertFrom-Json

$closedGateCount = @($manifest.closedFormulaGates).Count
$outOfScopeCount = @($manifest.outOfScopeV1).Count
$plannedValidationCount = @($manifest.plannedValidation).Count
$diagnosticCount = @($diagnosticsCatalog.diagnostics).Count

$errorCount = @($diagnosticsCatalog.diagnostics | Where-Object { $_.severity -eq "Error" }).Count
$warningCount = @($diagnosticsCatalog.diagnostics | Where-Object { $_.severity -eq "Warning" }).Count
$infoCount = @($diagnosticsCatalog.diagnostics | Where-Object { $_.severity -eq "Info" }).Count

$diagnosticCategories = @($diagnosticsCatalog.diagnostics |
    Group-Object category |
    Sort-Object Name |
    ForEach-Object { "| $($_.Name) | $($_.Count) |" })

$closedGateRows = @($manifest.closedFormulaGates |
    Sort-Object |
    ForEach-Object { "| $_ | ClosedV1 |" })

$nonClaimRows = @($manifest.explicitNonClaims |
    ForEach-Object { "- $_" })

$docRows = @($manifest.documentationFiles |
    Sort-Object |
    ForEach-Object { "| $_ | $(if (Test-Path $_) { "present" } else { "missing" }) |" })

$diagnosticRows = @($diagnosticsCatalog.diagnostics |
    Sort-Object code |
    ForEach-Object { "| $($_.code) | $($_.severity) | $($_.category) | $($_.closedV1Gate) |" })

$generatedAt = "2026-01-01 00:00:00 UTC"

$report = @"
# Engineering Core V1 Release Evidence

Generated at: $generatedAt

## Status summary

| Field | Value |
|---|---|
| Core name | $($manifest.coreName) |
| Version | $($manifest.version) |
| Status | $($manifest.status) |
| Release type | $($manifest.releaseType) |
| Formula gates closed | $($manifest.formulaGatesClosed) |
| Weather 8760 gates closed | $($manifest.weather8760GatesClosed) |
| Annual hourly 8760 gate closed | $($manifest.annualHourly8760GateClosed) |
| Success results must not contain Error diagnostics | $($manifest.successfulResultsMustNotContainErrorDiagnostics) |

## Counts

| Item | Count |
|---|---:|
| Closed formula gates | $closedGateCount |
| Out of scope v1 items | $outOfScopeCount |
| Planned validation items | $plannedValidationCount |
| Diagnostics total | $diagnosticCount |
| Error diagnostics | $errorCount |
| Warning diagnostics | $warningCount |
| Info diagnostics | $infoCount |

## Closed formula gates

| CalculationId | Status |
|---|---|
$($closedGateRows -join "`n")

## Annual 8760 requirements

True hourly annual energy requires:

$(@($manifest.requiredAnnual8760Flags | ForEach-Object { "- $_" }) -join "`n")

## Application endpoints

$(@($manifest.applicationEndpoints | ForEach-Object { "- $_" }) -join "`n")

## Frontend visibility files

$(@($manifest.frontendVisibility | ForEach-Object { "- $_" }) -join "`n")

## Backend visibility files

$(@($manifest.backendVisibility | ForEach-Object { "- $_" }) -join "`n")

## Verification scripts

$(@($manifest.verificationScripts | ForEach-Object { "- $_" }) -join "`n")

## CI workflows

$(@($manifest.ciWorkflows | ForEach-Object { "- $_" }) -join "`n")

## Out of scope v1

$(@($manifest.outOfScopeV1 | ForEach-Object { "- $_" }) -join "`n")

## Planned validation

$(@($manifest.plannedValidation | ForEach-Object { "- $_" }) -join "`n")

## Explicit non-claims

$($nonClaimRows -join "`n")

## Diagnostics by category

| Category | Count |
|---|---:|
$($diagnosticCategories -join "`n")

## Diagnostics catalog

| Code | Severity | Category | ClosedV1 gate |
|---|---|---|---|
$($diagnosticRows -join "`n")

## Documentation inventory

| File | Status |
|---|---|
$($docRows -join "`n")

## Required verification command

Full verification:

    $($manifest.releaseVerificationCommand)

Fast verification:

    $($manifest.fastVerificationCommand)

Manifest verification:

    $($manifest.manifestVerificationCommand)

## Release interpretation

Engineering Core V1 is closed as an engineering formula gate.

This release evidence does not claim exact EnergyPlus numerical parity, exact pyBuildingEnergy numerical parity, ASHRAE 140 validation coverage, full ISO 52016 node/matrix solver parity, full ISO 13370 implementation, full EN 15316 implementation or latent/moisture/humidity support in v1.
"@

$directory = Split-Path $OutputPath -Parent
if (-not [string]::IsNullOrWhiteSpace($directory)) {
    New-Item -ItemType Directory -Force $directory | Out-Null
}

Set-Content $OutputPath $report -Encoding utf8

Write-Host "Engineering Core V1 release evidence generated: $OutputPath" -ForegroundColor Green
Write-Host "Closed formula gates: $closedGateCount"
Write-Host "Diagnostics: $diagnosticCount total, $errorCount Error, $warningCount Warning, $infoCount Info"

