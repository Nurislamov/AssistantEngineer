param(
    [Parameter(Mandatory = $false)]
    [string] $RepoRoot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$candidateRepoRoot = if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        Join-Path $PSScriptRoot "..\.."
    }
    else {
        (Get-Location).Path
    }
}
else {
    $RepoRoot
}

$resolvedRepoRoot = (Resolve-Path -LiteralPath $candidateRepoRoot).Path
$solutionPath = Join-Path $resolvedRepoRoot "AssistantEngineer.sln"
if (-not (Test-Path -LiteralPath $solutionPath)) {
    throw "AssistantEngineer.sln was not found under repository root: $resolvedRepoRoot"
}

Push-Location $resolvedRepoRoot
try {
    Write-Host '[P3-14] Build solution (Debug)' -ForegroundColor Cyan
    dotnet build .\AssistantEngineer.sln -c Debug --no-restore

    Write-Host '[P3-14] Test solution (Debug)' -ForegroundColor Cyan
    dotnet test .\AssistantEngineer.sln -c Debug --no-restore --no-build

    Write-Host '[P3-14] Engineering-core release-ready gate' -ForegroundColor Cyan
    powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1

    Write-Host '[P3-14] Verification complete.' -ForegroundColor Green
}
finally {
    Pop-Location
}
