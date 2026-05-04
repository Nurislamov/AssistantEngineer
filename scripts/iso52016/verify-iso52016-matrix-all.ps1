param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests,
    [switch] $SkipStage,
    [switch] $SkipBaselines,
    [switch] $SkipApplicationBaselines,
    [switch] $SkipSummaryExporter,
    [switch] $SkipExternalValidation,
    [switch] $SkipExternalValidationAnchors
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ValidationAnchorOnly: ISO52016 Matrix external validation anchors are independent manual engineering validation anchors only, not pyBuildingEnergy/EnergyPlus parity.

function Invoke-RepoScript {
    param(
        [Parameter(Mandatory = $true)] [string] $RelativePath,
        [string[]] $Arguments = @()
    )

    $path = Join-Path $RepoRoot $RelativePath

    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix verification script is missing: $RelativePath"
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
    "scripts\iso52016\verify-iso52016-matrix-solver-stage.ps1",
    "scripts\iso52016\verify-iso52016-matrix-baselines.ps1",
    "scripts\iso52016\verify-iso52016-matrix-application-baselines.ps1",
    "scripts\iso52016\export-iso52016-matrix-baseline-summary.ps1",
    "scripts\iso52016\verify-iso52016-matrix-external-validation.ps1",
    "scripts\iso52016\verify-iso52016-matrix-external-validation-anchors.ps1",
    "scripts\iso52016\verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1",
    "docs\calculations\Iso52016MatrixVerificationRunbook.md",
    "docs\calculations\Iso52016MatrixExternalValidationAnchors.md",
    "docs\releases\Iso52016MatrixExternalValidationAnchorsManifest.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixAllVerificationScriptTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorManifestTests.cs"
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath

    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix all-verification file is missing: $relativePath"
    }
}

if (-not $SkipStage) {
    $args = @()

    if ($SkipTests) {
        $args += "-SkipTests"
    }

    if ($SkipBaselines) {
        $args += "-SkipBaselines"
    }

    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\verify-iso52016-matrix-solver-stage.ps1" `
        -Arguments $args
}

if (-not $SkipBaselines) {
    $args = @()

    if ($SkipTests) {
        $args += "-SkipTests"
    }

    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\verify-iso52016-matrix-baselines.ps1" `
        -Arguments $args
}

if (-not $SkipApplicationBaselines) {
    $args = @()

    if ($SkipTests) {
        $args += "-SkipTests"
    }

    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\verify-iso52016-matrix-application-baselines.ps1" `
        -Arguments $args
}

if (-not $SkipExternalValidation) {
    $args = @()

    if ($SkipTests) {
        $args += "-SkipTests"
    }

    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\verify-iso52016-matrix-external-validation.ps1" `
        -Arguments $args
}

if (-not $SkipExternalValidationAnchors) {
    $args = @()

    if ($SkipTests) {
        $args += "-SkipTests"
    }

    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1" `
        -Arguments $args
}


$engineeringEdgeCaseArgs = @()

if ($SkipTests) {
    $engineeringEdgeCaseArgs += "-SkipTests"
}

Invoke-RepoScript `
    -RelativePath "scripts\iso52016\verify-iso52016-matrix-engineering-edge-cases.ps1" `
    -Arguments $engineeringEdgeCaseArgs
if (-not $SkipSummaryExporter) {
    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\export-iso52016-matrix-baseline-summary.ps1"
}

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixAllVerificationScript|FullyQualifiedName~Iso52016MatrixVerificationGate|FullyQualifiedName~Iso52016MatrixBaselineFixture|FullyQualifiedName~Iso52016MatrixApplicationBaselineFixture|FullyQualifiedName~Iso52016MatrixBaselineSummaryExporter|FullyQualifiedName~Iso52016MatrixExternalValidationFixture|FullyQualifiedName~Iso52016MatrixExternalValidationAnchor|FullyQualifiedName~Iso52016MatrixEngineeringEdgeCase"
    }
    finally {
        Pop-Location
    }
}

Write-Host "ISO52016 Matrix all verification passed."