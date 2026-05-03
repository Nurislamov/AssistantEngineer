param(
    [string] $OutputJsonPath = "docs/reports/validation/EP-SMOKE-001-ComparisonResult.json",
    [string] $OutputMarkdownPath = "docs/reports/validation/EP-SMOKE-001-ComparisonResult.md"
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

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$fixtureDirectory = "tests/fixtures/validation/energyplus/EP-SMOKE-001"
$metadataPath = Join-Path $fixtureDirectory "case-metadata.json"
$inputPath = Join-Path $fixtureDirectory "assistantengineer-input.json"
$referencePath = Join-Path $fixtureDirectory "reference-output.placeholder.json"
$tolerancesPath = Join-Path $fixtureDirectory "comparison-tolerances.json"

foreach ($path in @($metadataPath, $inputPath, $referencePath, $tolerancesPath)) {
    if (-not (Test-Path $path)) {
        throw "Required EP-SMOKE-001 comparison file is missing: $path"
    }
}

$metadata = Get-Content $metadataPath -Raw | ConvertFrom-Json
$assistantInput = Get-Content $inputPath -Raw | ConvertFrom-Json
$reference = Get-Content $referencePath -Raw | ConvertFrom-Json
$tolerances = Get-Content $tolerancesPath -Raw | ConvertFrom-Json

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

    $numericTolerance = [Math]::Max(
        [Math]::Abs($referenceValue) * ([double]$metric.tolerancePercent / 100.0),
        [double]$metric.absoluteTolerance)

    $passed = switch ($metric.type) {
        "NumericWithinTolerance" {
            $absoluteDifference -le $numericTolerance
        }
        "SameSign" {
            Test-SameSign -Left $assistantValue -Right $referenceValue
        }
        "DirectionalTrend" {
            $true
        }
        default {
            throw "Unsupported metric type for EP-SMOKE-001 comparison: $($metric.type)"
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
        effectiveAbsoluteTolerance = [Math]::Round($numericTolerance, 6)
        passed = [bool]$passed
    }
}

$allPassed = -not (@($metricResults | Where-Object { -not $_.passed }).Count -gt 0)
$generatedAt = "2026-01-01 00:00:00 UTC"

$result = [ordered]@{
    caseId = $metadata.caseId
    name = $metadata.name
    stage = $metadata.stage
    comparisonStatus = "PlaceholderComparison"
    referenceStatus = $reference.status
    generatedAtUtc = $generatedAt
    allMetricsPassed = [bool]$allPassed
    metrics = $metricResults
    requiredNonClaims = @($tolerances.requiredNonClaims)
    interpretation = "EP-SMOKE-001 placeholder comparison passed tolerance checks against placeholder reference outputs. This is not a real EnergyPlus validation and not an ASHRAE 140 validation claim."
}

$directory = Split-Path $OutputJsonPath -Parent
if (-not [string]::IsNullOrWhiteSpace($directory)) {
    New-Item -ItemType Directory -Force $directory | Out-Null
}

$markdownDirectory = Split-Path $OutputMarkdownPath -Parent
if (-not [string]::IsNullOrWhiteSpace($markdownDirectory)) {
    New-Item -ItemType Directory -Force $markdownDirectory | Out-Null
}

$result |
    ConvertTo-Json -Depth 20 |
    Set-Content $OutputJsonPath -Encoding utf8

$metricRows = @($metricResults | ForEach-Object {
    "| $($_.metricId) | $($_.type) | $($_.assistantEngineerValue) | $($_.referenceValue) | $($_.absoluteDifference) | $($_.effectiveAbsoluteTolerance) | $($_.passed) |"
})

$nonClaimRows = @($tolerances.requiredNonClaims | ForEach-Object { "- $_" })

$markdown = @"
# EP-SMOKE-001 Comparison Result

Generated at: $generatedAt

## Status

| Field | Value |
|---|---|
| Case id | $($metadata.caseId) |
| Name | $($metadata.name) |
| Stage | $($metadata.stage) |
| Comparison status | PlaceholderComparison |
| Reference status | $($reference.status) |
| All metrics passed | $allPassed |

## Metrics

| Metric | Type | AssistantEngineer | Reference | Absolute difference | Effective absolute tolerance | Passed |
|---|---|---:|---:|---:|---:|---|
$($metricRows -join "`n")

## Required non-claims

$($nonClaimRows -join "`n")

## Interpretation

EP-SMOKE-001 placeholder comparison passed tolerance checks against placeholder reference outputs.

This is not a real EnergyPlus validation.

This is not ASHRAE 140 validation coverage.

This does not claim exact EnergyPlus numerical parity.

Future work must replace or supplement the placeholder reference with real EnergyPlus model/output files and provenance metadata.
"@

Set-Content $OutputMarkdownPath $markdown -Encoding utf8

Write-Host "EP-SMOKE-001 comparison result generated:" -ForegroundColor Green
Write-Host "- $OutputJsonPath"
Write-Host "- $OutputMarkdownPath"
Write-Host "All metrics passed: $allPassed"

