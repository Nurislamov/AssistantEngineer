param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $ToolArgs
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

dotnet run --project .\tools\AssistantEngineer.Tools.EnergyPlusValidation\AssistantEngineer.Tools.EnergyPlusValidation.csproj -- verify-validation @ToolArgs

# BEGIN AE-STAGE1-VALIDATION-PROFILE-GUARD-MARKERS
# SkipRegenerate
# RequireRealReferences
# regenerate-engineering-core-v1-validation-artifacts.ps1
# EnergyPlusValidationCaseRegistryTests
# EnergyPlusValidation
# EnergyPlusSmoke001FixtureScaffoldTests
# EnergyPlusSmoke001ComparisonHarnessTests
# EnergyPlusSmoke002And003FixtureScaffoldTests
# EnergyPlusValidationGenericComparisonRunnerTests
# EnergyPlusValidationComparisonSummaryTests
# EnergyPlusRealFixtureIntakeGateTests
# EnergyPlusValidationFixtureCatalogTests
# EnergyPlusValidationFixtureAuthoringKitTests
# END AE-STAGE1-VALIDATION-PROFILE-GUARD-MARKERS

