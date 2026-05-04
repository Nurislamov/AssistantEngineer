param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipStageGateVerification,
    [switch] $SkipTests,
    [switch] $RequireCleanGit
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# assert-iso52016-matrix-external-validation-anchors-release-ready.ps1
# Release gate scope: ValidationAnchorOnly.

function Resolve-RepoRoot {
    param(
        [string] $CandidateRoot,
        [string] $ScriptRoot
    )

    $candidates = New-Object System.Collections.Generic.List[string]

    if (-not [string]::IsNullOrWhiteSpace($CandidateRoot)) {
        $resolved = Resolve-Path -LiteralPath $CandidateRoot -ErrorAction SilentlyContinue

        if ($null -ne $resolved) {
            $candidates.Add($resolved.Path)
        }
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

        if ((Test-Path $tests) -and (Test-Path $src)) {
            return $candidate
        }

        if ((Test-Path $tests) -and (Test-Path $git)) {
            return $candidate
        }
    }

    throw ("Could not resolve AssistantEngineer repository root. CandidateRoot='{0}', ScriptRoot='{1}'." -f $CandidateRoot, $ScriptRoot)
}

$RepoRoot = Resolve-RepoRoot -CandidateRoot $RepoRoot -ScriptRoot $PSScriptRoot

$requiredFiles = @(
    "docs\calculations\Iso52016MatrixExternalValidationAnchorsReleaseGate.md",
    "docs\releases\Iso52016MatrixExternalValidationAnchorsReleaseManifest.json",
    "docs\releases\Iso52016MatrixExternalValidationAnchorsStageGateManifest.json",
    "docs\releases\Iso52016MatrixExternalValidationAnchorsManifest.json",
    "docs\releases\Iso52016MatrixExternalValidationAnnualAnchorsManifest.json",
    "docs\releases\Iso52016MatrixExternalValidationNamingAnchorsManifest.json",
    "scripts\iso52016\verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1",
    "scripts\iso52016\verify-iso52016-matrix-all.ps1",
    "scripts\iso52016\assert-iso52016-matrix-release-ready.ps1",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorsReleaseGateTests.cs"
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath

    if (-not (Test-Path $path)) {
        throw ("Required ISO52016 Matrix external validation anchors release file is missing: {0}. ResolvedRepoRoot={1}" -f $relativePath, $RepoRoot)
    }
}

$docPath = Join-Path $RepoRoot "docs\calculations\Iso52016MatrixExternalValidationAnchorsReleaseGate.md"
$doc = Get-Content -Path $docPath -Raw

$requiredDocFragments = @(
    "Validation anchors only, not full parity.",
    "No exact pyBuildingEnergy numerical parity claim.",
    "No exact EnergyPlus numerical parity claim.",
    "No ExternalParityCovered claim.",
    "No FullParityCovered claim."
)

foreach ($fragment in $requiredDocFragments) {
    if (-not $doc.Contains($fragment)) {
        throw ("External validation anchors release doc is missing required text: {0}" -f $fragment)
    }
}

$manifestPath = Join-Path $RepoRoot "docs\releases\Iso52016MatrixExternalValidationAnchorsReleaseManifest.json"

try {
    $manifest = Get-Content -Path $manifestPath -Raw | ConvertFrom-Json
}
catch {
    throw "External validation anchors release manifest is not valid JSON."
}

if ($manifest.scope -ne "ValidationAnchorOnly") { throw "Release manifest scope must be ValidationAnchorOnly." }
if ($manifest.stageGateIntegrated -ne $true) { throw "Release manifest must set stageGateIntegrated to true." }
if ($manifest.allInOneVerificationIntegrated -ne $true) { throw "Release manifest must set allInOneVerificationIntegrated to true." }
if ($manifest.releaseReadyGateIntegrated -ne $true) { throw "Release manifest must set releaseReadyGateIntegrated to true." }
if ($manifest.generatedArtifactsCommitted -ne $false) { throw "Release manifest must set generatedArtifactsCommitted to false." }

$manifestText = Get-Content -Path $manifestPath -Raw

$forbiddenClaims = @(
    '"ExternalParityCovered": true',
    '"FullParityCovered": true',
    '"pyBuildingEnergyParityCovered": true',
    '"EnergyPlusParityCovered": true'
)

foreach ($claim in $forbiddenClaims) {
    if ($manifestText.Contains($claim)) {
        throw ("External validation anchors release manifest contains forbidden parity claim: {0}" -f $claim)
    }
}

$mainReleaseScript = Get-Content -Path (Join-Path $RepoRoot "scripts\iso52016\assert-iso52016-matrix-release-ready.ps1") -Raw

if (-not $mainReleaseScript.Contains("assert-iso52016-matrix-external-validation-anchors-release-ready.ps1")) {
    throw "Main ISO52016 Matrix release-ready script does not reference the external validation anchors release gate."
}

if (-not $SkipStageGateVerification) {
    Push-Location $RepoRoot
    try {
        & .\scripts\iso52016\verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1 -RepoRoot $RepoRoot
        $stageGateExitCode = $LASTEXITCODE

        if ($stageGateExitCode -ne 0) {
            throw ("External validation anchors stage-gate verification failed with exit code {0}." -f $stageGateExitCode)
        }
    }
    finally {
        Pop-Location
    }
}

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixExternalValidationAnchorsReleaseGate"
        $testExitCode = $LASTEXITCODE

        if ($testExitCode -ne 0) {
            throw ("ISO52016 Matrix external validation anchors release gate tests failed with exit code {0}." -f $testExitCode)
        }
    }
    finally {
        Pop-Location
    }
}


# AE_STEP07_EXTERNAL_VALIDATION_ANCHORS_GENERATED_ARTIFACT_GUARD_BEGIN
Push-Location $RepoRoot
try {
    $trackedExternalValidationAnchorArtifacts = git ls-files artifacts/iso52016/external-validation-anchors

    if (-not [string]::IsNullOrWhiteSpace($trackedExternalValidationAnchorArtifacts)) {
        throw ("Generated ISO52016 Matrix external validation anchor artifacts are tracked by git. Remove them from the index: {0}" -f $trackedExternalValidationAnchorArtifacts)
    }
}
finally {
    Pop-Location
}
# AE_STEP07_EXTERNAL_VALIDATION_ANCHORS_GENERATED_ARTIFACT_GUARD_END
if ($RequireCleanGit) {
    Push-Location $RepoRoot
    try {
        $status = git status --porcelain

        if (-not [string]::IsNullOrWhiteSpace($status)) {
            throw "Working tree is not clean. Commit or stash changes before external validation anchors release assertion."
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host "ISO52016 Matrix external validation anchors release-ready assertion passed."