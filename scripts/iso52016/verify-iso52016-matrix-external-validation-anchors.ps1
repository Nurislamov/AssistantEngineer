param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

# TRACEABILITY-MARKERS-START
# MANUAL-ISO52016-ANCHOR-001
# MANUAL-ISO52016-ANCHOR-002
# MANUAL-ISO52016-ANCHOR-003
# MANUAL-ISO52016-ANCHOR-004
# MANUAL-ISO52016-ANNUAL-8760-001
# ManualEngineeringValidationAnchor
# IndependentManualEngineeringFormula
# ValidationAnchorsOnly
# No StandardReference equivalence claim.
# No EnergyPlus comparison workflow claim.
# hourCount 8760
# Chained script: verify-iso52016-matrix-external-validation-annual-anchors.ps1
# TRACEABILITY-MARKERS-END
$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
$toolProject = Join-Path $RepoRoot "tools\AssistantEngineer.Tools.Iso52016Verification\AssistantEngineer.Tools.Iso52016Verification.csproj"

$args = @(
    "run",
    "--project",
    $toolProject,
    "--",
    "verify-stage",
    "--stage-id",
    "ISO52016-MATRIX-EXTERNAL-VALIDATION-ANCHORS",
    "--repo-root",
    $RepoRoot
)

if ($SkipTests) {
    $args += "--skip-tests"
}

Push-Location $RepoRoot
try {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) {
        throw "ISO52016 stage verification failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
