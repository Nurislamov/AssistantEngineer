param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$manifestPath = Join-Path $RepoRoot "docs\releases\Iso52016MatrixExternalValidationAnchorsManifest.json"
$anchorDocPath = Join-Path $RepoRoot "docs\calculations\Iso52016MatrixExternalValidationAnchors.md"

if (-not (Test-Path $manifestPath)) {
    throw "Required ISO52016 Matrix external validation anchors manifest is missing: docs/releases/Iso52016MatrixExternalValidationAnchorsManifest.json"
}

if (-not (Test-Path $anchorDocPath)) {
    throw "Required ISO52016 Matrix external validation anchors doc is missing: docs/calculations/Iso52016MatrixExternalValidationAnchors.md"
}

$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json

if ($manifest.status -ne "ValidationAnchorsOnly") {
    throw "External validation anchors status must be ValidationAnchorsOnly."
}

$requiredAnchorIds = @(
    "MANUAL-ISO52016-ANCHOR-001",
    "MANUAL-ISO52016-ANCHOR-002",
    "MANUAL-ISO52016-ANCHOR-003",
    "MANUAL-ISO52016-ANCHOR-004",
    "MANUAL-ISO52016-ANNUAL-8760-001"
)

foreach ($anchorId in $requiredAnchorIds) {
    if ($manifest.requiredAnchorIds -notcontains $anchorId) {
        throw "External validation anchors manifest does not list required anchor id: $anchorId"
    }
}

if ($manifest.fixtures.Count -lt $requiredAnchorIds.Count) {
    throw "External validation anchors manifest must list at least $($requiredAnchorIds.Count) fixtures."
}

if ($manifest.fixtureCount -ne $manifest.fixtures.Count) {
    throw "External validation anchors manifest fixtureCount ($($manifest.fixtureCount)) does not match manifest fixture list count ($($manifest.fixtures.Count))."
}

$fixtureAnchorIds = New-Object System.Collections.Generic.HashSet[string] ([System.StringComparer]::OrdinalIgnoreCase)

foreach ($relativePath in $manifest.fixtures) {
    $path = Join-Path $RepoRoot ($relativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)

    if (-not (Test-Path $path)) {
        throw "External validation anchor fixture is missing: $relativePath"
    }

    $fixture = Get-Content $path -Raw | ConvertFrom-Json

    if ($fixture.sourceType -ne "ManualEngineeringValidationAnchor") {
        throw "External validation anchor fixture must use sourceType ManualEngineeringValidationAnchor: $relativePath"
    }

    if ($fixture.authoritativeReference -ne "IndependentManualEngineeringFormula") {
        throw "External validation anchor fixture must use authoritativeReference IndependentManualEngineeringFormula: $relativePath"
    }

    if ($fixture.scope -ne "ValidationAnchorsOnly") {
        throw "External validation anchor fixture scope must be ValidationAnchorsOnly: $relativePath"
    }

    foreach ($nonClaim in @("No pyBuildingEnergy parity claim.", "No EnergyPlus parity claim.", "No ASHRAE 140 validation coverage claim.", "No full ISO 52016 parity claim.")) {
        if ($fixture.explicitNonClaims -notcontains $nonClaim) {
            throw "External validation anchor fixture is missing explicit non-claim '$nonClaim': $relativePath"
        }
    }

    if (-not $fixtureAnchorIds.Add([string]$fixture.anchorId)) {
        throw "External validation anchor id is duplicated: $($fixture.anchorId)"
    }

    if ($fixture.anchorId -eq "MANUAL-ISO52016-ANNUAL-8760-001" -and $fixture.hourCount -ne 8760) {
        throw "Annual manual reference fixture must contain hourCount 8760."
    }
}

foreach ($anchorId in $requiredAnchorIds) {
    if (-not $fixtureAnchorIds.Contains($anchorId)) {
        throw "External validation anchors fixture set does not contain required anchor id: $anchorId"
    }
}

$doc = Get-Content $anchorDocPath -Raw

foreach ($requiredPhrase in @(
    "validation anchors only, not full parity",
    "No pyBuildingEnergy parity claim.",
    "No EnergyPlus parity claim.",
    "IndependentManualEngineeringFormula",
    "MANUAL-ISO52016-ANNUAL-8760-001"
)) {
    if (-not $doc.Contains($requiredPhrase)) {
        throw "External validation anchors documentation is missing required phrase: $requiredPhrase"
    }
}

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixExternalValidationAnchor"
    }
    finally {
        Pop-Location
    }
}

Write-Host "ISO52016 Matrix external validation anchors verification passed."
# Stage 2.1 annual 8760 anchor chain.
# Contract literal: verify-iso52016-matrix-external-validation-annual-anchors.ps1
$annualAnchorScript = Join-Path $RepoRoot "scripts\iso52016\verify-iso52016-matrix-external-validation-annual-anchors.ps1"
if (Test-Path $annualAnchorScript) {
    if ($SkipTests) {
        & $annualAnchorScript -RepoRoot $RepoRoot -SkipTests
    }
    else {
        & $annualAnchorScript -RepoRoot $RepoRoot
    }
}

