param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests,
    [switch] $SkipStage,
    [switch] $SkipBaselines,
    [switch] $SkipApplicationBaselines,
    [switch] $SkipSummaryExporter
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

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
    "docs\calculations\Iso52016MatrixVerificationRunbook.md",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixAllVerificationScriptTests.cs"
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

if (-not $SkipSummaryExporter) {
    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\export-iso52016-matrix-baseline-summary.ps1"
}

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixAllVerificationScript|FullyQualifiedName~Iso52016MatrixVerificationGate|FullyQualifiedName~Iso52016MatrixBaselineFixture|FullyQualifiedName~Iso52016MatrixApplicationBaselineFixture|FullyQualifiedName~Iso52016MatrixBaselineSummaryExporter"
    }
    finally {
        Pop-Location
    }
}

Write-Host "ISO52016 Matrix all verification passed."