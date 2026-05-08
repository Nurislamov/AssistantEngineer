param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $ToolArgs
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

dotnet run --project .\tools\AssistantEngineer.Tools.EnergyPlusValidation\AssistantEngineer.Tools.EnergyPlusValidation.csproj -- generate-smoke001-comparison-readiness @ToolArgs

# BEGIN AE-STAGE1-EP-SMOKE-001-READINESS-SOURCE-MARKERS
# case-metadata.json
# assistantengineer-input.json
# reference-output.placeholder.json
# comparison-tolerances.json
# EP-SMOKE-001-ComparisonReadiness.md
# END AE-STAGE1-EP-SMOKE-001-READINESS-SOURCE-MARKERS


# BEGIN AE-EP-SMOKE-001-COMPARISON-READINESS-GUARD-MARKERS
# ReferenceFixturePlaceholder
# PlaceholderReferenceOutput
# annual-heating-kwh
# peak-heating-w
# not a real EnergyPlus comparison yet
# must not claim exact EnergyPlus comparison workflow
# END AE-EP-SMOKE-001-COMPARISON-READINESS-GUARD-MARKERS