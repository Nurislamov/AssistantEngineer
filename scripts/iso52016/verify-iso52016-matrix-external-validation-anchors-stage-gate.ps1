param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1
# Stage gate scope: ValidationAnchorOnly.

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

$requiredFiles = @(
    "docs\calculations\Iso52016MatrixExternalValidationAnchorsStageGate.md",
    "docs\releases\Iso52016MatrixExternalValidationAnchorsStageGateManifest.json",
    "docs\releases\Iso52016MatrixExternalValidationAnchorsManifest.json",
    "docs\releases\Iso52016MatrixExternalValidationAnnualAnchorsManifest.json",
    "docs\releases\Iso52016MatrixExternalValidationNamingAnchorsManifest.json",
    "scripts\iso52016\verify-iso52016-matrix-external-validation-anchors.ps1",
    "scripts\iso52016\verify-iso52016-matrix-external-validation-annual-anchors.ps1",
    "scripts\iso52016\verify-iso52016-matrix-external-validation-naming-anchors.ps1",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnchorStageGateTests.cs"
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath

    if (-not (Test-Path $path)) {
        throw ("Required ISO52016 Matrix external validation anchors stage-gate file is missing: {0}. ResolvedRepoRoot={1}" -f $relativePath, $RepoRoot)
    }
}

$docPath = Join-Path $RepoRoot "docs\calculations\Iso52016MatrixExternalValidationAnchorsStageGate.md"
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
        throw ("External validation anchors stage-gate doc is missing required text: {0}" -f $fragment)
    }
}

$manifestPath = Join-Path $RepoRoot "docs\releases\Iso52016MatrixExternalValidationAnchorsStageGateManifest.json"

try {
    $manifest = Get-Content -Path $manifestPath -Raw | ConvertFrom-Json
}
catch {
    throw "External validation anchors stage-gate manifest is not valid JSON."
}

if ($manifest.scope -ne "ValidationAnchorOnly") { throw "Stage-gate manifest scope must be ValidationAnchorOnly." }
if ($manifest.simpleIndependentManualAnchorsIntegrated -ne $true) { throw "simpleIndependentManualAnchorsIntegrated must be true." }
if ($manifest.annual8760ManualReferenceIntegrated -ne $true) { throw "annual8760ManualReferenceIntegrated must be true." }
if ($manifest.pyBuildingEnergyStyleNamingIntegrated -ne $true) { throw "pyBuildingEnergyStyleNamingIntegrated must be true." }
if ($manifest.energyPlusStyleNamingIntegrated -ne $true) { throw "energyPlusStyleNamingIntegrated must be true." }
if ($manifest.allInOneVerificationIntegrated -ne $true) { throw "allInOneVerificationIntegrated must be true." }
if ($manifest.releaseReadyGateIntegrated -ne $true) { throw "releaseReadyGateIntegrated must be true." }

$manifestText = Get-Content -Path $manifestPath -Raw

$forbiddenClaims = @(
    '"ExternalParityCovered": true',
    '"FullParityCovered": true',
    '"pyBuildingEnergyParityCovered": true',
    '"EnergyPlusParityCovered": true'
)

foreach ($claim in $forbiddenClaims) {
    if ($manifestText.Contains($claim)) {
        throw ("Stage-gate manifest contains forbidden parity claim: {0}" -f $claim)
    }
}

$allScript = Get-Content -Path (Join-Path $RepoRoot "scripts\iso52016\verify-iso52016-matrix-all.ps1") -Raw
if (-not $allScript.Contains("verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1")) {
    throw "All-in-one verification script does not reference the external validation anchors stage gate."
}

$releaseScript = Get-Content -Path (Join-Path $RepoRoot "scripts\iso52016\assert-iso52016-matrix-release-ready.ps1") -Raw
if (-not $releaseScript.Contains("verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1")) {
    throw "Release-ready script does not reference the external validation anchors stage gate."
}

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixExternalValidationAnchorStageGate"
        $exitCode = $LASTEXITCODE

        if ($exitCode -ne 0) {
            throw ("ISO52016 Matrix external validation anchors stage-gate tests failed with exit code {0}." -f $exitCode)
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host "ISO52016 Matrix external validation anchors stage gate verification passed."