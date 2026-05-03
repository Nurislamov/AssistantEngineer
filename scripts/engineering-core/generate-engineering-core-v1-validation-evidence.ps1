param(
    [string] $OutputJsonPath = "docs/reports/validation/EngineeringCoreV1ValidationEvidence.json",
    [string] $OutputMarkdownPath = "docs/reports/validation/EngineeringCoreV1ValidationEvidence.md"
)

$ErrorActionPreference = "Stop"

function Read-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    if (-not (Test-Path $Path)) {
        throw "Required validation evidence source file is missing: $Path"
    }

    return Get-Content $Path -Raw | ConvertFrom-Json
}

function Test-FileExistsAsBool {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    return [bool](Test-Path $Path)
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$registryPath = "docs/validation/EnergyPlusValidationCaseRegistry.json"
$fixtureCatalogPath = "docs/validation/EnergyPlusValidationFixtureCatalog.json"
$genericSummaryPath = "docs/reports/validation/EnergyPlusValidationGenericComparisonSummary.json"
$comparisonSummaryPath = "docs/reports/validation/EngineeringCoreV1ValidationComparisonSummary.json"
$realFixtureReadinessPath = "docs/reports/validation/EP-SMOKE-001-RealFixtureReadiness.md"
$validationReadinessPath = "docs/reports/EngineeringCoreV1ValidationReadiness.md"

$registry = Read-JsonFile $registryPath
$fixtureCatalog = Read-JsonFile $fixtureCatalogPath
$genericSummary = Read-JsonFile $genericSummaryPath
$comparisonSummary = Read-JsonFile $comparisonSummaryPath

$caseEvidence = @()

foreach ($fixture in @($fixtureCatalog.fixtures | Sort-Object caseId)) {
    $caseId = [string]$fixture.caseId

    $caseEvidence += [ordered]@{
        caseId = $caseId
        registryListed = [bool]$fixture.registryListed
        registryStage = [string]$fixture.registryStage
        registryStatus = [string]$fixture.registryStatus
        metadataStatus = [string]$fixture.metadataStatus
        comparisonStatus = [string]$fixture.comparisonStatus
        referenceStatus = [string]$fixture.referenceStatus
        allMetricsPassed = [bool]$fixture.allMetricsPassed
        metricCount = [int]$fixture.metricCount
        hasFixtureReadme = [bool]$fixture.hasFixtureReadme
        hasComparisonJson = [bool]$fixture.hasComparisonJson
        hasComparisonMarkdown = [bool]$fixture.hasComparisonMarkdown
        hasRealReference = [bool]$fixture.hasRealReference
        hasProvenance = [bool]$fixture.hasProvenance
        resultJson = [string]$fixture.resultJson
        resultMarkdown = [string]$fixture.resultMarkdown
    }
}

$totalFixtures = @($caseEvidence).Count
$placeholderComparisons = @($caseEvidence | Where-Object { $_.comparisonStatus -eq "PlaceholderComparison" }).Count
$realComparisons = @($caseEvidence | Where-Object { $_.comparisonStatus -eq "RealEnergyPlusComparison" }).Count
$passingComparisons = @($caseEvidence | Where-Object { $_.allMetricsPassed }).Count
$fixturesWithReadme = @($caseEvidence | Where-Object { $_.hasFixtureReadme }).Count

$requiredEvidenceFiles = @(
    $registryPath,
    "docs/validation/EnergyPlusValidationCaseRegistry.md",
    $fixtureCatalogPath,
    "docs/validation/EnergyPlusValidationFixtureCatalog.md",
    "docs/validation/EnergyPlusValidationFixtureCatalogGuide.md",
    "docs/validation/EnergyPlusValidationGenericRunner.md",
    "docs/validation/EnergyPlusValidationFixtureAuthoringGuide.md",
    "docs/validation/EnergyPlusRealFixtureIntakePolicy.md",
    $genericSummaryPath,
    "docs/reports/validation/EnergyPlusValidationGenericComparisonSummary.md",
    $comparisonSummaryPath,
    "docs/reports/validation/EngineeringCoreV1ValidationComparisonSummary.md",
    $realFixtureReadinessPath,
    $validationReadinessPath,
    "docs/reports/validation/README.md",
    "scripts/engineering-core/regenerate-engineering-core-v1-validation-artifacts.ps1",
    "scripts/engineering-core/verify-engineering-core-v1-validation.ps1",
    ".github/workflows/engineering-core-v1-validation.yml"
)

$evidenceFileRows = @($requiredEvidenceFiles | ForEach-Object {
    [ordered]@{
        path = $_
        exists = Test-FileExistsAsBool $_
    }
})

$missingEvidenceFiles = @($evidenceFileRows |
    Where-Object { -not $_.exists } |
    ForEach-Object { $_.path })

$evidence = [ordered]@{
    evidenceName = "Engineering Core V1 Validation Evidence"
    version = "v1"
    status = "PlannedValidation"
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'")
    interpretation = "Validation evidence package proves validation infrastructure readiness, placeholder comparison coverage and fixture synchronization. It does not claim exact EnergyPlus parity or ASHRAE 140 validation coverage."
    sources = [ordered]@{
        registry = $registryPath
        fixtureCatalog = $fixtureCatalogPath
        genericComparisonSummary = $genericSummaryPath
        validationComparisonSummary = $comparisonSummaryPath
        realFixtureReadiness = $realFixtureReadinessPath
        validationReadiness = $validationReadinessPath
    }
    totals = [ordered]@{
        registryCases = @($registry.cases).Count
        registrySmokeCases = @($registry.cases | Where-Object { $_.caseId -like "EP-SMOKE-*" }).Count
        fixtureCatalogCases = [int]$fixtureCatalog.totals.fixtureDirectories
        genericRunnerFixturesDiscovered = [int]$genericSummary.totals.fixturesDiscovered
        genericRunnerComparisonsGenerated = [int]$genericSummary.totals.comparisonsGenerated
        validationSummaryTotalCases = [int]$comparisonSummary.totals.totalCases
        validationSummaryCasesWithComparison = [int]$comparisonSummary.totals.casesWithComparison
        evidenceFixtureRows = [int]$totalFixtures
        placeholderComparisons = [int]$placeholderComparisons
        realEnergyPlusComparisons = [int]$realComparisons
        passingComparisons = [int]$passingComparisons
        fixturesWithReadme = [int]$fixturesWithReadme
        missingEvidenceFiles = @($missingEvidenceFiles).Count
    }
    cases = $caseEvidence
    evidenceFiles = $evidenceFileRows
    requiredNonClaims = @(
        "Does not claim exact EnergyPlus numerical parity.",
        "Does not claim exact pyBuildingEnergy numerical parity.",
        "Does not claim ASHRAE 140 validation coverage.",
        "Does not claim full ISO 52016 node/matrix solver parity.",
        "PlaceholderComparison is not real EnergyPlus validation.",
        "Future real validation must remain tolerance-based."
    )
    nextMilestones = @(
        "Add first real EnergyPlus model and output for EP-SMOKE-001.",
        "Add provenance.json for real EnergyPlus fixture.",
        "Switch EP-SMOKE-001 from PlaceholderComparison to RealEnergyPlusComparison.",
        "Keep comparison tolerance-based and non-parity.",
        "Add additional real fixtures only through fixture authoring kit and intake gate."
    )
}

$jsonDirectory = Split-Path $OutputJsonPath -Parent
if (-not [string]::IsNullOrWhiteSpace($jsonDirectory)) {
    New-Item -ItemType Directory -Force $jsonDirectory | Out-Null
}

$markdownDirectory = Split-Path $OutputMarkdownPath -Parent
if (-not [string]::IsNullOrWhiteSpace($markdownDirectory)) {
    New-Item -ItemType Directory -Force $markdownDirectory | Out-Null
}

$evidence |
    ConvertTo-Json -Depth 30 |
    Set-Content $OutputJsonPath -Encoding utf8

$caseRows = @($caseEvidence | ForEach-Object {
    "| $($_.caseId) | $($_.registryStage) | $($_.metadataStatus) | $($_.comparisonStatus) | $($_.referenceStatus) | $($_.metricCount) | $($_.allMetricsPassed) | $($_.hasRealReference) |"
})

$fileRows = @($evidenceFileRows | ForEach-Object {
    "| $($_.path) | $($_.exists) |"
})

$nonClaimRows = @($evidence.requiredNonClaims | ForEach-Object { "- $_" })
$milestoneRows = @($evidence.nextMilestones | ForEach-Object { "- $_" })

$markdown = @"
# Engineering Core V1 Validation Evidence

Generated at: $($evidence.generatedAtUtc)

## Status

| Field | Value |
|---|---|
| Evidence package | $($evidence.evidenceName) |
| Version | $($evidence.version) |
| Status | $($evidence.status) |
| Registry cases | $($evidence.totals.registryCases) |
| Registry smoke cases | $($evidence.totals.registrySmokeCases) |
| Fixture catalog cases | $($evidence.totals.fixtureCatalogCases) |
| Generic runner fixtures discovered | $($evidence.totals.genericRunnerFixturesDiscovered) |
| Generic runner comparisons generated | $($evidence.totals.genericRunnerComparisonsGenerated) |
| Validation summary cases with comparison | $($evidence.totals.validationSummaryCasesWithComparison) |
| Placeholder comparisons | $($evidence.totals.placeholderComparisons) |
| Real EnergyPlus comparisons | $($evidence.totals.realEnergyPlusComparisons) |
| Passing comparisons | $($evidence.totals.passingComparisons) |
| Missing evidence files | $($evidence.totals.missingEvidenceFiles) |

## Interpretation

Validation evidence package proves validation infrastructure readiness, placeholder comparison coverage and fixture synchronization.

It does not claim exact EnergyPlus parity.

It does not claim ASHRAE 140 validation coverage.

## Cases

| CaseId | Stage | Metadata status | Comparison status | Reference status | Metrics | All passed | Has real reference |
|---|---|---|---|---|---:|---|---|
$($caseRows -join "`n")

## Evidence files

| File | Exists |
|---|---|
$($fileRows -join "`n")

## Required non-claims

$($nonClaimRows -join "`n")

## Next milestones

$($milestoneRows -join "`n")
"@

Set-Content $OutputMarkdownPath $markdown -Encoding utf8

if ($missingEvidenceFiles.Count -gt 0) {
    Write-Host "Validation evidence generated with missing evidence files:" -ForegroundColor Yellow
    foreach ($file in $missingEvidenceFiles) {
        Write-Host "- $file" -ForegroundColor Yellow
    }
}
else {
    Write-Host "Validation evidence package generated successfully." -ForegroundColor Green
}

Write-Host "- $OutputJsonPath"
Write-Host "- $OutputMarkdownPath"
