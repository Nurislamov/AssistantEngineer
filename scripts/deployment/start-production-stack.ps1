param(
    [string]$ComposeFile = "deploy/docker-compose.yml"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
Push-Location $repoRoot
try {
    if (-not (Test-Path "deploy/.env")) {
        throw "deploy/.env is required. Create it locally from deploy/.env.example and keep it out of Git."
    }
    docker compose -f $ComposeFile up -d
    if ($LASTEXITCODE -ne 0) { throw "Production-like stack start failed." }
}
finally {
    Pop-Location
}
