param(
    [string] $RepoRoot = (Get-Location).Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

Push-Location $RepoRoot
try {
    dotnet run --project .\tools\AssistantEngineer.Tools.Iso52016PhysicalRegistryVerification\AssistantEngineer.Tools.Iso52016PhysicalRegistryVerification.csproj
    if ($LASTEXITCODE -ne 0) {
        throw "ISO52016 physical chain stage registry C# verification failed with exit code ${LASTEXITCODE}."
    }
}
finally {
    Pop-Location
}