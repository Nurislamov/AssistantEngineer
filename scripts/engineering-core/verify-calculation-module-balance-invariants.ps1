param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $ToolArgs
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCore\AssistantEngineer.Tools.EngineeringCore.csproj -- verify-calculation-module-balance-invariants @ToolArgs
