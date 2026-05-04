param(
    [string] $RepoRoot = (Get-Location).Path,
    [string] $BranchName = "iso52016-matrix-engineering-edge-cases-001",
    [string] $CommitMessage = "Add ISO52016 Matrix engineering edge case anchors",
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

function Assert-RepositoryRoot {
    $gitDir = Join-Path $RepoRoot ".git"
    $testProject = Join-Path $RepoRoot "tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj"

    if (-not (Test-Path $gitDir)) {
        throw "RepoRoot does not look like a git repository: $RepoRoot"
    }

    if (-not (Test-Path $testProject)) {
        throw "AssistantEngineer test project was not found under RepoRoot: $RepoRoot"
    }
}

function Switch-ToWorkBranch {
    Invoke-RepoCommand {
        $currentBranch = (git branch --show-current).Trim()

        if ($currentBranch -eq $BranchName) {
            Write-Host "Already on branch $BranchName."
            return
        }

        $existingBranch = git branch --list $BranchName

        if ([string]::IsNullOrWhiteSpace($existingBranch)) {
            git switch -c $BranchName
        }
        else {
            git switch $BranchName
        }
    }
}

function Remove-TemporaryPatchScripts {
    Invoke-RepoCommand {
        $patterns = @(
            "AE-ISO52016-EDGE-001*.ps1"
        )

        foreach ($pattern in $patterns) {
            Get-ChildItem -Path "." -File -Filter $pattern -ErrorAction SilentlyContinue |
                ForEach-Object {
                    Write-Host "Removing temporary root patch script: $($_.Name)"
                    Remove-Item $_.FullName -Force
                }
        }
    }
}

function Assert-NoGeneratedArtifactsTracked {
    Invoke-RepoCommand {
        $trackedEdgeArtifacts = git ls-files artifacts/iso52016/engineering-edge-cases

        if (-not [string]::IsNullOrWhiteSpace($trackedEdgeArtifacts)) {
            throw "Generated ISO52016 engineering edge-case artifacts are tracked by git. Remove them from the index: $trackedEdgeArtifacts"
        }

        $trackedExternalAnchorArtifacts = git ls-files artifacts/iso52016/external-validation-anchors

        if (-not [string]::IsNullOrWhiteSpace($trackedExternalAnchorArtifacts)) {
            throw "Generated ISO52016 external validation anchor artifacts are tracked by git. Remove them from the index: $trackedExternalAnchorArtifacts"
        }

        $trackedMatrixBaselineArtifacts = git ls-files artifacts/iso52016/matrix-baselines

        if (-not [string]::IsNullOrWhiteSpace($trackedMatrixBaselineArtifacts)) {
            throw "Generated ISO52016 Matrix baseline artifacts are tracked by git. Remove them from the index: $trackedMatrixBaselineArtifacts"
        }
    }
}

function Invoke-Verification {
    Invoke-RepoCommand {
        if (-not (Test-Path ".\scripts\iso52016\verify-iso52016-matrix-engineering-edge-cases.ps1")) {
            throw "Engineering edge-case verification script is missing."
        }

        .\scripts\iso52016\verify-iso52016-matrix-engineering-edge-cases.ps1

        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixEngineeringEdgeCase"

        .\scripts\iso52016\verify-iso52016-matrix-all.ps1
    }
}

function Commit-Changes {
    Invoke-RepoCommand {
        git add -A

        $status = git status --porcelain

        if ([string]::IsNullOrWhiteSpace($status)) {
            Write-Host "No changes to commit."
            return
        }

        git commit -m $CommitMessage
    }
}

Assert-RepositoryRoot
Switch-ToWorkBranch
Remove-TemporaryPatchScripts
Assert-NoGeneratedArtifactsTracked

if ($RunVerification) {
    Invoke-Verification
}

if (-not $SkipCommit) {
    Commit-Changes
}
else {
    Invoke-RepoCommand {
        git add -A
        git status --short
    }

    Write-Host "SkipCommit was set. Changes are staged but not committed."
}

Write-Host "ISO52016 Matrix engineering edge cases patch 001 cleanup completed."
