param(
    [switch] $SkipFrontend
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

Write-Host "Engineering Core V1 smoke verification"
Write-Host "Repository: $repoRoot"

if (-not $SkipFrontend) {
    Invoke-Step "Frontend build smoke" {
        npm --prefix .\src\Frontend run build
    }
}

Invoke-Step "Core formula/status/report/diagnostics smoke tests" {
    dotnet test .\AssistantEngineer.sln --filter "FormulaAudit|EngineeringCoreStatus|EngineeringCoreReportDisclosureTests|EngineeringCoreDiagnosticsCatalogFacadeAndApiTests"
}

Invoke-Step "Frontend visibility smoke tests" {
    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreFrontendIntegrationGuardTests|EngineeringCoreDiagnosticsCatalogPanelFrontendGuardTests"
}

Invoke-Step "Weather/annual/hourly closure smoke tests" {
    dotnet test .\AssistantEngineer.sln --filter "AnnualEnergy8760ScenarioTests|EpwAnnualClimateDataImportServiceTests|PvgisAnnualClimateDataImportServiceTests|Iso52016EngineeringCoreV1ClosureTests"
}

Write-Host ""
Write-Host "Engineering Core V1 smoke verification completed successfully." -ForegroundColor Green
