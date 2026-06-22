param(
    [string]$OutputPath = "",
    [switch]$PrintGeneratedValues
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path

function New-SecretValue([int]$ByteCount) {
    $bytes = [byte[]]::new($ByteCount)
    $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $generator.GetBytes($bytes)
    }
    finally {
        $generator.Dispose()
    }
    return [Convert]::ToBase64String($bytes).TrimEnd("=").Replace("+", "-").Replace("/", "_")
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $timestamp = [DateTimeOffset]::UtcNow.ToString("yyyyMMddTHHmmssZ")
    $OutputPath = Join-Path $repoRoot "artifacts/operations/secret-rotation/production-secret-candidates-$timestamp.txt"
}

$resolvedOutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$apiKey = New-SecretValue 48
$webhookSecret = New-SecretValue 48
$postgresPassword = New-SecretValue 32

$content = @"
# AssistantEngineer production secret rotation candidates
# Generated: $([DateTimeOffset]::UtcNow.ToString("O"))
#
# This file is intentionally generated under ignored artifacts/operations by default.
# Do not commit it. Do not paste it into chats/issues/logs.
# The helper does not read deploy/.env and does not edit deploy/.env.

Authentication__ApiKey__Key=$apiKey
AssistantEngineer__EquipmentDiagnostics__Telegram__WebhookSecret=$webhookSecret
POSTGRES_PASSWORD_CANDIDATE=$postgresPassword

# Manual follow-up:
# - Update deploy/.env manually on the VPS.
# - If the PostgreSQL password changes, also update the application connection string that contains Password=...
# - Rotate the Telegram bot token in BotFather only if the bot token itself was exposed.
"@

Set-Content -LiteralPath $resolvedOutputPath -Value $content -NoNewline -Encoding UTF8

Write-Host "Generated production secret rotation candidates."
Write-Host "Output file: $resolvedOutputPath"
Write-Host "Do not commit this file. Do not paste generated values into chats/issues/logs."
Write-Host "The script did not read deploy/.env and did not edit deploy/.env."

if ($PrintGeneratedValues) {
    Write-Warning "Printing generated secrets is unsafe. Use only in a private local terminal and do not paste the output."
    Write-Host $content
}
