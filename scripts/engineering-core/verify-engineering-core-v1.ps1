param(
    [switch] $SkipFrontend,
    [switch] $SkipFullDotnet,
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

if ($Fast) {
    $toolArgs += "--fast"
}

dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreVerification\AssistantEngineer.Tools.EngineeringCoreVerification.csproj -- @toolArgs
