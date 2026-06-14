param(
    [string]$InputPath,
    [string]$OutputPath,
    [string]$InputContent
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path

function Redact-OperationalLogContent([string]$Content) {
    $redacted = $Content
    $redacted = [regex]::Replace($redacted, '\b\d{8,10}:[A-Za-z0-9_-]{30,}\b', '[REDACTED]')
    $redacted = [regex]::Replace(
        $redacted,
        '(?im)(?<name>Authorization|X-Telegram-Bot-Api-Secret-Token)\s*:\s*[^\r\n]+',
        '${name}: [REDACTED]')
    $redacted = [regex]::Replace(
        $redacted,
        '(?i)(?<name>BotToken|WebhookSecret|token|secret|Password|Pwd)\s*[=:]\s*([^&;\s]+)',
        '${name}=[REDACTED]')
    $redacted = [regex]::Replace(
        $redacted,
        '(?im)^(?<prefix>.*\b(?:AllowedChatIds|DeniedChatIds)\b\s*[=:]).*$',
        '${prefix}[REDACTED]')
    $redacted = [regex]::Replace(
        $redacted,
        '(?i)(?<prefix>"?chat_id"?\s*[=:]\s*)"?-?\d+"?',
        '${prefix}"[REDACTED]"')
    $redacted = [regex]::Replace(
        $redacted,
        '(?i)(?<prefix>"?(?:text|message_body|messageBody|telegramMessage)"?\s*[=:]\s*)"(?:\\.|[^"\\])*"',
        '${prefix}"[REDACTED]"')
    return $redacted
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    throw "OutputPath is required."
}

if (-not $PSBoundParameters.ContainsKey("InputContent")) {
    if ([string]::IsNullOrWhiteSpace($InputPath)) {
        throw "InputPath is required when InputContent is not provided."
    }
    if (-not (Test-Path -LiteralPath $InputPath -PathType Leaf)) {
        throw "Input log file was not found."
    }
    $InputContent = Get-Content -Raw -LiteralPath $InputPath
}

$resolvedOutput = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
} else {
    Join-Path $repoRoot $OutputPath
}
$outputDirectory = Split-Path -Parent $resolvedOutput
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$sanitized = Redact-OperationalLogContent $InputContent
[System.IO.File]::WriteAllText($resolvedOutput, $sanitized, [System.Text.UTF8Encoding]::new($false))
Write-Host "Sanitized log written: $resolvedOutput"
