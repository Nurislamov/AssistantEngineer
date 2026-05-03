param(
    [switch] $SkipDocs
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

Write-Host "Calculation module balance invariant verification" -ForegroundColor Cyan
Write-Host "Repository: $repoRoot"

if (-not $SkipDocs) {
    $requiredDocs = @(
        "docs/calculations/CalculationModuleBalanceInvariants.md"
    )

    foreach ($requiredDoc in $requiredDocs) {
        if (-not (Test-Path $requiredDoc)) {
            throw "Required balance invariant document is missing: $requiredDoc"
        }
    }
}

Write-Host ""
Write-Host "==> Run calculation module balance invariant tests" -ForegroundColor Cyan
dotnet test .\AssistantEngineer.sln --filter "CalculationModuleBalanceInvariantTests"
Write-Host "OK: Calculation module balance invariant tests" -ForegroundColor Green

Write-Host ""
Write-Host "Calculation module balance invariant verification completed successfully." -ForegroundColor Green
