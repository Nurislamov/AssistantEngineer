param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$manifestPath = Join-Path $RepoRoot "docs\releases\Iso52016MatrixExternalValidationAnchorsManifest.json"
$documentationPath = Join-Path $RepoRoot "docs\calculations\Iso52016MatrixExternalValidationAnchors.md"

$requiredFiles = @(
    "docs\calculations\Iso52016MatrixExternalValidationAnchors.md",
    "docs\releases\Iso52016MatrixExternalValidationAnchorsManifest.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorManifestTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\ExternalValidationAnchors\manual-iso52016-anchor-001-steady-heating.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\ExternalValidationAnchors\manual-iso52016-anchor-002-heating-with-gains.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\ExternalValidationAnchors\manual-iso52016-anchor-003-steady-cooling.json"
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath

    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix external validation anchor file is missing: $relativePath"
    }
}

$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$manifestFixtureFiles = @($manifest.fixtures | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$minimumFixtureCountForPatch = 3

if ($manifest.PSObject.Properties.Name -contains "minimumFixtureCountForPatch") {
    $minimumFixtureCountForPatch = [int]$manifest.minimumFixtureCountForPatch
}

if ($manifestFixtureFiles.Count -lt $minimumFixtureCountForPatch) {
    throw "Expected at least 3 ISO52016 Matrix external validation anchor fixtures for patch AE-ISO52016-ANCHORS-001, found $($manifestFixtureFiles.Count)."
}

$requiredPatchFixtures = @(
    "tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/ExternalValidationAnchors/manual-iso52016-anchor-001-steady-heating.json",
    "tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/ExternalValidationAnchors/manual-iso52016-anchor-002-heating-with-gains.json",
    "tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/ExternalValidationAnchors/manual-iso52016-anchor-003-steady-cooling.json"
)

foreach ($requiredFixture in $requiredPatchFixtures) {
    if ($manifestFixtureFiles -notcontains $requiredFixture) {
        throw "External validation anchor manifest does not list required patch fixture: $requiredFixture"
    }
}

foreach ($fixtureRelativePath in $manifestFixtureFiles) {
    if ($fixtureRelativePath -notmatch "^tests/AssistantEngineer\.Tests/Calculations/Iso52016/Matrix/ExternalValidationAnchors/manual-iso52016-anchor-") {
        throw "External validation anchor manifest fixture must stay under ExternalValidationAnchors/manual-iso52016-anchor-*.json: $fixtureRelativePath"
    }

    $fixturePath = Join-Path $RepoRoot $fixtureRelativePath

    if (-not (Test-Path $fixturePath)) {
        throw "External validation anchor manifest references a missing fixture: $fixtureRelativePath"
    }

    $fixture = Get-Content $fixturePath -Raw | ConvertFrom-Json

    if ($fixture.sourceType -ne "ManualEngineeringValidationAnchor") {
        throw "External validation anchor fixture must use sourceType ManualEngineeringValidationAnchor: $fixtureRelativePath"
    }

    if ($fixture.authoritativeReference -ne "IndependentManualEngineeringFormula") {
        throw "External validation anchor fixture must use authoritativeReference IndependentManualEngineeringFormula: $fixtureRelativePath"
    }

    if ($fixture.validationClaim -ne "Validation anchor only; not full parity.") {
        throw "External validation anchor fixture must declare validationClaim: Validation anchor only; not full parity. Fixture: $fixtureRelativePath"
    }
}

$combinedText = @()
$combinedText += Get-Content $documentationPath -Raw
$combinedText += Get-Content $manifestPath -Raw
foreach ($fixtureRelativePath in $manifestFixtureFiles) {
    $combinedText += Get-Content (Join-Path $RepoRoot $fixtureRelativePath) -Raw
}

$allText = $combinedText -join "`n"

if ($allText -notmatch "validation anchors only") {
    throw "External validation anchor docs/manifests must state: validation anchors only."
}

if ($allText -notmatch "not full parity") {
    throw "External validation anchor docs/manifests must state: not full parity."
}

if ($allText -match "authoritative pyBuildingEnergy|authoritative EnergyPlus|pyBuildingEnergy output is authoritative|EnergyPlus output is authoritative|claims full parity|claim full parity") {
    throw "External validation anchor files contain a forbidden parity/authority claim."
}

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixExternalValidationAnchor"
    }
    finally {
        Pop-Location
    }
}

Write-Host "ISO52016 Matrix external validation anchors verification passed."