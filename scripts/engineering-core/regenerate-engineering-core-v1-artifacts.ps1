param(
    [switch] $SkipMissing
)

$ErrorActionPreference = "Stop"

function Invoke-Generator {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    if (-not (Test-Path $Path)) {
        if ($SkipMissing) {
            Write-Host "SKIP missing generator: $Path" -ForegroundColor Yellow
            return
        }

        throw "Required generator not found: $Path"
    }

    Write-Host ""
    Write-Host "==> Generate: $Path" -ForegroundColor Cyan
    & $Path
    Write-Host "OK: $Path" -ForegroundColor Green
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

Write-Host "Engineering Core V1 artifact regeneration"
Write-Host "Repository: $repoRoot"

$generators = @(
    ".\scripts\engineering-core\generate-engineering-core-v1-release-evidence.ps1",
    ".\scripts\engineering-core\generate-engineering-core-v1-api-contract-snapshots.ps1",
    ".\scripts\engineering-core\generate-engineering-core-v1-report-contract-snapshots.ps1",
    ".\scripts\engineering-core\generate-engineering-core-v1-export-disclosure-checklist.ps1",
    ".\scripts\engineering-core\generate-engineering-core-v1-validation-readiness.ps1",
    ".\scripts\engineering-core\generate-engineering-core-v1-traceability-matrix.ps1",
    ".\scripts\engineering-core\generate-ep-smoke-001-comparison-readiness.ps1",
    ".\scripts\engineering-core\compare-ep-smoke-001-placeholder.ps1",
    ".\scripts\engineering-core\generate-engineering-core-v1-validation-comparison-summary.ps1",
    ".\scripts\engineering-core\assert-ep-smoke-001-real-fixture-ready.ps1",
    ".\scripts\engineering-core\compare-energyplus-validation-fixtures.ps1"
)

foreach ($generator in $generators) {
    Invoke-Generator $generator
}

Write-Host ""
Write-Host "Engineering Core V1 artifact regeneration completed successfully." -ForegroundColor Green





