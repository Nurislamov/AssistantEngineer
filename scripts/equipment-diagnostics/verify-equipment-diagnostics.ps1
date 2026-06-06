$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

dotnet run --project .\tools\AssistantEngineer.Tools.EquipmentDiagnosticsVerification\AssistantEngineer.Tools.EquipmentDiagnosticsVerification.csproj -- full-report --repo-root $repoRoot
