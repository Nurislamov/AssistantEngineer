param(
    [string]$BotToken = $env:ASSISTANTENGINEER_TELEGRAM_BOT_TOKEN,
    [switch]$DropPendingUpdates,
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($BotToken)) { throw "BotToken is required through a parameter or environment variable." }

Write-Host "Telegram operation: deleteWebhook"
Write-Host "Drop pending updates: $($DropPendingUpdates.IsPresent)"
if ($WhatIf) {
    Write-Host "WhatIf: deleteWebhook request was not sent."
    exit 0
}

$body = @{
    drop_pending_updates = $DropPendingUpdates.IsPresent
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod `
        -Method Post `
        -Uri "https://api.telegram.org/bot$BotToken/deleteWebhook" `
        -ContentType "application/json" `
        -Body $body
}
catch {
    throw "Telegram deleteWebhook request failed. Review network access and token configuration."
}

if (-not $response.ok) { throw "Telegram deleteWebhook request failed." }

Write-Host "Telegram webhook deleted."
