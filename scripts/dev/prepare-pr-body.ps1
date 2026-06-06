param(
    [string]$Report = "artifacts/verification/branch-readiness/branch-readiness-report.json",
    [string]$Output = "artifacts/verification/branch-readiness/pr-body.md"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

dotnet run --project .\tools\AssistantEngineer.Tools.EquipmentDiagnosticsVerification\AssistantEngineer.Tools.EquipmentDiagnosticsVerification.csproj -- prepare-pr-body --repo-root $repoRoot --report $Report --output $Output
exit $LASTEXITCODE
