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
    $toolArgs += "--s-ki-pf-ro-nt-en-d"
}
if ($SkipFullDotnet) {
    $toolArgs += "--s-ki-pf-ul-ld-ot-ne-t"
}
if ($SkipGitStatus) {
    $toolArgs += "--s-ki-pg-it-st-at-us"
}
if ($Fast) {
    $toolArgs += "--f-as-t"
}

dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreRelease\AssistantEngineer.Tools.EngineeringCoreRelease.csproj -- assert-release-ready @toolArgs
