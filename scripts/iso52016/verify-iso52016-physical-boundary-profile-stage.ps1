param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

function Assert-RequiredFiles {
    param([Parameter(Mandatory = $true)] [string[]] $RelativePaths)

    foreach ($relativePath in $RelativePaths) {
        $path = Join-Path $RepoRoot $relativePath
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Required ISO52016 physical boundary profile file is missing: $relativePath"
        }
    }
}

function Assert-TextContains {
    param(
        [Parameter(Mandatory = $true)] [string] $RelativePath,
        [Parameter(Mandatory = $true)] [string[]] $Needles
    )

    $path = Join-Path $RepoRoot $RelativePath
    $content = Get-Content -LiteralPath $path -Raw

    foreach ($needle in $Needles) {
        if (-not $content.Contains($needle)) {
            throw "Required text marker was not found in ${relativePath}: $needle"
        }
    }
}

function Assert-NoForbiddenPositiveClaims {
    param([Parameter(Mandatory = $true)] [string[]] $RelativePaths)

    $forbiddenPhrases = @(
        "complete numerical equivalence achieved",
        "full ISO 52016 parity achieved",
        "ISO52016 parity achieved",
        "pyBuildingEnergy parity achieved",
        "pyBuildingEnergy numerical equivalence achieved",
        "EnergyPlus parity achieved",
        "EnergyPlus numerical equivalence achieved",
        "ASHRAE Standard 140 benchmark-grade claim passed",
        "ASHRAE 140 validation passed"
    )

    foreach ($relativePath in $RelativePaths) {
        $path = Join-Path $RepoRoot $relativePath
        if (-not (Test-Path -LiteralPath $path)) {
            continue
        }

        $content = Get-Content -LiteralPath $path -Raw
        foreach ($phrase in $forbiddenPhrases) {
            if ($content.IndexOf($phrase, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                throw "Forbidden positive parity/validation claim found in ${relativePath}: $phrase"
            }
        }
    }
}

function Invoke-DotNetTestChecked {
    param([Parameter(Mandatory = $true)] [string] $Filter)

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

$requiredFiles = @(
    "docs\calculations\Iso52016PhysicalBoundaryProfileStage.md",
    "docs\releases\Iso52016PhysicalBoundaryProfileStageManifest.json",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalSurfaceHourlyBoundaryCondition.cs",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalRoomModelRequest.cs",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalSurface.cs",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalSurfaceBoundaryType.cs",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalNodeModelOptions.cs",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Services\Iso52016\Physical\Iso52016PhysicalRoomModelBuilder.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalSurfaceBoundaryConditionTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalBoundaryProfileStageTraceabilityTests.cs"
)

Assert-RequiredFiles -RelativePaths $requiredFiles

Assert-TextContains -RelativePath "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalNodeModelOptions.cs" -Needles @(
    "AdjacentConditionedBoundaryId",
    "AdjacentUnconditionedBoundaryId"
)

Assert-TextContains -RelativePath "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalSurface.cs" -Needles @(
    "BoundaryId",
    "AdjacentBoundaryTemperatureC"
)

Assert-TextContains -RelativePath "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalSurfaceBoundaryType.cs" -Needles @(
    "AdjacentConditioned",
    "AdjacentUnconditioned"
)

Assert-TextContains -RelativePath "src\Backend\AssistantEngineer.Modules.Calculations\Application\Services\Iso52016\Physical\Iso52016PhysicalRoomModelBuilder.cs" -Needles @(
    "SurfaceBoundaryConditions",
    "Iso52016PhysicalSurfaceHourlyBoundaryCondition",
    "ResolveBoundaryTemperatureC",
    "ResolveBoundaryId"
)

Assert-NoForbiddenPositiveClaims -RelativePaths @(
    "docs\calculations\Iso52016PhysicalBoundaryProfileStage.md",
    "docs\releases\Iso52016PhysicalBoundaryProfileStageManifest.json"
)

Invoke-DotNetTestChecked -Filter "FullyQualifiedName~Iso52016PhysicalSurfaceBoundaryCondition|FullyQualifiedName~Iso52016PhysicalBoundaryProfileStageTraceability"

Write-Host "ISO52016 physical boundary profile stage verification passed - validation/internal engineering anchors only."

# Traceability literal markers:
# AE-ISO52016-002-STEP-03
# Iso52016PhysicalSurfaceHourlyBoundaryCondition
# Iso52016PhysicalSurfaceBoundaryConditionTests
# Iso52016PhysicalBoundaryProfileStageTraceabilityTests
# Iso52016PhysicalBoundaryProfileStageManifest.json
# validation/internal engineering anchors only