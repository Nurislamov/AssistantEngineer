param(
    [string] $RepoRoot = (Get-Location).Path,
    [string] $OutputDirectory = "artifacts\iso52016\matrix-merge-summary",
    [switch] $SkipVerification
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$outputRoot = Join-Path $RepoRoot $OutputDirectory

if (-not (Test-Path $outputRoot)) {
    New-Item -ItemType Directory -Path $outputRoot | Out-Null
}

if (-not $SkipVerification) {
    Push-Location $RepoRoot
    try {
        .\scripts\iso52016\assert-iso52016-matrix-release-ready.ps1
    }
    finally {
        Pop-Location
    }
}

Push-Location $RepoRoot
try {
    $branch = (git branch --show-current).Trim()
    $head = (git rev-parse --short HEAD).Trim()
    $status = git status --porcelain
    $log = git log --oneline -20
    $trackedGeneratedBaselines = git ls-files artifacts/iso52016/matrix-baselines
    $trackedGeneratedMergeSummary = git ls-files artifacts/iso52016/matrix-merge-summary
}
finally {
    Pop-Location
}

$summary = [ordered]@{
    stage = "ISO52016 Matrix"
    branch = $branch
    head = $head
    generatedAtUtc = [DateTime]::UtcNow.ToString("O")
    releaseReadyCommand = ".\scripts\iso52016\assert-iso52016-matrix-release-ready.ps1"
    ciWorkflow = ".github/workflows/iso52016-matrix-release-ready.yml"
    workingTreeClean = [string]::IsNullOrWhiteSpace($status)
    generatedArtifactsTracked = -not (
        [string]::IsNullOrWhiteSpace($trackedGeneratedBaselines) -and
        [string]::IsNullOrWhiteSpace($trackedGeneratedMergeSummary))
    recentCommits = @($log)
    nonClaims = @(
        "No exact pyBuildingEnergy numerical parity claim.",
        "No exact EnergyPlus numerical parity claim.",
        "No ASHRAE 140 validation coverage claim.",
        "No full coupled multi-zone heat-balance parity claim.",
        "No latent, humidity or moisture balance calculation claim."
    )
}

$jsonPath = Join-Path $outputRoot "merge-summary.json"
$mdPath = Join-Path $outputRoot "merge-summary.md"

$summary | ConvertTo-Json -Depth 20 | Set-Content -Path $jsonPath -Encoding UTF8

$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add("# ISO 52016 Matrix merge summary")
$lines.Add("")
$lines.Add("| Field | Value |")
$lines.Add("| --- | --- |")
$lines.Add("| Branch | `$branch` |")
$lines.Add("| HEAD | `$head` |")
$lines.Add("| Working tree clean | `$($summary.workingTreeClean)` |")
$lines.Add("| Generated artifacts tracked | `$($summary.generatedArtifactsTracked)` |")
$lines.Add("| Release-ready command | `$($summary.releaseReadyCommand)` |")
$lines.Add("| CI workflow | `$($summary.ciWorkflow)` |")
$lines.Add("")
$lines.Add("## Recent commits")
$lines.Add("")

foreach ($commit in $log) {
    $lines.Add("- $commit")
}

$lines.Add("")
$lines.Add("## Non-claims")
$lines.Add("")

foreach ($nonClaim in $summary.nonClaims) {
    $lines.Add("- $nonClaim")
}

$lines.Add("")
$lines.Add("Generated summary files are review artifacts and should not be committed.")

Set-Content -Path $mdPath -Value $lines -Encoding UTF8

Write-Host "Wrote ISO52016 Matrix merge summary:"
Write-Host "  $jsonPath"
Write-Host "  $mdPath"