param(
    [switch] $RequireRealReferences
)

$ErrorActionPreference = "Stop"

function Invoke-Generator {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name,

        [Parameter(Mandatory = $true)]
        [scriptblock] $Command
    )

    Write-Host ""
    Write-Host "==> $Name" -ForegroundColor Cyan
    & $Command
    Write-Host "OK: $Name" -ForegroundColor Green
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

Write-Host "Engineering Core V1 validation artifact regeneration"
Write-Host "Repository: $repoRoot"

Invoke-Generator "Validation readiness report" {
    .\scripts\engineering-core\generate-engineering-core-v1-validation-readiness.ps1
}

Invoke-Generator "EP-SMOKE-001 comparison readiness report" {
    .\scripts\engineering-core\generate-ep-smoke-001-comparison-readiness.ps1
}

Invoke-Generator "EP-SMOKE-001 placeholder comparison report" {
    .\scripts\engineering-core\compare-ep-smoke-001-placeholder.ps1
}

Invoke-Generator "Generic EnergyPlus validation fixture comparisons" {
    .\scripts\engineering-core\compare-energyplus-validation-fixtures.ps1 -RequireRealReferences:$RequireRealReferences
}

Invoke-Generator "Validation comparison summary" {
    .\scripts\engineering-core\generate-engineering-core-v1-validation-comparison-summary.ps1
}

Invoke-Generator "EP-SMOKE-001 real fixture readiness report" {
    .\scripts\engineering-core\assert-ep-smoke-001-real-fixture-ready.ps1 -RequireRealFixture:$RequireRealReferences
}

Invoke-Generator "EnergyPlus validation fixture catalog" {
    .\scripts\engineering-core\generate-energyplus-validation-fixture-catalog.ps1
}

Write-Host ""
Write-Host "Engineering Core V1 validation artifact regeneration completed successfully." -ForegroundColor Green
Write-Host ""
Write-Host "Generated validation artifacts include:"
Write-Host "- docs/reports/EngineeringCoreV1ValidationReadiness.md"
Write-Host "- docs/reports/validation/EP-SMOKE-001-ComparisonReadiness.md"
Write-Host "- docs/reports/validation/EP-SMOKE-001-ComparisonResult.json/md"
Write-Host "- docs/reports/validation/EP-SMOKE-002-ComparisonResult.json/md"
Write-Host "- docs/reports/validation/EP-SMOKE-003-ComparisonResult.json/md"
Write-Host "- docs/reports/validation/EngineeringCoreV1ValidationComparisonSummary.json/md"
Write-Host "- docs/reports/validation/EnergyPlusValidationGenericComparisonSummary.json/md"
Write-Host "- docs/reports/validation/EP-SMOKE-001-RealFixtureReadiness.md"
Write-Host "- docs/validation/EnergyPlusValidationFixtureCatalog.json/md"
