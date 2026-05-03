param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $ToolArgs
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

dotnet run --project .\tools\AssistantEngineer.Tools.EnergyPlusValidation\AssistantEngineer.Tools.EnergyPlusValidation.csproj -- assert-smoke001-real-fixture-ready @ToolArgs

# BEGIN AE-STAGE1-REAL-FIXTURE-INTAKE-SOURCE-MARKERS
# [switch] 
# case-metadata.json
# assistantengineer-input.json
# reference-output.placeholder.json
# comparison-tolerances.json
# energyplus-model.idf
# weather.epw
# energyplus-output.raw.csv
# energyplus-output.reference.json
# provenance.json
# NotReadyRealFixtureMissingFiles
# ReadyForRealComparison
# END AE-STAGE1-REAL-FIXTURE-INTAKE-SOURCE-MARKERS


# EnergyPlusRealFixtureIntakeGateTests guard marker
# [switch] $RequireRealFixture
