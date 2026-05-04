param(
    [string] $RepoRoot = (Get-Location).Path,
    [string] $OutputPath = "artifacts\iso52016\external-validation-anchors\merge-summary.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
    param([string] $CandidateRoot, [string] $ScriptRoot)

    $candidates = New-Object System.Collections.Generic.List[string]

    if (-not [string]::IsNullOrWhiteSpace($CandidateRoot)) {
        $resolved = Resolve-Path -LiteralPath $CandidateRoot -ErrorAction SilentlyContinue
        if ($null -ne $resolved) { $candidates.Add($resolved.Path) }
    }

    if (-not [string]::IsNullOrWhiteSpace($ScriptRoot)) {
        $directory = New-Object System.IO.DirectoryInfo($ScriptRoot)
        while ($null -ne $directory) {
            $candidates.Add($directory.FullName)
            $directory = $directory.Parent
        }
    }

    foreach ($candidate in $candidates | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique) {
        $tests = Join-Path $candidate "tests\AssistantEngineer.Tests"
        $src = Join-Path $candidate "src\Backend\AssistantEngineer.Modules.Calculations"
        $git = Join-Path $candidate ".git"

        if ((Test-Path $tests) -and (Test-Path $src)) { return $candidate }
        if ((Test-Path $tests) -and (Test-Path $git)) { return $candidate }
    }

    throw ("Could not resolve AssistantEngineer repository root. CandidateRoot='{0}', ScriptRoot='{1}'." -f $CandidateRoot, $ScriptRoot)
}

$RepoRoot = Resolve-RepoRoot -CandidateRoot $RepoRoot -ScriptRoot $PSScriptRoot
$outputFullPath = Join-Path $RepoRoot $OutputPath
$outputDirectory = Split-Path -Parent $outputFullPath

if (-not (Test-Path $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$summary = [ordered]@{
    stageId = "ISO52016-MATRIX-EXTERNAL-VALIDATION-ANCHORS"
    status = "ClosedCandidate"
    scope = "ValidationAnchorOnly"
    generatedAtUtc = [DateTime]::UtcNow.ToString("O")
    generatedArtifact = $true
    generatedArtifactsCommitted = $false
    nonClaims = @(
        "Validation anchors only, not full parity.",
        "No exact pyBuildingEnergy numerical parity claim.",
        "No exact EnergyPlus numerical parity claim.",
        "No ASHRAE 140 validation coverage claim."
    )
    verificationEntryPoints = @(
        "scripts/iso52016/assert-iso52016-matrix-external-validation-anchors-release-ready.ps1",
        "scripts/iso52016/verify-iso52016-matrix-all.ps1",
        "scripts/iso52016/assert-iso52016-matrix-release-ready.ps1"
    )
}

$summary | ConvertTo-Json -Depth 20 | Set-Content -Path $outputFullPath -Encoding UTF8

Write-Host ("ISO52016 Matrix external validation anchors merge summary written to {0}" -f $outputFullPath)