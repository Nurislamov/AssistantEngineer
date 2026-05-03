param(
    [string] $RepoRoot = (Get-Location).Path,
    [string] $BaselineDirectory = "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Baselines",
    [string] $OutputDirectory = "artifacts\iso52016\matrix-baselines"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$baselineRoot = Join-Path $RepoRoot $BaselineDirectory

if (-not (Test-Path $baselineRoot)) {
    throw "ISO52016 Matrix baseline directory was not found: $BaselineDirectory"
}

$outputRoot = Join-Path $RepoRoot $OutputDirectory

if (-not (Test-Path $outputRoot)) {
    New-Item -ItemType Directory -Path $outputRoot | Out-Null
}

$fixtures = Get-ChildItem $baselineRoot -File -Filter *.json |
    Sort-Object Name |
    ForEach-Object {
        $fixture = Get-Content $_.FullName -Raw | ConvertFrom-Json

        [pscustomobject]@{
            ScenarioName = [string] $fixture.scenarioName
            FileName = $_.Name
            Description = [string] $fixture.description
            HourCount = [int] $fixture.hourCount
            OutdoorTemperatureC = [double] $fixture.outdoorTemperatureC
            AirNodeHeatGainW = [double] $fixture.airNodeHeatGainW
            MassNodeHeatGainW = [double] $fixture.massNodeHeatGainW
            AnnualHeatingEnergyKWh = [double] $fixture.expected.annualHeatingEnergyKWh
            AnnualCoolingEnergyKWh = [double] $fixture.expected.annualCoolingEnergyKWh
            PeakHeatingLoadW = [double] $fixture.expected.peakHeatingLoadW
            PeakCoolingLoadW = [double] $fixture.expected.peakCoolingLoadW
            AnnualTotalNodeHeatGainsKWh = [double] $fixture.expected.annualTotalNodeHeatGainsKWh
            RepresentativeHourCount = @($fixture.expected.representativeHours).Count
        }
    }

if (@($fixtures).Count -eq 0) {
    throw "No ISO52016 Matrix baseline fixtures were found in $BaselineDirectory"
}

$jsonPath = Join-Path $outputRoot "summary.json"
$mdPath = Join-Path $outputRoot "summary.md"

$fixtures | ConvertTo-Json -Depth 20 | Set-Content -Path $jsonPath -Encoding UTF8

$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add("# ISO 52016 Matrix baseline summary")
$lines.Add("")
$lines.Add("Generated from `$BaselineDirectory`.")
$lines.Add("")
$lines.Add("| Scenario | Hours | Outdoor °C | Air gains W | Mass gains W | Heating kWh | Cooling kWh | Peak heat W | Peak cool W | Rep. hours |")
$lines.Add("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |")

foreach ($fixture in $fixtures) {
    $lines.Add((
        "| {0} | {1} | {2:N3} | {3:N3} | {4:N3} | {5:N9} | {6:N9} | {7:N6} | {8:N6} | {9} |" -f
        $fixture.ScenarioName,
        $fixture.HourCount,
        $fixture.OutdoorTemperatureC,
        $fixture.AirNodeHeatGainW,
        $fixture.MassNodeHeatGainW,
        $fixture.AnnualHeatingEnergyKWh,
        $fixture.AnnualCoolingEnergyKWh,
        $fixture.PeakHeatingLoadW,
        $fixture.PeakCoolingLoadW,
        $fixture.RepresentativeHourCount))
}

$lines.Add("")
$lines.Add("This summary is a convenience report only. The authoritative regression values remain the JSON baseline fixtures and the C# baseline tests.")

Set-Content -Path $mdPath -Value $lines -Encoding UTF8

Write-Host "Wrote ISO52016 Matrix baseline summary:"
Write-Host "  $jsonPath"
Write-Host "  $mdPath"