param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Contract literal: ApplicationIntegrationHardeningOnly
# Contract literal: Validation anchors only, not full parity.
# Contract literal: No pyBuildingEnergy parity claim.
# Contract literal: No EnergyPlus parity claim.
# Contract literal: No ASHRAE 140 validation coverage claim.
# Contract literal: No full ISO 52016 parity claim.

function Invoke-RepoScript {
    param(
        [Parameter(Mandatory = $true)] [string] $RelativePath,
        [string[]] $Arguments = @()
    )

    $path = Join-Path $RepoRoot $RelativePath

    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix application integration hardening script is missing: $RelativePath"
    }

    Push-Location $RepoRoot
    try {
        & $path @Arguments
    }
    finally {
        Pop-Location
    }
}

$requiredFiles = @(
    "scripts\iso52016\verify-iso52016-matrix-application-integration-hardening.ps1",
    "docs\calculations\Iso52016MatrixApplicationIntegrationHardening.md",
    "docs\releases\Iso52016MatrixApplicationIntegrationHardeningManifest.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixApplicationIntegrationHardeningTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixApplicationIntegrationHardeningManifestTests.cs"
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath

    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix application integration hardening stage-gate file is missing: $relativePath"
    }
}

$args = @()
if ($SkipTests) {
    $args += "-SkipTests"
}

Invoke-RepoScript `
    -RelativePath "scripts\iso52016\verify-iso52016-matrix-application-integration-hardening.ps1" `
    -Arguments $args

Write-Host "ISO52016 Matrix application integration hardening stage gate passed."