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

dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreRelease\AssistantEngineer.Tools.EngineeringCoreRelease.csproj -- verify-smoke @toolArgs

# BEGIN AE-STAGE1-SMOKE-VERIFICATION-GUARD-MARKERS
# SkipFrontend
# npm --prefix .\src\Frontend run build
# FormulaAudit
# EngineeringCoreStatus
# EngineeringCoreReportDisclosureTests
# EngineeringCoreDiagnosticsCatalogFacadeAndApiTests
# EngineeringCoreFrontendIntegrationGuardTests
# AnnualEnergy8760ScenarioTests
# END AE-STAGE1-SMOKE-VERIFICATION-GUARD-MARKERS

