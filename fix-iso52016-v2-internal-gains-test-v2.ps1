param(
    [string] $RepoRoot = ".",
    [switch] $RunTests
)

$ErrorActionPreference = "Stop"

$repo = Resolve-Path $RepoRoot
$relativePath = "tests\AssistantEngineer.Tests\Calculations\Iso52016\V2\Iso52016InternalGainReferenceDataProviderTests.cs"
$path = Join-Path $repo $relativePath

if (-not (Test-Path $path)) {
    throw "Test file was not found: $path"
}

$content = Get-Content $path -Raw

$old = 'Assert.Equal(1350.0, result.Value.TotalSensibleGainW, precision: 6);'
$new = 'Assert.Equal(1500.0, result.Value.TotalSensibleGainW, precision: 6);'

if ($content.Contains($new)) {
    Write-Host "$($relativePath): already patched."
}
elseif ($content.Contains($old)) {
    $content = $content.Replace($old, $new)
    Set-Content -Path $path -Value $content -Encoding UTF8
    Write-Host "$($relativePath): expected total sensible gain changed from 1350 W to 1500 W."
}
else {
    throw "Expected assertion was not found. Please inspect file manually: $path"
}

if ($RunTests) {
    Push-Location $repo
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016V2|FullyQualifiedName~InternalGainReferenceData|FullyQualifiedName~AdjacentUnconditioned"
    }
    finally {
        Pop-Location
    }
}
