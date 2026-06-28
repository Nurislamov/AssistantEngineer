# Local, offline Gree diagnostics smoke runner.
# Uses the in-process test infrastructure; it does not call Telegram or require secrets.

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$solutionPath = Join-Path $repoRoot "AssistantEngineer.sln"
$testFilter = "FullyQualifiedName~GreeDiagnosticsSmokeTests"
$fullTestCommand = "dotnet test .\AssistantEngineer.sln"

if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
    Write-Error "FAIL: AssistantEngineer.sln was not found at repository root '$repoRoot'."
    exit 1
}

Write-Host "Starting local Gree diagnostics smoke tests."
Write-Host "Repository root: $repoRoot"
Write-Host "Test filter: $testFilter"

$exitCode = 1
Push-Location $repoRoot
try {
    & dotnet test ".\AssistantEngineer.sln" --filter $testFilter
    $exitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

if ($exitCode -ne 0) {
    Write-Host "FAIL: Gree diagnostics smoke tests failed with exit code $exitCode." -ForegroundColor Red
    Write-Host "Full test command: $fullTestCommand"
    exit $exitCode
}

Write-Host "PASS: Gree diagnostics smoke tests completed successfully." -ForegroundColor Green
Write-Host "Full test command: $fullTestCommand"
