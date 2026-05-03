param(
    [switch] $SkipDocs
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

Write-Host "Calculation module diagnostics consistency verification" -ForegroundColor Cyan
Write-Host "Repository: $repoRoot"

if (-not $SkipDocs) {
    $requiredDocs = @(
        "docs/calculations/CalculationModuleDiagnosticsConsistency.md"
    )

    foreach ($requiredDoc in $requiredDocs) {
        if (-not (Test-Path $requiredDoc)) {
            throw "Required diagnostics consistency document is missing: $requiredDoc"
        }
    }
}

Write-Host ""
Write-Host "==> Run calculation module diagnostics consistency tests" -ForegroundColor Cyan
dotnet test .\AssistantEngineer.sln --filter "CalculationModuleDiagnosticsConsistencyTests"
Write-Host "OK: Calculation module diagnostics consistency tests" -ForegroundColor Green

Write-Host ""
Write-Host "Calculation module diagnostics consistency verification completed successfully." -ForegroundColor Green
