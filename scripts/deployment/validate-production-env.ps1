param(
    [string]$EnvPath = "deploy/.env",
    [switch]$AllowPlaceholders
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$resolvedEnvPath = if ([System.IO.Path]::IsPathRooted($EnvPath)) {
    $EnvPath
} else {
    Join-Path $repoRoot $EnvPath
}

if (-not (Test-Path -LiteralPath $resolvedEnvPath -PathType Leaf)) {
    throw "Environment file was not found: $EnvPath"
}

function Read-EnvFile([string]$Path) {
    $values = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith("#")) {
            continue
        }

        $separator = $trimmed.IndexOf("=")
        if ($separator -lt 1) {
            throw "Invalid environment entry. Expected KEY=VALUE."
        }

        $key = $trimmed.Substring(0, $separator).Trim()
        $value = $trimmed.Substring($separator + 1).Trim().Trim('"').Trim("'")
        if ($values.ContainsKey($key)) {
            throw "Duplicate environment key: $key"
        }

        $values[$key] = $value
    }

    return $values
}

function Read-Boolean([hashtable]$Values, [string]$Key) {
    $parsed = $false
    if (-not [bool]::TryParse($Values[$Key], [ref]$parsed)) {
        throw "$Key must be true or false."
    }

    return $parsed
}

$keys = @{
    Environment = "ASPNETCORE_ENVIRONMENT"
    TelegramEnabled = "TELEGRAM_IS_ENABLED"
    TelegramInboundMode = "TELEGRAM_INBOUND_MODE"
    TelegramDeleteWebhookOnStartup = "TELEGRAM_DELETE_WEBHOOK_ON_STARTUP"
    TelegramPollingEnabled = "TELEGRAM_POLLING_ENABLED"
    TelegramPollingTimeout = "TELEGRAM_POLLING_TIMEOUT_SECONDS"
    TelegramPollingLimit = "TELEGRAM_POLLING_LIMIT"
    TelegramPollingDelayAfterError = "TELEGRAM_POLLING_DELAY_AFTER_ERROR_SECONDS"
    TelegramProcessedMessageStorePath = "TELEGRAM_PROCESSED_MESSAGE_STORE_FILE_PATH"
    TelegramProcessedMessageStoreMaxEntries = "TELEGRAM_PROCESSED_MESSAGE_STORE_MAX_ENTRIES"
    DiscoveryEnabled = "TELEGRAM_ENABLE_CHAT_ID_DISCOVERY"
    BotToken = "AssistantEngineer__EquipmentDiagnostics__Telegram__BotToken"
    WebhookSecret = "AssistantEngineer__EquipmentDiagnostics__Telegram__WebhookSecret"
    AllowedChat = "AssistantEngineer__EquipmentDiagnostics__Telegram__AllowedChatIds__0"
    DeniedChat = "AssistantEngineer__EquipmentDiagnostics__Telegram__DeniedChatIds__0"
    ApiUrl = "VITE_API_BASE_URL"
}

$values = Read-EnvFile $resolvedEnvPath
foreach ($key in $keys.Values) {
    if (-not $values.ContainsKey($key)) {
        throw "Required environment key is missing: $key"
    }
}

$telegramEnabled = Read-Boolean $values $keys.TelegramEnabled
$discoveryEnabled = Read-Boolean $values $keys.DiscoveryEnabled
$deleteWebhookOnStartup = Read-Boolean $values $keys.TelegramDeleteWebhookOnStartup
$pollingEnabled = Read-Boolean $values $keys.TelegramPollingEnabled
$inboundMode = $values[$keys.TelegramInboundMode]
if ($inboundMode -notin @("Webhook", "Polling")) {
    throw "TELEGRAM_INBOUND_MODE must be Webhook or Polling."
}
foreach ($numericKey in @(
    $keys.TelegramPollingTimeout,
    $keys.TelegramPollingLimit,
    $keys.TelegramPollingDelayAfterError,
    $keys.TelegramProcessedMessageStoreMaxEntries)) {
    $parsed = 0
    if (-not [int]::TryParse($values[$numericKey], [ref]$parsed) -or $parsed -lt 1) {
        throw "$numericKey must be a positive integer."
    }
}
if ([string]::IsNullOrWhiteSpace($values[$keys.TelegramProcessedMessageStorePath])) {
    throw "TELEGRAM_PROCESSED_MESSAGE_STORE_FILE_PATH must not be empty."
}

if ($AllowPlaceholders) {
    if ($telegramEnabled) {
        throw "The placeholder environment example must keep Telegram disabled."
    }
    if ($discoveryEnabled) {
        throw "The placeholder environment example must keep chat ID discovery disabled."
    }
} else {
    if ($values[$keys.Environment] -ne "Production") {
        throw "ASPNETCORE_ENVIRONMENT must be Production."
    }
    if ($discoveryEnabled) {
        throw "TELEGRAM_ENABLE_CHAT_ID_DISCOVERY must be false before production deployment."
    }
    if ($telegramEnabled) {
        if ([string]::IsNullOrWhiteSpace($values[$keys.BotToken])) {
            throw "Telegram is enabled but BotToken is empty."
        }
        if ($inboundMode -eq "Webhook" -and [string]::IsNullOrWhiteSpace($values[$keys.WebhookSecret])) {
            throw "Telegram is enabled but WebhookSecret is empty."
        }
        if ([string]::IsNullOrWhiteSpace($values[$keys.AllowedChat])) {
            throw "Telegram is enabled but AllowedChatIds is empty."
        }
        if ($inboundMode -eq "Polling" -and -not $pollingEnabled) {
            throw "Telegram polling mode requires TELEGRAM_POLLING_ENABLED=true."
        }
    }
}

$webhookSecret = $values[$keys.WebhookSecret]
if (-not [string]::IsNullOrWhiteSpace($webhookSecret) -and
    $webhookSecret -notmatch '^[A-Za-z0-9_-]{1,256}$') {
    throw "WebhookSecret must use 1-256 letters, digits, underscores, or hyphens."
}

Write-Host "PASS: production environment validation"
Write-Host "File: $([System.IO.Path]::GetFileName($resolvedEnvPath))"
Write-Host "Environment: $($values[$keys.Environment])"
Write-Host "Telegram enabled: $telegramEnabled"
Write-Host "Telegram inbound mode: $inboundMode"
Write-Host "Telegram polling enabled: $pollingEnabled"
Write-Host "Telegram processed message store configured: $(-not [string]::IsNullOrWhiteSpace($values[$keys.TelegramProcessedMessageStorePath]))"
Write-Host "Delete webhook on startup: $deleteWebhookOnStartup"
Write-Host "Chat ID discovery enabled: $discoveryEnabled"
Write-Host "Bot token configured: $(-not [string]::IsNullOrWhiteSpace($values[$keys.BotToken]))"
Write-Host "Webhook secret configured: $(-not [string]::IsNullOrWhiteSpace($webhookSecret))"
Write-Host "Allowed chat configured: $(-not [string]::IsNullOrWhiteSpace($values[$keys.AllowedChat]))"
