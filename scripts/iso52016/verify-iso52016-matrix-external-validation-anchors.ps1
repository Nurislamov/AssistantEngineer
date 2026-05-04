param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$anchorDirectory = Join-Path $RepoRoot "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\ExternalValidationAnchors"
$manifestPath = Join-Path $RepoRoot "docs\releases\Iso52016MatrixExternalValidationAnchorsManifest.json"
$documentationPath = Join-Path $RepoRoot "docs\calculations\Iso52016MatrixExternalValidationAnchors.md"
$anchorTestPath = Join-Path $RepoRoot "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorTests.cs"
$manifestTestPath = Join-Path $RepoRoot "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorManifestTests.cs"

$requiredFiles = @(
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\ExternalValidationAnnualAnchors\manual-independent-annual-8760-seasonal-loads.json",
    "docs\releases\Iso52016MatrixExternalValidationAnnualAnchorsManifest.json",
    "docs\calculations\Iso52016MatrixExternalValidationAnnualAnchors.md",
    $documentationPath,
    $manifestPath,
    $anchorTestPath,
    $manifestTestPath
)

foreach ($path in $requiredFiles) {
    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix external validation anchor file is missing: $path"
    }
}

if (-not (Test-Path $anchorDirectory)) {
    throw "ISO52016 Matrix external validation anchor directory is missing: $anchorDirectory"
}

$fixtureFiles = @(Get-ChildItem $anchorDirectory -File -Filter *.json | Sort-Object Name)

if ($fixtureFiles.Count -lt 10) {
    throw "Expected at least 10 ISO52016 Matrix external validation anchor fixtures after Step 02, found $($fixtureFiles.Count)."
}

$manifest = Get-Content -Raw -Path $manifestPath | ConvertFrom-Json

if ($manifest.claimScope -ne "ValidationAnchorOnly") {
    throw "External validation anchors manifest must use claimScope=ValidationAnchorOnly."
}

foreach ($nonClaim in @(
    "No exact pyBuildingEnergy numerical parity claim.",
    "No exact EnergyPlus numerical parity claim.",
    "No ASHRAE 140 validation coverage claim.",
    "Validation anchors only, not full parity."
)) {
    if (@($manifest.explicitNonClaims) -notcontains $nonClaim) {
        throw "External validation anchors manifest is missing non-claim: $nonClaim"
    }
}

$manifestFixtures = @($manifest.fixtures) | Sort-Object
$diskFixtures = @(
    $fixtureFiles | ForEach-Object {
        $_.FullName.Substring($RepoRoot.Length).TrimStart('\', '/') -replace '\\', '/'
    }
) | Sort-Object

foreach ($fixture in $diskFixtures) {
    if ($manifestFixtures -notcontains $fixture) {
        throw "External validation anchors manifest is missing fixture listed on disk: $fixture"
    }
}

$anchorIds = New-Object System.Collections.Generic.HashSet[string]

foreach ($fixtureFile in $fixtureFiles) {
    $fixture = Get-Content -Raw -Path $fixtureFile.FullName | ConvertFrom-Json

    if (-not $anchorIds.Add([string]$fixture.anchorId)) {
        throw "Duplicate ISO52016 Matrix external validation anchor id: $($fixture.anchorId)"
    }

    if ($fixture.claimScope -ne "ValidationAnchorOnly") {
        throw "Fixture $($fixtureFile.Name) must use claimScope=ValidationAnchorOnly."
    }

    foreach ($nonClaim in @(
        "No exact pyBuildingEnergy numerical parity claim.",
        "No exact EnergyPlus numerical parity claim.",
        "No ASHRAE 140 validation coverage claim."
    )) {
        if (@($fixture.explicitNonClaims) -notcontains $nonClaim) {
            throw "Fixture $($fixtureFile.Name) is missing non-claim: $nonClaim"
        }
    }
}

$annualFixturePath = Join-Path $anchorDirectory "energyplus-style-annual-manual-8760.json"
$annualFixture = Get-Content -Raw -Path $annualFixturePath | ConvertFrom-Json

if ($annualFixture.hourCount -ne 8760) {
    throw "Annual external validation anchor must declare exactly 8760 hours."
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

$annualAnchorVerificationScript = Join-Path $RepoRoot "scripts\iso52016\verify-iso52016-matrix-external-validation-annual-anchors.ps1"

if (-not (Test-Path $annualAnchorVerificationScript)) {
    throw "Required ISO52016 Matrix annual external validation anchor verification script is missing: scripts\iso52016\verify-iso52016-matrix-external-validation-annual-anchors.ps1"
}

$annualAnchorArguments = @("-RepoRoot", $RepoRoot)

if ($SkipTests) {
    $annualAnchorArguments += "-SkipTests"
}

& $annualAnchorVerificationScript @annualAnchorArguments


# AE_STEP03_ANNUAL_ANCHORS_BEGIN
$annualExternalValidationAnchorsScript = Join-Path $RepoRoot "scripts\iso52016\verify-iso52016-matrix-external-validation-annual-anchors.ps1"

if (Test-Path $annualExternalValidationAnchorsScript) {
    $annualExternalValidationAnchorsArgs = @("-RepoRoot", $RepoRoot)

    if ($SkipTests) {
        $annualExternalValidationAnchorsArgs += "-SkipTests"
    }

    & $annualExternalValidationAnchorsScript @annualExternalValidationAnchorsArgs
    $annualExternalValidationAnchorsExitCode = $LASTEXITCODE

    if ($annualExternalValidationAnchorsExitCode -ne 0) {
        throw ("ISO52016 Matrix annual external validation anchors verification failed with exit code {0}." -f $annualExternalValidationAnchorsExitCode)
    }
}
# AE_STEP03_ANNUAL_ANCHORS_END

# AE_STEP04_NAMING_ANCHORS_BEGIN
$namingExternalValidationAnchorsScript = Join-Path $RepoRoot "scripts\iso52016\verify-iso52016-matrix-external-validation-naming-anchors.ps1"

if (Test-Path $namingExternalValidationAnchorsScript) {
    $namingExternalValidationAnchorsArgs = @("-RepoRoot", $RepoRoot)

    if ($SkipTests) {
        $namingExternalValidationAnchorsArgs += "-SkipTests"
    }

    & $namingExternalValidationAnchorsScript @namingExternalValidationAnchorsArgs
    $namingExternalValidationAnchorsExitCode = $LASTEXITCODE

    if ($namingExternalValidationAnchorsExitCode -ne 0) {
        throw ("ISO52016 Matrix external validation naming anchors verification failed with exit code {0}." -f $namingExternalValidationAnchorsExitCode)
    }
}
# AE_STEP04_NAMING_ANCHORS_END
Write-Host "ISO52016 Matrix external validation anchors verification passed."



