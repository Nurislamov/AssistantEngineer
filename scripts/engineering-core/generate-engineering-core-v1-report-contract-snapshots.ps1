param(
    [string] $OutputDirectory = "docs/reports/engineering-core-v1"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

New-Item -ItemType Directory -Force $OutputDirectory | Out-Null

$documentationFiles = @(
    "docs/calculations/EngineeringCoreV1Scope.md",
    "docs/calculations/EngineeringCoreV1ReleaseNotes.md",
    "docs/calculations/EnergyPlusAshrae140ValidationPlan.md"
)

$explicitNonClaims = @(
    "No exact pyBuildingEnergy numerical parity claim.",
    "No exact EnergyPlus numerical parity claim.",
    "No ASHRAE 140 validation coverage claim.",
    "No full ISO 52016 node/matrix solver parity claim.",
    "No full ISO 52010 climate conversion parity claim.",
    "No full ISO 13370 implementation claim.",
    "No full EN 15316 generation/distribution/storage/emission chain claim.",
    "No full coupled multi-zone heat-balance simulation claim.",
    "No latent/moisture/humidity calculation claim."
)

$outOfScopeV1 = @(
    "HVAC.LATENT_LOAD",
    "HVAC.MOISTURE_BALANCE",
    "Humidification/dehumidification conditions",
    "Detailed psychrometric supply-air treatment",
    "Detailed HVAC plant simulation"
)

$heatingDisclosure = [ordered]@{
    coreStatus = "ClosedV1"
    calculationScope = "Engineering-core v1 heating design-point report."
    calculationMethod = "EngineeringCoreV1.DesignPointHeating"
    actualMethod = "EngineeringCoreV1.DesignPointHeating"
    warnings = @(
        "Heating report uses engineering design-point load calculation.",
        "Report does not claim full ISO 52016 node/matrix solver parity.",
        "Report does not claim exact EnergyPlus, ASHRAE 140 or pyBuildingEnergy numerical parity.",
        "Latent load, moisture balance and detailed psychrometrics are out of scope for engineering-core v1."
    )
    assumptions = @(
        "Heating load is assembled from transmission and ventilation/infiltration components.",
        "Transmission uses steady-state U*A*ΔT component heat transfer.",
        "Ventilation and infiltration use sensible-only airflow heat transfer.",
        "Ground and adjacent boundaries are simplified engineering models when present."
    )
    explicitNonClaims = $explicitNonClaims
    outOfScopeV1 = $outOfScopeV1
    documentationFiles = $documentationFiles
}

$coolingDisclosure = [ordered]@{
    coreStatus = "ClosedV1"
    calculationScope = "Engineering-core v1 cooling design-point report."
    calculationMethod = "EngineeringCoreV1.DesignPointCooling"
    actualMethod = "EngineeringCoreV1.DesignPointCooling"
    warnings = @(
        "Cooling report uses engineering design-point load calculation.",
        "Report does not claim full ISO 52016 node/matrix solver parity.",
        "Report does not claim exact EnergyPlus, ASHRAE 140 or pyBuildingEnergy numerical parity.",
        "Latent load, moisture balance and detailed psychrometrics are out of scope for engineering-core v1."
    )
    assumptions = @(
        "Cooling load is assembled from transmission, ventilation, infiltration, solar and internal gain components.",
        "Window solar gains use simplified SHGC/shading based engineering model.",
        "Surface irradiance uses ISO52010-inspired solar geometry and isotropic sky transposition.",
        "Equipment selection, when requested, is capacity-margin based and does not model part-load curves."
    )
    explicitNonClaims = $explicitNonClaims
    outOfScopeV1 = $outOfScopeV1
    documentationFiles = $documentationFiles
}

$annualEnergyDisclosure = [ordered]@{
    coreStatus = "ClosedV1"
    calculationScope = "Engineering-core v1 hourly annual energy integration report."
    calculationMethod = "TrueHourlySimulation"
    actualMethod = "EngineeringCoreV1.TrueHourly8760"
    warnings = @(
        "Annual energy is true hourly 8760 only when EnergyDataSource=TrueHourlySimulation, IsTrueHourly8760=true and HourlyRecordCount=8760.",
        "Monthly adapter, synthetic weather and deterministic short fixtures must not be presented as true hourly 8760 annual simulation.",
        "Report does not claim exact EnergyPlus, ASHRAE 140 or pyBuildingEnergy numerical parity."
    )
    assumptions = @(
        "Annual energy is calculated as sum of hourly W*h divided by 1000.",
        "Monthly totals are aggregated from hourly records.",
        "EPW and PVGIS import gates normalize weather to 8760 hourly records."
    )
    explicitNonClaims = $explicitNonClaims
    outOfScopeV1 = $outOfScopeV1
    documentationFiles = $documentationFiles
}

$heatingReport = [ordered]@{
    projectName = "Engineering Core V1 Sample"
    buildingName = "Heating Sample Building"
    calculationMethod = "EngineeringCoreV1.DesignPointHeating"
    generatedAtUtc = "2026-01-01T00:00:00Z"
    outdoorDesignTemperatureC = -10
    indoorDesignTemperatureC = 20
    roomsCount = 2
    totalTransmissionLossW = 3200
    totalVentilationLossW = 850
    totalDesignHeatingLoadW = 4050
    totalDesignHeatingLoadKw = 4.05
    calculationDisclosure = $heatingDisclosure
    rooms = @(
        [ordered]@{
            roomId = 101
            roomName = "Office 101"
            transmissionHeatLossW = 1800
            ventilationHeatLossW = 450
            totalDesignHeatingLoadW = 2250
        },
        [ordered]@{
            roomId = 102
            roomName = "Office 102"
            transmissionHeatLossW = 1400
            ventilationHeatLossW = 400
            totalDesignHeatingLoadW = 1800
        }
    )
}

$coolingReport = [ordered]@{
    projectName = "Engineering Core V1 Sample"
    buildingName = "Cooling Sample Building"
    calculationMethod = "EngineeringCoreV1.DesignPointCooling"
    peakHourOfYear = 5200
    generatedAtUtc = "2026-01-01T00:00:00Z"
    floorsCount = 1
    roomsCount = 2
    designReserveFactor = 1.15
    designCapacityW = 6900
    designCapacityKw = 6.9
    coolingLoadW = 6000
    coolingLoadKw = 6.0
    calculationDisclosure = $coolingDisclosure
    floorSummaries = @(
        [ordered]@{
            floorId = 1
            floorName = "Level 1"
            roomsCount = 2
            coolingLoadW = 6000
            coolingLoadKw = 6.0
        }
    )
    rooms = @(
        [ordered]@{
            roomId = 201
            roomName = "Meeting Room"
            coolingLoadW = 3600
            coolingLoadKw = 3.6
            windowHeatGainW = 900
            wallHeatGainW = 700
            internalHeatGainW = 1200
        },
        [ordered]@{
            roomId = 202
            roomName = "Open Office"
            coolingLoadW = 2400
            coolingLoadKw = 2.4
            windowHeatGainW = 550
            wallHeatGainW = 500
            internalHeatGainW = 850
        }
    )
}

$annualEnergyReport = [ordered]@{
    projectName = "Engineering Core V1 Sample"
    buildingName = "Annual Energy Sample Building"
    energyDataSource = "TrueHourlySimulation"
    isTrueHourly8760 = $true
    hourlyRecordCount = 8760
    annualHeatingKwh = 18000
    annualCoolingKwh = 6200
    annualTotalKwh = 24200
    euiKwhPerM2 = 121
    calculationDisclosure = $annualEnergyDisclosure
    requiredAnnual8760Flags = @(
        "EnergyDataSource = TrueHourlySimulation",
        "IsTrueHourly8760 = true",
        "HourlyRecordCount = 8760"
    )
}

$heatingPath = Join-Path $OutputDirectory "heating-report.sample.json"
$coolingPath = Join-Path $OutputDirectory "cooling-report.sample.json"
$annualPath = Join-Path $OutputDirectory "annual-energy-disclosure.sample.json"

$heatingReport | ConvertTo-Json -Depth 20 | Set-Content $heatingPath -Encoding utf8
$coolingReport | ConvertTo-Json -Depth 20 | Set-Content $coolingPath -Encoding utf8
$annualEnergyReport | ConvertTo-Json -Depth 20 | Set-Content $annualPath -Encoding utf8

Write-Host "Engineering Core V1 report contract snapshots generated:" -ForegroundColor Green
Write-Host "- $heatingPath"
Write-Host "- $coolingPath"
Write-Host "- $annualPath"
