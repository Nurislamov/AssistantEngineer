param(
    [switch] $SkipFrontend,
    [switch] $SkipFullDotnet,
    [switch] $Fast
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

Write-Host "Engineering Core V1 verification"
Write-Host "Repository: $repoRoot"

if (-not $SkipFrontend) {
    Invoke-Step "Frontend TypeScript/Vite build" {
        npm --prefix .\src\Frontend run build
    }
}

Invoke-Step "Engineering Core status and formula audit tests" {
    dotnet test .\AssistantEngineer.sln --filter "FormulaAudit|EngineeringCoreStatus|EngineeringCoreReportDisclosureTests"
}

Invoke-Step "Engineering Core documentation guard tests" {
    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreV1ProjectDocumentationTests|EngineeringCoreV1ReleaseDocumentationTests|EngineeringCoreV1ScopeDocumentationTests|EngineeringCoreV1FrontendDisclosureDocumentationTests"
}

Invoke-Step "Engineering Core diagnostics catalog guard tests" {
    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreV1FormulaAuditDiagnosticsCatalogTests|EngineeringCoreDiagnosticsCatalogFacadeAndApiTests|EngineeringCoreDiagnosticsCatalogFrontendGuardTests"
}

Invoke-Step "Engineering Core release evidence package guard tests" {
    .\scripts\engineering-core\generate-engineering-core-v1-release-evidence.ps1
    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreV1ReleaseEvidencePackageTests"
}

Invoke-Step "Engineering Core API contract snapshot guard tests" {
    .\scripts\engineering-core\generate-engineering-core-v1-api-contract-snapshots.ps1
    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreV1ApiContractSnapshotTests"
}

Invoke-Step "Engineering Core report contract snapshot guard tests" {
    .\scripts\engineering-core\generate-engineering-core-v1-report-contract-snapshots.ps1
    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreV1ReportContractSnapshotTests"
}

Invoke-Step "Engineering Core OpenAPI contract guard tests" {
    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreV1OpenApiContractTests"
}

Invoke-Step "Engineering Core frontend visibility guard tests" {
    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreFrontendIntegrationGuardTests|EngineeringCoreDiagnosticsCatalogPanelFrontendGuardTests"
}

Invoke-Step "Engineering Core weather and annual 8760 gate tests" {
    dotnet test .\AssistantEngineer.sln --filter "EpwAnnualClimateDataImportServiceTests|PvgisAnnualClimateDataImportServiceTests|AnnualEnergy8760ScenarioTests"
}

Invoke-Step "Engineering Core hourly heat-balance, zone, ground and adjacent closure tests" {
    dotnet test .\AssistantEngineer.sln --filter "Iso52016EngineeringCoreV1ClosureTests|GroundSimplifiedEngineeringCoreV1ClosureTests|AdjacentZoneSimplifiedEngineeringCoreV1ClosureTests"
}

Invoke-Step "EnergyPlus/ASHRAE 140 validation harness guard tests" {
    dotnet test .\AssistantEngineer.sln --filter "EnergyPlusValidation"
}

Invoke-Step "Engineering Core validation registry guard tests" {
    .\scripts\engineering-core\generate-engineering-core-v1-validation-readiness.ps1
    dotnet test .\AssistantEngineer.sln --filter "EnergyPlusValidationCaseRegistryTests"
}

if (-not $SkipFullDotnet -and -not $Fast) {
    Invoke-Step "Full backend test suite" {
        dotnet test .\AssistantEngineer.sln
    }
}

Write-Host ""
Write-Host "Engineering Core V1 verification completed successfully." -ForegroundColor Green
Write-Host ""
Write-Host "Verified:"
Write-Host "- frontend build"
Write-Host "- formula audit matrix"
Write-Host "- Engineering Core V1 status endpoint/facade"
Write-Host "- report disclosures"
Write-Host "- diagnostics catalog"
Write-Host "- release evidence package"
Write-Host "- API contract snapshots"
Write-Host "- frontend visibility guards"
Write-Host "- EPW/PVGIS 8760 gates"
Write-Host "- annual true hourly 8760 gate"
Write-Host "- hourly heat-balance and single-zone gates"
Write-Host "- ground and adjacent simplified gates"
Write-Host "- EnergyPlus/ASHRAE 140 validation harness scaffold"
Write-Host "- release/scope/developer documentation"




