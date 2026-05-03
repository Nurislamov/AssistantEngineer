param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $ToolArgs
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

dotnet run --project .\tools\AssistantEngineer.Tools.EnergyPlusValidation\AssistantEngineer.Tools.EnergyPlusValidation.csproj -- compare-fixtures @ToolArgs

# BEGIN AE-STAGE1-GENERIC-VALIDATION-RUNNER-SOURCE-MARKERS
# tests/fixtures/validation/energyplus
# comparison-tolerances.json
# assistantengineer-input.json
# reference-output.placeholder.json
# energyplus-output.reference.json
# RequireRealReferences
# GenericEnergyPlusValidationFixtureRunner
# PlaceholderComparison
# RealEnergyPlusComparison
# NumericWithinTolerance
# SameSign
# DirectionalTrend
# EnergyPlusValidationGenericComparisonSummary.json
# EnergyPlusValidationGenericComparisonSummary.md
# END AE-STAGE1-GENERIC-VALIDATION-RUNNER-SOURCE-MARKERS

