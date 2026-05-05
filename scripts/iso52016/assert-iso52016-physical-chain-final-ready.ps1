param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipReleaseGate,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

function Assert-RequiredFiles {
    param(
        [Parameter(Mandatory = $true)] [string[]] $RelativePaths
    )

    foreach ($relativePath in $RelativePaths) {
        $path = Join-Path $RepoRoot $relativePath
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Required ISO52016 physical final readiness file is missing: ${relativePath}"
        }
    }
}

function Invoke-RepoScript {
    param(
        [Parameter(Mandatory = $true)] [string] $RelativePath,
        [hashtable] $Arguments = @{}
    )

    $path = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required ISO52016 physical dependency verifier is missing: ${RelativePath}"
    }

    Push-Location $RepoRoot
    try {
        & $path @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Dependency verifier failed with exit code ${LASTEXITCODE}: ${RelativePath}"
        }
    }
    finally {
        Pop-Location
    }
}

function Invoke-StageTests {
    param(
        [Parameter(Mandatory = $true)] [string] $Filter
    )

    if ($SkipTests) {
        return
    }

    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter $Filter
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet test failed with exit code ${LASTEXITCODE} for filter: ${Filter}"
        }
    }
    finally {
        Pop-Location
    }
}

$requiredFiles = @(
    'docs\calculations\Iso52016PhysicalChainFinalReadiness.md',
    'docs\releases\Iso52016PhysicalChainFinalReadinessManifest.json',
    'docs\traceability\Iso52016PhysicalChainTraceabilityMatrix.json',
    'scripts\iso52016\assert-iso52016-physical-chain-final-ready.ps1',
    'scripts\iso52016\assert-iso52016-physical-model-chain-release-ready.ps1',
    'scripts\iso52016\verify-iso52016-physical-model-chain.ps1',
    'tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalChainFinalReadinessTests.cs'
)

Assert-RequiredFiles -RelativePaths $requiredFiles

if (-not $SkipReleaseGate) {
    $releaseGateArgs = @{
        RepoRoot = $RepoRoot
    }

    if ($SkipTests) {
        $releaseGateArgs.SkipTests = $true
    }

    Invoke-RepoScript `
        -RelativePath 'scripts\iso52016\assert-iso52016-physical-model-chain-release-ready.ps1' `
        -Arguments $releaseGateArgs
}

Invoke-StageTests -Filter 'FullyQualifiedName~Iso52016PhysicalChainFinalReadiness'

Write-Host 'ISO52016 physical chain final readiness passed - validation/internal engineering anchors only.'

# Traceability literal markers:
# AE-ISO52016-002-STEP-12
# Iso52016PhysicalChainFinalReadinessManifest.json
# Iso52016PhysicalChainTraceabilityMatrix.json
# assert-iso52016-physical-chain-final-ready.ps1
# validation/internal engineering anchors only
# ReducedMatrix default path
# PhysicalNodeModel explicit opt-in path