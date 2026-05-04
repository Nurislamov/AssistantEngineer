param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipIsoVerification,
    [switch] $SkipFullTests,
    [switch] $SkipGeneratedArtifactCheck,
    [switch] $RequireCleanGit
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

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
    "docs\releases\Iso52016MatrixExternalValidationAnnualAnchorsManifest.json",
    "scripts\iso52016\verify-iso52016-matrix-external-validation-annual-anchors.ps1",
    "scripts\iso52016\verify-iso52016-matrix-all.ps1",
    "scripts\iso52016\verify-iso52016-matrix-solver-stage.ps1",
    "scripts\iso52016\verify-iso52016-matrix-baselines.ps1",
    "scripts\iso52016\verify-iso52016-matrix-application-baselines.ps1",
    "scripts\iso52016\verify-iso52016-matrix-external-validation.ps1",
    "scripts\iso52016\verify-iso52016-matrix-external-validation-anchors.ps1",
    "scripts\iso52016\export-iso52016-matrix-baseline-summary.ps1",
    "docs\calculations\Iso52016MatrixReleaseReadyGate.md",
    "docs\calculations\Iso52016MatrixExternalValidationAnchors.md",
    "docs\releases\Iso52016MatrixExternalValidationAnchorsManifest.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixReleaseReadyGateTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorManifestTests.cs",
    "tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj"
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath

    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix release-ready file is missing: $relativePath"
    }
}


# AE_STEP04_NAMING_ANCHORS_BEGIN
$namingAnchorsScript = Join-Path $RepoRoot "scripts\iso52016\verify-iso52016-matrix-external-validation-naming-anchors.ps1"

if (-not (Test-Path $namingAnchorsScript)) {
    throw "Required ISO52016 Matrix release-ready naming anchors verification script is missing: scripts\iso52016\verify-iso52016-matrix-external-validation-naming-anchors.ps1"
}
# AE_STEP04_NAMING_ANCHORS_END

# AE_STEP05_EXTERNAL_VALIDATION_STAGE_GATE_BEGIN
$externalValidationStageGateScript = Join-Path $RepoRoot "scripts\iso52016\verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1"

if (-not (Test-Path $externalValidationStageGateScript)) {
    throw "Required ISO52016 Matrix release-ready external validation anchors stage-gate script is missing: scripts\iso52016\verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1"
}
# AE_STEP05_EXTERNAL_VALIDATION_STAGE_GATE_END

# AE_STEP06_EXTERNAL_VALIDATION_ANCHORS_RELEASE_GATE_BEGIN
$externalValidationAnchorsReleaseReadyScript = Join-Path $RepoRoot "scripts\iso52016\assert-iso52016-matrix-external-validation-anchors-release-ready.ps1"

if (-not (Test-Path $externalValidationAnchorsReleaseReadyScript)) {
    throw "Required ISO52016 Matrix external validation anchors release-ready script is missing: scripts\iso52016\assert-iso52016-matrix-external-validation-anchors-release-ready.ps1"
}
# AE_STEP06_EXTERNAL_VALIDATION_ANCHORS_RELEASE_GATE_END
if (-not $SkipIsoVerification) {
    Invoke-RepoCommand {
        .\scripts\iso52016\verify-iso52016-matrix-all.ps1
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

if ($RequireCleanGit) {
    Invoke-RepoCommand {
        $status = git status --porcelain

        if (-not [string]::IsNullOrWhiteSpace($status)) {
            throw "Working tree is not clean. Commit or stash changes before release-ready assertion."
        }
    }
}

Write-Host "ISO52016 Matrix release-ready assertion passed."

