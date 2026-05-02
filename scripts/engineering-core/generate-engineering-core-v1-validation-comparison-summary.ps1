param(
    [string] $OutputJsonPath = "docs/reports/validation/EngineeringCoreV1ValidationComparisonSummary.json",
    [string] $OutputMarkdownPath = "docs/reports/validation/EngineeringCoreV1ValidationComparisonSummary.md"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$registryPath = "docs/validation/EnergyPlusValidationCaseRegistry.json"
$epSmoke001ResultPath = "docs/reports/validation/EP-SMOKE-001-ComparisonResult.json"

if (-not (Test-Path $registryPath)) {
    throw "Validation registry not found: $registryPath"
}

$registry = Get-Content $registryPath -Raw | ConvertFrom-Json

$comparisonResults = @()

if (Test-Path $epSmoke001ResultPath) {
    $comparisonResults += Get-Content $epSmoke001ResultPath -Raw | ConvertFrom-Json
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
        resultFile = if ($hasComparison) { $epSmoke001ResultPath } else { "" }
        nonClaims = @($case.nonClaims)
    }
}

$totalCases = @($caseSummaries).Count
$casesWithComparison = @($caseSummaries | Where-Object { $_.hasComparisonResult }).Count
$casesPassing = @($caseSummaries | Where-Object { $_.hasComparisonResult -and $_.allMetricsPassed }).Count
$placeholderComparisons = @($caseSummaries | Where-Object { $_.comparisonStatus -eq "PlaceholderComparison" }).Count
$plannedOnly = @($caseSummaries | Where-Object { -not $_.hasComparisonResult }).Count

$summary = [ordered]@{
    summaryName = "Engineering Core V1 Validation Comparison Summary"
    version = "v1"
    status = "PlannedValidation"
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'")
    registryFile = $registryPath
    comparisonResultFiles = @($comparisonResults | ForEach-Object {
        if ($_.caseId -eq "EP-SMOKE-001") { $epSmoke001ResultPath } else { "" }
    } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    totals = [ordered]@{
        totalCases = [int]$totalCases
        casesWithComparison = [int]$casesWithComparison
        casesPassing = [int]$casesPassing
        placeholderComparisons = [int]$placeholderComparisons
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
    interpretation = "Validation summary is a readiness and comparison index. Current EP-SMOKE-001 result is PlaceholderComparison only. This does not claim exact EnergyPlus parity or ASHRAE 140 validation coverage."
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
| Planned-only cases | $($summary.totals.plannedOnly) |

## Cases

| CaseId | Stage | Registry status | Comparison status | Reference status | Metrics passed | All passed |
|---|---|---|---|---|---:|---|
$($caseRows -join "`n")

## Comparison result files

$(@($summary.comparisonResultFiles | ForEach-Object { "- $_" }) -join "`n")

## Required non-claims

$($nonClaimRows -join "`n")

## Interpretation

Validation summary is a readiness and comparison index.

Current EP-SMOKE-001 result is PlaceholderComparison only.

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
