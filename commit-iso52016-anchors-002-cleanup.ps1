param(
    [string] $RepoRoot = (Get-Location).Path,
    [string] $BranchName = "iso52016-matrix-external-validation-anchors-001",
    [string] $CommitMessage = "Complete ISO52016 Matrix external validation anchors",
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
    $srcDir = Join-Path $RepoRoot "src\Backend\AssistantEngineer.Modules.Calculations"

    if (-not (Test-Path $gitDir)) {
        throw "RepoRoot does not look like a git repository: $RepoRoot"
    }

    if (-not (Test-Path $testsDir) -or -not (Test-Path $srcDir)) {
        throw "RepoRoot does not look like AssistantEngineer repository root: $RepoRoot"
    }
}

function Remove-TemporaryPatchScripts {
    $patterns = @(
        "AE-ISO52016-ANCHORS-002*.ps1"
    )

    foreach ($pattern in $patterns) {
        Get-ChildItem -Path $RepoRoot -File -Filter $pattern -ErrorAction SilentlyContinue |
            ForEach-Object {
                Write-Host "Removing temporary patch script: $($_.Name)"
                Remove-Item -LiteralPath $_.FullName -Force
            }
    }
}

function Ensure-Branch {
    $currentBranch = (git -C $RepoRoot branch --show-current).Trim()

    if ([string]::IsNullOrWhiteSpace($currentBranch)) {
        throw "Could not determine current git branch."
    }

    if ($currentBranch -eq $BranchName) {
        Write-Host "Already on branch $BranchName."
        return
    }

    $existingBranch = (git -C $RepoRoot branch --list $BranchName).Trim()

    if ([string]::IsNullOrWhiteSpace($existingBranch)) {
        Write-Host "Creating branch $BranchName from $currentBranch."
        git -C $RepoRoot checkout -b $BranchName
    }
    else {
        Write-Host "Switching to branch $BranchName."
        git -C $RepoRoot checkout $BranchName
    }
}

function Assert-NoTrackedGeneratedArtifacts {
    $trackedArtifacts = git -C $RepoRoot ls-files artifacts/iso52016/external-validation-anchors

    if (-not [string]::IsNullOrWhiteSpace($trackedArtifacts)) {
        throw "Generated ISO52016 external validation anchor artifacts are tracked by git. Remove them from index: $trackedArtifacts"
    }
}

Assert-RepoRoot
Ensure-Branch

if ($RunVerification) {
    Invoke-RepoCommand {
        .\scripts\iso52016\verify-iso52016-matrix-all.ps1
    }
}

Remove-TemporaryPatchScripts
Assert-NoTrackedGeneratedArtifacts

git -C $RepoRoot add -A

Write-Host "Staged changes:"
git -C $RepoRoot status --short

if ($SkipCommit) {
    Write-Host "SkipCommit was set. Review staged changes and commit manually."
    exit 0
}

$stagedChanges = git -C $RepoRoot diff --cached --name-only

if ([string]::IsNullOrWhiteSpace($stagedChanges)) {
    Write-Host "No staged changes to commit."
    exit 0
}

git -C $RepoRoot commit -m $CommitMessage

Write-Host "Patch 002 committed. Run git status and verify CI before opening/merging PR."
