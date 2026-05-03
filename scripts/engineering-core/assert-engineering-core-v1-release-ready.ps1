param(
    [switch] $SkipFrontend,
    [switch] $SkipFullDotnet,
    [switch] $SkipGitStatus,
    [switch] $Fast
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$toolArgs = @()

if ($SkipFrontend) {
    $toolArgs += "--skip-frontend"
}

if ($SkipFullDotnet) {
    $toolArgs += "--skip-full-dotnet"
}

if ($SkipGitStatus) {
    $toolArgs += "--skip-git-status"
}

if ($Fast) {
    $toolArgs += "--fast"
}

dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreRelease\AssistantEngineer.Tools.EngineeringCoreRelease.csproj -- assert-release-ready @toolArgs
