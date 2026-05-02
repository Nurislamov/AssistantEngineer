param(
    [switch] $SkipFrontend,
    [switch] $SkipFullDotnet,
    [switch] $SkipGitStatus,
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

function Assert-FileExists {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    if (-not (Test-Path $Path)) {
        throw "Required release readiness file is missing: $Path"
    }
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

Write-Host "Engineering Core V1 release readiness gate"
Write-Host "Repository: $repoRoot"

Invoke-Step "Check required release readiness artifacts" {
    $requiredFiles = @(
        "docs/releases/EngineeringCoreV1Manifest.json",
        "docs/releases/EngineeringCoreV1ReleaseManifest.md",
        "docs/releases/EngineeringCoreV1ReleaseChecklist.md",
        "docs/releases/EngineeringCoreV1OwnerHandoff.md",
        "docs/reports/EngineeringCoreV1ReleaseEvidence.md",
        "docs/traceability/EngineeringCoreV1TraceabilityMatrix.json",
        "docs/traceability/EngineeringCoreV1TraceabilityMatrix.md",
        "docs/calculations/EngineeringCoreV1DiagnosticsCatalog.json",
        "docs/api/engineering-core-v1/status.sample.json",
        "docs/api/engineering-core-v1/diagnostics-catalog.sample.json",
        "docs/reports/engineering-core-v1/heating-report.sample.json",
        "docs/reports/engineering-core-v1/cooling-report.sample.json",
        "docs/reports/engineering-core-v1/annual-energy-disclosure.sample.json",
        "docs/validation/EnergyPlusValidationCaseRegistry.json",
        "docs/reports/EngineeringCoreV1ValidationReadiness.md",
        ".github/workflows/engineering-core-v1.yml",
        "scripts/engineering-core/verify-engineering-core-v1.ps1",
        "scripts/engineering-core/verify-engineering-core-v1-smoke.ps1",
        "scripts/engineering-core/verify-engineering-core-v1-contracts.ps1",
        "scripts/engineering-core/regenerate-engineering-core-v1-artifacts.ps1"
    )

    foreach ($file in $requiredFiles) {
        Assert-FileExists $file
    }
}

Invoke-Step "Regenerate Engineering Core V1 generated artifacts" {
    .\scripts\engineering-core\regenerate-engineering-core-v1-artifacts.ps1
}

if (-not $SkipFrontend) {
    Invoke-Step "Frontend build" {
        npm --prefix .\src\Frontend run build
    }
}

Invoke-Step "Smoke verification profile" {
    .\scripts\engineering-core\verify-engineering-core-v1-smoke.ps1 -SkipFrontend:$SkipFrontend
}

Invoke-Step "Contracts verification profile" {
    .\scripts\engineering-core\verify-engineering-core-v1-contracts.ps1 -SkipFrontend:$SkipFrontend -SkipRegenerate
}

Invoke-Step "Manifest verification" {
    .\scripts\engineering-core\verify-engineering-core-v1-manifest.ps1 -SkipFrontend:$SkipFrontend
}

if (-not $Fast) {
    Invoke-Step "Full Engineering Core V1 verification" {
        .\scripts\engineering-core\verify-engineering-core-v1.ps1 -SkipFrontend:$SkipFrontend -SkipFullDotnet:$SkipFullDotnet
    }
}

if (-not $SkipFullDotnet -and -not $Fast) {
    Invoke-Step "Full backend test suite" {
        dotnet test .\AssistantEngineer.sln
    }
}

if (-not $SkipGitStatus) {
    Invoke-Step "Git working tree status" {
        git status --short
    }
}

Write-Host ""
Write-Host "Engineering Core V1 release readiness gate completed successfully." -ForegroundColor Green
Write-Host ""
Write-Host "Release-ready interpretation:"
Write-Host "- Engineering Core V1 is closed as an engineering formula gate."
Write-Host "- FormulaAuditMatrix, manifest, diagnostics, API contracts, report disclosures, frontend visibility, validation registry and traceability are verified."
Write-Host "- This does not claim exact EnergyPlus numerical parity, exact pyBuildingEnergy numerical parity or ASHRAE 140 validation coverage."
Write-Host "- Future validation remains comparative and tolerance-based."
