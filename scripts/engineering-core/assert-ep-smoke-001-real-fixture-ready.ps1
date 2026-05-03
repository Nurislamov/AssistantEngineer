param(
    [string] $OutputPath = "docs/reports/validation/EP-SMOKE-001-RealFixtureReadiness.md",
    [switch] $RequireRealFixture
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$fixtureDirectory = "tests/fixtures/validation/energyplus/EP-SMOKE-001"

$requiredPlaceholderFiles = @(
    "case-metadata.json",
    "assistantengineer-input.json",
    "reference-output.placeholder.json",
    "comparison-tolerances.json"
)

$requiredRealFixtureFiles = @(
    "energyplus-model.idf",
    "weather.epw",
    "energyplus-output.raw.csv",
    "energyplus-output.reference.json",
    "provenance.json"
)

$placeholderStatusRows = @()
$missingPlaceholderFiles = @()

foreach ($file in $requiredPlaceholderFiles) {
    $path = Join-Path $fixtureDirectory $file
    $exists = Test-Path $path

    if (-not $exists) {
        $missingPlaceholderFiles += $file
    }

    $placeholderStatusRows += [ordered]@{
        file = $file
        exists = [bool]$exists
    }
}

$realStatusRows = @()
$missingRealFixtureFiles = @()

foreach ($file in $requiredRealFixtureFiles) {
    $path = Join-Path $fixtureDirectory $file
    $exists = Test-Path $path

    if (-not $exists) {
        $missingRealFixtureFiles += $file
    }

    $realStatusRows += [ordered]@{
        file = $file
        exists = [bool]$exists
    }
}

if ($missingPlaceholderFiles.Count -gt 0) {
    throw "EP-SMOKE-001 placeholder scaffold is incomplete. Missing: $($missingPlaceholderFiles -join ', ')"
}

$realFixtureReady = $missingRealFixtureFiles.Count -eq 0
$status = if ($realFixtureReady) { "ReadyForRealComparison" } else { "NotReadyRealFixtureMissingFiles" }
$generatedAt = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'")

$placeholderRowsMarkdown = @($placeholderStatusRows | ForEach-Object {
    "| $($_.file) | $($_.exists) |"
})

$realRowsMarkdown = @($realStatusRows | ForEach-Object {
    "| $($_.file) | $($_.exists) |"
})

$missingRealMarkdown = if ($missingRealFixtureFiles.Count -eq 0) {
    "- none"
}
else {
    @($missingRealFixtureFiles | ForEach-Object { "- $_" }) -join "`n"
}

$report = @"
# EP-SMOKE-001 Real Fixture Readiness

Generated at: $generatedAt

## Status

| Field | Value |
|---|---|
| Case id | EP-SMOKE-001 |
| Status | $status |
| Real fixture ready | $realFixtureReady |
| Require real fixture | $RequireRealFixture |

## Existing placeholder scaffold files

| File | Exists |
|---|---|
$($placeholderRowsMarkdown -join "`n")

## Required future real fixture files

| File | Exists |
|---|---|
$($realRowsMarkdown -join "`n")

## Missing real fixture files

$missingRealMarkdown

## Interpretation

EP-SMOKE-001 currently remains a placeholder comparison unless all real fixture files are present.

Missing real fixture files do not fail Engineering Core V1 closure.

They only fail when this script is run with -RequireRealFixture.

## Required non-claims

- Does not claim exact EnergyPlus numerical parity.
- Does not claim ASHRAE 140 validation coverage.
- Does not claim full ISO 52016 node/matrix solver parity.
- PlaceholderComparison is not real EnergyPlus validation.
- Future real validation must remain tolerance-based.
"@

$directory = Split-Path $OutputPath -Parent
if (-not [string]::IsNullOrWhiteSpace($directory)) {
    New-Item -ItemType Directory -Force $directory | Out-Null
}

Set-Content $OutputPath $report -Encoding utf8

if ($RequireRealFixture -and -not $realFixtureReady) {
    throw "EP-SMOKE-001 real fixture is not ready. Missing: $($missingRealFixtureFiles -join ', ')"
}

Write-Host "EP-SMOKE-001 real fixture readiness report generated: $OutputPath" -ForegroundColor Green
Write-Host "Status: $status"
