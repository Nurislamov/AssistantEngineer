param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# verify-iso52016-matrix-external-validation-annual-anchors.ps1
# Annual external validation anchors are validation anchors only, not full parity.

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
    "docs\calculations\Iso52016MatrixExternalValidationAnnualAnchors.md",
    "docs\releases\Iso52016MatrixExternalValidationAnnualAnchorsManifest.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\ExternalValidationAnnualAnchors\manual-independent-annual-8760-seasonal-loads.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationAnnualAnchorTests.cs"
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath

    if (-not (Test-Path $path)) {
        throw ("Required ISO52016 Matrix annual external validation anchor file is missing: {0}. ResolvedRepoRoot={1}" -f $relativePath, $RepoRoot)
    }
}

$docPath = Join-Path $RepoRoot "docs\calculations\Iso52016MatrixExternalValidationAnnualAnchors.md"
$doc = Get-Content -Path $docPath -Raw

$requiredDocFragments = @(
    "8760",
    "Validation anchors only, not full parity.",
    "No exact pyBuildingEnergy numerical parity claim.",
    "No exact EnergyPlus numerical parity claim.",
    "No ASHRAE 140 validation coverage claim."
)

foreach ($fragment in $requiredDocFragments) {
    if (-not $doc.Contains($fragment)) {
        throw ("Annual external validation anchors doc is missing required text: {0}" -f $fragment)
    }
}

$manifestPath = Join-Path $RepoRoot "docs\releases\Iso52016MatrixExternalValidationAnnualAnchorsManifest.json"

try {
    $manifest = Get-Content -Path $manifestPath -Raw | ConvertFrom-Json
}
catch {
    throw "Annual external validation anchor manifest is not valid JSON."
}

if ($manifest.scope -ne "ValidationAnchorOnly") {
    throw "Annual external validation anchor manifest scope must be ValidationAnchorOnly."
}

if ($manifest.annual8760ManualReferenceIntegrated -ne $true) {
    throw "Annual external validation anchor manifest must set annual8760ManualReferenceIntegrated to true."
}

if ([int]$manifest.annualHourCount -ne 8760) {
    throw "Annual external validation anchor manifest must set annualHourCount to 8760."
}

$manifestText = Get-Content -Path $manifestPath -Raw
$requiredManifestFragments = @(
    "Validation anchors only, not full parity.",
    "No exact pyBuildingEnergy numerical parity claim.",
    "No exact EnergyPlus numerical parity claim.",
    "No ASHRAE 140 validation coverage claim."
)

foreach ($fragment in $requiredManifestFragments) {
    if (-not $manifestText.Contains($fragment)) {
        throw ("Annual external validation anchor manifest is missing required non-claim: {0}" -f $fragment)
    }
}

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixExternalValidationAnnualAnchor"
        $exitCode = $LASTEXITCODE

        if ($exitCode -ne 0) {
            throw ("ISO52016 Matrix annual external validation anchor tests failed with exit code {0}." -f $exitCode)
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host "ISO52016 Matrix annual external validation anchors verification passed."