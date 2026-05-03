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

dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreContracts\AssistantEngineer.Tools.EngineeringCoreContracts.csproj -- generate-report-contract-snapshots @toolArgs
