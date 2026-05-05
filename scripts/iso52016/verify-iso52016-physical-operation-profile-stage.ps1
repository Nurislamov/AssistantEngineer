param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

function Assert-RequiredFiles {
    param([Parameter(Mandatory = $true)] [string[]] $RelativePaths)

    foreach ($relativePath in $RelativePaths) {
        $path = Join-Path $RepoRoot $relativePath
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Required ISO52016 physical operation profile file is missing: $relativePath"
        }
    }
}

function Assert-NoPositiveParityClaims {
    param([Parameter(Mandatory = $true)] [string[]] $RelativePaths)

    $forbidden = @(
        'full ISO 52016 parity',
        'ISO52016 parity achieved',
        'complete ISO 52016 numerical equivalence achieved',
        'pyBuildingEnergy parity achieved',
        'EnergyPlus parity achieved',
        'ASHRAE 140 validation passed'
    )

    foreach ($relativePath in $RelativePaths) {
        $path = Join-Path $RepoRoot $relativePath
        if (-not (Test-Path -LiteralPath $path)) { continue }
        $text = Get-Content -LiteralPath $path -Raw
        foreach ($claim in $forbidden) {
            if ($text.IndexOf($claim, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                throw "Forbidden positive parity/validation claim found in ${relativePath}: $claim"
            }
        }
    }
}

$dependencyVerifier = Join-Path $RepoRoot 'scripts\iso52016\verify-iso52016-physical-boundary-profile-stage.ps1'
if (-not (Test-Path -LiteralPath $dependencyVerifier)) {
    throw 'Required dependency verifier is missing: scripts\iso52016\verify-iso52016-physical-boundary-profile-stage.ps1'
}

$dependencyArgs = @{ RepoRoot = $RepoRoot }
if ($SkipTests) { $dependencyArgs.SkipTests = $true }
& $dependencyVerifier @dependencyArgs

$requiredFiles = @(
    'docs\calculations\Iso52016PhysicalOperationProfileStage.md',
    'docs\releases\Iso52016PhysicalOperationProfileStageManifest.json',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalHourlyOperationCondition.cs',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalRoomModelRequest.cs',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Matrix\Iso52016MatrixHourlyBoundaryConductanceOverride.cs',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Matrix\Iso52016MatrixHourlyInputRecord.cs',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Services\Iso52016\Physical\Iso52016PhysicalRoomModelBuilder.cs',
    'tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalOperationProfileTests.cs',
    'tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalOperationProfileStageTraceabilityTests.cs',
    'tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixHourlyBoundaryConductanceOverrideTests.cs'
)

Assert-RequiredFiles -RelativePaths $requiredFiles
Assert-NoPositiveParityClaims -RelativePaths @(
    'docs\calculations\Iso52016PhysicalOperationProfileStage.md',
    'docs\releases\Iso52016PhysicalOperationProfileStageManifest.json'
)

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter 'FullyQualifiedName~Iso52016PhysicalOperationProfile|FullyQualifiedName~Iso52016MatrixHourlyBoundaryConductanceOverride'
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet test failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host 'ISO52016 physical operation profile stage verification passed - validation/internal engineering anchors only.'

# Traceability literal markers:
# Iso52016PhysicalOperationProfileTests
# Iso52016PhysicalOperationProfileStageTraceabilityTests
# Iso52016MatrixHourlyBoundaryConductanceOverrideTests
# Iso52016PhysicalHourlyOperationCondition
# Iso52016MatrixHourlyBoundaryConductanceOverride
# Iso52016PhysicalOperationProfileStageManifest.json
# validation/internal engineering anchors only
# AE-ISO52016-002
