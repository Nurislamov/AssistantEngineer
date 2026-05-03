param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$requiredFiles = @(
    "docs\calculations\Iso52016MatrixExternalValidationFixtures.md",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixExternalValidationFixtureTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\ExternalValidation\manual-steady-state-heating.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\ExternalValidation\manual-steady-state-heating-with-gains.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\ExternalValidation\manual-steady-state-cooling.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\ExternalValidation\manual-steady-state-cooling-with-gains.json"
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath

    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix external validation file is missing: $relativePath"
    }
}

$validationDirectory = Join-Path $RepoRoot "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\ExternalValidation"
$fixtureCount = (Get-ChildItem $validationDirectory -File -Filter *.json).Count

if ($fixtureCount -lt 4) {
    throw "Expected at least 4 ISO52016 Matrix external validation fixtures, found $fixtureCount."
}

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixExternalValidationFixture"
    }
    finally {
        Pop-Location
    }
}

Write-Host "ISO52016 Matrix external validation verification passed."