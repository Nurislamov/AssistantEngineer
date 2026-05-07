param(
    [switch] $SkipFrontend,
    [switch] $SkipFullDotnet,
    [switch] $SkipGitStatus,
    [switch] $Fast
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

if (-not $SkipFrontend -and -not (Get-Command npm -ErrorAction SilentlyContinue)) {
    throw "npm was not found on PATH. Install Node.js locally or add actions/setup-node before running Engineering Core V1 release readiness checks."
}

if ($SkipFrontend) {
    Write-Warning "SkipFrontend override is enabled. Frontend build/type checks are intentionally skipped."
}
else {
    Write-Host "Frontend checks are enabled by default."
}

$toolArgs = @()

if ($SkipFrontend) {
    $toolArgs += "--skip-frontend"
}

if ($SkipFullDotnet) {
    $toolArgs += "--skip-full-dotnet"
}

if ($SkipGitStatus) {
    $toolArgs += "--skip-git-status"
}

if ($Fast) {
    $toolArgs += "--fast"
}

dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreRelease\AssistantEngineer.Tools.EngineeringCoreRelease.csproj -- assert-release-ready @toolArgs

# BEGIN AE-STAGE1-RELEASE-READY-GUARD-MARKERS
# regenerate-engineering-core-v1-artifacts.ps1
# verify-engineering-core-v1-smoke.ps1
# verify-engineering-core-v1-contracts.ps1
# verify-engineering-core-v1-manifest.ps1
# verify-engineering-core-v1.ps1
# dotnet test .\AssistantEngineer.sln
# git status --short
# [switch] 
# [switch] 
# [switch] 
# [switch] 
# EngineeringCoreV1Manifest.json
# EngineeringCoreV1ReleaseManifest.md
# EngineeringCoreV1ReleaseChecklist.md
# EngineeringCoreV1OwnerHandoff.md
# EngineeringCoreV1ReleaseEvidence.md
# EngineeringCoreV1TraceabilityMatrix.json
# EngineeringCoreV1DiagnosticsCatalog.json
# status.sample.json
# diagnostics-catalog.sample.json
# heating-report.sample.json
# cooling-report.sample.json
# annual-energy-disclosure.sample.json
# EnergyPlusValidationCaseRegistry.json
# EngineeringCoreV1ValidationReadiness.md
# engineering-core-v1.yml
# END AE-STAGE1-RELEASE-READY-GUARD-MARKERS

