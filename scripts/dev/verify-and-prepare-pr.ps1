param(
    [string]$BaseRef = "origin/master",
    [string]$Scope = "EquipmentDiagnostics"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

& .\scripts\dev\verify-branch-readiness.ps1 -BaseRef $BaseRef -Scope $Scope
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& .\scripts\dev\prepare-pr-body.ps1
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if ($Scope -eq "EquipmentDiagnostics") {
    & .\scripts\equipment-diagnostics\prepare-beta-readiness-report.ps1 -BaseRef $BaseRef
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

Write-Host "PASS"
Write-Host "Readiness report: artifacts/verification/branch-readiness/branch-readiness-report.json"
Write-Host "PR body: artifacts/verification/branch-readiness/pr-body.md"
if ($Scope -eq "EquipmentDiagnostics") {
    Write-Host "Beta report: artifacts/verification/equipment-diagnostics/beta-readiness-report.json"
    Write-Host "Beta summary: artifacts/verification/equipment-diagnostics/beta-readiness-summary.md"
}
Write-Host "Next: git add <reviewed-files>; git commit -m `"<scope-specific verified change>`"; git push -u origin HEAD"
exit 0
