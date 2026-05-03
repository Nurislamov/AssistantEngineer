param(
    [switch] $SkipFrontend
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$toolArgs = @()

if ($SkipFrontend) {
    $toolArgs += "--s-ki-pf-ro-nt-en-d"
}

dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreRelease\AssistantEngineer.Tools.EngineeringCoreRelease.csproj -- verify-manifest @toolArgs
