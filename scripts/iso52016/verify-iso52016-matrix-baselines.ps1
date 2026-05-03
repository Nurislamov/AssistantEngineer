param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$requiredFiles = @(
    "docs\calculations\Iso52016MatrixBaselineFixtures.md",
    "docs\releases\Iso52016MatrixBaselineFixturesManifest.json",
    "scripts\iso52016\export-iso52016-matrix-baseline-summary.ps1",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixBaselineFixtureTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Baselines\neutral-no-hvac.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Baselines\winter-heating-24h.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Baselines\summer-cooling-24h.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Baselines\mass-lag-heating-1h.json"
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath
    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix baseline file is missing: $relativePath"
    }
}

$baselineDirectory = Join-Path $RepoRoot "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Baselines"
$fixtureCount = (Get-ChildItem $baselineDirectory -File -Filter *.json).Count

if ($fixtureCount -lt 4) {
    throw "Expected at least 4 ISO52016 Matrix baseline fixtures, found $fixtureCount."
}

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixBaselineFixture"
    }
    finally {
        Pop-Location
    }
}

Write-Host "ISO52016 Matrix baseline verification passed."