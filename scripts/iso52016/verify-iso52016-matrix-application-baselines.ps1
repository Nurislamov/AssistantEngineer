param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$requiredFiles = @(
    "docs\calculations\Iso52016MatrixApplicationBaselineFixtures.md",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixApplicationBaselineFixtureTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\ApplicationBaselines\building-cold-two-room-heating.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\ApplicationBaselines\building-hot-single-room-cooling.json"
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath
    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix application baseline file is missing: $relativePath"
    }
}

$baselineDirectory = Join-Path $RepoRoot "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\ApplicationBaselines"
$fixtureCount = (Get-ChildItem $baselineDirectory -File -Filter *.json).Count

if ($fixtureCount -lt 2) {
    throw "Expected at least 2 ISO52016 Matrix application baseline fixtures, found $fixtureCount."
}

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixApplicationBaselineFixture"
    }
    finally {
        Pop-Location
    }
}

Write-Host "ISO52016 Matrix application baseline verification passed."