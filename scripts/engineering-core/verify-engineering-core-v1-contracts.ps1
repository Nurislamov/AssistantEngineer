param(
    [switch] $SkipFrontend,
    [switch] $SkipRegenerate
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$toolArgs = @()

if ($SkipFrontend) {
    $toolArgs += "--s-ki-pf-ro-nt-en-d"
}
if ($SkipRegenerate) {
    $toolArgs += "--s-ki-pr-eg-en-er-at-e"
}

dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreRelease\AssistantEngineer.Tools.EngineeringCoreRelease.csproj -- verify-contracts @toolArgs

# BEGIN AE-STAGE1-CONTRACTS-VERIFICATION-GUARD-MARKERS
# SkipFrontend
# SkipRegenerate
# regenerate-engineering-core-v1-artifacts.ps1
# EngineeringCoreV1ApiContractSnapshotTests
# EngineeringCoreV1OpenApiContractTests
# EngineeringCoreV1ReportContractSnapshotTests
# EngineeringCoreV1ReportExportDisclosureGuardTests
# EngineeringCoreV1ReleaseEvidencePackageTests
# EngineeringCoreV1TraceabilityMatrixTests
# EnergyPlusValidationCaseRegistryTests
# END AE-STAGE1-CONTRACTS-VERIFICATION-GUARD-MARKERS

