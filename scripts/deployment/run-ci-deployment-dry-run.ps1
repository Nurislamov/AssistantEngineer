param(
    [switch]$RequireDocker,
    [switch]$BuildImages,
    [string]$ComposeFile = "deploy/docker-compose.yml",
    [string]$EnvFile = "deploy/.env.example"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$results = [System.Collections.Generic.List[string]]::new()

Push-Location $repoRoot
try {
    & "$PSScriptRoot/validate-deployment-scaffold.ps1" -ComposeFile $ComposeFile
    $results.Add("PASS: static deployment scaffold validation")

    & "$PSScriptRoot/validate-production-env.ps1" -EnvPath $EnvFile -AllowPlaceholders
    $results.Add("PASS: production environment example validation")

    $dockerCliAvailable = $null -ne (Get-Command docker -ErrorAction SilentlyContinue)
    $composeAvailable = $false
    $dockerDaemonAvailable = $false

    if ($dockerCliAvailable) {
        $previousErrorActionPreference = $ErrorActionPreference
        $ErrorActionPreference = "SilentlyContinue"
        try {
            docker compose version *> $null
            $composeAvailable = $LASTEXITCODE -eq 0

            docker info *> $null
            $dockerDaemonAvailable = $LASTEXITCODE -eq 0
        } finally {
            $ErrorActionPreference = $previousErrorActionPreference
        }
    }

    if ($composeAvailable) {
        docker compose --env-file $EnvFile -f $ComposeFile config --quiet
        if ($LASTEXITCODE -ne 0) {
            throw "Docker Compose config validation failed."
        }
        $results.Add("PASS: Docker Compose config validation")
    } elseif ($RequireDocker) {
        throw "Docker Compose is required but unavailable."
    } else {
        $results.Add("SKIP: Docker Compose config validation (Docker Compose unavailable)")
    }

    if ($BuildImages) {
        if (-not $dockerDaemonAvailable) {
            throw "Docker image builds were requested but the Docker daemon is unavailable."
        }

        docker build --file deploy/docker/backend/Dockerfile --tag assistantengineer-api:ci .
        if ($LASTEXITCODE -ne 0) {
            throw "Backend deployment image build failed."
        }
        $results.Add("PASS: backend image build dry-run")

        docker build --file deploy/docker/frontend/Dockerfile --tag assistantengineer-frontend:ci .
        if ($LASTEXITCODE -ne 0) {
            throw "Frontend deployment image build failed."
        }
        $results.Add("PASS: frontend image build dry-run")
    } elseif ($dockerDaemonAvailable) {
        $results.Add("SKIP: image build dry-run (use -BuildImages to enable)")
    } else {
        $results.Add("SKIP: image build dry-run (Docker daemon unavailable)")
    }

    if ($RequireDocker -and -not $dockerDaemonAvailable) {
        throw "Docker is required but the Docker daemon is unavailable."
    }
} finally {
    Pop-Location
}

$results | ForEach-Object { Write-Host $_ }
Write-Host "PASS: deployment CI dry-run completed without deploy, registry push, or secrets."
