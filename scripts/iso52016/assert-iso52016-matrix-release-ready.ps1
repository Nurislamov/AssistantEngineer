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
    "scripts\iso52016\verify-iso52016-matrix-all.ps1",
    "scripts\iso52016\verify-iso52016-matrix-solver-stage.ps1",
    "scripts\iso52016\verify-iso52016-matrix-baselines.ps1",
    "scripts\iso52016\verify-iso52016-matrix-application-baselines.ps1",
    "scripts\iso52016\export-iso52016-matrix-baseline-summary.ps1",
    "docs\calculations\Iso52016MatrixReleaseReadyGate.md",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixReleaseReadyGateTests.cs",
    "tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj"
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath

    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix release-ready file is missing: $relativePath"
    }
}

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