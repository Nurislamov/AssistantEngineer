param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $ToolArgs
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

dotnet run --project .\tools\AssistantEngineer.Tools.EnergyPlusValidation\AssistantEngineer.Tools.EnergyPlusValidation.csproj -- generate-validation-evidence @ToolArgs

# BEGIN AE-STAGE1-VALIDATION-EVIDENCE-SOURCE-MARKERS
# EnergyPlusValidationCaseRegistry.json
# EnergyPlusValidationFixtureCatalog.json
# EnergyPlusValidationGenericComparisonSummary.json
# EngineeringCoreV1ValidationComparisonSummary.json
# EP-SMOKE-001-RealFixtureReadiness.md
# EngineeringCoreV1ValidationReadiness.md
# EngineeringCoreV1ValidationEvidence.json
# EngineeringCoreV1ValidationEvidence.md
# PlaceholderComparison
# Future real validation must remain tolerance-based
# END AE-STAGE1-VALIDATION-EVIDENCE-SOURCE-MARKERS

