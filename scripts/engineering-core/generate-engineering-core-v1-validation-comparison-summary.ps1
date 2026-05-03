param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $ToolArgs
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

dotnet run --project .\tools\AssistantEngineer.Tools.EnergyPlusValidation\AssistantEngineer.Tools.EnergyPlusValidation.csproj -- generate-comparison-summary @ToolArgs

# BEGIN AE-STAGE1-VALIDATION-COMPARISON-SUMMARY-SOURCE-MARKERS
# EnergyPlusValidationCaseRegistry.json
# EP-SMOKE-*-ComparisonResult.json
# EngineeringCoreV1ValidationComparisonSummary.json
# EngineeringCoreV1ValidationComparisonSummary.md
# PlaceholderComparison
# END AE-STAGE1-VALIDATION-COMPARISON-SUMMARY-SOURCE-MARKERS

