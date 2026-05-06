param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests,
    [switch] $RequireCleanGit
)

$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
$toolProject = Join-Path $RepoRoot "tools\AssistantEngineer.Tools.Iso52016Verification\AssistantEngineer.Tools.Iso52016Verification.csproj"

$args = @(
    "run",
    "--project",
    $toolProject,
    "--",
    "assert-release-ready",
    "--repo-root",
    $RepoRoot
)

if ($SkipTests) {
    $args += "--skip-tests"
}

if ($RequireCleanGit) {
    $args += "--require-clean-git"
}

Push-Location $RepoRoot
try {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) {
        throw "ISO52016 release-ready assertion failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
