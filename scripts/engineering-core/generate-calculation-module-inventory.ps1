param(
    [string] $SourceRoot = "src/Backend/AssistantEngineer.Modules.Calculations",
    [string] $TestsRoot = "tests/AssistantEngineer.Tests",
    [string] $OutputJsonPath = "docs/reports/calculations/CalculationModuleInventory.json",
    [string] $OutputMarkdownPath = "docs/reports/calculations/CalculationModuleInventory.md"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

if (-not (Test-Path $SourceRoot)) {
    throw "Calculations module source root not found: $SourceRoot"
}

if (-not (Test-Path $TestsRoot)) {
    throw "Tests root not found: $TestsRoot"
}

function Test-FileExists {
    param([string] $RelativePath)

    return Test-Path $RelativePath
}

function New-KeyEngineEntry {
    param(
        [string] $Name,
        [string] $Path,
        [string] $Layer,
        [string] $Purpose
    )

    return [ordered]@{
        name = $Name
        path = $Path
        layer = $Layer
        purpose = $Purpose
        exists = [bool](Test-FileExists $Path)
    }
}

$serviceFiles = @(Get-ChildItem -Path (Join-Path $SourceRoot "Application\Services") -Recurse -File -Filter "*.cs")
$contractFiles = @(Get-ChildItem -Path (Join-Path $SourceRoot "Application\Contracts") -Recurse -File -Filter "*.cs" -ErrorAction SilentlyContinue)
$abstractionFiles = @(Get-ChildItem -Path (Join-Path $SourceRoot "Application\Abstractions") -Recurse -File -Filter "*.cs" -ErrorAction SilentlyContinue)
$calculationTests = @(Get-ChildItem -Path (Join-Path $TestsRoot "Calculations") -Recurse -File -Filter "*.cs" -ErrorAction SilentlyContinue)
$parityTests = @(Get-ChildItem -Path (Join-Path $TestsRoot "Parity\EnergyCalculationParity") -Recurse -File -Filter "*.cs" -ErrorAction SilentlyContinue)

$keyEngines = [ordered]@{
    RoomLoadCalculationEngine = New-KeyEngineEntry `
        -Name "RoomLoadCalculationEngine" `
        -Path "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/RoomLoads/RoomLoadCalculationEngine.cs" `
        -Layer "Room load orchestration" `
        -Purpose "Combines room-level heating/cooling load components."

    LoadAggregationEngine = New-KeyEngineEntry `
        -Name "LoadAggregationEngine" `
        -Path "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Aggregation/LoadAggregationEngine.cs" `
        -Layer "Aggregation" `
        -Purpose "Aggregates room/floor/building load results."

    AnnualEnergyBalanceEngine = New-KeyEngineEntry `
        -Name "AnnualEnergyBalanceEngine" `
        -Path "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/AnnualEnergy/AnnualEnergyBalanceEngine.cs" `
        -Layer "Annual energy" `
        -Purpose "Calculates annual energy balance from hourly/monthly inputs."

    HourlySimulationToAnnualEnergyInputMapper = New-KeyEngineEntry `
        -Name "HourlySimulationToAnnualEnergyInputMapper" `
        -Path "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/AnnualEnergy/HourlySimulationToAnnualEnergyInputMapper.cs" `
        -Layer "Annual energy input mapping" `
        -Purpose "Maps hourly simulation records into annual energy input."

    SystemEnergyEngine = New-KeyEngineEntry `
        -Name "SystemEnergyEngine" `
        -Path "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/SystemEnergy/SystemEnergyEngine.cs" `
        -Layer "System energy" `
        -Purpose "Keeps useful, final and primary energy distinct."

    EquipmentSizingEngine = New-KeyEngineEntry `
        -Name "EquipmentSizingEngine" `
        -Path "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/EquipmentSizing/EquipmentSizingEngine.cs" `
        -Layer "Equipment sizing" `
        -Purpose "Applies capacity margin/sizing rules."

    TransmissionHeatTransferEngine = New-KeyEngineEntry `
        -Name "TransmissionHeatTransferEngine" `
        -Path "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Transmission/TransmissionHeatTransferEngine.cs" `
        -Layer "Envelope transmission" `
        -Purpose "Calculates transmission heat transfer."

    VentilationAndInfiltrationLoadEngine = New-KeyEngineEntry `
        -Name "VentilationAndInfiltrationLoadEngine" `
        -Path "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Ventilation/VentilationAndInfiltrationLoadEngine.cs" `
        -Layer "Ventilation" `
        -Purpose "Calculates sensible ventilation/infiltration loads."

    InternalGainEngine = New-KeyEngineEntry `
        -Name "InternalGainEngine" `
        -Path "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/InternalGains/InternalGainEngine.cs" `
        -Layer "Internal gains" `
        -Purpose "Calculates sensible internal gain components."

    WindowSolarGainEngine = New-KeyEngineEntry `
        -Name "WindowSolarGainEngine" `
        -Path "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/SolarGains/WindowSolarGainEngine.cs" `
        -Layer "Window solar gains" `
        -Purpose "Calculates simplified SHGC/window solar gains."

    AnnualWeatherSolarProfileBuilder = New-KeyEngineEntry `
        -Name "AnnualWeatherSolarProfileBuilder" `
        -Path "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/WeatherSolar/AnnualWeatherSolarProfileBuilder.cs" `
        -Layer "Weather/solar profile" `
        -Purpose "Builds annual solar/weather profile context."

    EnergyCalculationPipelineService = New-KeyEngineEntry `
        -Name "EnergyCalculationPipelineService" `
        -Path "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Pipeline/EnergyCalculationPipelineService.cs" `
        -Layer "Application pipeline" `
        -Purpose "Coordinates the real application calculation path."
}

$missingKeyEngines = @(
    $keyEngines.GetEnumerator() |
        Where-Object { -not $_.Value.exists } |
        ForEach-Object { $_.Key }
)

$requiredDocuments = [ordered]@{
    CalculationModuleDeepeningPlan = [ordered]@{
        path = "docs/calculations/CalculationModuleDeepeningPlan.md"
        exists = [bool](Test-FileExists "docs/calculations/CalculationModuleDeepeningPlan.md")
    }
    CalculationModuleBoundaryPolicy = [ordered]@{
        path = "docs/calculations/CalculationModuleBoundaryPolicy.md"
        exists = [bool](Test-FileExists "docs/calculations/CalculationModuleBoundaryPolicy.md")
    }
}

$inventory = [ordered]@{
    inventoryName = "Calculation Module Deepening Inventory"
    version = "v1"
    status = "DeepeningBaseline"
    generatedAtUtc = "2026-01-01 00:00:00 UTC"
    sourceRoot = $SourceRoot
    testsRoot = $TestsRoot
    totals = [ordered]@{
        serviceFiles = @($serviceFiles).Count
        contractFiles = @($contractFiles).Count
        abstractionFiles = @($abstractionFiles).Count
        calculationTests = @($calculationTests).Count
        parityTests = @($parityTests).Count
        keyEngines = @($keyEngines.Keys).Count
        missingKeyEngines = @($missingKeyEngines).Count
    }
    keyEngines = $keyEngines
    missingKeyEngines = $missingKeyEngines
    requiredDocuments = $requiredDocuments
    deepeningAxes = @(
        "Input normalization and units policy.",
        "Scenario fixtures for room, floor, building and annual-energy paths.",
        "Diagnostics consistency across all calculation engines.",
        "Cross-engine balance invariants: component sum, aggregation sum, useful/final/primary energy separation.",
        "Method strategy isolation: simplified, ISO-inspired and future external validation paths must stay explicit.",
        "No silent fallback: simplifications and adapters must be visible as diagnostics."
    )
    requiredNonClaims = @(
        "Does not claim exact EnergyPlus numerical parity.",
        "Does not claim ASHRAE 140 validation coverage.",
        "Does not claim full ISO 52016 node/matrix solver parity.",
        "Does not claim full ISO 13370 implementation.",
        "Does not claim full EN 15316 system-chain implementation."
    )
}

New-Item -ItemType Directory -Force (Split-Path $OutputJsonPath -Parent) | Out-Null
New-Item -ItemType Directory -Force (Split-Path $OutputMarkdownPath -Parent) | Out-Null

$inventory |
    ConvertTo-Json -Depth 30 |
    Set-Content $OutputJsonPath -Encoding utf8

$keyEngineRows = @(
    $keyEngines.GetEnumerator() |
        ForEach-Object {
            $engine = $_.Value
            "| $($engine.name) | $($engine.layer) | $($engine.exists) | `$($engine.path)` |"
        }
)

$missingRows = if (@($missingKeyEngines).Count -eq 0) {
    "- none"
}
else {
    @($missingKeyEngines | ForEach-Object { "- $_" }) -join "`n"
}

$axisRows = @($inventory.deepeningAxes | ForEach-Object { "- $_" })
$nonClaimRows = @($inventory.requiredNonClaims | ForEach-Object { "- $_" })

$markdown = @"
# Calculation Module Deepening Inventory

Generated at: $($inventory.generatedAtUtc)

## Status

| Field | Value |
|---|---|
| Inventory | $($inventory.inventoryName) |
| Version | $($inventory.version) |
| Status | $($inventory.status) |
| Source root | $SourceRoot |
| Tests root | $TestsRoot |
| Service files | $($inventory.totals.serviceFiles) |
| Contract files | $($inventory.totals.contractFiles) |
| Abstraction files | $($inventory.totals.abstractionFiles) |
| Calculation tests | $($inventory.totals.calculationTests) |
| Parity tests | $($inventory.totals.parityTests) |
| Key engines | $($inventory.totals.keyEngines) |
| Missing key engines | $($inventory.totals.missingKeyEngines) |

## Key engines

| Engine | Layer | Exists | Path |
|---|---|---|---|
$($keyEngineRows -join "`n")

## Missing key engines

$missingRows

## Deepening axes

$($axisRows -join "`n")

## Required non-claims

$($nonClaimRows -join "`n")

## Interpretation

This inventory is a calculation-module deepening baseline.

It does not add new physics by itself.

It defines which calculation engines and guard rails must remain visible before deeper formula changes are made.
"@

Set-Content $OutputMarkdownPath $markdown -Encoding utf8

Write-Host "Calculation module inventory generated:" -ForegroundColor Green
Write-Host "- $OutputJsonPath"
Write-Host "- $OutputMarkdownPath"
Write-Host "Key engines: $($inventory.totals.keyEngines)"
Write-Host "Missing key engines: $($inventory.totals.missingKeyEngines)"
