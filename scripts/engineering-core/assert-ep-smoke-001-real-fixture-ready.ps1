param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $ToolArgs
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

dotnet run --project .\tools\AssistantEngineer.Tools.EnergyPlusValidation\AssistantEngineer.Tools.EnergyPlusValidation.csproj -- assert-smoke001-real-fixture-ready @ToolArgs
