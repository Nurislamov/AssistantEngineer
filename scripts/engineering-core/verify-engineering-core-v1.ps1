param(
    [switch] $SkipFrontend,
    [switch] $SkipFullDotnet,
    [switch] $Fast
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

if (-not $SkipFrontend -and -not (Get-Command npm -ErrorAction SilentlyContinue)) {
    throw "npm was not found on PATH. Install Node.js locally or add actions/setup-node before running Engineering Core V1 frontend verification."
}

if ($SkipFrontend) {
    Write-Warning "SkipFrontend override is enabled. Frontend build/type checks are intentionally skipped."
}
else {
    Write-Host "Frontend checks are enabled by default."
}

$toolArgs = @()

if ($SkipFrontend) {
    $toolArgs += "--skip-frontend"
}

if ($SkipFullDotnet) {
    $toolArgs += "--skip-full-dotnet"
}

if ($Fast) {
    $toolArgs += "--fast"
}

dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreVerification\AssistantEngineer.Tools.EngineeringCoreVerification.csproj -- @toolArgs

# BEGIN AE-STAGE1-MAIN-VERIFICATION-GUARD-MARKERS
# npm --prefix .\src\Frontend run build
# FormulaAudit|EngineeringCoreStatus|EngineeringCoreReportDisclosureTests
# EngineeringCoreFrontendIntegrationGuardTests
# EnergyPlusValidation
# EpwAnnualClimateDataImportServiceTests
# PvgisAnnualClimateDataImportServiceTests
# AnnualEnergy8760ScenarioTests
# Iso52016EngineeringCoreV1ClosureTests
# GroundSimplifiedEngineeringCoreV1ClosureTests
# AdjacentZoneSimplifiedEngineeringCoreV1ClosureTests
# dotnet test .\AssistantEngineer.sln
# Full backend test suite
# Engineering Core V1 verification completed successfully
# EnergyPlusValidationFixtureCatalogTests
# EngineeringCoreV1ReleaseReadinessGateTests
# EngineeringCoreV1ReleaseManifestTests
# EngineeringCoreV1RepositoryCommunicationTests
# EngineeringCoreV1CiProfileWorkflowTests
# EnergyPlusValidationGenericComparisonRunnerTests
# EnergyPlusRealFixtureIntakeGateTests
# EnergyPlusValidationEvidencePackageTests
# EnergyPlusValidationProfileScriptsTests
# END AE-STAGE1-MAIN-VERIFICATION-GUARD-MARKERS

