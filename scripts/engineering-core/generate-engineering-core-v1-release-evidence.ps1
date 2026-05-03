param(
    [string] $OutputPath = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$toolArgs = @()

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $toolArgs += "--output-path"
    $toolArgs += $OutputPath
}

dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreEvidence\AssistantEngineer.Tools.EngineeringCoreEvidence.csproj -- generate-release-evidence @toolArgs
