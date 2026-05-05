param(
    [string] $RepoRoot = (Get-Location).Path,
    [string] $FeatureBranch = "iso52016-matrix-engineering-edge-cases-001",
    [string] $TargetBranch = "master",
    [switch] $SkipPull,
    [switch] $SkipVerification,
    [switch] $Push
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

function Assert-CleanTrackedWorkingTree {
    Invoke-RepoCommand {
        $status = git status --porcelain
        $trackedChanges = $status |
            Where-Object {
                $_ -and
                -not $_.StartsWith("?? ")
            }

        if ($trackedChanges) {
            throw "Working tree has tracked changes. Commit or stash before merge:`n$($trackedChanges -join [Environment]::NewLine)"
        }
    }
}

function Assert-BranchExists {
    param(
        [Parameter(Mandatory = $true)] [string] $BranchName
    )

    Invoke-RepoCommand {
        git rev-parse --verify $BranchName | Out-Null
    }
}

function Invoke-Verification {
    param(
        [Parameter(Mandatory = $true)] [string] $Phase
    )

    if ($SkipVerification) {
        Write-Host "Skipping verification during $Phase."
        return
    }

    Invoke-RepoCommand {
        $edgeReleaseReady = ".\scripts\iso52016\assert-iso52016-matrix-engineering-edge-cases-release-ready.ps1"
        $matrixAll = ".\scripts\iso52016\verify-iso52016-matrix-all.ps1"

        if (-not (Test-Path $edgeReleaseReady)) {
            throw "Required engineering edge-case release-ready script is missing: $edgeReleaseReady"
        }

        if (-not (Test-Path $matrixAll)) {
            throw "Required Matrix all verification script is missing: $matrixAll"
        }

        & $edgeReleaseReady
        & $matrixAll
    }
}

Push-Location $RepoRoot
try {
    if (-not (Test-Path ".git")) {
        throw "RepoRoot does not look like a git repository: $RepoRoot"
    }
}
finally {
    Pop-Location
}

Assert-BranchExists -BranchName $FeatureBranch
Assert-BranchExists -BranchName $TargetBranch
Assert-CleanTrackedWorkingTree

Invoke-RepoCommand {
    git checkout $FeatureBranch
}

Invoke-Verification -Phase "feature branch pre-merge"

Invoke-RepoCommand {
    git checkout $TargetBranch
}

if (-not $SkipPull) {
    Invoke-RepoCommand {
        git pull --ff-only origin $TargetBranch
    }
}

Assert-CleanTrackedWorkingTree

Invoke-RepoCommand {
    $alreadyMerged = git merge-base --is-ancestor $FeatureBranch $TargetBranch
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Feature branch '$FeatureBranch' is already merged into '$TargetBranch'."
    }
    else {
        git merge --no-ff $FeatureBranch -m "Merge ISO52016 Matrix engineering edge cases stage"
    }
}

Invoke-Verification -Phase "target branch post-merge"

if ($Push) {
    Invoke-RepoCommand {
        git push origin $TargetBranch
    }
}

Write-Host "ISO52016 Matrix engineering edge cases stage merged into $TargetBranch."
if (-not $Push) {
    Write-Host "Push was not requested. To publish, run: git push origin $TargetBranch"
}
