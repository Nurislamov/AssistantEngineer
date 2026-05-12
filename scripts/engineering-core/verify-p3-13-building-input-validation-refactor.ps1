param(
    [string] $RepoRoot
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$candidateRepoRoot = if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        Join-Path $PSScriptRoot "..\.."
    }
    else {
        (Get-Location).Path
    }
}
else {
    $RepoRoot
}

$resolvedRepoRoot = (Resolve-Path -LiteralPath $candidateRepoRoot).Path
$solutionPath = Join-Path $resolvedRepoRoot "AssistantEngineer.sln"
if (-not (Test-Path -LiteralPath $solutionPath)) {
    throw "AssistantEngineer.sln was not found under repository root: $resolvedRepoRoot"
}

Write-Host "[P3-13] Repository root: $resolvedRepoRoot" -ForegroundColor Cyan

Push-Location $resolvedRepoRoot
try {
    $serviceFile = Get-ChildItem -LiteralPath (Join-Path $resolvedRepoRoot "src") -Recurse -File -Filter "BuildingInputValidationService.cs" | Select-Object -First 1
    if (-not $serviceFile) {
        throw "BuildingInputValidationService.cs was not found under src."
    }

    $lineCount = @(Get-Content -LiteralPath $serviceFile.FullName).Count
    Write-Host "[P3-13] BuildingInputValidationService.cs line count: $lineCount" -ForegroundColor Cyan
    if ($lineCount -gt 550) {
        throw "BuildingInputValidationService.cs exceeds P3-13 threshold: $lineCount > 550"
    }

    $srcText = Get-ChildItem -LiteralPath (Join-Path $resolvedRepoRoot "src") -Recurse -File -Filter "*.cs" |
        Where-Object { $_.FullName -like "*Validation*" } |
        ForEach-Object { $_.BaseName }

    foreach ($required in @("RoomValidator", "EnvelopeValidator", "VentilationValidator", "DiagnosticFactory")) {
        if (-not ($srcText | Where-Object { $_ -like "*$required*" })) {
            throw "Missing focused validation component containing '$required'."
        }
    }

    Write-Host "[P3-13] Running build..." -ForegroundColor Cyan
    dotnet build .\AssistantEngineer.sln

    Write-Host "[P3-13] Running tests..." -ForegroundColor Cyan
    dotnet test .\AssistantEngineer.sln --no-restore

    $releaseReady = Join-Path $resolvedRepoRoot "scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1"
    if (Test-Path -LiteralPath $releaseReady) {
        Write-Host "[P3-13] Running engineering-core release-ready gate..." -ForegroundColor Cyan
        powershell -NoProfile -ExecutionPolicy Bypass -File $releaseReady
    }
    else {
        Write-Host "[P3-13] Release-ready gate script not found; skipping." -ForegroundColor Yellow
    }

    Write-Host "[P3-13] Verification complete." -ForegroundColor Green
}
finally {
    Pop-Location
}
