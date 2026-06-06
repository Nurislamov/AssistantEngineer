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

Write-Host "PASS"
Write-Host "Readiness report: artifacts/verification/branch-readiness/branch-readiness-report.json"
Write-Host "PR body: artifacts/verification/branch-readiness/pr-body.md"
Write-Host "Next: git add <reviewed-files>; git commit -m `"<scope-specific verified change>`"; git push -u origin HEAD"
exit 0
