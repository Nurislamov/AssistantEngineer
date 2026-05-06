param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $RequireClean,
    [switch] $CheckRootPatchScripts
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

$toolProject = Join-Path $RepoRoot 'tools\AssistantEngineer.Tools.RepositoryHygieneVerification\AssistantEngineer.Tools.RepositoryHygieneVerification.csproj'
if (-not (Test-Path -LiteralPath $toolProject)) {
    throw "Repository hygiene verification tool project is missing: $toolProject"
}

$arguments = @('run', '--project', $toolProject, '--', '--repo-root', $RepoRoot)

if ($RequireClean) {
    $arguments += '--require-clean'
}

if ($CheckRootPatchScripts) {
    $arguments += '--check-root-patch-scripts'
}

Push-Location $RepoRoot
try {
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Repository hygiene C# verification failed with exit code ${LASTEXITCODE}."
    }
}
finally {
    Pop-Location
}

Write-Host 'ISO52016 physical branch hygiene verification passed.'