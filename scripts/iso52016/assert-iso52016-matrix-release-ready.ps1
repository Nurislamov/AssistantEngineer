param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipIsoVerification,
    [switch] $SkipFullTests,
    [switch] $SkipGeneratedArtifactCheck,
    [switch] $RequireCleanGit
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ValidationAnchorOnly: ISO52016 Matrix external validation anchors are independent manual engineering validation anchors only, not pyBuildingEnergy/EnergyPlus parity.

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
    "scripts\iso52016\verify-iso52016-matrix-all.ps1",
    "scripts\iso52016\verify-iso52016-matrix-solver-stage.ps1",
    "scripts\iso52016\verify-iso52016-matrix-baselines.ps1",
    "scripts\iso52016\verify-iso52016-matrix-application-baselines.ps1",
    "scripts\iso52016\verify-iso52016-matrix-external-validation.ps1",
    "scripts\iso52016\verify-iso52016-matrix-external-validation-anchors.ps1",
    "scripts\iso52016\verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1",
    "scripts\iso52016\assert-iso52016-matrix-external-validation-anchors-release-ready.ps1",
    "scripts\iso52016\export-iso52016-matrix-baseline-summary.ps1",
    "docs\calculations\Iso52016MatrixReleaseReadyGate.md",
    "docs\calculations\Iso52016MatrixExternalValidationAnchors.md",
    "docs\releases\Iso52016MatrixExternalValidationAnchorsManifest.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixReleaseReadyGateTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorManifestTests.cs",
    "tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj"
)

    "scripts\iso52016\verify-iso52016-matrix-engineering-edge-cases.ps1",
    "docs\calculations\Iso52016MatrixEngineeringEdgeCases.md",
    "docs\releases\Iso52016MatrixEngineeringEdgeCasesManifest.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixEngineeringEdgeCaseTests.cs",
foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath

    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix release-ready file is missing: $relativePath"
    }
}

if (-not $SkipIsoVerification) {
    Invoke-RepoCommand {
        .\scripts\iso52016\assert-iso52016-matrix-external-validation-anchors-release-ready.ps1 -SkipFullTests
    }

    Invoke-RepoCommand {
        .\scripts\iso52016\verify-iso52016-matrix-all.ps1
    }
}


if (-not $SkipIsoVerification) {
    Invoke-RepoCommand {
        .\scripts\iso52016\assert-iso52016-matrix-engineering-edge-cases-release-ready.ps1 -SkipGeneratedSummary
    }
}
if (-not $SkipFullTests) {
    Invoke-RepoCommand {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj
    }
}

if (-not $SkipGeneratedArtifactCheck) {
    Invoke-RepoCommand {
        $trackedArtifacts = git ls-files artifacts/iso52016/matrix-baselines

        if (-not [string]::IsNullOrWhiteSpace($trackedArtifacts)) {
            throw "Generated ISO52016 Matrix baseline summaries are tracked by git. Remove them from the index: $trackedArtifacts"
        }
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
            throw "Working tree is not clean. Commit or stash changes before release-ready assertion."
        }
    }
}

Write-Host "ISO52016 Matrix release-ready assertion passed."