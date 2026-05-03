# new-energyplus-validation-fixture.ps1
# Thin wrapper over the C# EnergyPlus fixture authoring tool.
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

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$toolArgs = @(
    "new-fixture",
    "--case-id",
    $CaseId,
    "--name",
    $Name,
    "--stage",
    $Stage,
    "--purpose",
    $Purpose,
    "--weather-source",
    $WeatherSource
)

if ($Force) {
    $toolArgs += "--force"
}

dotnet run --project .\tools\AssistantEngineer.Tools.EnergyPlusFixtureAuthoring\AssistantEngineer.Tools.EnergyPlusFixtureAuthoring.csproj -- @toolArgs
