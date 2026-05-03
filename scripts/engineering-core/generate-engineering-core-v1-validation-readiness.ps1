param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $ToolArgs
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

dotnet run --project .\tools\AssistantEngineer.Tools.EnergyPlusValidation\AssistantEngineer.Tools.EnergyPlusValidation.csproj -- generate-validation-readiness @ToolArgs

# BEGIN AE-STAGE1-VALIDATION-READINESS-SOURCE-MARKERS
# EnergyPlusValidationCaseRegistry.json
# EngineeringCoreV1ValidationReadiness.md
# Default tolerances
# Required non-claims
# END AE-STAGE1-VALIDATION-READINESS-SOURCE-MARKERS

