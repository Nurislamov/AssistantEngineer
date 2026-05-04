param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests,
    [switch] $SkipGeneratedArtifactCheck
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
        throw "Required ISO52016 Matrix engineering edge-case script is missing: $RelativePath"
    }

    Push-Location $RepoRoot
    try {
        & $path @Arguments
    }
    finally {
        Pop-Location
    }
}

# Contract literal: EngineeringHardeningOnly
# Contract literal: Validation anchors only, not full parity.

$requiredFiles = @(
    "scripts\iso52016\verify-iso52016-matrix-engineering-edge-cases.ps1",
    "scripts\iso52016\verify-iso52016-matrix-engineering-edge-cases-stage-gate.ps1",
    "scripts\iso52016\assert-iso52016-matrix-engineering-edge-cases-release-ready.ps1",
    "scripts\iso52016\write-iso52016-matrix-engineering-edge-cases-merge-summary.ps1",
    "docs\calculations\Iso52016MatrixEngineeringEdgeCases.md",
    "docs\calculations\Iso52016MatrixEngineeringEdgeCasesReleaseGate.md",
    "docs\releases\Iso52016MatrixEngineeringEdgeCasesManifest.json",
    "docs\releases\Iso52016MatrixEngineeringEdgeCasesReleaseManifest.json",
    "docs\releases\Iso52016MatrixEngineeringEdgeCasesReleaseNotes.md",
    "docs\runbooks\Iso52016MatrixEngineeringEdgeCasesMergeRunbook.md",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixEngineeringEdgeCaseTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixEngineeringEdgeCasesReleaseGateTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixEngineeringEdgeCasesClosureTests.cs"
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath

    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix engineering edge-case stage-gate file is missing: $relativePath"
    }
}

$manifestPath = Join-Path $RepoRoot "docs\releases\Iso52016MatrixEngineeringEdgeCasesReleaseManifest.json"
$manifest = Get-Content -Raw -Path $manifestPath | ConvertFrom-Json

if ($manifest.scope -ne "EngineeringHardeningOnly") {
    throw "Engineering edge-case release manifest scope must be EngineeringHardeningOnly."
}

if ($manifest.explicitNonClaims -notcontains "Validation anchors only, not full parity.") {
    throw "Engineering edge-case release manifest must keep the validation-anchor-only non-claim."
}

if (-not $SkipGeneratedArtifactCheck) {
    Push-Location $RepoRoot
    try {
        $trackedArtifacts = git ls-files artifacts/iso52016/engineering-edge-cases

        if (-not [string]::IsNullOrWhiteSpace($trackedArtifacts)) {
            throw "Generated ISO52016 Matrix engineering edge-case artifacts are tracked by git. Remove them from the index: $trackedArtifacts"
        }
    }
    finally {
        Pop-Location
    }
}

$args = @()

if ($SkipTests) {
    $args += "-SkipTests"
}

Invoke-RepoScript `
    -RelativePath "scripts\iso52016\verify-iso52016-matrix-engineering-edge-cases.ps1" `
    -Arguments $args

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixEngineeringEdgeCase"
    }
    finally {
        Pop-Location
    }
}

Write-Host "ISO52016 Matrix engineering edge cases stage gate passed."