param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

function Assert-RequiredFiles {
    param(
        [Parameter(Mandatory = $true)] [string[]] $RelativePaths
    )

    foreach ($relativePath in $RelativePaths) {
        $path = Join-Path $RepoRoot $relativePath
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Required ISO52016 physical room simulation service file is missing: $relativePath"
        }
    }
}

function Assert-NoPositiveParityClaims {
    param(
        [Parameter(Mandatory = $true)] [string[]] $RelativePaths
    )

    $forbiddenPositivePhrases = @(
        'full ISO 52016 parity achieved',
        'ISO52016 parity achieved',
        'complete numerical equivalence achieved',
        'pyBuildingEnergy parity achieved',
        'pyBuildingEnergy numerical equivalence achieved',
        'EnergyPlus parity achieved',
        'EnergyPlus numerical equivalence achieved',
        'ASHRAE 140 validation passed',
        'ASHRAE Standard 140 benchmark-grade claim passed'
    )

    foreach ($relativePath in $RelativePaths) {
        $path = Join-Path $RepoRoot $relativePath
        if (-not (Test-Path -LiteralPath $path)) {
            continue
        }

        $content = Get-Content -LiteralPath $path -Raw

        foreach ($phrase in $forbiddenPositivePhrases) {
            if ($content.IndexOf($phrase, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                throw "Forbidden positive parity/validation claim found in ${relativePath}: $phrase"
            }
        }
    }
}

function Invoke-RepoVerifier {
    param(
        [Parameter(Mandatory = $true)] [string] $RelativePath
    )

    $path = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required ISO52016 physical dependency verifier is missing: $RelativePath"
    }

    $arguments = @{
        RepoRoot = $RepoRoot
    }

    if ($SkipTests) {
        $arguments.SkipTests = $true
    }

    & $path @arguments
}

function Invoke-StageTests {
    param(
        [Parameter(Mandatory = $true)] [string] $Filter
    )

    if ($SkipTests) {
        return
    }

    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter $Filter
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet test failed with exit code $LASTEXITCODE for filter: $Filter"
        }
    }
    finally {
        Pop-Location
    }
}

Invoke-RepoVerifier -RelativePath 'scripts\iso52016\verify-iso52016-physical-operation-profile-stage.ps1'

$requiredFiles = @(
    'docs\calculations\Iso52016PhysicalRoomSimulationServiceStage.md',
    'docs\releases\Iso52016PhysicalRoomSimulationServiceStageManifest.json',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Abstractions\Iso52016\Physical\IIso52016PhysicalRoomEnergySimulationService.cs',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalRoomEnergySimulationResult.cs',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Services\Iso52016\Physical\Iso52016PhysicalRoomEnergySimulationService.cs',
    'tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalRoomEnergySimulationServiceTests.cs',
    'tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalRoomSimulationServiceStageTraceabilityTests.cs'
)

Assert-RequiredFiles -RelativePaths $requiredFiles
Assert-NoPositiveParityClaims -RelativePaths @(
    'docs\calculations\Iso52016PhysicalRoomSimulationServiceStage.md',
    'docs\releases\Iso52016PhysicalRoomSimulationServiceStageManifest.json'
)

Invoke-StageTests -Filter 'FullyQualifiedName~Iso52016PhysicalRoomEnergySimulationService|FullyQualifiedName~Iso52016PhysicalRoomSimulationServiceStageTraceability'

Write-Host 'ISO52016 physical room simulation service stage verification passed - validation/internal engineering anchors only.'

# Traceability literal markers:
# Iso52016PhysicalRoomEnergySimulationServiceTests
# Iso52016PhysicalRoomSimulationServiceStageTraceabilityTests
# IIso52016PhysicalRoomEnergySimulationService
# Iso52016PhysicalRoomEnergySimulationService
# Iso52016PhysicalRoomEnergySimulationResult
# Iso52016PhysicalRoomSimulationServiceStageManifest.json
# validation/internal engineering anchors only
# AE-ISO52016-002 traceability marker
# AE-ISO52016-002