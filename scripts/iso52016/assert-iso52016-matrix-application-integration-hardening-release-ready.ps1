param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests,
    [switch] $SkipGeneratedSummary,
    [switch] $SkipGeneratedArtifactCheck,
    [switch] $RequireCleanGit
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Contract literal: ApplicationIntegrationHardeningOnly
# Contract literal: Validation anchors only, not full parity.
# Contract literal: No pyBuildingEnergy parity claim.
# Contract literal: No EnergyPlus parity claim.
# Contract literal: No ASHRAE 140 validation coverage claim.
# Contract literal: No full ISO 52016 parity claim.

function Invoke-RepoCommand {
    param([Parameter(Mandatory = $true)] [scriptblock] $Command)

    Push-Location $RepoRoot
    try {
        & $Command
    }
    finally {
        Pop-Location
    }
}

function Invoke-RepoScript {
    param(
        [Parameter(Mandatory = $true)] [string] $RelativePath,
        [string[]] $Arguments = @()
    )

    $path = Join-Path $RepoRoot $RelativePath

    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix application integration hardening release-ready script is missing: $RelativePath"
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
    "scripts\iso52016\verify-iso52016-matrix-application-integration-hardening.ps1",
    "scripts\iso52016\verify-iso52016-matrix-application-integration-hardening-stage-gate.ps1",
    "scripts\iso52016\write-iso52016-matrix-application-integration-hardening-merge-summary.ps1",
    "docs\calculations\Iso52016MatrixApplicationIntegrationHardening.md",
    "docs\calculations\Iso52016MatrixApplicationIntegrationHardeningReleaseGate.md",
    "docs\releases\Iso52016MatrixApplicationIntegrationHardeningManifest.json",
    "docs\releases\Iso52016MatrixApplicationIntegrationHardeningReleaseManifest.json",
    "docs\releases\Iso52016MatrixApplicationIntegrationHardeningReleaseNotes.md",
    "docs\runbooks\Iso52016MatrixApplicationIntegrationHardeningMergeRunbook.md",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixApplicationIntegrationHardeningTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixApplicationIntegrationHardeningManifestTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixApplicationIntegrationHardeningReleaseGateTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixApplicationIntegrationHardeningClosureTests.cs"
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath

    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix application integration hardening release-ready file is missing: $relativePath"
    }
}

$args = @()
if ($SkipTests) {
    $args += "-SkipTests"
}

Invoke-RepoScript `
    -RelativePath "scripts\iso52016\verify-iso52016-matrix-application-integration-hardening-stage-gate.ps1" `
    -Arguments $args

if (-not $SkipGeneratedSummary) {
    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\write-iso52016-matrix-application-integration-hardening-merge-summary.ps1"
}

if (-not $SkipTests) {
    Invoke-RepoCommand {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixApplicationIntegrationHardening"
    }
}

if (-not $SkipGeneratedArtifactCheck) {
    Invoke-RepoCommand {
        $trackedArtifacts = git ls-files artifacts/iso52016/application-integration-hardening

        if (-not [string]::IsNullOrWhiteSpace($trackedArtifacts)) {
            throw "Generated ISO52016 Matrix application integration hardening artifacts are tracked by git. Remove them from the index: $trackedArtifacts"
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

Write-Host "ISO52016 Matrix application integration hardening release-ready assertion passed."