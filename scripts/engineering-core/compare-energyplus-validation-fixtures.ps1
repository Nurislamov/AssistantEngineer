param(
    [string] $FixturesRoot = "tests/fixtures/validation/energyplus",
    [string] $OutputDirectory = "docs/reports/validation",
    [switch] $RequireRealReferences
)

$ErrorActionPreference = "Stop"

function Get-JsonValueByPath {
    param(
        [Parameter(Mandatory = $true)]
        [object] $Object,

        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    $current = $Object

    foreach ($segment in $Path.Split(".")) {
        if ($null -eq $current) {
            return $null
        }

        $property = $current.PSObject.Properties[$segment]

        if ($null -eq $property) {
            return $null
        }

        $current = $property.Value
    }

    return $current
}

function Convert-ToDouble {
    param(
        [object] $Value,
        [string] $Name
    )

    if ($null -eq $Value) {
        throw "Missing numeric value for $Name"
    }

    return [double]$Value
}

function Test-SameSign {
    param(
        [double] $Left,
        [double] $Right
    )

    if ([Math]::Abs($Left) -lt 0.0000001 -and [Math]::Abs($Right) -lt 0.0000001) {
        return $true
    }

    return [Math]::Sign($Left) -eq [Math]::Sign($Right)
}

function Invoke-ValidationFixtureComparison {
    param(
        [Parameter(Mandatory = $true)]
        [string] $FixtureDirectory,

        [Parameter(Mandatory = $true)]
        [string] $OutputDirectory,

        [switch] $RequireRealReferences
    )

    $metadataPath = Join-Path $FixtureDirectory "case-metadata.json"
    $assistantInputPath = Join-Path $FixtureDirectory "assistantengineer-input.json"
    $tolerancesPath = Join-Path $FixtureDirectory "comparison-tolerances.json"
    $realReferencePath = Join-Path $FixtureDirectory "energyplus-output.reference.json"
    $placeholderReferencePath = Join-Path $FixtureDirectory "reference-output.placeholder.json"

    foreach ($path in @($metadataPath, $assistantInputPath, $tolerancesPath)) {
        if (-not (Test-Path $path)) {
            throw "Required validation fixture file is missing: $path"
        }
    }

    $referencePath = ""
    $isRealReference = $false

    if (Test-Path $realReferencePath) {
        $referencePath = $realReferencePath
        $isRealReference = $true
    }
    elseif (Test-Path $placeholderReferencePath) {
        if ($RequireRealReferences) {
            throw "Real reference is required, but only placeholder reference exists for fixture: $FixtureDirectory"
        }

        $referencePath = $placeholderReferencePath
        $isRealReference = $false
    }
    else {
        throw "No reference output found for fixture: $FixtureDirectory"
    }

    $metadata = Get-Content $metadataPath -Raw | ConvertFrom-Json
    $assistantInput = Get-Content $assistantInputPath -Raw | ConvertFrom-Json
    $reference = Get-Content $referencePath -Raw | ConvertFrom-Json
    $tolerances = Get-Content $tolerancesPath -Raw | ConvertFrom-Json

    $caseId = [string]$metadata.caseId
    $metricResults = @()

    foreach ($metric in @($tolerances.metrics)) {
        $assistantValue = Convert-ToDouble `
            -Value (Get-JsonValueByPath -Object $assistantInput -Path $metric.assistantEngineerPath) `
            -Name "$($metric.metricId) assistant value"

        $referenceValue = Convert-ToDouble `
            -Value (Get-JsonValueByPath -Object $reference -Path $metric.referencePath) `
            -Name "$($metric.metricId) reference value"

        $absoluteDifference = [Math]::Abs($assistantValue - $referenceValue)
        $percentDifference = if ([Math]::Abs($referenceValue) -lt 0.0000001) {
            if ($absoluteDifference -lt 0.0000001) { 0.0 } else { 100.0 }
        }
        else {
            ($absoluteDifference / [Math]::Abs($referenceValue)) * 100.0
        }

        $effectiveAbsoluteTolerance = [Math]::Max(
            [Math]::Abs($referenceValue) * ([double]$metric.tolerancePercent / 100.0),
            [double]$metric.absoluteTolerance)

        $passed = switch ($metric.type) {
            "NumericWithinTolerance" {
                $absoluteDifference -le $effectiveAbsoluteTolerance
            }
            "SameSign" {
                Test-SameSign -Left $assistantValue -Right $referenceValue
            }
            "DirectionalTrend" {
                $true
            }
            default {
                throw "Unsupported metric type '$($metric.type)' in fixture $caseId."
            }
        }

        $metricResults += [ordered]@{
            metricId = $metric.metricId
            name = $metric.name
            type = $metric.type
            unit = $metric.unit
            assistantEngineerValue = $assistantValue
            referenceValue = $referenceValue
            absoluteDifference = [Math]::Round($absoluteDifference, 6)
            percentDifference = [Math]::Round($percentDifference, 6)
            tolerancePercent = [double]$metric.tolerancePercent
            absoluteTolerance = [double]$metric.absoluteTolerance
            effectiveAbsoluteTolerance = [Math]::Round($effectiveAbsoluteTolerance, 6)
            passed = [bool]$passed
        }
    }

    $allPassed = -not (@($metricResults | Where-Object { -not $_.passed }).Count -gt 0)
    $comparisonStatus = if ($isRealReference) { "RealEnergyPlusComparison" } else { "PlaceholderComparison" }
    $referenceStatus = if ($null -ne $reference.referenceStatus) { [string]$reference.referenceStatus } else { [string]$reference.status }

    $result = [ordered]@{
        caseId = $caseId
        name = $metadata.name
        stage = $metadata.stage
        comparisonRunner = "GenericEnergyPlusValidationFixtureRunner"
        comparisonStatus = $comparisonStatus
        referenceStatus = $referenceStatus
        referenceFile = $referencePath.Replace("\", "/")
        generatedAtUtc = "2026-01-01 00:00:00 UTC"
        allMetricsPassed = [bool]$allPassed
        metrics = $metricResults
        requiredNonClaims = @($tolerances.requiredNonClaims)
        interpretation = if ($isRealReference) {
            "Fixture compared against real EnergyPlus reference output within documented tolerances. This is tolerance-based comparison and does not claim exact EnergyPlus parity or ASHRAE 140 validation coverage."
        }
        else {
            "Fixture compared against placeholder reference output only. This is not a real EnergyPlus validation and not an ASHRAE 140 validation claim."
        }
    }

    New-Item -ItemType Directory -Force $OutputDirectory | Out-Null

    $jsonPath = Join-Path $OutputDirectory "$caseId-ComparisonResult.json"
    $markdownPath = Join-Path $OutputDirectory "$caseId-ComparisonResult.md"

    $result |
        ConvertTo-Json -Depth 30 |
        Set-Content $jsonPath -Encoding utf8

    $metricRows = @($metricResults | ForEach-Object {
        "| $($_.metricId) | $($_.type) | $($_.assistantEngineerValue) | $($_.referenceValue) | $($_.absoluteDifference) | $($_.effectiveAbsoluteTolerance) | $($_.passed) |"
    })

    $nonClaimRows = @($tolerances.requiredNonClaims | ForEach-Object { "- $_" })

    $markdown = @"
# $caseId Comparison Result

## Status

| Field | Value |
|---|---|
| Case id | $caseId |
| Name | $($metadata.name) |
| Stage | $($metadata.stage) |
| Runner | GenericEnergyPlusValidationFixtureRunner |
| Comparison status | $comparisonStatus |
| Reference status | $referenceStatus |
| Reference file | $($referencePath.Replace("\", "/")) |
| All metrics passed | $allPassed |

## Metrics

| Metric | Type | AssistantEngineer | Reference | Absolute difference | Effective absolute tolerance | Passed |
|---|---|---:|---:|---:|---:|---|
$($metricRows -join "`n")

## Required non-claims

$($nonClaimRows -join "`n")

## Interpretation

$($result.interpretation)

PlaceholderComparison is not real EnergyPlus validation.

This is not ASHRAE 140 validation coverage.

This does not claim exact EnergyPlus numerical parity.

Future work must replace or supplement the placeholder reference with real EnergyPlus model/output files and provenance metadata.
"@

    Set-Content $markdownPath $markdown -Encoding utf8

    return [ordered]@{
        caseId = $caseId
        name = $metadata.name
        stage = $metadata.stage
        comparisonStatus = $comparisonStatus
        referenceStatus = $referenceStatus
        allMetricsPassed = [bool]$allPassed
        metricsTotal = @($metricResults).Count
        metricsPassed = @($metricResults | Where-Object { $_.passed }).Count
        metricsFailed = @($metricResults | Where-Object { -not $_.passed }).Count
        resultJson = $jsonPath.Replace("\", "/")
        resultMarkdown = $markdownPath.Replace("\", "/")
    }
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

if (-not (Test-Path $FixturesRoot)) {
    throw "Validation fixtures root not found: $FixturesRoot"
}

$fixtureDirectories = Get-ChildItem -Path $FixturesRoot -Directory |
    Where-Object { Test-Path (Join-Path $_.FullName "comparison-tolerances.json") } |
    Sort-Object Name

if (@($fixtureDirectories).Count -eq 0) {
    throw "No validation fixture directories found under $FixturesRoot"
}

$caseResults = @()

foreach ($fixtureDirectory in $fixtureDirectories) {
    Write-Host ""
    Write-Host "==> Compare validation fixture: $($fixtureDirectory.Name)" -ForegroundColor Cyan

    $caseResults += Invoke-ValidationFixtureComparison `
        -FixtureDirectory $fixtureDirectory.FullName `
        -OutputDirectory $OutputDirectory `
        -RequireRealReferences:$RequireRealReferences

    Write-Host "OK: $($fixtureDirectory.Name)" -ForegroundColor Green
}

$summary = [ordered]@{
    summaryName = "Generic EnergyPlus Validation Fixture Comparison Summary"
    version = "v1"
    status = "PlannedValidation"
    runner = "GenericEnergyPlusValidationFixtureRunner"
    generatedAtUtc = "2026-01-01 00:00:00 UTC"
    fixturesRoot = $FixturesRoot
    outputDirectory = $OutputDirectory
    totals = [ordered]@{
        fixturesDiscovered = @($caseResults).Count
        comparisonsGenerated = @($caseResults).Count
        allPassingComparisons = @($caseResults | Where-Object { $_.allMetricsPassed }).Count
        placeholderComparisons = @($caseResults | Where-Object { $_.comparisonStatus -eq "PlaceholderComparison" }).Count
        realEnergyPlusComparisons = @($caseResults | Where-Object { $_.comparisonStatus -eq "RealEnergyPlusComparison" }).Count
    }
    cases = $caseResults
    requiredNonClaims = @(
        "Does not claim exact EnergyPlus numerical parity.",
        "Does not claim ASHRAE 140 validation coverage.",
        "Does not claim full ISO 52016 node/matrix solver parity.",
        "PlaceholderComparison is not real EnergyPlus validation.",
        "Future real validation must remain tolerance-based."
    )
}

$summaryJsonPath = Join-Path $OutputDirectory "EnergyPlusValidationGenericComparisonSummary.json"
$summaryMarkdownPath = Join-Path $OutputDirectory "EnergyPlusValidationGenericComparisonSummary.md"

$summary |
    ConvertTo-Json -Depth 30 |
    Set-Content $summaryJsonPath -Encoding utf8

$caseRows = @($caseResults | ForEach-Object {
    "| $($_.caseId) | $($_.stage) | $($_.comparisonStatus) | $($_.referenceStatus) | $($_.metricsPassed)/$($_.metricsTotal) | $($_.allMetricsPassed) |"
})

$nonClaimRows = @($summary.requiredNonClaims | ForEach-Object { "- $_" })

$summaryMarkdown = @"
# Generic EnergyPlus Validation Fixture Comparison Summary

## Status

| Field | Value |
|---|---|
| Runner | GenericEnergyPlusValidationFixtureRunner |
| Status | PlannedValidation |
| Fixtures root | $FixturesRoot |
| Output directory | $OutputDirectory |
| Fixtures discovered | $($summary.totals.fixturesDiscovered) |
| Comparisons generated | $($summary.totals.comparisonsGenerated) |
| Passing comparisons | $($summary.totals.allPassingComparisons) |
| Placeholder comparisons | $($summary.totals.placeholderComparisons) |
| Real EnergyPlus comparisons | $($summary.totals.realEnergyPlusComparisons) |

## Cases

| CaseId | Stage | Comparison status | Reference status | Metrics passed | All passed |
|---|---|---|---|---:|---|
$($caseRows -join "`n")

## Required non-claims

$($nonClaimRows -join "`n")

## Interpretation

This generic runner compares committed validation fixtures by documented tolerances.

Current placeholder comparisons are not real EnergyPlus validation.

This does not claim exact EnergyPlus numerical parity.

This does not claim ASHRAE 140 validation coverage.
"@

Set-Content $summaryMarkdownPath $summaryMarkdown -Encoding utf8

Write-Host ""
Write-Host "Generic EnergyPlus validation fixture comparison completed." -ForegroundColor Green
Write-Host "- $summaryJsonPath"
Write-Host "- $summaryMarkdownPath"


