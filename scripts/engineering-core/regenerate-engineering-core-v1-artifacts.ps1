param(
    [switch] $SkipMissing
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$toolArgs = @()

if ($SkipMissing) {
    $toolArgs += "--s-ki-pm-is-si-ng"
}

dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreRelease\AssistantEngineer.Tools.EngineeringCoreRelease.csproj -- regenerate-artifacts @toolArgs

# BEGIN AE-STAGE1-REGENERATE-ARTIFACT-MARKERS
# generate-engineering-core-v1-release-evidence.ps1
# generate-engineering-core-v1-api-contract-snapshots.ps1
# generate-engineering-core-v1-report-contract-snapshots.ps1
# generate-engineering-core-v1-export-disclosure-checklist.ps1
# generate-engineering-core-v1-validation-readiness.ps1
# generate-engineering-core-v1-traceability-matrix.ps1
# compare-energyplus-validation-fixtures.ps1
# generate-energyplus-validation-fixture-catalog.ps1
# END AE-STAGE1-REGENERATE-ARTIFACT-MARKERS

