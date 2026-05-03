# new-energyplus-validation-fixture.ps1
# Scaffolds a new EnergyPlus validation fixture from repository templates.
# Keeps fixture authoring standardized and non-parity by default.
param(
    [Parameter(Mandatory = $true)]
    [string] $CaseId,

    [Parameter(Mandatory = $true)]
    [string] $Name,

    [string] $Stage = "Smoke",

    [string] $Purpose = "Prepare comparative validation fixture structure",

    [string] $WeatherSource = "Synthetic weather fixture.",

    [switch] $Force
)

$ErrorActionPreference = "Stop"

function Expand-Template {
    param(
        [Parameter(Mandatory = $true)]
        [string] $TemplatePath,

        [Parameter(Mandatory = $true)]
        [string] $DestinationPath,

        [Parameter(Mandatory = $true)]
        [hashtable] $Tokens
    )

    if (-not (Test-Path $TemplatePath)) {
        throw "Template not found: $TemplatePath"
    }

    if ((Test-Path $DestinationPath) -and -not $Force) {
        throw "Destination already exists: $DestinationPath. Use -Force to overwrite."
    }

    $content = Get-Content $TemplatePath -Raw

    foreach ($key in $Tokens.Keys) {
        $content = $content.Replace("{{$key}}", [string]$Tokens[$key])
    }

    Set-Content $DestinationPath $content -Encoding utf8
}

if ($CaseId -notmatch "^[A-Z0-9]+(-[A-Z0-9]+)*$") {
    throw "CaseId must use uppercase letters, digits and hyphens only. Example: EP-SMOKE-004."
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$templateDirectory = "docs/validation/fixtures/_template"
$fixtureDirectory = "tests/fixtures/validation/energyplus/$CaseId"
$docsDirectory = "docs/validation/fixtures/$CaseId"

if ((Test-Path $fixtureDirectory) -and -not $Force) {
    throw "Fixture directory already exists: $fixtureDirectory. Use -Force to overwrite."
}

New-Item -ItemType Directory -Force $fixtureDirectory | Out-Null
New-Item -ItemType Directory -Force $docsDirectory | Out-Null

$tokens = @{
    CASE_ID = $CaseId
    CASE_NAME = $Name
    STAGE = $Stage
    PURPOSE = $Purpose
    WEATHER_SOURCE = $WeatherSource
    GEOMETRY_DESCRIPTION = "Describe the fixture geometry."
    ENVELOPE_DESCRIPTION = "Describe the fixture envelope."
    WEATHER_PROFILE = "synthetic"
    INTERNAL_GAINS_DESCRIPTION = "Describe internal gains."
    VENTILATION_DESCRIPTION = "Describe ventilation and infiltration."
    HVAC_CONTROL_DESCRIPTION = "Describe ideal loads control."
    EXPECTED_BEHAVIOR_1 = "Describe expected engineering behavior."
    EXPECTED_BEHAVIOR_2 = "Describe expected directional response."
    EXPECTED_BEHAVIOR_3 = "Describe expected non-claim boundary."
    CALCULATION_SCOPE = $Name
    PRIMARY_METRIC_FORMULA = "Describe primary formula."
    ENERGYPLUS_VERSION = "TODO"
    OPERATING_SYSTEM = "TODO"
    RUN_DATE_UTC = "TODO"
    OUTPUT_VARIABLE_1 = "TODO"
    OUTPUT_VARIABLE_2 = "TODO"
    UNIT_CONVERSION_1 = "TODO"
}

Expand-Template "$templateDirectory/case-metadata.template.json" "$fixtureDirectory/case-metadata.json" $tokens
Expand-Template "$templateDirectory/assistantengineer-input.template.json" "$fixtureDirectory/assistantengineer-input.json" $tokens
Expand-Template "$templateDirectory/reference-output.placeholder.template.json" "$fixtureDirectory/reference-output.placeholder.json" $tokens
Expand-Template "$templateDirectory/comparison-tolerances.template.json" "$fixtureDirectory/comparison-tolerances.json" $tokens
Expand-Template "$templateDirectory/README.template.md" "$docsDirectory/README.md" $tokens

Write-Host "Validation fixture scaffold created:" -ForegroundColor Green
Write-Host "- $fixtureDirectory"
Write-Host "- $docsDirectory"
Write-Host ""
Write-Host "Next steps:"
Write-Host "1. Edit generated JSON values and tolerances."
Write-Host "2. Add the case to docs/validation/EnergyPlusValidationCaseRegistry.json."
Write-Host "3. Run .\scripts\engineering-core\compare-energyplus-validation-fixtures.ps1"
Write-Host "4. Run .\scripts\engineering-core\generate-energyplus-validation-fixture-catalog.ps1"

