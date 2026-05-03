param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $ToolArgs
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

dotnet run --project .\tools\AssistantEngineer.Tools.EnergyPlusValidation\AssistantEngineer.Tools.EnergyPlusValidation.csproj -- compare-fixtures @ToolArgs

# BEGIN AE-STAGE1-EP-SMOKE-001-COMPARISON-SOURCE-MARKERS
# case-metadata.json
# assistantengineer-input.json
# reference-output.placeholder.json
# comparison-tolerances.json
# EP-SMOKE-001-ComparisonResult.json
# EP-SMOKE-001-ComparisonResult.md
# PlaceholderComparison
# NumericWithinTolerance
# SameSign
# END AE-STAGE1-EP-SMOKE-001-COMPARISON-SOURCE-MARKERS

