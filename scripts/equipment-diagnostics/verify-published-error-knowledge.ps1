param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$publishDirectory = Join-Path ([System.IO.Path]::GetTempPath()) (
    "assistant-engineer-published-knowledge-" + [Guid]::NewGuid().ToString("N"))

Push-Location $repoRoot
try {
    dotnet publish `
        src/Backend/AssistantEngineer.Api/AssistantEngineer.Api.csproj `
        --configuration $Configuration `
        --no-restore `
        --output $publishDirectory `
        /p:UseAppHost=false
    if ($LASTEXITCODE -ne 0) {
        throw "API publish failed."
    }

    $assemblyPath = Join-Path $publishDirectory "AssistantEngineer.Modules.EquipmentDiagnostics.dll"
    dotnet run `
        --project tools/AssistantEngineer.Tools.EquipmentDiagnosticsVerification `
        --configuration $Configuration `
        --no-restore `
        -- `
        verify-published-knowledge `
        --assembly $assemblyPath
    if ($LASTEXITCODE -ne 0) {
        throw "Published error knowledge smoke failed."
    }
}
finally {
    Pop-Location
    if (Test-Path -LiteralPath $publishDirectory -PathType Container) {
        Remove-Item -LiteralPath $publishDirectory -Recurse -Force
    }
}
