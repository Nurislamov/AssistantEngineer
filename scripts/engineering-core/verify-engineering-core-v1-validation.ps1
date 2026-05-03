param(
    [switch] $SkipRegenerate,
    [switch] $RequireRealReferences
)

$ErrorActionPreference = "Stop"

function Invoke-Step {
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

Write-Host "Engineering Core V1 validation profile"
Write-Host "Repository: $repoRoot"

if (-not $SkipRegenerate) {
    Invoke-Step "Regenerate validation artifacts" {
        .\scripts\engineering-core\regenerate-engineering-core-v1-validation-artifacts.ps1 -RequireRealReferences:$RequireRealReferences
    }
}

Invoke-Step "Validation registry and readiness tests" {
    dotnet test .\AssistantEngineer.sln --filter "EnergyPlusValidationCaseRegistryTests|EnergyPlusValidation"
}

Invoke-Step "EP-SMOKE-001 scaffold and comparison tests" {
    dotnet test .\AssistantEngineer.sln --filter "EnergyPlusSmoke001FixtureScaffoldTests|EnergyPlusSmoke001ComparisonHarnessTests"
}

Invoke-Step "EP-SMOKE-002 and EP-SMOKE-003 fixture tests" {
    dotnet test .\AssistantEngineer.sln --filter "EnergyPlusSmoke002And003FixtureScaffoldTests"
}

Invoke-Step "Generic validation runner and summary tests" {
    dotnet test .\AssistantEngineer.sln --filter "EnergyPlusValidationGenericComparisonRunnerTests|EnergyPlusValidationComparisonSummaryTests"
}

Invoke-Step "Real fixture intake and catalog sync tests" {
    dotnet test .\AssistantEngineer.sln --filter "EnergyPlusRealFixtureIntakeGateTests|EnergyPlusValidationFixtureCatalogTests"
}

Invoke-Step "Validation fixture authoring kit tests" {
    dotnet test .\AssistantEngineer.sln --filter "EnergyPlusValidationFixtureAuthoringKitTests"
}

Invoke-Step "Validation evidence package tests" {
    .\scripts\engineering-core\generate-engineering-core-v1-validation-evidence.ps1
    dotnet test .\AssistantEngineer.sln --filter "EnergyPlusValidationEvidencePackageTests"
}

Write-Host ""
Write-Host "Engineering Core V1 validation profile completed successfully." -ForegroundColor Green
Write-Host ""
Write-Host "Verified:"
Write-Host "- validation registry"
Write-Host "- validation readiness report"
Write-Host "- EP-SMOKE-001 scaffold"
Write-Host "- EP-SMOKE-001 comparison harness"
Write-Host "- EP-SMOKE-002 solar cooling fixture"
Write-Host "- EP-SMOKE-003 internal gains fixture"
Write-Host "- generic EnergyPlus validation fixture runner"
Write-Host "- validation comparison summaries"
Write-Host "- real fixture intake gate"
Write-Host "- fixture catalog synchronization"
Write-Host "- fixture authoring kit"
Write-Host "- non-parity and non-ASHRAE-140 claims"

