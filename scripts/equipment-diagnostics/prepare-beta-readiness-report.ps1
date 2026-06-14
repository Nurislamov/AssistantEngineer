param(
    [string]$BaseRef = "origin/master",
    [switch]$AllowWarningsOnly
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$arguments = @(
    "run",
    "--project", "tools/AssistantEngineer.Tools.EquipmentDiagnosticsVerification",
    "--",
    "beta-readiness",
    "--repo-root", ".",
    "--base-ref", $BaseRef,
    "--report", "artifacts/verification/branch-readiness/branch-readiness-report.json",
    "--output", "artifacts/verification/equipment-diagnostics/beta-readiness-report.json",
    "--markdown-output", "artifacts/verification/equipment-diagnostics/beta-readiness-summary.md"
)

& dotnet @arguments
$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
    exit $exitCode
}

exit 0
