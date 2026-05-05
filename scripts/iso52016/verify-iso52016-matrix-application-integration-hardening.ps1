param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([Parameter(Mandatory = $true)] [string] $RelativePath)

    return Join-Path $RepoRoot $RelativePath
}

$requiredFiles = @(
    "docs\calculations\Iso52016MatrixApplicationIntegrationHardening.md",
    "docs\releases\Iso52016MatrixApplicationIntegrationHardeningManifest.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixApplicationIntegrationHardeningTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixApplicationIntegrationHardeningManifestTests.cs"
)

foreach ($relativePath in $requiredFiles) {
    $path = Resolve-RepoPath -RelativePath $relativePath

    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix application integration hardening file is missing: $relativePath"
    }
}

$manifestPath = Resolve-RepoPath -RelativePath "docs\releases\Iso52016MatrixApplicationIntegrationHardeningManifest.json"
$manifest = Get-Content -Raw $manifestPath | ConvertFrom-Json

if ($manifest.stageId -ne "ISO52016-MATRIX-APPLICATION-INTEGRATION-HARDENING") {
    throw "Application integration hardening manifest has unexpected stageId: $($manifest.stageId)"
}

if ($manifest.scope -ne "ApplicationIntegrationHardening") {
    throw "Application integration hardening manifest must use scope ApplicationIntegrationHardening."
}

if ($manifest.claimScope -ne "ValidationAnchorOnly") {
    throw "Application integration hardening manifest must use claimScope ValidationAnchorOnly."
}

if ($manifest.fixtureCount -ne 5) {
    throw "Application integration hardening manifest fixtureCount must be 5."
}

if ($manifest.fixtures.Count -ne 5) {
    throw "Application integration hardening manifest must list exactly 5 fixtures."
}

foreach ($relativeFixturePath in $manifest.fixtures) {
    $fixturePath = Join-Path $RepoRoot $relativeFixturePath

    if (-not (Test-Path $fixturePath)) {
        throw "Application integration hardening fixture is missing: $relativeFixturePath"
    }

    $fixture = Get-Content -Raw $fixturePath | ConvertFrom-Json

    if ($fixture.sourceType -ne "ManualEngineeringIntegrationAnchor") {
        throw "Application integration hardening fixture must use sourceType ManualEngineeringIntegrationAnchor: $relativeFixturePath"
    }

    if ($fixture.scope -ne "ApplicationIntegrationHardening") {
        throw "Application integration hardening fixture must use scope ApplicationIntegrationHardening: $relativeFixturePath"
    }

    if (-not ($fixture.id -like "APPLICATION-ISO52016-MATRIX-INTEGRATION-*")) {
        throw "Application integration hardening fixture has unexpected id: $($fixture.id)"
    }
}

$docPath = Resolve-RepoPath -RelativePath "docs\calculations\Iso52016MatrixApplicationIntegrationHardening.md"
$doc = Get-Content -Raw $docPath

$requiredDocPhrases = @(
    "Application integration hardening only.",
    "Validation anchors only, not full parity.",
    "No pyBuildingEnergy parity claim.",
    "No EnergyPlus parity claim.",
    "No ASHRAE 140 validation coverage claim.",
    "No full ISO 52016 parity claim.",
    "ManualEngineeringIntegrationAnchor"
)

foreach ($phrase in $requiredDocPhrases) {
    if ($doc -notlike "*$phrase*") {
        throw "Application integration hardening documentation is missing required phrase: $phrase"
    }
}

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixApplicationIntegrationHardening"
    }
    finally {
        Pop-Location
    }
}

Write-Host "ISO52016 Matrix application integration hardening verification passed."