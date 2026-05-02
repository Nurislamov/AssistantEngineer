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

Write-Host "Engineering Core V1 manifest verification"
Write-Host "Repository: $repoRoot"

Invoke-Step "Manifest consistency tests" {
    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreV1ReleaseManifestTests"
}

Invoke-Step "Release documentation guard tests" {
    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreV1ProjectDocumentationTests|EngineeringCoreV1ReleaseDocumentationTests|EngineeringCoreV1VerificationRunbookTests"
}

Invoke-Step "Status/disclosure/frontend guard tests" {
    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreStatus|EngineeringCoreReportDisclosureTests|EngineeringCoreFrontendIntegrationGuardTests"
}

if (-not $SkipFrontend) {
    Invoke-Step "Frontend build" {
        npm --prefix .\src\Frontend run build
    }
}

Write-Host ""
Write-Host "Engineering Core V1 manifest verification completed successfully." -ForegroundColor Green
