param(
    [string]$BotToken = $env:ASSISTANTENGINEER_TELEGRAM_BOT_TOKEN,
    [string]$WebhookUrl = $env:ASSISTANTENGINEER_TELEGRAM_WEBHOOK_URL,
    [string]$WebhookSecret = $env:ASSISTANTENGINEER_TELEGRAM_WEBHOOK_SECRET,
    [switch]$DropPendingUpdates,
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($BotToken)) { throw "BotToken is required through a parameter or environment variable." }
if ([string]::IsNullOrWhiteSpace($WebhookUrl)) { throw "WebhookUrl is required through a parameter or environment variable." }
if ($WebhookSecret -notmatch '^[A-Za-z0-9_-]{1,256}$') { throw "WebhookSecret must use 1-256 letters, digits, underscores, or hyphens." }

$uri = [Uri]$WebhookUrl
if ($uri.Scheme -ne "https") { throw "Telegram webhook URL must use HTTPS." }

$safeWebhook = "$($uri.Scheme)://$($uri.Host)$($uri.AbsolutePath)"
Write-Host "Telegram webhook target: $safeWebhook"
Write-Host "Drop pending updates: $($DropPendingUpdates.IsPresent)"

if ($WhatIf) {
    Write-Host "WhatIf: setWebhook request was not sent."
    exit 0
}

$body = @{
    url = $WebhookUrl
    secret_token = $WebhookSecret
    drop_pending_updates = $DropPendingUpdates.IsPresent
    allowed_updates = @("message")
} | ConvertTo-Json

$endpoint = "https://api.telegram.org/bot$BotToken/setWebhook"
try {
    $response = Invoke-RestMethod -Method Post -Uri $endpoint -ContentType "application/json" -Body $body
}
catch {
    throw "Telegram setWebhook request failed. Review network access and secret configuration."
}
if (-not $response.ok) { throw "Telegram setWebhook request failed." }

Write-Host "Telegram webhook configured for: $safeWebhook"
