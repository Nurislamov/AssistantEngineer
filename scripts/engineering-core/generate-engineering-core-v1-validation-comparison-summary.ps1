param(
    [string] $OutputJsonPath = "docs/reports/validation/EngineeringCoreV1ValidationComparisonSummary.json",
    [string] $OutputMarkdownPath = "docs/reports/validation/EngineeringCoreV1ValidationComparisonSummary.md"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$registryPath = "docs/validation/EnergyPlusValidationCaseRegistry.json"
$reportsDirectory = "docs/reports/validation"

if (-not (Test-Path $registryPath)) {
    throw "Validation registry not found: $registryPath"
}

if (-not (Test-Path $reportsDirectory)) {
    throw "Validation reports directory not found: $reportsDirectory"
}

$registry = Get-Content $registryPath -Raw | ConvertFrom-Json

$comparisonResultFiles = Get-ChildItem -Path $reportsDirectory -File -Filter "EP-SMOKE-*-ComparisonResult.json" |
    Sort-Object Name

$comparisonResults = @()

foreach ($file in $comparisonResultFiles) {
    $comparisonResults += Get-Content $file.FullName -Raw | ConvertFrom-Json
}

$comparisonByCaseId = @{}

foreach ($comparison in $comparisonResults) {
    $comparisonByCaseId[[string]$comparison.caseId] = $comparison
}

$caseSummaries = @()

foreach ($case in @($registry.cases | Sort-Object caseId)) {
    $caseId = [string]$case.caseId
    $comparison = $null

    if ($comparisonByCaseId.ContainsKey($caseId)) {
        $comparison = $comparisonByCaseId[$caseId]
    }

    $hasComparison = $null -ne $comparison
    $metricsTotal = if ($hasComparison) { @($comparison.metrics).Count } else { @($case.metrics).Count }
    $metricsPassed = if ($hasComparison) { @($comparison.metrics | Where-Object { $_.passed -eq $true }).Count } else { 0 }

    $resultFile = ""

    if ($hasComparison) {
        $matchingFile = $comparisonResultFiles |
            Where-Object {
                (Get-Content $_.FullName -Raw | ConvertFrom-Json).caseId -eq $caseId
            } |
            Select-Object -First 1

        if ($null -ne $matchingFile) {
            $resultFile = $matchingFile.FullName.Replace($repoRoot.Path + "\", "").Replace("\", "/")
        }
    }

    $caseSummaries += [ordered]@{
        caseId = $caseId
        name = $case.name
        stage = $case.stage
        registryStatus = $case.status
        comparisonStatus = if ($hasComparison) { $comparison.comparisonStatus } else { "NotGenerated" }
        referenceStatus = if ($hasComparison) { $comparison.referenceStatus } else { "NotAvailable" }
        hasComparisonResult = [bool]$hasComparison
        allMetricsPassed = if ($hasComparison) { [bool]$comparison.allMetricsPassed } else { $false }
        metricsTotal = [int]$metricsTotal
        metricsPassed = [int]$metricsPassed
        metricsFailed = [int]($metricsTotal - $metricsPassed)
        resultFile = $resultFile
        nonClaims = @($case.nonClaims)
    }
}

$totalCases = @($caseSummaries).Count
$casesWithComparison = @($caseSummaries | Where-Object { $_.hasComparisonResult }).Count
$casesPassing = @($caseSummaries | Where-Object { $_.hasComparisonResult -and $_.allMetricsPassed }).Count
$placeholderComparisons = @($caseSummaries | Where-Object { $_.comparisonStatus -eq "PlaceholderComparison" }).Count
$realComparisons = @($caseSummaries | Where-Object { $_.comparisonStatus -eq "RealEnergyPlusComparison" }).Count
$plannedOnly = @($caseSummaries | Where-Object { -not $_.hasComparisonResult }).Count

$summary = [ordered]@{
    summaryName = "Engineering Core V1 Validation Comparison Summary"
    version = "v1"
    status = "PlannedValidation"
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'")
    registryFile = $registryPath
    comparisonResultFiles = @($comparisonResultFiles | ForEach-Object {
        $_.FullName.Replace($repoRoot.Path + "\", "").Replace("\", "/")
    })
    totals = [ordered]@{
        totalCases = [int]$totalCases
        casesWithComparison = [int]$casesWithComparison
        casesPassing = [int]$casesPassing
        placeholderComparisons = [int]$placeholderComparisons
        realEnergyPlusComparisons = [int]$realComparisons
        plannedOnly = [int]$plannedOnly
    }
    cases = $caseSummaries
    requiredNonClaims = @(
        "Does not claim exact EnergyPlus numerical parity.",
        "Does not claim ASHRAE 140 validation coverage.",
        "Does not claim full ISO 52016 node/matrix solver parity.",
        "PlaceholderComparison is not real EnergyPlus validation.",
        "Future real validation must remain tolerance-based."
    )
    interpretation = "Validation summary is a readiness and comparison index. Current EP-SMOKE results are PlaceholderComparison only. This does not claim exact EnergyPlus parity or ASHRAE 140 validation coverage."
}

$jsonDirectory = Split-Path $OutputJsonPath -Parent
if (-not [string]::IsNullOrWhiteSpace($jsonDirectory)) {
    New-Item -ItemType Directory -Force $jsonDirectory | Out-Null
}

$markdownDirectory = Split-Path $OutputMarkdownPath -Parent
if (-not [string]::IsNullOrWhiteSpace($markdownDirectory)) {
    New-Item -ItemType Directory -Force $markdownDirectory | Out-Null
}

$summary |
    ConvertTo-Json -Depth 30 |
    Set-Content $OutputJsonPath -Encoding utf8

$caseRows = @($caseSummaries | ForEach-Object {
    "| $($_.caseId) | $($_.stage) | $($_.registryStatus) | $($_.comparisonStatus) | $($_.referenceStatus) | $($_.metricsPassed)/$($_.metricsTotal) | $($_.allMetricsPassed) |"
})

$comparisonFileRows = @($summary.comparisonResultFiles | ForEach-Object { "- $_" })
$nonClaimRows = @($summary.requiredNonClaims | ForEach-Object { "- $_" })

$markdown = @"
# Engineering Core V1 Validation Comparison Summary

Generated at: $($summary.generatedAtUtc)

## Status

| Field | Value |
|---|---|
| Summary | $($summary.summaryName) |
| Version | $($summary.version) |
| Status | $($summary.status) |
| Registry file | $($summary.registryFile) |
| Total cases | $($summary.totals.totalCases) |
| Cases with comparison | $($summary.totals.casesWithComparison) |
| Cases passing | $($summary.totals.casesPassing) |
| Placeholder comparisons | $($summary.totals.placeholderComparisons) |
| Real EnergyPlus comparisons | $($summary.totals.realEnergyPlusComparisons) |
| Planned-only cases | $($summary.totals.plannedOnly) |

## Cases

| CaseId | Stage | Registry status | Comparison status | Reference status | Metrics passed | All passed |
|---|---|---|---|---|---:|---|
$($caseRows -join "`n")

## Comparison result files

$($comparisonFileRows -join "`n")

## Required non-claims

$($nonClaimRows -join "`n")

## Interpretation

Validation summary is a readiness and comparison index.

Current EP-SMOKE results are PlaceholderComparison only.

This does not claim exact EnergyPlus numerical parity.

This does not claim ASHRAE 140 validation coverage.

Future real validation must use committed EnergyPlus/reference model files, provenance metadata and documented tolerances.
"@

Set-Content $OutputMarkdownPath $markdown -Encoding utf8

Write-Host "Engineering Core V1 validation comparison summary generated:" -ForegroundColor Green
Write-Host "- $OutputJsonPath"
Write-Host "- $OutputMarkdownPath"
Write-Host "Cases: $totalCases"
Write-Host "Cases with comparison: $casesWithComparison"
Write-Host "Placeholder comparisons: $placeholderComparisons"
