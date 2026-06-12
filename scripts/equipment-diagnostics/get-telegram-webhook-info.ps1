param(
    [string]$BotToken = $env:ASSISTANTENGINEER_TELEGRAM_BOT_TOKEN,
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($BotToken)) { throw "BotToken is required through a parameter or environment variable." }

Write-Host "Telegram operation: getWebhookInfo"
if ($WhatIf) {
    Write-Host "WhatIf: getWebhookInfo request was not sent."
    exit 0
}

try {
    $response = Invoke-RestMethod -Method Get -Uri "https://api.telegram.org/bot$BotToken/getWebhookInfo"
}
catch {
    throw "Telegram getWebhookInfo request failed. Review network access and token configuration."
}

if (-not $response.ok) { throw "Telegram getWebhookInfo request failed." }

$result = $response.result
$safeUrl = if ([string]::IsNullOrWhiteSpace($result.url)) {
    "not configured"
} else {
    $uri = [Uri]$result.url
    "$($uri.Scheme)://$($uri.Host)$($uri.AbsolutePath)"
}

Write-Host "Webhook URL: $safeUrl"
Write-Host "Pending updates: $($result.pending_update_count)"
Write-Host "Has custom certificate: $($result.has_custom_certificate)"
Write-Host "Last error date: $($result.last_error_date)"
Write-Host "Last error message: $($result.last_error_message)"
