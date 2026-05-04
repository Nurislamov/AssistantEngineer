param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path $RepoRoot).Path

$stageId = "ISO52016-MATRIX-ENGINEERING-EDGE-CASES"

$requiredFiles = @(
    "docs\calculations\Iso52016MatrixEngineeringEdgeCases.md",
    "docs\releases\Iso52016MatrixEngineeringEdgeCasesManifest.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixEngineeringEdgeCaseTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\EngineeringEdgeCases\engineering-iso52016-matrix-edge-001-two-node-free-floating.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\EngineeringEdgeCases\engineering-iso52016-matrix-edge-002-adjacent-unconditioned-boundary.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\EngineeringEdgeCases\engineering-iso52016-matrix-edge-003-timestep-energy-scaling.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\EngineeringEdgeCases\engineering-iso52016-matrix-edge-004-gain-sign-conventions.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\EngineeringEdgeCases\engineering-iso52016-matrix-edge-005-monthly-aggregation.json"
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath

    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix engineering edge-case file is missing: $relativePath"
    }
}

$manifestPath = Join-Path $RepoRoot "docs\releases\Iso52016MatrixEngineeringEdgeCasesManifest.json"
$manifest = Get-Content -Raw -Path $manifestPath | ConvertFrom-Json

if ($manifest.stageId -ne $stageId) {
    throw "Engineering edge-case manifest stageId mismatch: $($manifest.stageId)"
}

if ($manifest.scope -ne "EngineeringHardeningOnly") {
    throw "Engineering edge-case manifest scope must be EngineeringHardeningOnly."
}

if (-not $manifest.multiNodeResponseAnchorsIntegrated) {
    throw "Engineering edge-case manifest must integrate multi-node response anchors."
}

if (-not $manifest.adjacentUnconditionedBoundaryAnchorsIntegrated) {
    throw "Engineering edge-case manifest must integrate adjacent/unconditioned boundary anchors."
}

if (-not $manifest.timeStepSensitivityAnchorsIntegrated) {
    throw "Engineering edge-case manifest must integrate timestep sensitivity anchors."
}

if (-not $manifest.signConventionAnchorsIntegrated) {
    throw "Engineering edge-case manifest must integrate sign convention anchors."
}

if (-not $manifest.aggregationEdgeCaseAnchorsIntegrated) {
    throw "Engineering edge-case manifest must integrate aggregation edge-case anchors."
}

if ($manifest.fixtures.Count -ne 5) {
    throw "Expected exactly 5 ISO52016 Matrix engineering edge-case fixtures, found $($manifest.fixtures.Count)."
}

foreach ($fixture in $manifest.fixtures) {
    $fixturePath = Join-Path $RepoRoot $fixture

    if (-not (Test-Path $fixturePath)) {
        throw "Engineering edge-case manifest references a missing fixture: $fixture"
    }

    $fixtureJson = Get-Content -Raw -Path $fixturePath | ConvertFrom-Json

    if ($fixtureJson.scope -ne "EngineeringEdgeCaseHardening") {
        throw "Engineering edge-case fixture must use scope EngineeringEdgeCaseHardening: $fixture"
    }

    if ($fixtureJson.validationStatus -ne "InternalEngineeringAnchorOnly") {
        throw "Engineering edge-case fixture must use validationStatus InternalEngineeringAnchorOnly: $fixture"
    }
}

$docPath = Join-Path $RepoRoot "docs\calculations\Iso52016MatrixEngineeringEdgeCases.md"
$doc = Get-Content -Raw -Path $docPath

foreach ($literal in @(
    "Engineering edge-case hardening only.",
    "Validation anchors only, not full parity.",
    "No pyBuildingEnergy parity claim.",
    "No EnergyPlus parity claim.",
    "No ASHRAE 140 validation coverage claim.",
    "No full ISO 52016 parity claim."
)) {
    if (-not $doc.Contains($literal)) {
        throw "Engineering edge-case documentation is missing required non-claim literal: $literal"
    }
}

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixEngineeringEdgeCase"
    }
    finally {
        Pop-Location
    }
}

Write-Host "ISO52016 Matrix engineering edge-case verification passed."