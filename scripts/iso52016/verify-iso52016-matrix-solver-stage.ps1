param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

# TRACEABILITY-MARKERS-START
# Chained script: verify-iso52016-matrix-baselines.ps1
# Flag: SkipBaselines
# Test guard: Iso52016MatrixBaselineFixtureTests.cs
# Matrix stage verification
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
    "ISO52016-MATRIX-SOLVER",
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
