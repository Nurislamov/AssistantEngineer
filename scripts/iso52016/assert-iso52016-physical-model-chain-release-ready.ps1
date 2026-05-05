param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
$toolProject = Join-Path $RepoRoot "tools\AssistantEngineer.Tools.Iso52016PhysicalVerification\AssistantEngineer.Tools.Iso52016PhysicalVerification.csproj"

if (-not (Test-Path -LiteralPath $toolProject)) {
    throw "ISO52016 physical verification C# tool project was not found: $toolProject"
}

$arguments = @(
    "run",
    "--project",
    $toolProject,
    "--",
    "--repo-root",
    $RepoRoot,
    "--assert-release-ready"
)

if ($SkipTests) {
    $arguments += "--skip-tests"
}

Push-Location $RepoRoot
try {
    & dotnet @arguments

    if ($LASTEXITCODE -ne 0) {
        throw "ISO52016 physical model release-ready gate failed with exit code ${LASTEXITCODE}."
    }
}
finally {
    Pop-Location
}

Write-Host "ISO52016 physical model release-ready gate passed - validation/internal engineering anchors only."
