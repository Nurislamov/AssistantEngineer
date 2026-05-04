param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# verify-iso52016-matrix-external-validation-naming-anchors.ps1
# Naming anchors are validation anchors only, not full parity.

function Resolve-AssistantEngineerRepoRoot {
    param(
        [Parameter(Mandatory = $true)] [string] $CandidateRoot,
        [Parameter(Mandatory = $true)] [string] $ScriptRoot
    )

    $candidates = New-Object System.Collections.Generic.List[string]

    if (-not [string]::IsNullOrWhiteSpace($CandidateRoot)) {
        $resolvedCandidate = Resolve-Path -LiteralPath $CandidateRoot -ErrorAction SilentlyContinue

        if ($null -ne $resolvedCandidate) {
            $candidates.Add($resolvedCandidate.Path)
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

$RepoRoot = Resolve-AssistantEngineerRepoRoot -CandidateRoot $RepoRoot -ScriptRoot $PSScriptRoot

$requiredFiles = @(
    "docs\calculations\Iso52016MatrixExternalValidationNamingAnchors.md",
    "docs\releases\Iso52016MatrixExternalValidationNamingAnchorsManifest.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationNamingAnchorTests.cs"
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath

    if (-not (Test-Path $path)) {
        throw ("Required ISO52016 Matrix external validation naming anchor file is missing: {0}. ResolvedRepoRoot={1}" -f $relativePath, $RepoRoot)
    }
}

$fixtureDirectory = Join-Path $RepoRoot "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\ExternalValidationNamingAnchors"

if (-not (Test-Path $fixtureDirectory)) {
    throw ("Required ISO52016 Matrix external validation naming anchor fixture directory is missing: {0}" -f $fixtureDirectory)
}

$fixtureCount = (Get-ChildItem -Path $fixtureDirectory -File -Filter *.json).Count

if ($fixtureCount -lt 4) {
    throw ("Expected at least 4 ISO52016 Matrix external validation naming anchor fixtures, found {0}." -f $fixtureCount)
}

$docPath = Join-Path $RepoRoot "docs\calculations\Iso52016MatrixExternalValidationNamingAnchors.md"
$doc = Get-Content -Path $docPath -Raw

$requiredDocFragments = @(
    "Validation anchors only, not full parity.",
    "No exact pyBuildingEnergy numerical parity claim.",
    "No exact EnergyPlus numerical parity claim.",
    "No ExternalParityCovered claim."
)

foreach ($fragment in $requiredDocFragments) {
    if (-not $doc.Contains($fragment)) {
        throw ("External validation naming anchors doc is missing required text: {0}" -f $fragment)
    }
}

$manifestPath = Join-Path $RepoRoot "docs\releases\Iso52016MatrixExternalValidationNamingAnchorsManifest.json"

try {
    $manifest = Get-Content -Path $manifestPath -Raw | ConvertFrom-Json
}
catch {
    throw "External validation naming anchors manifest is not valid JSON."
}

if ($manifest.scope -ne "ValidationAnchorOnly") {
    throw "External validation naming anchors manifest scope must be ValidationAnchorOnly."
}

if ($manifest.namingAnchorsIntegrated -ne $true) {
    throw "External validation naming anchors manifest must set namingAnchorsIntegrated to true."
}

if ($manifest.pyBuildingEnergyStyleNamingIntegrated -ne $true) {
    throw "External validation naming anchors manifest must set pyBuildingEnergyStyleNamingIntegrated to true."
}

if ($manifest.energyPlusStyleNamingIntegrated -ne $true) {
    throw "External validation naming anchors manifest must set energyPlusStyleNamingIntegrated to true."
}

$manifestText = Get-Content -Path $manifestPath -Raw

$forbiddenClaims = @(
    '"ExternalParityCovered": true',
    '"FullParityCovered": true'
)

foreach ($claim in $forbiddenClaims) {
    if ($manifestText.Contains($claim)) {
        throw ("External validation naming anchors manifest contains forbidden parity claim: {0}" -f $claim)
    }
}

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixExternalValidationNamingAnchor"
        $exitCode = $LASTEXITCODE

        if ($exitCode -ne 0) {
            throw ("ISO52016 Matrix external validation naming anchor tests failed with exit code {0}." -f $exitCode)
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host "ISO52016 Matrix external validation naming anchors verification passed."