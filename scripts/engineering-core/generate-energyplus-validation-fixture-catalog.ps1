param(
    [string] $FixturesRoot = "tests/fixtures/validation/energyplus",
    [string] $RegistryPath = "docs/validation/EnergyPlusValidationCaseRegistry.json",
    [string] $ReportsDirectory = "docs/reports/validation",
    [string] $OutputJsonPath = "docs/validation/EnergyPlusValidationFixtureCatalog.json",
    [string] $OutputMarkdownPath = "docs/validation/EnergyPlusValidationFixtureCatalog.md"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

if (-not (Test-Path $RegistryPath)) {
    throw "Validation registry not found: $RegistryPath"
}

if (-not (Test-Path $FixturesRoot)) {
    throw "Validation fixtures root not found: $FixturesRoot"
}

$registry = Get-Content $RegistryPath -Raw | ConvertFrom-Json
$registryCases = @($registry.cases)

$registryByCaseId = @{}
foreach ($case in $registryCases) {
    $registryByCaseId[[string]$case.caseId] = $case
}

$fixtureDirectories = Get-ChildItem -Path $FixturesRoot -Directory |
    Sort-Object Name

$fixtureRows = @()

foreach ($fixtureDirectory in $fixtureDirectories) {
    $caseId = $fixtureDirectory.Name
    $metadataPath = Join-Path $fixtureDirectory.FullName "case-metadata.json"
    $inputPath = Join-Path $fixtureDirectory.FullName "assistantengineer-input.json"
    $placeholderReferencePath = Join-Path $fixtureDirectory.FullName "reference-output.placeholder.json"
    $realReferencePath = Join-Path $fixtureDirectory.FullName "energyplus-output.reference.json"
    $tolerancesPath = Join-Path $fixtureDirectory.FullName "comparison-tolerances.json"
    $provenancePath = Join-Path $fixtureDirectory.FullName "provenance.json"

    $comparisonJsonPath = Join-Path $ReportsDirectory "$caseId-ComparisonResult.json"
    $comparisonMarkdownPath = Join-Path $ReportsDirectory "$caseId-ComparisonResult.md"
    $fixtureReadmePath = "docs/validation/fixtures/$caseId/README.md"

    $metadata = $null
    $comparison = $null
    $tolerances = $null

    if (Test-Path $metadataPath) {
        $metadata = Get-Content $metadataPath -Raw | ConvertFrom-Json
    }

    if (Test-Path $comparisonJsonPath) {
        $comparison = Get-Content $comparisonJsonPath -Raw | ConvertFrom-Json
    }

    if (Test-Path $tolerancesPath) {
        $tolerances = Get-Content $tolerancesPath -Raw | ConvertFrom-Json
    }

    $hasRegistryCase = $registryByCaseId.ContainsKey($caseId)
    $registryCase = if ($hasRegistryCase) { $registryByCaseId[$caseId] } else { $null }

    $missingRequiredFixtureFiles = @()

    foreach ($file in @(
        "case-metadata.json",
        "assistantengineer-input.json",
        "comparison-tolerances.json")) {
        if (-not (Test-Path (Join-Path $fixtureDirectory.FullName $file))) {
            $missingRequiredFixtureFiles += $file
        }
    }

    if (-not ((Test-Path $placeholderReferencePath) -or (Test-Path $realReferencePath))) {
        $missingRequiredFixtureFiles += "reference-output.placeholder.json or energyplus-output.reference.json"
    }

    $fixtureRows += [ordered]@{
        caseId = $caseId
        registryListed = [bool]$hasRegistryCase
        registryStage = if ($hasRegistryCase) { [string]$registryCase.stage } else { "" }
        registryStatus = if ($hasRegistryCase) { [string]$registryCase.status } else { "" }
        metadataStage = if ($null -ne $metadata) { [string]$metadata.stage } else { "" }
        metadataStatus = if ($null -ne $metadata) { [string]$metadata.status } else { "" }
        hasMetadata = [bool](Test-Path $metadataPath)
        hasAssistantEngineerInput = [bool](Test-Path $inputPath)
        hasComparisonTolerances = [bool](Test-Path $tolerancesPath)
        hasPlaceholderReference = [bool](Test-Path $placeholderReferencePath)
        hasRealReference = [bool](Test-Path $realReferencePath)
        hasProvenance = [bool](Test-Path $provenancePath)
        hasFixtureReadme = [bool](Test-Path $fixtureReadmePath)
        hasComparisonJson = [bool](Test-Path $comparisonJsonPath)
        hasComparisonMarkdown = [bool](Test-Path $comparisonMarkdownPath)
        comparisonStatus = if ($null -ne $comparison) { [string]$comparison.comparisonStatus } else { "NotGenerated" }
        referenceStatus = if ($null -ne $comparison) { [string]$comparison.referenceStatus } else { "NotAvailable" }
        allMetricsPassed = if ($null -ne $comparison) { [bool]$comparison.allMetricsPassed } else { $false }
        metricCount = if ($null -ne $tolerances) { @($tolerances.metrics).Count } else { 0 }
        missingRequiredFixtureFiles = $missingRequiredFixtureFiles
        resultJson = if (Test-Path $comparisonJsonPath) { $comparisonJsonPath.Replace("\", "/") } else { "" }
        resultMarkdown = if (Test-Path $comparisonMarkdownPath) { $comparisonMarkdownPath.Replace("\", "/") } else { "" }
    }
}

$fixtureCaseIds = @($fixtureRows | ForEach-Object { $_.caseId })
$registrySmokeCaseIds = @($registryCases |
    Where-Object { $_.caseId -like "EP-SMOKE-*" } |
    ForEach-Object { [string]$_.caseId })

$registryCasesWithoutFixture = @($registrySmokeCaseIds |
    Where-Object { $fixtureCaseIds -notcontains $_ } |
    Sort-Object)

$fixturesWithoutRegistry = @($fixtureCaseIds |
    Where-Object { -not $registryByCaseId.ContainsKey($_) } |
    Sort-Object)

$fixturesMissingRequiredFiles = @($fixtureRows |
    Where-Object { @($_.missingRequiredFixtureFiles).Count -gt 0 } |
    ForEach-Object { $_.caseId } |
    Sort-Object)

$fixturesMissingComparison = @($fixtureRows |
    Where-Object { -not $_.hasComparisonJson -or -not $_.hasComparisonMarkdown } |
    ForEach-Object { $_.caseId } |
    Sort-Object)

$catalog = [ordered]@{
    catalogName = "EnergyPlus Validation Fixture Catalog"
    version = "v1"
    status = "PlannedValidation"
    generatedAtUtc = "2026-01-01 00:00:00 UTC"
    registryPath = $RegistryPath
    fixturesRoot = $FixturesRoot
    reportsDirectory = $ReportsDirectory
    totals = [ordered]@{
        registryCases = @($registryCases).Count
        registrySmokeCases = @($registrySmokeCaseIds).Count
        fixtureDirectories = @($fixtureRows).Count
        fixturesWithComparison = @($fixtureRows | Where-Object { $_.hasComparisonJson -and $_.hasComparisonMarkdown }).Count
        placeholderComparisons = @($fixtureRows | Where-Object { $_.comparisonStatus -eq "PlaceholderComparison" }).Count
        realEnergyPlusComparisons = @($fixtureRows | Where-Object { $_.comparisonStatus -eq "RealEnergyPlusComparison" }).Count
        fixturesWithoutRegistry = @($fixturesWithoutRegistry).Count
        registryCasesWithoutFixture = @($registryCasesWithoutFixture).Count
        fixturesMissingRequiredFiles = @($fixturesMissingRequiredFiles).Count
        fixturesMissingComparison = @($fixturesMissingComparison).Count
    }
    sync = [ordered]@{
        registryCasesWithoutFixture = $registryCasesWithoutFixture
        fixturesWithoutRegistry = $fixturesWithoutRegistry
        fixturesMissingRequiredFiles = $fixturesMissingRequiredFiles
        fixturesMissingComparison = $fixturesMissingComparison
    }
    fixtures = $fixtureRows
    requiredNonClaims = @(
        "Does not claim exact EnergyPlus numerical parity.",
        "Does not claim ASHRAE 140 validation coverage.",
        "Does not claim full ISO 52016 node/matrix solver parity.",
        "PlaceholderComparison is not real EnergyPlus validation.",
        "Future real validation must remain tolerance-based."
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

$catalog |
    ConvertTo-Json -Depth 30 |
    Set-Content $OutputJsonPath -Encoding utf8

$fixtureTableRows = @($fixtureRows | ForEach-Object {
    "| $($_.caseId) | $($_.registryListed) | $($_.metadataStatus) | $($_.comparisonStatus) | $($_.referenceStatus) | $($_.metricCount) | $($_.allMetricsPassed) |"
})

$nonClaimRows = @($catalog.requiredNonClaims | ForEach-Object { "- $_" })

$registryMissingMarkdown = if ($registryCasesWithoutFixture.Count -eq 0) {
    "- none"
}
else {
    @($registryCasesWithoutFixture | ForEach-Object { "- $_" }) -join "`n"
}

$fixturesWithoutRegistryMarkdown = if ($fixturesWithoutRegistry.Count -eq 0) {
    "- none"
}
else {
    @($fixturesWithoutRegistry | ForEach-Object { "- $_" }) -join "`n"
}

$missingFilesMarkdown = if ($fixturesMissingRequiredFiles.Count -eq 0) {
    "- none"
}
else {
    @($fixturesMissingRequiredFiles | ForEach-Object { "- $_" }) -join "`n"
}

$missingComparisonMarkdown = if ($fixturesMissingComparison.Count -eq 0) {
    "- none"
}
else {
    @($fixturesMissingComparison | ForEach-Object { "- $_" }) -join "`n"
}

$markdown = @"
# EnergyPlus Validation Fixture Catalog

Generated at: $($catalog.generatedAtUtc)

## Status

| Field | Value |
|---|---|
| Catalog | $($catalog.catalogName) |
| Version | $($catalog.version) |
| Status | $($catalog.status) |
| Registry | $($catalog.registryPath) |
| Fixtures root | $($catalog.fixturesRoot) |
| Reports directory | $($catalog.reportsDirectory) |
| Registry cases | $($catalog.totals.registryCases) |
| Registry smoke cases | $($catalog.totals.registrySmokeCases) |
| Fixture directories | $($catalog.totals.fixtureDirectories) |
| Fixtures with comparison | $($catalog.totals.fixturesWithComparison) |
| Placeholder comparisons | $($catalog.totals.placeholderComparisons) |
| Real EnergyPlus comparisons | $($catalog.totals.realEnergyPlusComparisons) |

## Fixtures

| CaseId | Registry listed | Metadata status | Comparison status | Reference status | Metrics | All metrics passed |
|---|---|---|---|---|---:|---|
$($fixtureTableRows -join "`n")

## Registry cases without fixture

$registryMissingMarkdown

## Fixtures without registry entry

$fixturesWithoutRegistryMarkdown

## Fixtures missing required files

$missingFilesMarkdown

## Fixtures missing comparison output

$missingComparisonMarkdown

## Required non-claims

$($nonClaimRows -join "`n")

## Interpretation

The fixture catalog checks synchronization between the validation registry, committed fixture folders and generated comparison outputs.

Current smoke fixtures are PlaceholderComparison unless a real EnergyPlus reference output is committed.

PlaceholderComparison is not real EnergyPlus validation.

This does not claim exact EnergyPlus numerical parity or ASHRAE 140 validation coverage.
"@

Set-Content $OutputMarkdownPath $markdown -Encoding utf8

Write-Host "EnergyPlus validation fixture catalog generated:" -ForegroundColor Green
Write-Host "- $OutputJsonPath"
Write-Host "- $OutputMarkdownPath"
Write-Host "Fixture directories: $($catalog.totals.fixtureDirectories)"
Write-Host "Fixtures with comparison: $($catalog.totals.fixturesWithComparison)"

