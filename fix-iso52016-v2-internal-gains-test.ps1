param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $RunTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)
$relativePath = 'tests\AssistantEngineer.Tests\Calculations\Iso52016\V2\Iso52016InternalGainReferenceDataProviderTests.cs'
$path = Join-Path $RepoRoot $relativePath

if (-not [System.IO.File]::Exists($path)) {
    throw "Cannot find $relativePath. Run this script from repository root or pass -RepoRoot."
}

$text = [System.IO.File]::ReadAllText($path)

$old = '        Assert.Equal(1350.0, result.Value.TotalSensibleGainW, precision: 6);'
$new = '        Assert.Equal(1500.0, result.Value.TotalSensibleGainW, precision: 6);'

if ($text.Contains($old)) {
    $text = $text.Replace($old, $new)
    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
    Write-Host "Patched $relativePath: expected total sensible gain changed from 1350 W to 1500 W."
}
elseif ($text.Contains($new)) {
    Write-Host "$relativePath already contains the corrected 1500 W expectation."
}
else {
    throw "Expected assertion line was not found. Open $relativePath and check the TotalSensibleGainW assertion manually."
}

Write-Host ''
Write-Host 'Why 1500 W is correct for this test fixture:'
Write-Host '  Occupants: 100 m2 * 0.10 person/m2 * 75 W/person * 1.00 = 750 W'
Write-Host '  Lighting:  100 m2 * 9 W/m2 * 0.50 = 450 W'
Write-Host '  Equipment: 100 m2 * 12 W/m2 * 0.25 = 300 W'
Write-Host '  Total:     750 + 450 + 300 = 1500 W'

if ($RunTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016V2|FullyQualifiedName~InternalGainReferenceData|FullyQualifiedName~AdjacentUnconditioned"
    }
    finally {
        Pop-Location
    }
}
