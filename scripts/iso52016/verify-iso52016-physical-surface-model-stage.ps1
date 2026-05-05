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
            throw "Required ISO52016 physical surface stage file is missing: $relativePath"
        }
    }
}

function Assert-NoForbiddenPositiveClaims {
    param(
        [Parameter(Mandatory = $true)] [string[]] $RelativePaths
    )

    $forbiddenPositiveClaims = @(
        "full ISO 52016 parity achieved",
        "ISO52016 parity achieved",
        "complete ISO 52016 numerical equivalence achieved",
        "pyBuildingEnergy parity achieved",
        "pyBuildingEnergy numerical equivalence achieved",
        "EnergyPlus parity achieved",
        "EnergyPlus numerical equivalence achieved",
        "ASHRAE 140 validation passed",
        "ASHRAE Standard 140 benchmark-grade claim passed"
    )

    foreach ($relativePath in $RelativePaths) {
        $path = Join-Path $RepoRoot $relativePath
        $text = Get-Content -LiteralPath $path -Raw

        foreach ($claim in $forbiddenPositiveClaims) {
            if ($text.IndexOf($claim, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                throw "Forbidden positive parity/validation claim found in ${relativePath}: $claim"
            }
        }
    }
}

$requiredFiles = @(
    "docs\calculations\Iso52016PhysicalNodeModelStage.md",
    "docs\releases\Iso52016PhysicalNodeModelStageManifest.json",
    "scripts\iso52016\verify-iso52016-physical-node-model-stage.ps1",
    "docs\calculations\Iso52016PhysicalSurfaceModelExpansion.md",
    "docs\releases\Iso52016PhysicalSurfaceModelExpansionManifest.json",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalSurface.cs",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalConstructionLayer.cs",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalSurfaceBoundaryType.cs",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalRoomModelRequest.cs",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalNodeModelOptions.cs",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Services\Iso52016\Physical\Iso52016PhysicalRoomModelBuilder.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalSurfaceModelBuilderTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalNodeModelSurfaceExpansionTraceabilityTests.cs"
)

Assert-RequiredFiles -RelativePaths $requiredFiles
Assert-NoForbiddenPositiveClaims -RelativePaths @(
    "docs\calculations\Iso52016PhysicalSurfaceModelExpansion.md",
    "docs\releases\Iso52016PhysicalSurfaceModelExpansionManifest.json"
)

$nodeModelVerifier = Join-Path $RepoRoot "scripts\iso52016\verify-iso52016-physical-node-model-stage.ps1"
if (Test-Path -LiteralPath $nodeModelVerifier) {
    $nodeVerifierArguments = @{
        RepoRoot = $RepoRoot
    }

    if ($SkipTests) {
        $nodeVerifierArguments.SkipTests = $true
    }

    & $nodeModelVerifier @nodeVerifierArguments
}

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016PhysicalRoomModelBuilder|FullyQualifiedName~Iso52016PhysicalSurfaceModelBuilder|FullyQualifiedName~Iso52016PhysicalNodeModelSurfaceExpansionTraceability"
    }
    finally {
        Pop-Location
    }
}

Write-Host "ISO52016 physical surface model expansion verification passed - validation/internal engineering anchors only."

# Traceability literal markers:
# AE-ISO52016-002
# Iso52016PhysicalSurface
# Iso52016PhysicalConstructionLayer
# Iso52016PhysicalSurfaceBoundaryType
# Iso52016PhysicalSurfaceModelBuilderTests
# Iso52016PhysicalNodeModelSurfaceExpansionTraceabilityTests
# Iso52016PhysicalSurfaceModelExpansionManifest.json
# validation/internal engineering anchors only