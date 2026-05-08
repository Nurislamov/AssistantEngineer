param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

# TRACEABILITY-MARKERS-START
# artifacts\iso52016\engineering-edge-cases\merge-summary.json
# generatedArtifactsCommitted
# Engineering edge-case hardening only.
# Validation anchors only, not full equivalence claim.
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
    "ISO52016-MATRIX-ENGINEERING-EDGE-CASES",
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
