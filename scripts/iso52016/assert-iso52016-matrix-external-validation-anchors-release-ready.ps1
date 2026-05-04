param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipStageGate,
    [switch] $SkipTests,
    [switch] $SkipGeneratedArtifactCheck,
    [switch] $RequireCleanGit
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ValidationAnchorOnly: this release-ready gate verifies independent manual engineering validation anchors only.
# It does not claim pyBuildingEnergy parity, EnergyPlus parity, ASHRAE 140 validation, or full ISO 52016 conformance.

function Invoke-RepoCommand {
    param(
        [Parameter(Mandatory = $true)] [scriptblock] $Command
    )

    Push-Location $RepoRoot
    try {
        & $Command
    }
    finally {
        Pop-Location
    }
}

$requiredFiles = @(
    "scripts\iso52016\verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1",
    "scripts\iso52016\verify-iso52016-matrix-external-validation-anchors.ps1",
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
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorsClosureTests.cs",
    "tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj"
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath

    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix external validation anchor release-ready file is missing: $relativePath"
    }
}

if (-not $SkipStageGate) {
    Invoke-RepoCommand {
        .\scripts\iso52016\verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1 -SkipTests:$SkipTests
    }
}

if (-not $SkipTests) {
    Invoke-RepoCommand {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixExternalValidationAnchor"
    }
}

if (-not $SkipGeneratedArtifactCheck) {
    Invoke-RepoCommand {
        $trackedExternalValidationAnchorArtifacts = git ls-files artifacts/iso52016/external-validation-anchors

        if (-not [string]::IsNullOrWhiteSpace($trackedExternalValidationAnchorArtifacts)) {
            throw "Generated ISO52016 Matrix external validation anchor artifacts are tracked by git. Remove them from the index: $trackedExternalValidationAnchorArtifacts"
        }
    }
}

if ($RequireCleanGit) {
    Invoke-RepoCommand {
        $status = git status --porcelain

        if (-not [string]::IsNullOrWhiteSpace($status)) {
            throw "Working tree is not clean. Commit or stash changes before external validation anchor release-ready assertion."
        }
    }
}

Write-Host "ISO52016 Matrix external validation anchors release-ready assertion passed."