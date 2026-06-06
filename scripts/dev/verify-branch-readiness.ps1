param(
    [string]$BaseRef = "origin/master",
    [string]$Scope = "EquipmentDiagnostics"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

dotnet run --project .\tools\AssistantEngineer.Tools.EquipmentDiagnosticsVerification\AssistantEngineer.Tools.EquipmentDiagnosticsVerification.csproj -- verify-branch --repo-root $repoRoot --base-ref $BaseRef --scope $Scope
exit $LASTEXITCODE
