param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

function Assert-FileContains {
    param(
        [Parameter(Mandatory = $true)] [string] $RelativePath,
        [Parameter(Mandatory = $true)] [string] $ExpectedText
    )

    $path = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required file is missing: $RelativePath"
    }

    $content = Get-Content -LiteralPath $path -Raw
    if (-not $content.Contains($ExpectedText)) {
        throw "Expected text was not found in ${RelativePath}: $ExpectedText"
    }
}

$requiredFiles = @(
    'tools\AssistantEngineer.Tools.RepositoryHygieneVerification\AssistantEngineer.Tools.RepositoryHygieneVerification.csproj',
    'tools\AssistantEngineer.Tools.RepositoryHygieneVerification\Program.cs',
    'scripts\iso52016\assert-iso52016-physical-branch-hygiene.ps1',
    'scripts\iso52016\verify-iso52016-physical-branch-hygiene-stage.ps1',
    'docs\calculations\Iso52016PhysicalBranchHygieneStage.md',
    'docs\releases\Iso52016PhysicalBranchHygieneStageManifest.json',
    'tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalBranchHygieneStageTests.cs'
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required ISO52016 physical branch hygiene stage file is missing: $relativePath"
    }
}

Assert-FileContains -RelativePath 'tools\AssistantEngineer.Tools.RepositoryHygieneVerification\Program.cs' -ExpectedText 'AssertNoRebaseInProgress'
Assert-FileContains -RelativePath 'tools\AssistantEngineer.Tools.RepositoryHygieneVerification\Program.cs' -ExpectedText 'AssertNoConflictMarkers'
Assert-FileContains -RelativePath 'tools\AssistantEngineer.Tools.RepositoryHygieneVerification\Program.cs' -ExpectedText 'AssertJsonFilesParse'
Assert-FileContains -RelativePath 'tools\AssistantEngineer.Tools.RepositoryHygieneVerification\Program.cs' -ExpectedText 'AssertNoRootPatchScripts'
Assert-FileContains -RelativePath 'docs\releases\Iso52016PhysicalBranchHygieneStageManifest.json' -ExpectedText '"stageId": "AE-ISO52016-002-STEP-14"'
Assert-FileContains -RelativePath 'docs\releases\Iso52016PhysicalBranchHygieneStageManifest.json' -ExpectedText '"status": "internal-engineering-gate"'
Assert-FileContains -RelativePath 'docs\calculations\Iso52016PhysicalBranchHygieneStage.md' -ExpectedText 'validation/internal engineering anchors only'
Assert-FileContains -RelativePath 'docs\calculations\Iso52016PhysicalBranchHygieneStage.md' -ExpectedText 'not ASHRAE Standard 140 validation'
Assert-FileContains -RelativePath 'scripts\iso52016\assert-iso52016-physical-branch-hygiene.ps1' -ExpectedText 'AssistantEngineer.Tools.RepositoryHygieneVerification'
Assert-FileContains -RelativePath 'scripts\iso52016\verify-iso52016-matrix-all.ps1' -ExpectedText 'verify-iso52016-physical-branch-hygiene-stage.ps1'

& (Join-Path $RepoRoot 'scripts\iso52016\assert-iso52016-physical-branch-hygiene.ps1') -RepoRoot $RepoRoot

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter 'FullyQualifiedName~Iso52016PhysicalBranchHygieneStage'
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet test failed with exit code ${LASTEXITCODE} for filter: FullyQualifiedName~Iso52016PhysicalBranchHygieneStage"
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host 'ISO52016 physical branch hygiene stage verification passed - validation/internal engineering anchors only.'