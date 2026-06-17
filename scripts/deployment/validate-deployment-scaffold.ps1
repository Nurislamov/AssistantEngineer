param(
    [string]$ComposeFile = "deploy/docker-compose.yml",
    [switch]$RunDockerComposeConfig
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path

function Read-RequiredFile([string]$RelativePath) {
    $path = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required deployment scaffold file is missing: $RelativePath"
    }
    return Get-Content -Raw -LiteralPath $path
}

$backendDockerfile = Read-RequiredFile "deploy/docker/backend/Dockerfile"
$frontendDockerfile = Read-RequiredFile "deploy/docker/frontend/Dockerfile"
$compose = Read-RequiredFile $ComposeFile
$proxy = Read-RequiredFile "deploy/reverse-proxy/Caddyfile.example"
$envExample = Read-RequiredFile "deploy/.env.example"
$deployIgnore = Read-RequiredFile "deploy/.gitignore"

foreach ($service in @("assistantengineer-api:", "assistantengineer-frontend:", "reverse-proxy:")) {
    if ($compose.IndexOf($service, [StringComparison]::Ordinal) -lt 0) {
        throw "Compose scaffold is missing expected service: $service"
    }
}

if ($compose -match '(?im)^\s*(postgres|mysql|mariadb|sqlserver|mssql|mongodb|redis|database|db)\s*:') {
    throw "Compose scaffold must not add a database service."
}
if ($compose -match '\b\d{8,10}:[A-Za-z0-9_-]{30,}\b') {
    throw "Compose scaffold contains a token-like value."
}
if ($compose -match '(?im)(BotToken|WebhookSecret)\s*:\s*[A-Za-z0-9_-]{12,}\s*$') {
    throw "Compose scaffold contains an embedded Telegram secret."
}
if ($proxy.IndexOf("handle /api/*", [StringComparison]::Ordinal) -lt 0 -or
    $proxy.IndexOf("reverse_proxy assistantengineer-api:8080", [StringComparison]::Ordinal) -lt 0) {
    throw "Reverse proxy example must route /api/* to assistantengineer-api."
}
if ($proxy.IndexOf("/api/v1/equipment-diagnostics/telegram/webhook", [StringComparison]::Ordinal) -lt 0) {
    throw "Reverse proxy example must document the existing Telegram webhook path."
}
if ($deployIgnore.IndexOf(".env", [StringComparison]::Ordinal) -lt 0) {
    throw "deploy/.gitignore must ignore deploy/.env."
}

$allowedDomains = @("example.com", "api.example.com")
$domainMatches = [regex]::Matches($proxy, '(?i)\b(?:[a-z0-9-]+\.)+[a-z]{2,}\b')
foreach ($match in $domainMatches) {
    if ($allowedDomains -notcontains $match.Value.ToLowerInvariant()) {
        throw "Reverse proxy example contains a non-placeholder domain: $($match.Value)"
    }
}
if ($proxy -match '(?im)^\s*email\s+\S+') {
    throw "Reverse proxy example must not contain a real ACME email."
}
if ($proxy -match '\b\d{8,10}:[A-Za-z0-9_-]{30,}\b') {
    throw "Reverse proxy example contains a token-like value."
}
if ($envExample.IndexOf("TELEGRAM_IS_ENABLED=false", [StringComparison]::Ordinal) -lt 0 -or
    $envExample.IndexOf("TELEGRAM_INBOUND_MODE=Webhook", [StringComparison]::Ordinal) -lt 0 -or
    $envExample.IndexOf("TELEGRAM_POLLING_ENABLED=false", [StringComparison]::Ordinal) -lt 0 -or
    $envExample.IndexOf("TELEGRAM_PROCESSED_MESSAGE_STORE_MAX_ENTRIES=5000", [StringComparison]::Ordinal) -lt 0 -or
    $envExample.IndexOf("TELEGRAM_ENABLE_CHAT_ID_DISCOVERY=false", [StringComparison]::Ordinal) -lt 0) {
    throw "Environment example must keep Telegram, polling, and chat ID discovery disabled."
}

if ($RunDockerComposeConfig) {
    Push-Location $repoRoot
    try {
        docker compose --env-file deploy/.env.example -f $ComposeFile config --quiet
        if ($LASTEXITCODE -ne 0) {
            throw "docker compose config validation failed."
        }
    } finally {
        Pop-Location
    }
}

Write-Host "PASS: deployment scaffold validation"
Write-Host "Compose services: assistantengineer-api, assistantengineer-frontend, reverse-proxy"
Write-Host "Database service: absent"
Write-Host "Telegram secrets: not embedded"
Write-Host "Placeholder domains: example.com, api.example.com"
Write-Host "Docker Compose config run: $($RunDockerComposeConfig.IsPresent)"
