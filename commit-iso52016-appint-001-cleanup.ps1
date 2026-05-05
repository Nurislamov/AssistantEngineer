param(
    [string] $RepoRoot = (Get-Location).Path,
    [string] $BranchName = "iso52016-matrix-application-integration-hardening-001",
    [string] $CommitMessage = "Add ISO52016 Matrix application integration hardening",
    [switch] $RunVerification,
    [switch] $SkipCommit
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

    if (-not (Test-Path $testsDir) -or -not (Test-Path $srcDir)) {
        throw "RepoRoot does not look like AssistantEngineer repo: $RepoRoot"
    }
}

function Assert-NoMergeInProgress {
    Invoke-RepoCommand {
        $mergeHead = Join-Path (git rev-parse --git-dir) "MERGE_HEAD"
        if (Test-Path $mergeHead) {
            throw "A merge is in progress. Resolve or abort it before committing APPINT-001."
        }
    }
}

function Switch-ToBranch {
    Invoke-RepoCommand {
        $currentBranch = git rev-parse --abbrev-ref HEAD

        if ($currentBranch -eq $BranchName) {
            Write-Host "Already on branch $BranchName."
            return
        }

        $existingBranch = git branch --list $BranchName

        if ([string]::IsNullOrWhiteSpace($existingBranch)) {
            git checkout -b $BranchName
        }
        else {
            git checkout $BranchName
        }
    }
}

function Remove-TemporaryPatchScripts {
    $patterns = @(
        "AE-ISO52016-APPINT-001*.ps1"
    )

    foreach ($pattern in $patterns) {
        Get-ChildItem -Path $RepoRoot -Filter $pattern -File -ErrorAction SilentlyContinue |
            ForEach-Object {
                Write-Host "Removing temporary root patch script: $($_.Name)"
                Remove-Item $_.FullName -Force
            }
    }
}

function Assert-GeneratedArtifactsNotTracked {
    Invoke-RepoCommand {
        $generatedArtifactRoots = @(
            "artifacts/iso52016/application-integration-hardening",
            "artifacts/iso52016/engineering-edge-cases",
            "artifacts/iso52016/external-validation-anchors",
            "artifacts/iso52016/matrix-baselines"
        )

        foreach ($artifactRoot in $generatedArtifactRoots) {
            $tracked = git ls-files $artifactRoot

            if (-not [string]::IsNullOrWhiteSpace($tracked)) {
                throw "Generated ISO52016 artifact path is tracked by git: $artifactRoot`n$tracked"
            }
        }
    }
}

function Invoke-Verification {
    Invoke-RepoCommand {
        $applicationIntegrationVerify = ".\scripts\iso52016\verify-iso52016-matrix-application-integration-hardening.ps1"
        $allVerify = ".\scripts\iso52016\verify-iso52016-matrix-all.ps1"

        if (-not (Test-Path $applicationIntegrationVerify)) {
            throw "Application integration verification script is missing: $applicationIntegrationVerify"
        }

        if (-not (Test-Path $allVerify)) {
            throw "Matrix all verification script is missing: $allVerify"
        }

        & $applicationIntegrationVerify
        & $allVerify
    }
}

function Add-AndCommit {
    Invoke-RepoCommand {
        git add -A

        $staged = git diff --cached --name-only

        if ([string]::IsNullOrWhiteSpace($staged)) {
            Write-Host "No staged changes found. Nothing to commit."
            return
        }

        Write-Host "Staged files:"
        $staged | ForEach-Object { Write-Host " - $_" }

        if ($SkipCommit) {
            Write-Host "SkipCommit was specified. Review staged changes and commit manually."
            return
        }

        git commit -m $CommitMessage
    }
}

Assert-RepoRoot
Assert-NoMergeInProgress
Switch-ToBranch
Remove-TemporaryPatchScripts
Assert-GeneratedArtifactsNotTracked

if ($RunVerification) {
    Invoke-Verification
}

Add-AndCommit

Write-Host "APPINT-001 cleanup complete."
Write-Host "Next checks:"
Write-Host "  git status"
Write-Host "  git log --oneline -5"
Write-Host "  .\scripts\iso52016\verify-iso52016-matrix-application-integration-hardening.ps1"
Write-Host "  .\scripts\iso52016\verify-iso52016-matrix-all.ps1"
