param(
    [string]$ComposeFile = "deploy/docker-compose.yml"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
Push-Location $repoRoot
try {
    docker compose -f $ComposeFile down
    if ($LASTEXITCODE -ne 0) { throw "Production-like stack stop failed." }
}
finally {
    Pop-Location
}
