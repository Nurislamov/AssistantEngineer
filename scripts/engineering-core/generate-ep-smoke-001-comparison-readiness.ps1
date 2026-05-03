param(
    [string] $OutputPath = "docs/reports/validation/EP-SMOKE-001-ComparisonReadiness.md"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$fixtureDirectory = "tests/fixtures/validation/energyplus/EP-SMOKE-001"
$metadataPath = Join-Path $fixtureDirectory "case-metadata.json"
$inputPath = Join-Path $fixtureDirectory "assistantengineer-input.json"
$referencePath = Join-Path $fixtureDirectory "reference-output.placeholder.json"
$tolerancesPath = Join-Path $fixtureDirectory "comparison-tolerances.json"

foreach ($path in @($metadataPath, $inputPath, $referencePath, $tolerancesPath)) {
    if (-not (Test-Path $path)) {
        throw "Required EP-SMOKE-001 fixture file is missing: $path"
    }
}

$metadata = Get-Content $metadataPath -Raw | ConvertFrom-Json
$input = Get-Content $inputPath -Raw | ConvertFrom-Json
$reference = Get-Content $referencePath -Raw | ConvertFrom-Json
$tolerances = Get-Content $tolerancesPath -Raw | ConvertFrom-Json

$metricRows = @($tolerances.metrics | ForEach-Object {
    "| $($_.metricId) | $($_.type) | $($_.unit) | $($_.tolerancePercent) | $($_.absoluteTolerance) |"
})

$nonClaimRows = @($tolerances.requiredNonClaims | ForEach-Object { "- $_" })

$generatedAt = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'")

$report = @"
# EP-SMOKE-001 Comparison Readiness

Generated at: $generatedAt

## Status

| Field | Value |
|---|---|
| Case id | $($metadata.caseId) |
| Name | $($metadata.name) |
| Stage | $($metadata.stage) |
| Status | $($metadata.status) |
| Reference status | $($reference.status) |
| Source | $($metadata.source) |

## Engineering input summary

| Field | Value |
|---|---|
| Floor area | $($input.building.floorAreaM2) m² |
| Volume | $($input.building.volumeM3) m³ |
| Opaque area | $($input.envelope.opaqueAreaM2) m² |
| U-value | $($input.envelope.uValueWPerM2K) W/(m²·K) |
| Indoor setpoint | $($input.zone.indoorHeatingSetpointC) °C |
| Outdoor dry-bulb | $($input.weather.outdoorDryBulbC) °C |
| Duration | $($input.weather.durationHours) h |
| Expected heat loss | $($input.calculationFormula.expectedTransmissionHeatLossW) W |
| Expected heating energy | $($input.calculationFormula.expectedDailyHeatingEnergyKwh) kWh |

## Metrics and tolerances

| Metric | Type | Unit | Tolerance percent | Absolute tolerance |
|---|---|---|---:|---:|
$($metricRows -join "`n")

## Required non-claims

$($nonClaimRows -join "`n")

## Readiness interpretation

EP-SMOKE-001 is ready as a fixture scaffold.

It is not a real EnergyPlus comparison yet.

The current reference output is a placeholder.

Future work must replace or supplement the placeholder with real EnergyPlus model/output files and provenance metadata.

Comparison must remain tolerance-based and must not claim exact EnergyPlus parity or ASHRAE 140 validation coverage.
"@

$directory = Split-Path $OutputPath -Parent
if (-not [string]::IsNullOrWhiteSpace($directory)) {
    New-Item -ItemType Directory -Force $directory | Out-Null
}

Set-Content $OutputPath $report -Encoding utf8

Write-Host "EP-SMOKE-001 comparison readiness report generated: $OutputPath" -ForegroundColor Green
