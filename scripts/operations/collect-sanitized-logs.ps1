param(
    [string]$ComposeFile = "deploy/docker-compose.yml",
    [string]$ServiceName,
    [string]$Since = "30m",
    [int]$Tail = 300,
    [string]$CorrelationId,
    [string]$OutputPath,
    [switch]$IncludeDockerComposeLogs,
    [string]$RedactOnlyInputPath
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$redactor = Join-Path $PSScriptRoot "redact-log-file.ps1"

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $timestamp = [DateTimeOffset]::UtcNow.ToString("yyyyMMdd-HHmmss")
    $OutputPath = "artifacts/operations/sanitized-logs/$timestamp/sanitized-log.txt"
}

if (-not [string]::IsNullOrWhiteSpace($RedactOnlyInputPath)) {
    & $redactor -InputPath $RedactOnlyInputPath -OutputPath $OutputPath
    return
}

if (-not $IncludeDockerComposeLogs) {
    throw "No log source selected. Use -IncludeDockerComposeLogs or -RedactOnlyInputPath."
}
if ($Tail -lt 1 -or $Tail -gt 10000) {
    throw "Tail must be between 1 and 10000."
}
if (-not [string]::IsNullOrWhiteSpace($CorrelationId) -and
    $CorrelationId -notmatch '^[A-Za-z0-9_.-]{1,128}$') {
    throw "CorrelationId contains unsupported characters or exceeds 128 characters."
}
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker CLI is unavailable. Use -RedactOnlyInputPath for offline sanitization."
}

$composePath = if ([System.IO.Path]::IsPathRooted($ComposeFile)) {
    $ComposeFile
} else {
    Join-Path $repoRoot $ComposeFile
}
if (-not (Test-Path -LiteralPath $composePath -PathType Leaf)) {
    throw "Compose file was not found."
}

$arguments = @("compose", "-f", $composePath, "logs", "--no-color", "--tail", $Tail, "--since", $Since)
if (-not [string]::IsNullOrWhiteSpace($ServiceName)) {
    $arguments += $ServiceName
}

$rawLogs = (& docker @arguments 2>&1 | Out-String)
if ($LASTEXITCODE -ne 0) {
    throw "Docker Compose log collection failed. No output was written."
}
if (-not [string]::IsNullOrWhiteSpace($CorrelationId)) {
    $rawLogs = ($rawLogs -split "\r?\n" | Where-Object {
        $_.Contains($CorrelationId, [StringComparison]::Ordinal)
    }) -join [Environment]::NewLine
}

& $redactor -InputContent $rawLogs -OutputPath $OutputPath
