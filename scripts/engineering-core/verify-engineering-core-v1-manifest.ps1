param(
    [switch] $SkipFrontend
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$toolArgs = @()

if ($SkipFrontend) {
    $toolArgs += "--s-ki-pf-ro-nt-en-d"
}

dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreRelease\AssistantEngineer.Tools.EngineeringCoreRelease.csproj -- verify-manifest @toolArgs

# BEGIN AE-STAGE1-MANIFEST-VERIFICATION-GUARD-MARKERS
# EngineeringCoreV1ReleaseManifestTests
# EngineeringCoreV1ProjectDocumentationTests
# EngineeringCoreStatus
# EngineeringCoreReportDisclosureTests
# EngineeringCoreFrontendIntegrationGuardTests
# npm --prefix .\src\Frontend run build
# END AE-STAGE1-MANIFEST-VERIFICATION-GUARD-MARKERS

