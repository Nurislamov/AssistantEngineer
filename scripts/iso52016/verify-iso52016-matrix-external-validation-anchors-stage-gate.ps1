param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ValidationAnchorOnly: this gate verifies independent manual engineering validation anchors only.
# It does not claim pyBuildingEnergy parity, EnergyPlus parity, ASHRAE 140 validation, or full ISO 52016 conformance.

function Invoke-RepoScript {
    param(
        [Parameter(Mandatory = $true)] [string] $RelativePath,
        [string[]] $Arguments = @()
    )

    $path = Join-Path $RepoRoot $RelativePath

    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix external validation anchor script is missing: $RelativePath"
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
    "scripts\iso52016\verify-iso52016-matrix-external-validation-anchors.ps1",
    "scripts\iso52016\assert-iso52016-matrix-external-validation-anchors-release-ready.ps1",
    "scripts\iso52016\write-iso52016-matrix-external-validation-anchors-merge-summary.ps1",
    "docs\calculations\Iso52016MatrixExternalValidationAnchors.md",
    "docs\releases\Iso52016MatrixExternalValidationAnchorsManifest.json",
    "docs\releases\Iso52016MatrixExternalValidationAnchorsReleaseManifest.json",
    "docs\releases\Iso52016MatrixExternalValidationAnchorsReleaseNotes.md",
    "docs\runbooks\Iso52016MatrixExternalValidationAnchorsMergeRunbook.md",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorManifestTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorStageGateTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorsReleaseGateTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorsClosureTests.cs"
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath

    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix external validation anchor stage gate file is missing: $relativePath"
    }
}

$args = @()

if ($SkipTests) {
    $args += "-SkipTests"
}

Invoke-RepoScript `
    -RelativePath "scripts\iso52016\verify-iso52016-matrix-external-validation-anchors.ps1" `
    -Arguments $args

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixExternalValidationAnchor"
    }
    finally {
        Pop-Location
    }
}

Write-Host "ISO52016 Matrix external validation anchors stage gate passed."