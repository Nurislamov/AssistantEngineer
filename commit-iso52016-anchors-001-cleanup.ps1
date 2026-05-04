param(
    [string] $RepoRoot = (Get-Location).Path,
    [string] $BranchName = "iso52016-matrix-external-validation-anchors-001",
    [string] $CommitMessage = "Add ISO52016 Matrix manual validation anchors",
    [string[]] $RootPatchScriptPatterns = @(
        "AE-ISO52016-ANCHORS-001*.ps1"
    ),
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

function Assert-CommandExists {
    param(
        [Parameter(Mandatory = $true)] [string] $CommandName
    )

    $command = Get-Command $CommandName -ErrorAction SilentlyContinue

    if ($null -eq $command) {
        throw "Required command was not found on PATH: $CommandName"
    }
}

function Remove-RootPatchScripts {
    $removed = New-Object System.Collections.Generic.List[string]

    foreach ($pattern in $RootPatchScriptPatterns) {
        $matches = Get-ChildItem -Path $RepoRoot -File -Filter $pattern -ErrorAction SilentlyContinue

        foreach ($file in $matches) {
            $relativePath = Resolve-Path -Path $file.FullName -Relative
            Remove-Item -LiteralPath $file.FullName -Force
            $removed.Add($relativePath)
        }
    }

    if ($removed.Count -eq 0) {
        Write-Host "No root patch scripts matched cleanup patterns."
        return
    }

    Write-Host "Removed root patch scripts:"
    foreach ($path in $removed) {
        Write-Host "  $path"
    }
}

function Switch-OrCreateBranch {
    Invoke-RepoCommand {
        $existingBranch = git branch --list $BranchName

        if ([string]::IsNullOrWhiteSpace($existingBranch)) {
            git switch -c $BranchName
        }
        else {
            git switch $BranchName
        }

        if ($LASTEXITCODE -ne 0) {
            throw "Failed to switch/create branch: $BranchName"
        }
    }
}

function Invoke-VerificationIfRequested {
    if (-not $RunVerification) {
        Write-Host "Verification was not requested. Use -RunVerification to run the anchor gate before commit."
        return
    }

    Invoke-RepoCommand {
        .\scripts\iso52016\verify-iso52016-matrix-external-validation-anchors.ps1

        if ($LASTEXITCODE -ne 0) {
            throw "External validation anchors verification failed."
        }

        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixExternalValidationAnchor"

        if ($LASTEXITCODE -ne 0) {
            throw "External validation anchor tests failed."
        }
    }
}

function Stage-AndCommit {
    Invoke-RepoCommand {
        git add -A

        if ($LASTEXITCODE -ne 0) {
            throw "git add -A failed."
        }

        $staged = git diff --cached --name-only

        if ([string]::IsNullOrWhiteSpace($staged)) {
            Write-Host "No staged changes found. Nothing to commit."
            return
        }

        Write-Host "Staged files:"
        $staged -split "`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object {
            Write-Host "  $_"
        }

        if ($SkipCommit) {
            Write-Host "SkipCommit was specified. Review staged changes and commit manually."
            return
        }

        git commit -m $CommitMessage

        if ($LASTEXITCODE -ne 0) {
            throw "git commit failed."
        }

        Write-Host "Commit created on branch '$BranchName'."
    }
}

$RepoRoot = (Resolve-Path $RepoRoot).Path

if (-not (Test-Path (Join-Path $RepoRoot ".git"))) {
    throw "Repository root does not contain .git: $RepoRoot"
}

Assert-CommandExists -CommandName "git"

Write-Host "Repository root: $RepoRoot"
Write-Host "Target branch: $BranchName"

Switch-OrCreateBranch
Remove-RootPatchScripts
Invoke-VerificationIfRequested
Stage-AndCommit

Write-Host "Branch cleanup and commit step completed."
