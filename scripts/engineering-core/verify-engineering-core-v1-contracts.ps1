param(
    [switch] $SkipFrontend,
    [switch] $SkipRegenerate
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

Write-Host "Engineering Core V1 contracts verification"
Write-Host "Repository: $repoRoot"

if (-not $SkipRegenerate) {
    Invoke-Step "Regenerate Engineering Core V1 artifacts" {
        .\scripts\engineering-core\regenerate-engineering-core-v1-artifacts.ps1
    }
}

if (-not $SkipFrontend) {
    Invoke-Step "Frontend build" {
        npm --prefix .\src\Frontend run build
    }
}

Invoke-Step "API/OpenAPI/report/export/diagnostics contract tests" {
    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreV1ApiContractSnapshotTests|EngineeringCoreV1OpenApiContractTests|EngineeringCoreV1ReportContractSnapshotTests|EngineeringCoreV1ReportExportDisclosureGuardTests|EngineeringCoreV1FormulaAuditDiagnosticsCatalogTests|EngineeringCoreDiagnosticsCatalogFacadeAndApiTests|EngineeringCoreDiagnosticsCatalogFrontendGuardTests"
}

Invoke-Step "Release evidence/manifest/traceability/validation registry tests" {
    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreV1ReleaseEvidencePackageTests|EngineeringCoreV1ReleaseManifestTests|EngineeringCoreV1TraceabilityMatrixTests|EnergyPlusValidationCaseRegistryTests"
}

Invoke-Step "Documentation and contribution guard tests" {
    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreV1ProjectDocumentationTests|EngineeringCoreV1ReleaseDocumentationTests|EngineeringCoreV1ScopeDocumentationTests|EngineeringCoreV1VerificationRunbookTests|EngineeringCoreV1CiWorkflowTests|EngineeringCoreV1ContributionGuardTests"
}

Write-Host ""
Write-Host "Engineering Core V1 contracts verification completed successfully." -ForegroundColor Green
