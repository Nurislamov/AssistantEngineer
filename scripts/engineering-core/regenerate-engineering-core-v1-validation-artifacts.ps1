param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $ToolArgs
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

dotnet run --project .\tools\AssistantEngineer.Tools.EnergyPlusValidation\AssistantEngineer.Tools.EnergyPlusValidation.csproj -- regenerate-validation-artifacts @ToolArgs

# BEGIN AE-STAGE1-VALIDATION-REGENERATE-MARKERS
# generate-engineering-core-v1-validation-readiness.ps1
# generate-ep-smoke-001-comparison-readiness.ps1
# compare-ep-smoke-001-placeholder.ps1
# compare-energyplus-validation-fixtures.ps1
# generate-engineering-core-v1-validation-comparison-summary.ps1
# assert-ep-smoke-001-real-fixture-ready.ps1
# generate-energyplus-validation-fixture-catalog.ps1
# generate-engineering-core-v1-validation-evidence.ps1
# RequireRealReferences
# END AE-STAGE1-VALIDATION-REGENERATE-MARKERS

