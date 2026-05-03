param(
    [string] $OutputDirectory = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$toolArgs = @()

if (-not [string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $toolArgs += "--output-directory"
    $toolArgs += $OutputDirectory
}

dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreEvidence\AssistantEngineer.Tools.EngineeringCoreEvidence.csproj -- generate-traceability-matrix @toolArgs
