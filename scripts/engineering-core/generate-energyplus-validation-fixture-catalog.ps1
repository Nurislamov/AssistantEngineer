param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $ToolArgs
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

dotnet run --project .\tools\AssistantEngineer.Tools.EnergyPlusValidation\AssistantEngineer.Tools.EnergyPlusValidation.csproj -- generate-fixture-catalog @ToolArgs

# BEGIN AE-STAGE1-FIXTURE-CATALOG-SOURCE-MARKERS
# EnergyPlusValidationCaseRegistry.json
# tests/fixtures/validation/energyplus
# ComparisonResult.json
# EnergyPlusValidationFixtureCatalog.json
# EnergyPlusValidationFixtureCatalog.md
# END AE-STAGE1-FIXTURE-CATALOG-SOURCE-MARKERS

