param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests,
    [switch] $SkipGeneratedSummary,
    [switch] $SkipGeneratedArtifactCheck,
    [switch] $RequireCleanGit
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
        throw "Required ISO52016 Matrix engineering edge-case release script is missing: $RelativePath"
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
# Contract literal: git ls-files artifacts/iso52016/engineering-edge-cases

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
        throw "Required ISO52016 Matrix engineering edge-case release-ready file is missing: $relativePath"
    }
}

$stageGateArgs = @()

if ($SkipTests) {
    $stageGateArgs += "-SkipTests"
}

if ($SkipGeneratedArtifactCheck) {
    $stageGateArgs += "-SkipGeneratedArtifactCheck"
}

Invoke-RepoScript `
    -RelativePath "scripts\iso52016\verify-iso52016-matrix-engineering-edge-cases-stage-gate.ps1" `
    -Arguments $stageGateArgs

if (-not $SkipGeneratedSummary) {
    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\write-iso52016-matrix-engineering-edge-cases-merge-summary.ps1"
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

if ($RequireCleanGit) {
    Push-Location $RepoRoot
    try {
        $status = git status --porcelain

        if (-not [string]::IsNullOrWhiteSpace($status)) {
            throw "Working tree is not clean. Commit or stash changes before release-ready assertion."
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host "ISO52016 Matrix engineering edge cases release-ready assertion passed."