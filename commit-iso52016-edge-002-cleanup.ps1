param(
    [string] $RepoRoot = (Get-Location).Path,
    [string] $BranchName = "iso52016-matrix-engineering-edge-cases-001",
    [string] $CommitMessage = "Close ISO52016 Matrix engineering edge cases stage",
    [switch] $RunVerification,
    [switch] $SkipCommit,
    [switch] $AllowDirtyTrackedBeforeCleanup
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

function Assert-RepoRoot {
    $gitDir = Join-Path $RepoRoot ".git"
    $testsDir = Join-Path $RepoRoot "tests\AssistantEngineer.Tests"
    $srcDir = Join-Path $RepoRoot "src\Backend"

    if (-not (Test-Path $gitDir)) {
        throw "RepoRoot does not look like a git repository: $RepoRoot"
    }

    if (-not (Test-Path $testsDir)) {
        throw "AssistantEngineer tests directory was not found under RepoRoot: $testsDir"
    }

    if (-not (Test-Path $srcDir)) {
        throw "AssistantEngineer src directory was not found under RepoRoot: $srcDir"
    }
}

function Assert-NoTrackedGeneratedArtifacts {
    $generatedArtifactRoots = @(
        "artifacts/iso52016/engineering-edge-cases",
        "artifacts/iso52016/external-validation-anchors",
        "artifacts/iso52016/matrix-baselines"
    )

    foreach ($artifactRoot in $generatedArtifactRoots) {
        $trackedArtifacts = Invoke-RepoCommand {
            git ls-files $artifactRoot
        }

        if (-not [string]::IsNullOrWhiteSpace($trackedArtifacts)) {
            throw "Generated ISO52016 artifacts are tracked by git under $artifactRoot. Remove them from the index before committing: $trackedArtifacts"
        }
    }
}

function Remove-TemporaryPatchScripts {
    $patterns = @(
        "AE-ISO52016-EDGE-002*.ps1"
    )

    foreach ($pattern in $patterns) {
        Get-ChildItem -Path $RepoRoot -File -Filter $pattern -ErrorAction SilentlyContinue |
            ForEach-Object {
                Write-Host "Removing temporary root patch script: $($_.Name)"
                Remove-Item -LiteralPath $_.FullName -Force -ErrorAction SilentlyContinue
            }
    }
}

function Ensure-Branch {
    $currentBranch = Invoke-RepoCommand {
        git branch --show-current
    }

    if ($currentBranch -eq $BranchName) {
        Write-Host "Already on branch $BranchName."
        return
    }

    $branchExists = Invoke-RepoCommand {
        git branch --list $BranchName
    }

    if ([string]::IsNullOrWhiteSpace($branchExists)) {
        Write-Host "Creating branch $BranchName from current HEAD."
        Invoke-RepoCommand {
            git checkout -b $BranchName
        }
    }
    else {
        Write-Host "Switching to branch $BranchName."
        Invoke-RepoCommand {
            git checkout $BranchName
        }
    }
}

function Invoke-Verification {
    $releaseReadyScript = Join-Path $RepoRoot "scripts\iso52016\assert-iso52016-matrix-engineering-edge-cases-release-ready.ps1"
    $allVerificationScript = Join-Path $RepoRoot "scripts\iso52016\verify-iso52016-matrix-all.ps1"

    if (-not (Test-Path $releaseReadyScript)) {
        throw "Engineering edge cases release-ready script was not found: $releaseReadyScript"
    }

    if (-not (Test-Path $allVerificationScript)) {
        throw "ISO52016 Matrix all-verification script was not found: $allVerificationScript"
    }

    Invoke-RepoCommand {
        .\scripts\iso52016\assert-iso52016-matrix-engineering-edge-cases-release-ready.ps1
    }

    Invoke-RepoCommand {
        .\scripts\iso52016\verify-iso52016-matrix-all.ps1
    }
}

Assert-RepoRoot
Ensure-Branch

if (-not $AllowDirtyTrackedBeforeCleanup) {
    $trackedDirtyBeforeCleanup = Invoke-RepoCommand {
        git status --porcelain
    }

    if ([string]::IsNullOrWhiteSpace($trackedDirtyBeforeCleanup)) {
        Write-Host "Working tree is clean before cleanup."
    }
    else {
        Write-Host "Working tree has pending Stage 2.2 changes; continuing because this cleanup script is intended to commit them."
    }
}

Remove-TemporaryPatchScripts
Assert-NoTrackedGeneratedArtifacts

if ($RunVerification) {
    Invoke-Verification
}

Invoke-RepoCommand {
    git add -A
}

$staged = Invoke-RepoCommand {
    git diff --cached --name-only
}

if ([string]::IsNullOrWhiteSpace($staged)) {
    Write-Host "No staged changes found. Nothing to commit."
    exit 0
}

Write-Host "Staged files:"
Write-Host $staged

if ($SkipCommit) {
    Write-Host "SkipCommit was specified. Review staged changes with: git diff --cached"
    exit 0
}

Invoke-RepoCommand {
    git commit -m $CommitMessage
}

Write-Host "EDGE-002 cleanup and commit complete."
Write-Host "Next checks:"
Write-Host "  git status"
Write-Host "  git log --oneline -5"
Write-Host "  .\scripts\iso52016\verify-iso52016-matrix-all.ps1"
