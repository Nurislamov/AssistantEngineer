param(
    [string] $OutputPath = "docs/reports/EngineeringCoreV1ValidationReadiness.md"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$registryPath = "docs/validation/EnergyPlusValidationCaseRegistry.json"

if (-not (Test-Path $registryPath)) {
    throw "Validation case registry not found: $registryPath"
}

$registry = Get-Content $registryPath -Raw | ConvertFrom-Json
$cases = @($registry.cases)

$caseCount = $cases.Count
$smokeCount = @($cases | Where-Object { $_.stage -eq "Smoke" }).Count
$ashraeStyleCount = @($cases | Where-Object { $_.stage -eq "Ashrae140Style" }).Count
$plannedCount = @($cases | Where-Object { $_.status -eq "Planned" }).Count
$placeholderCount = @($cases | Where-Object { $_.status -eq "ReferenceFixturePlaceholder" }).Count
$metricCount = @($cases | ForEach-Object { $_.metrics } | ForEach-Object { $_ }).Count

$caseRows = @($cases | Sort-Object caseId | ForEach-Object {
    "| $($_.caseId) | $($_.stage) | $($_.status) | $($_.metrics.Count) | $($_.name) |"
})

$metricRows = @($cases | Sort-Object caseId | ForEach-Object {
    $caseId = $_.caseId
    @($_.metrics | ForEach-Object {
        "| $caseId | $($_.metricId) | $($_.type) | $($_.unit) | $($_.tolerancePercent) |"
    })
})

$nonClaimRows = @($registry.requiredNonClaims | ForEach-Object { "- $_" })

$generatedAt = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'")

$report = @"
# Engineering Core V1 Validation Readiness

Generated at: $generatedAt

## Registry summary

| Field | Value |
|---|---|
| Registry name | $($registry.registryName) |
| Version | $($registry.version) |
| Status | $($registry.status) |
| Case count | $caseCount |
| Smoke cases | $smokeCount |
| ASHRAE 140-style cases | $ashraeStyleCount |
| Planned cases | $plannedCount |
| Reference fixture placeholders | $placeholderCount |
| Metric count | $metricCount |

## Default tolerances

| Metric | Tolerance |
|---|---|
| Annual heating energy | $($registry.defaultTolerances.annualHeatingEnergyPercent)% |
| Annual cooling energy | $($registry.defaultTolerances.annualCoolingEnergyPercent)% |
| Peak heating load | $($registry.defaultTolerances.peakHeatingLoadPercent)% |
| Peak cooling load | $($registry.defaultTolerances.peakCoolingLoadPercent)% |
| Directional trend | $($registry.defaultTolerances.directionalTrend) |
| Same sign | $($registry.defaultTolerances.sameSign) |

## Validation cases

| CaseId | Stage | Status | Metrics | Name |
|---|---|---|---:|---|
$($caseRows -join "`n")

## Metrics

| CaseId | MetricId | Type | Unit | Tolerance percent |
|---|---|---|---|---:|
$($metricRows -join "`n")

## Required non-claims

$($nonClaimRows -join "`n")

## Readiness interpretation

This registry is ready as a future validation backlog and smoke-fixture scaffold.

It is not exact EnergyPlus numerical parity.

It is not ASHRAE 140 certification.

It does not claim full ISO 52016 node/matrix solver parity.

Real external validation requires future committed EnergyPlus/reference model files, exported reference outputs, documented tolerances and passing comparison tests.
"@

$directory = Split-Path $OutputPath -Parent
if (-not [string]::IsNullOrWhiteSpace($directory)) {
    New-Item -ItemType Directory -Force $directory | Out-Null
}

Set-Content $OutputPath $report -Encoding utf8

Write-Host "Engineering Core V1 validation readiness report generated: $OutputPath" -ForegroundColor Green
Write-Host "Cases: $caseCount"
Write-Host "Metrics: $metricCount"
