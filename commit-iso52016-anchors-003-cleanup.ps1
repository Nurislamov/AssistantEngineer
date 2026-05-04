param(
    [string] $RepoRoot = (Get-Location).Path,
    [string] $BranchName = "iso52016-matrix-external-validation-anchors-001",
    [string] $CommitMessage = "Close ISO52016 Matrix external validation anchors stage",
    [switch] $RunVerification,
    [switch] $SkipCommit,
    [switch] $AllowEmptyCommit
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

function Remove-RootPatchScripts {
    Push-Location $RepoRoot
    try {
        $patterns = @(
            "AE-ISO52016-ANCHORS-003*.ps1",
            "AE-ISO52016-ANCHORS-002*.ps1"
        )

        foreach ($pattern in $patterns) {
            Get-ChildItem -Path . -File -Filter $pattern -ErrorAction SilentlyContinue |
                ForEach-Object {
                    Write-Host "Removing temporary patch script: $($_.Name)"
                    Remove-Item -LiteralPath $_.FullName -Force
                }
        }
    }
    finally {
        Pop-Location
    }
}

function Assert-GeneratedArtifactsAreNotTracked {
    Invoke-RepoCommand {
        $trackedExternalValidationArtifacts = git ls-files artifacts/iso52016/external-validation-anchors

        if (-not [string]::IsNullOrWhiteSpace($trackedExternalValidationArtifacts)) {
            throw "Generated ISO52016 external validation anchor artifacts are tracked by git. Remove them from the index: $trackedExternalValidationArtifacts"
        }

        $trackedMatrixBaselineArtifacts = git ls-files artifacts/iso52016/matrix-baselines

        if (-not [string]::IsNullOrWhiteSpace($trackedMatrixBaselineArtifacts)) {
            throw "Generated ISO52016 Matrix baseline artifacts are tracked by git. Remove them from the index: $trackedMatrixBaselineArtifacts"
        }
    }
}

function Assert-RequiredFilesExist {
    $requiredFiles = @(
        "docs\calculations\Iso52016MatrixExternalValidationAnchors.md",
        "docs\releases\Iso52016MatrixExternalValidationAnchorsManifest.json",
        "docs\releases\Iso52016MatrixExternalValidationAnchorsReleaseManifest.json",
        "docs\releases\Iso52016MatrixExternalValidationAnchorsReleaseNotes.md",
        "docs\runbooks\Iso52016MatrixExternalValidationAnchorsMergeRunbook.md",
        "scripts\iso52016\verify-iso52016-matrix-external-validation-anchors.ps1",
        "scripts\iso52016\verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1",
        "scripts\iso52016\assert-iso52016-matrix-external-validation-anchors-release-ready.ps1",
        "scripts\iso52016\write-iso52016-matrix-external-validation-anchors-merge-summary.ps1",
        "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\ExternalValidationAnchors\manual-iso52016-anchor-001-steady-heating.json",
        "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\ExternalValidationAnchors\manual-iso52016-anchor-002-heating-with-gains.json",
        "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\ExternalValidationAnchors\manual-iso52016-anchor-003-steady-cooling.json",
        "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\ExternalValidationAnchors\manual-iso52016-anchor-004-free-floating-no-hvac.json",
        "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\ExternalValidationAnchors\manual-iso52016-annual-8760-001-constant-weather-heating.json",
        "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorTests.cs",
        "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorManifestTests.cs",
        "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorStageGateTests.cs",
        "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorsReleaseGateTests.cs",
        "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorsClosureTests.cs"
    )

    foreach ($relativePath in $requiredFiles) {
        $path = Join-Path $RepoRoot $relativePath

        if (-not (Test-Path $path)) {
            throw "Required Stage 2.1 closure file is missing: $relativePath"
        }
    }
}

Invoke-RepoCommand {
    if (-not (Test-Path ".git")) {
        throw "RepoRoot does not appear to be a git repository: $RepoRoot"
    }

    $currentBranch = git branch --show-current

    if ([string]::IsNullOrWhiteSpace($currentBranch)) {
        throw "Could not determine current git branch."
    }

    if ($currentBranch -ne $BranchName) {
        $branchExists = git branch --list $BranchName

        if ([string]::IsNullOrWhiteSpace($branchExists)) {
            Write-Host "Creating branch $BranchName from $currentBranch"
            git checkout -b $BranchName
        }
        else {
            Write-Host "Switching to branch $BranchName"
            git checkout $BranchName
        }
    }
    else {
        Write-Host "Already on branch $BranchName"
    }
}

Assert-RequiredFilesExist
Remove-RootPatchScripts

if ($RunVerification) {
    Invoke-RepoCommand {
        .\scripts\iso52016\assert-iso52016-matrix-external-validation-anchors-release-ready.ps1
        .\scripts\iso52016\verify-iso52016-matrix-all.ps1
    }
}

Assert-GeneratedArtifactsAreNotTracked

Invoke-RepoCommand {
    git add -A

    $staged = git diff --cached --name-only

    if ([string]::IsNullOrWhiteSpace($staged)) {
        if ($AllowEmptyCommit) {
            git commit --allow-empty -m $CommitMessage
            Write-Host "Created empty commit: $CommitMessage"
        }
        else {
            Write-Host "No staged changes found. Nothing to commit."
        }

        return
    }

    Write-Host "Staged files:"
    $staged | ForEach-Object { Write-Host " - $_" }

    if (-not $SkipCommit) {
        git commit -m $CommitMessage
        Write-Host "Committed Stage 2.1 closure: $CommitMessage"
    }
    else {
        Write-Host "SkipCommit was specified. Review staged changes and commit manually."
    }
}

Write-Host "ISO52016 Matrix external validation anchors Stage 2.1 cleanup/commit step completed."
