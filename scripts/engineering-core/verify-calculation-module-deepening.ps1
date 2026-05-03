param(
    [switch] $SkipGenerate
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

Write-Host "Calculation module deepening verification" -ForegroundColor Cyan
Write-Host "Repository: $repoRoot"

if (-not $SkipGenerate) {
    Write-Host ""
    Write-Host "==> Generate calculation module inventory" -ForegroundColor Cyan
    .\scripts\engineering-core\generate-calculation-module-inventory.ps1
    Write-Host "OK: Generate calculation module inventory" -ForegroundColor Green
}

Write-Host ""
Write-Host "==> Run calculation module deepening guard tests" -ForegroundColor Cyan
dotnet test .\AssistantEngineer.sln --filter "CalculationModuleDeepeningGuardTests"
Write-Host "OK: Calculation module deepening guard tests" -ForegroundColor Green

Write-Host ""
Write-Host "Calculation module deepening verification completed successfully." -ForegroundColor Green
