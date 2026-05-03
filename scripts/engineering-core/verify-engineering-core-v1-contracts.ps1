param(
    [switch] $SkipFrontend,
    [switch] $SkipRegenerate
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$toolArgs = @()

if ($SkipFrontend) {
    $toolArgs += "--s-ki-pf-ro-nt-en-d"
}
if ($SkipRegenerate) {
    $toolArgs += "--s-ki-pr-eg-en-er-at-e"
}

dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreRelease\AssistantEngineer.Tools.EngineeringCoreRelease.csproj -- verify-contracts @toolArgs
