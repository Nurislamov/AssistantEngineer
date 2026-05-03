param(
    [Parameter(Mandatory = $true)]
    [string] $SourceDirectory,

    [Parameter(Mandatory = $true)]
    [string] $EnergyPlusVersion,

    [string] $CaseId = "EP-SMOKE-001",

    [string] $FixtureDirectory = "tests/fixtures/validation/energyplus/EP-SMOKE-001",

    [string] $IdfFileName = "energyplus-model.idf",

    [string] $WeatherFileName = "weather.epw",

    [string] $RawCsvFileName = "energyplus-output.raw.csv",

    [double] $AnnualHeatingEnergyKwh = [double]::NaN,

    [double] $PeakHeatingLoadW = [double]::NaN,

    [double] $AnnualCoolingEnergyKwh = [double]::NaN,

    [string] $HeatingEnergyColumn = "",

    [string] $HeatingLoadColumn = "",

    [string] $CoolingEnergyColumn = "",

    [string] $Notes = "Imported real EnergyPlus fixture for EP-SMOKE-001.",

    [switch] $SkipValidation
)

$ErrorActionPreference = "Stop"

function Resolve-RequiredPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $true)]
        [string] $Description
    )

    if (-not (Test-Path $Path)) {
        throw "$Description not found: $Path"
    }

    return (Resolve-Path $Path).Path
}

function Copy-RequiredFile {
    param(
        [Parameter(Mandatory = $true)]
        [string] $SourcePath,

        [Parameter(Mandatory = $true)]
        [string] $DestinationPath,

        [Parameter(Mandatory = $true)]
        [string] $Description
    )

    Resolve-RequiredPath -Path $SourcePath -Description $Description | Out-Null

    $destinationDirectory = Split-Path $DestinationPath -Parent
    New-Item -ItemType Directory -Force $destinationDirectory | Out-Null

    Copy-Item -Path $SourcePath -Destination $DestinationPath -Force
}

function Get-FirstMatchingColumn {
    param(
        [Parameter(Mandatory = $true)]
        [object] $Row,

        [Parameter(Mandatory = $true)]
        [string[]] $Patterns
    )

    $properties = @($Row.PSObject.Properties.Name)

    foreach ($pattern in $Patterns) {
        $match = $properties | Where-Object { $_ -match $pattern } | Select-Object -First 1
        if (-not [string]::IsNullOrWhiteSpace($match)) {
            return $match
        }
    }

    return ""
}

function Convert-ColumnValuesToDouble {
    param(
        [Parameter(Mandatory = $true)]
        [object[]] $Rows,

        [Parameter(Mandatory = $true)]
        [string] $ColumnName
    )

    if ([string]::IsNullOrWhiteSpace($ColumnName)) {
        throw "Column name is empty."
    }

    $values = @()

    foreach ($row in $Rows) {
        $property = $row.PSObject.Properties[$ColumnName]
        if ($null -eq $property) {
            continue
        }

        $raw = [string]$property.Value
        if ([string]::IsNullOrWhiteSpace($raw)) {
            continue
        }

        $normalized = $raw.Trim().Replace(",", ".")

        $parsed = 0.0
        if ([double]::TryParse(
            $normalized,
            [System.Globalization.NumberStyles]::Float,
            [System.Globalization.CultureInfo]::InvariantCulture,
            [ref]$parsed)) {
            $values += $parsed
        }
    }

    if ($values.Count -eq 0) {
        throw "No numeric values found in column '$ColumnName'."
    }

    return $values
}

function Convert-EnergySeriesToKwh {
    param(
        [Parameter(Mandatory = $true)]
        [double[]] $Values,

        [Parameter(Mandatory = $true)]
        [string] $ColumnName
    )

    $sum = ($Values | Measure-Object -Sum).Sum

    if ($ColumnName -match "\[J\]" -or $ColumnName -match "\(J\)") {
        return $sum / 3600000.0
    }

    if ($ColumnName -match "\[Wh\]" -or $ColumnName -match "\(Wh\)") {
        return $sum / 1000.0
    }

    if ($ColumnName -match "\[kWh\]" -or $ColumnName -match "\(kWh\)") {
        return $sum
    }

    throw "Cannot infer energy unit for '$ColumnName'. Rename/pass explicit kWh values or use a column with [J], [Wh], or [kWh]."
}

function Convert-LoadSeriesToW {
    param(
        [Parameter(Mandatory = $true)]
        [double[]] $Values,

        [Parameter(Mandatory = $true)]
        [string] $ColumnName
    )

    $max = ($Values | Measure-Object -Maximum).Maximum

    if ($ColumnName -match "\[W\]" -or $ColumnName -match "\(W\)") {
        return $max
    }

    if ($ColumnName -match "\[kW\]" -or $ColumnName -match "\(kW\)") {
        return $max * 1000.0
    }

    throw "Cannot infer load unit for '$ColumnName'. Rename/pass explicit peak W value or use a column with [W] or [kW]."
}

function Get-ReferenceValuesFromCsv {
    param(
        [Parameter(Mandatory = $true)]
        [string] $CsvPath,

        [double] $AnnualHeatingEnergyKwh,

        [double] $PeakHeatingLoadW,

        [double] $AnnualCoolingEnergyKwh,

        [string] $HeatingEnergyColumn,

        [string] $HeatingLoadColumn,

        [string] $CoolingEnergyColumn
    )

    $rows = @(Import-Csv $CsvPath)

    if ($rows.Count -eq 0) {
        throw "Raw EnergyPlus CSV has no rows: $CsvPath"
    }

    $firstRow = $rows[0]

    if ([double]::IsNaN($AnnualHeatingEnergyKwh)) {
        if ([string]::IsNullOrWhiteSpace($HeatingEnergyColumn)) {
            $HeatingEnergyColumn = Get-FirstMatchingColumn -Row $firstRow -Patterns @(
                "DistrictHeating.*\[J\]",
                "Heating.*Energy.*\[J\]",
                "Heating.*Energy.*\[Wh\]",
                "Heating.*Energy.*\[kWh\]",
                "Zone.*Heating.*Energy"
            )
        }

        if ([string]::IsNullOrWhiteSpace($HeatingEnergyColumn)) {
            throw "Could not auto-detect heating energy column. Pass -HeatingEnergyColumn or -AnnualHeatingEnergyKwh."
        }

        $values = Convert-ColumnValuesToDouble -Rows $rows -ColumnName $HeatingEnergyColumn
        $AnnualHeatingEnergyKwh = Convert-EnergySeriesToKwh -Values $values -ColumnName $HeatingEnergyColumn
    }

    if ([double]::IsNaN($PeakHeatingLoadW)) {
        if ([string]::IsNullOrWhiteSpace($HeatingLoadColumn)) {
            $HeatingLoadColumn = Get-FirstMatchingColumn -Row $firstRow -Patterns @(
                "Heating.*Rate.*\[W\]",
                "Heating.*Load.*\[W\]",
                "Zone.*Heating.*Rate",
                "Ideal Loads.*Heating.*Rate"
            )
        }

        if ([string]::IsNullOrWhiteSpace($HeatingLoadColumn)) {
            throw "Could not auto-detect peak heating load column. Pass -HeatingLoadColumn or -PeakHeatingLoadW."
        }

        $values = Convert-ColumnValuesToDouble -Rows $rows -ColumnName $HeatingLoadColumn
        $PeakHeatingLoadW = Convert-LoadSeriesToW -Values $values -ColumnName $HeatingLoadColumn
    }

    if ([double]::IsNaN($AnnualCoolingEnergyKwh)) {
        if ([string]::IsNullOrWhiteSpace($CoolingEnergyColumn)) {
            $CoolingEnergyColumn = Get-FirstMatchingColumn -Row $firstRow -Patterns @(
                "DistrictCooling.*\[J\]",
                "Cooling.*Energy.*\[J\]",
                "Cooling.*Energy.*\[Wh\]",
                "Cooling.*Energy.*\[kWh\]",
                "Zone.*Cooling.*Energy"
            )
        }

        if ([string]::IsNullOrWhiteSpace($CoolingEnergyColumn)) {
            $AnnualCoolingEnergyKwh = 0.0
        }
        else {
            $values = Convert-ColumnValuesToDouble -Rows $rows -ColumnName $CoolingEnergyColumn
            $AnnualCoolingEnergyKwh = Convert-EnergySeriesToKwh -Values $values -ColumnName $CoolingEnergyColumn
        }
    }

    return [ordered]@{
        annualHeatingEnergyKwh = [Math]::Round($AnnualHeatingEnergyKwh, 6)
        peakHeatingLoadW = [Math]::Round($PeakHeatingLoadW, 6)
        annualCoolingEnergyKwh = [Math]::Round($AnnualCoolingEnergyKwh, 6)
        detectedColumns = [ordered]@{
            heatingEnergyColumn = $HeatingEnergyColumn
            heatingLoadColumn = $HeatingLoadColumn
            coolingEnergyColumn = $CoolingEnergyColumn
        }
    }
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$SourceDirectory = Resolve-RequiredPath -Path $SourceDirectory -Description "EnergyPlus artifact source directory"

$sourceIdfPath = Join-Path $SourceDirectory $IdfFileName
$sourceWeatherPath = Join-Path $SourceDirectory $WeatherFileName
$sourceRawCsvPath = Join-Path $SourceDirectory $RawCsvFileName

$destinationIdfPath = Join-Path $FixtureDirectory "energyplus-model.idf"
$destinationWeatherPath = Join-Path $FixtureDirectory "weather.epw"
$destinationRawCsvPath = Join-Path $FixtureDirectory "energyplus-output.raw.csv"
$destinationReferenceJsonPath = Join-Path $FixtureDirectory "energyplus-output.reference.json"
$destinationProvenancePath = Join-Path $FixtureDirectory "provenance.json"

Copy-RequiredFile -SourcePath $sourceIdfPath -DestinationPath $destinationIdfPath -Description "EnergyPlus IDF model"
Copy-RequiredFile -SourcePath $sourceWeatherPath -DestinationPath $destinationWeatherPath -Description "EnergyPlus EPW weather"
Copy-RequiredFile -SourcePath $sourceRawCsvPath -DestinationPath $destinationRawCsvPath -Description "EnergyPlus raw CSV output"

$referenceValues = Get-ReferenceValuesFromCsv `
    -CsvPath $destinationRawCsvPath `
    -AnnualHeatingEnergyKwh $AnnualHeatingEnergyKwh `
    -PeakHeatingLoadW $PeakHeatingLoadW `
    -AnnualCoolingEnergyKwh $AnnualCoolingEnergyKwh `
    -HeatingEnergyColumn $HeatingEnergyColumn `
    -HeatingLoadColumn $HeatingLoadColumn `
    -CoolingEnergyColumn $CoolingEnergyColumn

$reference = [ordered]@{
    caseId = $CaseId
    engine = "EnergyPlus"
    referenceStatus = "RealEnergyPlusReferenceOutput"
    generatedAtUtc = "2026-01-01 00:00:00 UTC"
    referenceOutputs = [ordered]@{
        annualHeatingEnergyKwh = $referenceValues.annualHeatingEnergyKwh
        peakHeatingLoadW = $referenceValues.peakHeatingLoadW
        annualCoolingEnergyKwh = $referenceValues.annualCoolingEnergyKwh
    }
    sourceFiles = [ordered]@{
        idf = "tests/fixtures/validation/energyplus/EP-SMOKE-001/energyplus-model.idf"
        weather = "tests/fixtures/validation/energyplus/EP-SMOKE-001/weather.epw"
        rawCsv = "tests/fixtures/validation/energyplus/EP-SMOKE-001/energyplus-output.raw.csv"
    }
    detectedColumns = $referenceValues.detectedColumns
    requiredNonClaims = @(
        "Does not claim exact EnergyPlus numerical parity.",
        "Does not claim ASHRAE 140 validation coverage.",
        "Does not claim full ISO 52016 node/matrix solver parity.",
        "RealEnergyPlusComparison remains tolerance-based."
    )
}

$reference |
    ConvertTo-Json -Depth 20 |
    Set-Content $destinationReferenceJsonPath -Encoding utf8

$sourceHashes = [ordered]@{
    idfSha256 = (Get-FileHash $destinationIdfPath -Algorithm SHA256).Hash
    weatherSha256 = (Get-FileHash $destinationWeatherPath -Algorithm SHA256).Hash
    rawCsvSha256 = (Get-FileHash $destinationRawCsvPath -Algorithm SHA256).Hash
    referenceJsonSha256 = (Get-FileHash $destinationReferenceJsonPath -Algorithm SHA256).Hash
}

$provenance = [ordered]@{
    caseId = $CaseId
    engine = "EnergyPlus"
    energyPlusVersion = $EnergyPlusVersion
    importedAtUtc = "2026-01-01 00:00:00 UTC"
    sourceDirectory = $SourceDirectory
    notes = $Notes
    files = [ordered]@{
        idf = "energyplus-model.idf"
        weather = "weather.epw"
        rawCsv = "energyplus-output.raw.csv"
        normalizedReference = "energyplus-output.reference.json"
    }
    hashes = $sourceHashes
    referenceOutputs = $reference.referenceOutputs
    detectedColumns = $referenceValues.detectedColumns
    validationInterpretation = "This fixture is a real EnergyPlus reference comparison input. It is tolerance-based and does not claim exact numerical parity or ASHRAE 140 validation coverage."
}

$provenance |
    ConvertTo-Json -Depth 20 |
    Set-Content $destinationProvenancePath -Encoding utf8

Write-Host ""
Write-Host "Imported real EnergyPlus fixture files:" -ForegroundColor Green
Write-Host "- $destinationIdfPath"
Write-Host "- $destinationWeatherPath"
Write-Host "- $destinationRawCsvPath"
Write-Host "- $destinationReferenceJsonPath"
Write-Host "- $destinationProvenancePath"

if (-not $SkipValidation) {
    Write-Host ""
    Write-Host "Regenerating validation artifacts..." -ForegroundColor Cyan
    .\scripts\engineering-core\regenerate-engineering-core-v1-validation-artifacts.ps1

    Write-Host ""
    Write-Host "Running strict real fixture gate..." -ForegroundColor Cyan
    .\scripts\engineering-core\assert-ep-smoke-001-real-fixture-ready.ps1 -RequireRealFixture

    Write-Host ""
    Write-Host "Running generic fixture comparison with real references required..." -ForegroundColor Cyan
    .\scripts\engineering-core\compare-energyplus-validation-fixtures.ps1 -RequireRealReferences

    Write-Host ""
    Write-Host "Running validation profile..." -ForegroundColor Cyan
    .\scripts\engineering-core\verify-engineering-core-v1-validation.ps1
}

Write-Host ""
Write-Host "EP-SMOKE-001 real EnergyPlus fixture import completed." -ForegroundColor Green
